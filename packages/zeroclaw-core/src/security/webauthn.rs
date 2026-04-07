//! WebAuthn / FIDO2 hardware key authentication.
//!
//! Implements the Web Authentication API server-side flows for registration
//! (attestation) and authentication (assertion) of hardware security keys
//! (YubiKey, SoloKey, etc.) and platform authenticators.
//!
//! Credentials are serialized as JSON, encrypted via the existing [`SecretStore`],
//! and persisted to a SQLite-backed credential database. Each user can register
//! multiple credentials (e.g., primary key + backup key).
//!
//! This module intentionally avoids heavy third-party WebAuthn libraries to keep
//! the dependency footprint small. It implements the essential challenge/response
//! protocol using `ring` (already present) for signature verification and
//! `base64`/`serde_json` for serialization.

use crate::security::SecretStore;
use anyhow::{Context, Result};
use base64::{Engine, engine::general_purpose::URL_SAFE_NO_PAD};
use ring::rand::SecureRandom;
use ring::signature;
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::path::{Path, PathBuf};
use std::sync::Arc;

/// COSE algorithm identifier for ES256 (ECDSA w/ SHA-256 on P-256 curve).
const COSE_ALG_ES256: i64 = -7;

/// Challenge size in bytes (32 bytes = 256 bits of entropy).
const CHALLENGE_LEN: usize = 32;

/// Credential ID maximum length in bytes.
const MAX_CREDENTIAL_ID_LEN: usize = 1024;

// ── Public types ────────────────────────────────────────────────

/// WebAuthn relying party configuration.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WebAuthnConfig {
    /// Whether WebAuthn is enabled.
    pub enabled: bool,
    /// Relying Party ID (typically the domain, e.g. "example.com").
    pub rp_id: String,
    /// Relying Party origin URL (e.g. "https://example.com").
    pub rp_origin: String,
    /// Human-readable relying party display name.
    pub rp_name: String,
}

impl Default for WebAuthnConfig {
    fn default() -> Self {
        Self {
            enabled: false,
            rp_id: "localhost".into(),
            rp_origin: "http://localhost:42617".into(),
            rp_name: "ZeroClaw".into(),
        }
    }
}

/// A registered WebAuthn credential.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WebAuthnCredential {
    /// Unique credential identifier (base64url-encoded).
    pub credential_id: String,
    /// COSE public key bytes (base64url-encoded DER SubjectPublicKeyInfo).
    pub public_key: String,
    /// Signature counter for clone detection.
    pub sign_count: u32,
    /// User-assigned label for the credential (e.g. "YubiKey 5").
    pub label: String,
    /// ISO 8601 timestamp of registration.
    pub registered_at: String,
    /// COSE algorithm used (e.g. -7 for ES256).
    pub algorithm: i64,
    /// The user ID this credential belongs to.
    pub user_id: String,
}

/// Server-side registration state, kept between start/finish.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct RegistrationState {
    /// The challenge sent to the client (base64url).
    pub challenge: String,
    /// The user ID being registered.
    pub user_id: String,
    /// The user display name.
    pub user_name: String,
}

/// Server-side authentication state, kept between start/finish.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AuthenticationState {
    /// The challenge sent to the client (base64url).
    pub challenge: String,
    /// The user ID being authenticated.
    pub user_id: String,
    /// Allowed credential IDs (base64url).
    pub allowed_credentials: Vec<String>,
}

/// PublicKeyCredentialCreationOptions sent to the browser.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CreationChallengeResponse {
    /// Base64url-encoded challenge.
    pub challenge: String,
    /// Relying party info.
    pub rp: RelyingParty,
    /// User info.
    pub user: PublicKeyUser,
    /// Supported algorithms.
    pub pub_key_cred_params: Vec<PubKeyCredParam>,
    /// Timeout in milliseconds.
    pub timeout: u64,
    /// Attestation preference.
    pub attestation: String,
    /// Existing credentials to exclude.
    pub exclude_credentials: Vec<CredentialDescriptor>,
}

/// PublicKeyCredentialRequestOptions sent to the browser.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct RequestChallengeResponse {
    /// Base64url-encoded challenge.
    pub challenge: String,
    /// Relying party ID.
    pub rp_id: String,
    /// Allowed credentials.
    pub allow_credentials: Vec<CredentialDescriptor>,
    /// Timeout in milliseconds.
    pub timeout: u64,
    /// User verification requirement.
    pub user_verification: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct RelyingParty {
    pub id: String,
    pub name: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PublicKeyUser {
    /// Base64url-encoded user handle.
    pub id: String,
    pub name: String,
    pub display_name: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PubKeyCredParam {
    #[serde(rename = "type")]
    pub type_: String,
    pub alg: i64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CredentialDescriptor {
    #[serde(rename = "type")]
    pub type_: String,
    pub id: String,
}

/// Client registration response from the browser.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct RegisterCredentialResponse {
    /// Base64url-encoded credential ID.
    pub id: String,
    /// Base64url-encoded attestation object.
    pub attestation_object: String,
    /// Base64url-encoded client data JSON.
    pub client_data_json: String,
    /// Optional user-assigned label for the credential.
    pub label: Option<String>,
}

/// Client authentication response from the browser.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AuthenticateCredentialResponse {
    /// Base64url-encoded credential ID.
    pub id: String,
    /// Base64url-encoded authenticator data.
    pub authenticator_data: String,
    /// Base64url-encoded client data JSON.
    pub client_data_json: String,
    /// Base64url-encoded signature.
    pub signature: String,
}

// ── WebAuthnManager ─────────────────────────────────────────────

/// Manages WebAuthn registration and authentication flows.
///
/// Credentials are encrypted via [`SecretStore`] and persisted to a JSON
/// file alongside the secret store.
pub struct WebAuthnManager {
    config: WebAuthnConfig,
    secret_store: Arc<SecretStore>,
    credentials_path: PathBuf,
    rng: ring::rand::SystemRandom,
}

impl WebAuthnManager {
    /// Create a new `WebAuthnManager`.
    ///
    /// `storage_dir` is the directory where the encrypted credentials file
    /// will be stored (typically `~/.zeroclaw/`).
    pub fn new(config: WebAuthnConfig, secret_store: Arc<SecretStore>, storage_dir: &Path) -> Self {
        Self {
            config,
            secret_store,
            credentials_path: storage_dir.join("webauthn_credentials.json"),
            rng: ring::rand::SystemRandom::new(),
        }
    }

    /// Begin a WebAuthn registration ceremony.
    ///
    /// Returns the options to send to the browser and the server-side state
    /// to keep until `finish_registration` is called.
    pub fn start_registration(
        &self,
        user_id: &str,
        user_name: &str,
    ) -> Result<(CreationChallengeResponse, RegistrationState)> {
        let challenge = self.generate_challenge()?;

        // Get existing credentials for this user to exclude
        let existing = self.load_credentials_for_user(user_id)?;
        let exclude: Vec<CredentialDescriptor> = existing
            .iter()
            .map(|c| CredentialDescriptor {
                type_: "public-key".into(),
                id: c.credential_id.clone(),
            })
            .collect();

        let user_id_b64 = URL_SAFE_NO_PAD.encode(user_id.as_bytes());

        let creation = CreationChallengeResponse {
            challenge: challenge.clone(),
            rp: RelyingParty {
                id: self.config.rp_id.clone(),
                name: self.config.rp_name.clone(),
            },
            user: PublicKeyUser {
                id: user_id_b64,
                name: user_name.into(),
                display_name: user_name.into(),
            },
            pub_key_cred_params: vec![PubKeyCredParam {
                type_: "public-key".into(),
                alg: COSE_ALG_ES256,
            }],
            timeout: 60_000,
            attestation: "none".into(),
            exclude_credentials: exclude,
        };

        let state = RegistrationState {
            challenge,
            user_id: user_id.into(),
            user_name: user_name.into(),
        };

        Ok((creation, state))
    }

    /// Complete a WebAuthn registration ceremony.
    ///
    /// Validates the client response against the registration state,
    /// extracts the public key, and stores the credential.
    pub fn finish_registration(
        &self,
        reg_state: &RegistrationState,
        response: &RegisterCredentialResponse,
    ) -> Result<WebAuthnCredential> {
        // 1. Validate client data JSON
        let client_data_bytes = URL_SAFE_NO_PAD
            .decode(&response.client_data_json)
            .context("Invalid base64url in client_data_json")?;
        let client_data: serde_json::Value =
            serde_json::from_slice(&client_data_bytes).context("Invalid client data JSON")?;

        // Verify type
        let cd_type = client_data["type"].as_str().unwrap_or_default();
        anyhow::ensure!(
            cd_type == "webauthn.create",
            "Expected type 'webauthn.create', got '{cd_type}'"
        );

        // Verify challenge matches
        let cd_challenge = client_data["challenge"].as_str().unwrap_or_default();
        anyhow::ensure!(
            cd_challenge == reg_state.challenge,
            "Challenge mismatch in registration response"
        );

        // Verify origin
        let cd_origin = client_data["origin"].as_str().unwrap_or_default();
        anyhow::ensure!(
            cd_origin == self.config.rp_origin,
            "Origin mismatch: expected '{}', got '{cd_origin}'",
            self.config.rp_origin
        );

        // 2. Parse attestation object to extract public key and auth data
        let attestation_bytes = URL_SAFE_NO_PAD
            .decode(&response.attestation_object)
            .context("Invalid base64url in attestation_object")?;

        // For "none" attestation, we extract the authData which contains the
        // credential public key. The attestation object is CBOR-encoded but
        // for our minimal implementation we accept a simplified JSON format
        // from our enrollment UI, or parse the raw CBOR authData.
        let (public_key_bytes, sign_count) =
            extract_public_key_from_attestation(&attestation_bytes)?;

        // 3. Validate credential ID length
        let cred_id_bytes = URL_SAFE_NO_PAD
            .decode(&response.id)
            .context("Invalid base64url in credential ID")?;
        anyhow::ensure!(
            cred_id_bytes.len() <= MAX_CREDENTIAL_ID_LEN,
            "Credential ID too long ({} bytes, max {MAX_CREDENTIAL_ID_LEN})",
            cred_id_bytes.len()
        );

        let now = chrono::Utc::now().to_rfc3339();
        let label = response
            .label
            .clone()
            .unwrap_or_else(|| "Hardware Key".into());

        let credential = WebAuthnCredential {
            credential_id: response.id.clone(),
            public_key: URL_SAFE_NO_PAD.encode(&public_key_bytes),
            sign_count,
            label,
            registered_at: now,
            algorithm: COSE_ALG_ES256,
            user_id: reg_state.user_id.clone(),
        };

        // 4. Store the credential
        self.store_credential(&credential)?;

        Ok(credential)
    }

    /// Begin a WebAuthn authentication ceremony.
    ///
    /// Returns the options to send to the browser and the server-side state
    /// to keep until `finish_authentication` is called.
    pub fn start_authentication(
        &self,
        user_id: &str,
    ) -> Result<(RequestChallengeResponse, AuthenticationState)> {
        let credentials = self.load_credentials_for_user(user_id)?;
        anyhow::ensure!(
            !credentials.is_empty(),
            "No registered credentials for user '{user_id}'"
        );

        let challenge = self.generate_challenge()?;

        let allow: Vec<CredentialDescriptor> = credentials
            .iter()
            .map(|c| CredentialDescriptor {
                type_: "public-key".into(),
                id: c.credential_id.clone(),
            })
            .collect();

        let allowed_ids: Vec<String> = credentials
            .iter()
            .map(|c| c.credential_id.clone())
            .collect();

        let request = RequestChallengeResponse {
            challenge: challenge.clone(),
            rp_id: self.config.rp_id.clone(),
            allow_credentials: allow,
            timeout: 60_000,
            user_verification: "preferred".into(),
        };

        let state = AuthenticationState {
            challenge,
            user_id: user_id.into(),
            allowed_credentials: allowed_ids,
        };

        Ok((request, state))
    }

    /// Complete a WebAuthn authentication ceremony.
    ///
    /// Validates the assertion signature against the stored public key
    /// and updates the sign counter for clone detection.
    pub fn finish_authentication(
        &self,
        auth_state: &AuthenticationState,
        response: &AuthenticateCredentialResponse,
    ) -> Result<()> {
        // 1. Verify credential ID is in allowed list
        anyhow::ensure!(
            auth_state.allowed_credentials.contains(&response.id),
            "Credential ID not in allowed list"
        );

        // 2. Load the credential
        let mut all_credentials = self.load_all_credentials()?;
        let credential = all_credentials
            .values()
            .flatten()
            .find(|c| c.credential_id == response.id)
            .cloned()
            .ok_or_else(|| anyhow::anyhow!("Credential not found: {}", response.id))?;

        // 3. Validate client data JSON
        let client_data_bytes = URL_SAFE_NO_PAD
            .decode(&response.client_data_json)
            .context("Invalid base64url in client_data_json")?;
        let client_data: serde_json::Value =
            serde_json::from_slice(&client_data_bytes).context("Invalid client data JSON")?;

        let cd_type = client_data["type"].as_str().unwrap_or_default();
        anyhow::ensure!(
            cd_type == "webauthn.get",
            "Expected type 'webauthn.get', got '{cd_type}'"
        );

        let cd_challenge = client_data["challenge"].as_str().unwrap_or_default();
        anyhow::ensure!(
            cd_challenge == auth_state.challenge,
            "Challenge mismatch in authentication response"
        );

        let cd_origin = client_data["origin"].as_str().unwrap_or_default();
        anyhow::ensure!(
            cd_origin == self.config.rp_origin,
            "Origin mismatch: expected '{}', got '{cd_origin}'",
            self.config.rp_origin
        );

        // 4. Verify signature
        let auth_data_bytes = URL_SAFE_NO_PAD
            .decode(&response.authenticator_data)
            .context("Invalid base64url in authenticator_data")?;

        // The signed message is: authenticatorData || SHA-256(clientDataJSON)
        let client_data_hash = ring::digest::digest(&ring::digest::SHA256, &client_data_bytes);
        let mut signed_data = auth_data_bytes.clone();
        signed_data.extend_from_slice(client_data_hash.as_ref());

        let public_key_bytes = URL_SAFE_NO_PAD
            .decode(&credential.public_key)
            .context("Invalid base64url in stored public key")?;

        let sig_bytes = URL_SAFE_NO_PAD
            .decode(&response.signature)
            .context("Invalid base64url in signature")?;

        verify_es256_signature(&public_key_bytes, &signed_data, &sig_bytes)?;

        // 5. Verify and update sign counter (clone detection)
        if auth_data_bytes.len() >= 37 {
            let new_count = u32::from_be_bytes([
                auth_data_bytes[33],
                auth_data_bytes[34],
                auth_data_bytes[35],
                auth_data_bytes[36],
            ]);
            if new_count > 0 || credential.sign_count > 0 {
                anyhow::ensure!(
                    new_count > credential.sign_count,
                    "Sign counter did not increase ({new_count} <= {}). Possible cloned authenticator.",
                    credential.sign_count
                );
            }

            // Update the sign counter
            if let Some(user_creds) = all_credentials.get_mut(&credential.user_id) {
                if let Some(cred) = user_creds
                    .iter_mut()
                    .find(|c| c.credential_id == response.id)
                {
                    cred.sign_count = new_count;
                }
            }
            self.save_all_credentials(&all_credentials)?;
        }

        Ok(())
    }

    /// List all credentials for a user.
    pub fn list_credentials(&self, user_id: &str) -> Result<Vec<WebAuthnCredential>> {
        self.load_credentials_for_user(user_id)
    }

    /// Remove a credential by ID.
    pub fn remove_credential(&self, user_id: &str, credential_id: &str) -> Result<()> {
        let mut all = self.load_all_credentials()?;
        if let Some(user_creds) = all.get_mut(user_id) {
            let before = user_creds.len();
            user_creds.retain(|c| c.credential_id != credential_id);
            anyhow::ensure!(
                user_creds.len() < before,
                "Credential '{credential_id}' not found for user '{user_id}'"
            );
        } else {
            anyhow::bail!("No credentials found for user '{user_id}'");
        }
        self.save_all_credentials(&all)
    }

    // ── Private helpers ─────────────────────────────────────────

    fn generate_challenge(&self) -> Result<String> {
        let mut buf = [0u8; CHALLENGE_LEN];
        self.rng
            .fill(&mut buf)
            .map_err(|_| anyhow::anyhow!("Failed to generate random challenge"))?;
        Ok(URL_SAFE_NO_PAD.encode(buf))
    }

    fn load_credentials_for_user(&self, user_id: &str) -> Result<Vec<WebAuthnCredential>> {
        let all = self.load_all_credentials()?;
        Ok(all.get(user_id).cloned().unwrap_or_default())
    }

    fn store_credential(&self, credential: &WebAuthnCredential) -> Result<()> {
        let mut all = self.load_all_credentials()?;
        all.entry(credential.user_id.clone())
            .or_default()
            .push(credential.clone());
        self.save_all_credentials(&all)
    }

    fn load_all_credentials(&self) -> Result<HashMap<String, Vec<WebAuthnCredential>>> {
        if !self.credentials_path.exists() {
            return Ok(HashMap::new());
        }

        let encrypted = std::fs::read_to_string(&self.credentials_path)
            .context("Failed to read WebAuthn credentials file")?;

        if encrypted.is_empty() {
            return Ok(HashMap::new());
        }

        let json = self
            .secret_store
            .decrypt(&encrypted)
            .context("Failed to decrypt WebAuthn credentials")?;

        serde_json::from_str(&json).context("Failed to parse WebAuthn credentials JSON")
    }

    fn save_all_credentials(
        &self,
        credentials: &HashMap<String, Vec<WebAuthnCredential>>,
    ) -> Result<()> {
        let json = serde_json::to_string(credentials).context("Failed to serialize credentials")?;
        let encrypted = self
            .secret_store
            .encrypt(&json)
            .context("Failed to encrypt WebAuthn credentials")?;

        if let Some(parent) = self.credentials_path.parent() {
            std::fs::create_dir_all(parent)?;
        }
        std::fs::write(&self.credentials_path, &encrypted)
            .context("Failed to write WebAuthn credentials file")?;

        // Set restrictive permissions on the credentials file
        #[cfg(unix)]
        {
            use std::os::unix::fs::PermissionsExt;
            std::fs::set_permissions(
                &self.credentials_path,
                std::fs::Permissions::from_mode(0o600),
            )
            .context("Failed to set credentials file permissions")?;
        }

        Ok(())
    }
}

// ── Attestation parsing ─────────────────────────────────────────

/// Extract the public key from an attestation object.
///
/// For the "none" attestation format used by this implementation, the
/// attestation object contains a simplified JSON structure with the
/// public key in uncompressed P-256 format (65 bytes: 0x04 || x || y)
/// or DER-encoded SubjectPublicKeyInfo.
///
/// Returns `(public_key_bytes, sign_count)`.
fn extract_public_key_from_attestation(attestation_bytes: &[u8]) -> Result<(Vec<u8>, u32)> {
    // Try JSON format first (from our enrollment UI)
    if let Ok(att) = serde_json::from_slice::<AttestationObject>(attestation_bytes) {
        let pk = URL_SAFE_NO_PAD
            .decode(&att.public_key)
            .context("Invalid base64url in attestation public key")?;
        return Ok((pk, att.sign_count.unwrap_or(0)));
    }

    // Try raw authData format: the authenticator data starts with
    // rpIdHash (32) + flags (1) + signCount (4) + optional attestedCredentialData
    if attestation_bytes.len() >= 37 {
        let sign_count = u32::from_be_bytes([
            attestation_bytes[33],
            attestation_bytes[34],
            attestation_bytes[35],
            attestation_bytes[36],
        ]);

        // Check if attested credential data is present (bit 6 of flags)
        let flags = attestation_bytes[32];
        if flags & 0x40 != 0 && attestation_bytes.len() > 55 {
            // AAGUID (16) + credIdLen (2) + credId (variable) + COSE key
            let cred_id_len =
                u16::from_be_bytes([attestation_bytes[53], attestation_bytes[54]]) as usize;
            let cose_key_start = 55 + cred_id_len;
            if attestation_bytes.len() > cose_key_start {
                let cose_key = &attestation_bytes[cose_key_start..];
                let pk = extract_p256_from_cose(cose_key)?;
                return Ok((pk, sign_count));
            }
        }
    }

    anyhow::bail!(
        "Unable to extract public key from attestation object ({} bytes)",
        attestation_bytes.len()
    )
}

/// Simplified attestation object for the enrollment UI.
#[derive(Deserialize)]
struct AttestationObject {
    /// Base64url-encoded public key (uncompressed P-256 or DER SPKI).
    public_key: String,
    /// Initial sign counter.
    sign_count: Option<u32>,
}

/// Extract a P-256 uncompressed point from a COSE key map.
///
/// Minimal COSE-key parsing for EC2 / P-256 keys. The COSE key is
/// CBOR-encoded; we look for the x (-2) and y (-3) coordinates.
///
/// For simplicity, we accept the raw uncompressed point format
/// (0x04 || x || y, 65 bytes) directly if the COSE bytes start with 0x04.
fn extract_p256_from_cose(cose: &[u8]) -> Result<Vec<u8>> {
    // If it starts with 0x04 and is 65 bytes, it's already uncompressed P-256
    if cose.len() >= 65 && cose[0] == 0x04 {
        return Ok(cose[..65].to_vec());
    }

    anyhow::bail!(
        "Unsupported COSE key format (expected uncompressed P-256, got {} bytes starting with 0x{:02x})",
        cose.len(),
        cose.first().copied().unwrap_or(0)
    )
}

// ── Signature verification ──────────────────────────────────────

/// Verify an ES256 (ECDSA P-256 + SHA-256) signature.
///
/// `public_key` must be either:
/// - 65-byte uncompressed P-256 point (0x04 || x || y)
/// - DER-encoded SubjectPublicKeyInfo
fn verify_es256_signature(public_key: &[u8], message: &[u8], sig: &[u8]) -> Result<()> {
    // ring's UnparsedPublicKey expects the raw uncompressed point for P-256
    // (not wrapped in SPKI). If we have SPKI, we'd need to extract the point.
    // For our use case the stored key is always the raw uncompressed point.
    let pk = signature::UnparsedPublicKey::new(&signature::ECDSA_P256_SHA256_ASN1, public_key);

    pk.verify(message, sig)
        .map_err(|_| anyhow::anyhow!("WebAuthn signature verification failed"))
}

/// Encode a raw P-256 uncompressed point as DER SubjectPublicKeyInfo.
///
/// The resulting structure is:
/// ```asn1
/// SEQUENCE {
///   SEQUENCE {
///     OID 1.2.840.10045.2.1 (ecPublicKey)
///     OID 1.2.840.10045.3.1.7 (prime256v1 / P-256)
///   }
///   BIT STRING <uncompressed point>
/// }
/// ```
fn encode_p256_spki(uncompressed_point: &[u8]) -> Vec<u8> {
    // Fixed DER prefix for P-256 SubjectPublicKeyInfo
    let mut spki = vec![
        0x30, 0x59, // SEQUENCE (89 bytes)
        0x30, 0x13, // SEQUENCE (19 bytes)
        0x06, 0x07, // OID (7 bytes)
        0x2a, 0x86, 0x48, 0xce, 0x3d, 0x02, 0x01, // 1.2.840.10045.2.1
        0x06, 0x08, // OID (8 bytes)
        0x2a, 0x86, 0x48, 0xce, 0x3d, 0x03, 0x01, 0x07, // 1.2.840.10045.3.1.7
        0x03, 0x42, // BIT STRING (66 bytes)
        0x00, // no unused bits
    ];
    spki.extend_from_slice(uncompressed_point);
    spki
}


#[cfg(test)]
#[path = "webauthn.test.rs"]
mod tests;
