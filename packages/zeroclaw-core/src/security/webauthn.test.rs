    use super::*;
    use ring::signature::KeyPair;
    use tempfile::TempDir;

    fn test_config() -> WebAuthnConfig {
        WebAuthnConfig {
            enabled: true,
            rp_id: "localhost".into(),
            rp_origin: "http://localhost:42617".into(),
            rp_name: "ZeroClaw Test".into(),
        }
    }

    fn test_manager(tmp: &TempDir) -> WebAuthnManager {
        let store = Arc::new(SecretStore::new(tmp.path(), true));
        WebAuthnManager::new(test_config(), store, tmp.path())
    }

    #[test]
    fn start_registration_returns_valid_challenge() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        let (creation, state) = mgr.start_registration("user1", "Alice").unwrap();

        assert_eq!(creation.rp.id, "localhost");
        assert_eq!(creation.rp.name, "ZeroClaw Test");
        assert_eq!(creation.user.name, "Alice");
        assert_eq!(creation.attestation, "none");
        assert!(!creation.challenge.is_empty());
        assert_eq!(creation.challenge, state.challenge);
        assert_eq!(state.user_id, "user1");

        // Challenge should be 32 bytes = 43 base64url chars (no padding)
        let decoded = URL_SAFE_NO_PAD.decode(&creation.challenge).unwrap();
        assert_eq!(decoded.len(), CHALLENGE_LEN);
    }

    #[test]
    fn start_registration_produces_unique_challenges() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        let (c1, _) = mgr.start_registration("user1", "Alice").unwrap();
        let (c2, _) = mgr.start_registration("user1", "Alice").unwrap();

        assert_ne!(
            c1.challenge, c2.challenge,
            "Each registration should produce a unique challenge"
        );
    }

    #[test]
    fn finish_registration_validates_challenge() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        let (_, state) = mgr.start_registration("user1", "Alice").unwrap();

        // Create client data with wrong challenge
        let client_data = serde_json::json!({
            "type": "webauthn.create",
            "challenge": "wrong-challenge",
            "origin": "http://localhost:42617"
        });
        let client_data_b64 = URL_SAFE_NO_PAD.encode(serde_json::to_vec(&client_data).unwrap());

        let attestation = serde_json::json!({
            "public_key": URL_SAFE_NO_PAD.encode(vec![0x04; 65]),
            "sign_count": 0
        });
        let att_b64 = URL_SAFE_NO_PAD.encode(serde_json::to_vec(&attestation).unwrap());

        let response = RegisterCredentialResponse {
            id: URL_SAFE_NO_PAD.encode(b"cred-123"),
            attestation_object: att_b64,
            client_data_json: client_data_b64,
            label: None,
        };

        let result = mgr.finish_registration(&state, &response);
        assert!(result.is_err());
        assert!(
            result
                .unwrap_err()
                .to_string()
                .contains("Challenge mismatch"),
            "Should fail on challenge mismatch"
        );
    }

    #[test]
    fn finish_registration_validates_origin() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        let (_, state) = mgr.start_registration("user1", "Alice").unwrap();

        let client_data = serde_json::json!({
            "type": "webauthn.create",
            "challenge": state.challenge,
            "origin": "https://evil.com"
        });
        let client_data_b64 = URL_SAFE_NO_PAD.encode(serde_json::to_vec(&client_data).unwrap());

        let attestation = serde_json::json!({
            "public_key": URL_SAFE_NO_PAD.encode(vec![0x04; 65]),
            "sign_count": 0
        });
        let att_b64 = URL_SAFE_NO_PAD.encode(serde_json::to_vec(&attestation).unwrap());

        let response = RegisterCredentialResponse {
            id: URL_SAFE_NO_PAD.encode(b"cred-123"),
            attestation_object: att_b64,
            client_data_json: client_data_b64,
            label: None,
        };

        let result = mgr.finish_registration(&state, &response);
        assert!(result.is_err());
        assert!(
            result.unwrap_err().to_string().contains("Origin mismatch"),
            "Should fail on origin mismatch"
        );
    }

    #[test]
    fn finish_registration_validates_type() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        let (_, state) = mgr.start_registration("user1", "Alice").unwrap();

        let client_data = serde_json::json!({
            "type": "webauthn.get",
            "challenge": state.challenge,
            "origin": "http://localhost:42617"
        });
        let client_data_b64 = URL_SAFE_NO_PAD.encode(serde_json::to_vec(&client_data).unwrap());

        let attestation = serde_json::json!({
            "public_key": URL_SAFE_NO_PAD.encode(vec![0x04; 65]),
            "sign_count": 0
        });
        let att_b64 = URL_SAFE_NO_PAD.encode(serde_json::to_vec(&attestation).unwrap());

        let response = RegisterCredentialResponse {
            id: URL_SAFE_NO_PAD.encode(b"cred-123"),
            attestation_object: att_b64,
            client_data_json: client_data_b64,
            label: None,
        };

        let result = mgr.finish_registration(&state, &response);
        assert!(result.is_err());
        assert!(
            result
                .unwrap_err()
                .to_string()
                .contains("Expected type 'webauthn.create'"),
        );
    }

    #[test]
    fn registration_stores_credential_and_lists_it() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        let (_, state) = mgr.start_registration("user1", "Alice").unwrap();

        // Generate a real P-256 key pair for testing
        let rng = ring::rand::SystemRandom::new();
        let pkcs8 = ring::signature::EcdsaKeyPair::generate_pkcs8(
            &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
            &rng,
        )
        .unwrap();
        let key_pair = ring::signature::EcdsaKeyPair::from_pkcs8(
            &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
            pkcs8.as_ref(),
            &rng,
        )
        .unwrap();
        let public_key = key_pair.public_key().as_ref();

        let client_data = serde_json::json!({
            "type": "webauthn.create",
            "challenge": state.challenge,
            "origin": "http://localhost:42617"
        });
        let client_data_b64 = URL_SAFE_NO_PAD.encode(serde_json::to_vec(&client_data).unwrap());

        let attestation = serde_json::json!({
            "public_key": URL_SAFE_NO_PAD.encode(public_key),
            "sign_count": 0
        });
        let att_b64 = URL_SAFE_NO_PAD.encode(serde_json::to_vec(&attestation).unwrap());

        let response = RegisterCredentialResponse {
            id: URL_SAFE_NO_PAD.encode(b"test-cred-1"),
            attestation_object: att_b64,
            client_data_json: client_data_b64,
            label: Some("Test YubiKey".into()),
        };

        let credential = mgr.finish_registration(&state, &response).unwrap();
        assert_eq!(credential.user_id, "user1");
        assert_eq!(credential.label, "Test YubiKey");
        assert_eq!(credential.algorithm, COSE_ALG_ES256);
        assert_eq!(credential.sign_count, 0);

        // List should contain the credential
        let creds = mgr.list_credentials("user1").unwrap();
        assert_eq!(creds.len(), 1);
        assert_eq!(creds[0].credential_id, credential.credential_id);
    }

    #[test]
    fn multiple_credentials_per_user() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        for i in 0..3 {
            let (_, state) = mgr.start_registration("user1", "Alice").unwrap();

            let rng = ring::rand::SystemRandom::new();
            let pkcs8 = ring::signature::EcdsaKeyPair::generate_pkcs8(
                &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
                &rng,
            )
            .unwrap();
            let key_pair = ring::signature::EcdsaKeyPair::from_pkcs8(
                &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
                pkcs8.as_ref(),
                &rng,
            )
            .unwrap();

            let client_data = serde_json::json!({
                "type": "webauthn.create",
                "challenge": state.challenge,
                "origin": "http://localhost:42617"
            });
            let client_data_b64 = URL_SAFE_NO_PAD.encode(serde_json::to_vec(&client_data).unwrap());

            let attestation = serde_json::json!({
                "public_key": URL_SAFE_NO_PAD.encode(key_pair.public_key().as_ref()),
                "sign_count": 0
            });
            let att_b64 = URL_SAFE_NO_PAD.encode(serde_json::to_vec(&attestation).unwrap());

            let response = RegisterCredentialResponse {
                id: URL_SAFE_NO_PAD.encode(format!("cred-{i}").as_bytes()),
                attestation_object: att_b64,
                client_data_json: client_data_b64,
                label: Some(format!("Key {i}")),
            };

            mgr.finish_registration(&state, &response).unwrap();
        }

        let creds = mgr.list_credentials("user1").unwrap();
        assert_eq!(creds.len(), 3);
    }

    #[test]
    fn remove_credential_works() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        // Register a credential
        let (_, state) = mgr.start_registration("user1", "Alice").unwrap();

        let rng = ring::rand::SystemRandom::new();
        let pkcs8 = ring::signature::EcdsaKeyPair::generate_pkcs8(
            &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
            &rng,
        )
        .unwrap();
        let key_pair = ring::signature::EcdsaKeyPair::from_pkcs8(
            &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
            pkcs8.as_ref(),
            &rng,
        )
        .unwrap();

        let client_data = serde_json::json!({
            "type": "webauthn.create",
            "challenge": state.challenge,
            "origin": "http://localhost:42617"
        });
        let client_data_b64 = URL_SAFE_NO_PAD.encode(serde_json::to_vec(&client_data).unwrap());

        let attestation = serde_json::json!({
            "public_key": URL_SAFE_NO_PAD.encode(key_pair.public_key().as_ref()),
            "sign_count": 0
        });
        let att_b64 = URL_SAFE_NO_PAD.encode(serde_json::to_vec(&attestation).unwrap());

        let cred_id = URL_SAFE_NO_PAD.encode(b"cred-to-remove");
        let response = RegisterCredentialResponse {
            id: cred_id.clone(),
            attestation_object: att_b64,
            client_data_json: client_data_b64,
            label: None,
        };

        mgr.finish_registration(&state, &response).unwrap();
        assert_eq!(mgr.list_credentials("user1").unwrap().len(), 1);

        mgr.remove_credential("user1", &cred_id).unwrap();
        assert_eq!(mgr.list_credentials("user1").unwrap().len(), 0);
    }

    #[test]
    fn remove_nonexistent_credential_fails() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        let result = mgr.remove_credential("user1", "nonexistent");
        assert!(result.is_err());
    }

    #[test]
    fn start_authentication_fails_without_credentials() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        let result = mgr.start_authentication("user1");
        assert!(result.is_err());
        assert!(
            result
                .unwrap_err()
                .to_string()
                .contains("No registered credentials"),
        );
    }

    #[test]
    fn start_authentication_returns_valid_options() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        // Register first
        let (_, state) = mgr.start_registration("user1", "Alice").unwrap();
        let rng = ring::rand::SystemRandom::new();
        let pkcs8 = ring::signature::EcdsaKeyPair::generate_pkcs8(
            &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
            &rng,
        )
        .unwrap();
        let key_pair = ring::signature::EcdsaKeyPair::from_pkcs8(
            &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
            pkcs8.as_ref(),
            &rng,
        )
        .unwrap();

        let client_data = serde_json::json!({
            "type": "webauthn.create",
            "challenge": state.challenge,
            "origin": "http://localhost:42617"
        });
        let cred_id = URL_SAFE_NO_PAD.encode(b"auth-test-cred");
        let attestation = serde_json::json!({
            "public_key": URL_SAFE_NO_PAD.encode(key_pair.public_key().as_ref()),
            "sign_count": 0
        });

        mgr.finish_registration(
            &state,
            &RegisterCredentialResponse {
                id: cred_id.clone(),
                attestation_object: URL_SAFE_NO_PAD
                    .encode(serde_json::to_vec(&attestation).unwrap()),
                client_data_json: URL_SAFE_NO_PAD.encode(serde_json::to_vec(&client_data).unwrap()),
                label: None,
            },
        )
        .unwrap();

        // Now start authentication
        let (request, auth_state) = mgr.start_authentication("user1").unwrap();
        assert_eq!(request.rp_id, "localhost");
        assert!(!request.challenge.is_empty());
        assert_eq!(request.allow_credentials.len(), 1);
        assert_eq!(request.allow_credentials[0].id, cred_id);
        assert_eq!(auth_state.user_id, "user1");
    }

    #[test]
    fn full_authentication_flow_with_real_keys() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        // 1. Register
        let (_, reg_state) = mgr.start_registration("user1", "Alice").unwrap();
        let rng = ring::rand::SystemRandom::new();
        let pkcs8 = ring::signature::EcdsaKeyPair::generate_pkcs8(
            &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
            &rng,
        )
        .unwrap();
        let key_pair = ring::signature::EcdsaKeyPair::from_pkcs8(
            &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
            pkcs8.as_ref(),
            &rng,
        )
        .unwrap();

        let reg_client_data = serde_json::json!({
            "type": "webauthn.create",
            "challenge": reg_state.challenge,
            "origin": "http://localhost:42617"
        });

        let cred_id = URL_SAFE_NO_PAD.encode(b"full-flow-cred");
        let attestation = serde_json::json!({
            "public_key": URL_SAFE_NO_PAD.encode(key_pair.public_key().as_ref()),
            "sign_count": 0
        });

        mgr.finish_registration(
            &reg_state,
            &RegisterCredentialResponse {
                id: cred_id.clone(),
                attestation_object: URL_SAFE_NO_PAD
                    .encode(serde_json::to_vec(&attestation).unwrap()),
                client_data_json: URL_SAFE_NO_PAD
                    .encode(serde_json::to_vec(&reg_client_data).unwrap()),
                label: Some("Full Flow Key".into()),
            },
        )
        .unwrap();

        // 2. Authenticate
        let (_, auth_state) = mgr.start_authentication("user1").unwrap();

        let auth_client_data = serde_json::json!({
            "type": "webauthn.get",
            "challenge": auth_state.challenge,
            "origin": "http://localhost:42617"
        });
        let auth_client_data_bytes = serde_json::to_vec(&auth_client_data).unwrap();

        // Build authenticator data:
        // rpIdHash (32) + flags (1, 0x01 = UP) + signCount (4, = 1)
        let rp_id_hash = ring::digest::digest(&ring::digest::SHA256, b"localhost");
        let mut auth_data = Vec::with_capacity(37);
        auth_data.extend_from_slice(rp_id_hash.as_ref()); // 32 bytes
        auth_data.push(0x01); // flags: UP
        auth_data.extend_from_slice(&1u32.to_be_bytes()); // sign count = 1

        // Sign: authenticatorData || SHA-256(clientDataJSON)
        let client_data_hash = ring::digest::digest(&ring::digest::SHA256, &auth_client_data_bytes);
        let mut signed_data = auth_data.clone();
        signed_data.extend_from_slice(client_data_hash.as_ref());

        let sig = key_pair.sign(&rng, &signed_data).unwrap();

        let auth_response = AuthenticateCredentialResponse {
            id: cred_id,
            authenticator_data: URL_SAFE_NO_PAD.encode(&auth_data),
            client_data_json: URL_SAFE_NO_PAD.encode(&auth_client_data_bytes),
            signature: URL_SAFE_NO_PAD.encode(sig.as_ref()),
        };

        mgr.finish_authentication(&auth_state, &auth_response)
            .unwrap();

        // Verify sign count was updated
        let creds = mgr.list_credentials("user1").unwrap();
        assert_eq!(creds[0].sign_count, 1);
    }

    #[test]
    fn authentication_rejects_wrong_credential_id() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        // Register
        let (_, reg_state) = mgr.start_registration("user1", "Alice").unwrap();
        let rng = ring::rand::SystemRandom::new();
        let pkcs8 = ring::signature::EcdsaKeyPair::generate_pkcs8(
            &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
            &rng,
        )
        .unwrap();
        let key_pair = ring::signature::EcdsaKeyPair::from_pkcs8(
            &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
            pkcs8.as_ref(),
            &rng,
        )
        .unwrap();

        let client_data = serde_json::json!({
            "type": "webauthn.create",
            "challenge": reg_state.challenge,
            "origin": "http://localhost:42617"
        });
        let attestation = serde_json::json!({
            "public_key": URL_SAFE_NO_PAD.encode(key_pair.public_key().as_ref()),
            "sign_count": 0
        });

        mgr.finish_registration(
            &reg_state,
            &RegisterCredentialResponse {
                id: URL_SAFE_NO_PAD.encode(b"real-cred"),
                attestation_object: URL_SAFE_NO_PAD
                    .encode(serde_json::to_vec(&attestation).unwrap()),
                client_data_json: URL_SAFE_NO_PAD.encode(serde_json::to_vec(&client_data).unwrap()),
                label: None,
            },
        )
        .unwrap();

        let (_, auth_state) = mgr.start_authentication("user1").unwrap();

        let response = AuthenticateCredentialResponse {
            id: URL_SAFE_NO_PAD.encode(b"wrong-cred"),
            authenticator_data: URL_SAFE_NO_PAD.encode(b"dummy"),
            client_data_json: URL_SAFE_NO_PAD.encode(b"{}"),
            signature: URL_SAFE_NO_PAD.encode(b"dummy"),
        };

        let result = mgr.finish_authentication(&auth_state, &response);
        assert!(result.is_err());
        assert!(
            result
                .unwrap_err()
                .to_string()
                .contains("not in allowed list"),
        );
    }

    #[test]
    fn credentials_are_encrypted_on_disk() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        let (_, state) = mgr.start_registration("user1", "Alice").unwrap();
        let rng = ring::rand::SystemRandom::new();
        let pkcs8 = ring::signature::EcdsaKeyPair::generate_pkcs8(
            &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
            &rng,
        )
        .unwrap();
        let key_pair = ring::signature::EcdsaKeyPair::from_pkcs8(
            &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
            pkcs8.as_ref(),
            &rng,
        )
        .unwrap();

        let client_data = serde_json::json!({
            "type": "webauthn.create",
            "challenge": state.challenge,
            "origin": "http://localhost:42617"
        });
        let attestation = serde_json::json!({
            "public_key": URL_SAFE_NO_PAD.encode(key_pair.public_key().as_ref()),
            "sign_count": 0
        });

        mgr.finish_registration(
            &state,
            &RegisterCredentialResponse {
                id: URL_SAFE_NO_PAD.encode(b"enc-test"),
                attestation_object: URL_SAFE_NO_PAD
                    .encode(serde_json::to_vec(&attestation).unwrap()),
                client_data_json: URL_SAFE_NO_PAD.encode(serde_json::to_vec(&client_data).unwrap()),
                label: None,
            },
        )
        .unwrap();

        // Read raw file — it should be encrypted (enc2: prefix)
        let raw = std::fs::read_to_string(tmp.path().join("webauthn_credentials.json")).unwrap();
        assert!(
            raw.starts_with("enc2:"),
            "Credentials file should be encrypted"
        );
        assert!(
            !raw.contains("user1"),
            "User ID should not appear in encrypted file"
        );
    }

    #[test]
    fn exclude_credentials_populated_on_second_registration() {
        let tmp = TempDir::new().unwrap();
        let mgr = test_manager(&tmp);

        // Register first credential
        let (_, state) = mgr.start_registration("user1", "Alice").unwrap();
        let rng = ring::rand::SystemRandom::new();
        let pkcs8 = ring::signature::EcdsaKeyPair::generate_pkcs8(
            &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
            &rng,
        )
        .unwrap();
        let key_pair = ring::signature::EcdsaKeyPair::from_pkcs8(
            &signature::ECDSA_P256_SHA256_ASN1_SIGNING,
            pkcs8.as_ref(),
            &rng,
        )
        .unwrap();

        let first_cred_id = URL_SAFE_NO_PAD.encode(b"first-cred");
        let client_data = serde_json::json!({
            "type": "webauthn.create",
            "challenge": state.challenge,
            "origin": "http://localhost:42617"
        });
        let attestation = serde_json::json!({
            "public_key": URL_SAFE_NO_PAD.encode(key_pair.public_key().as_ref()),
            "sign_count": 0
        });

        mgr.finish_registration(
            &state,
            &RegisterCredentialResponse {
                id: first_cred_id.clone(),
                attestation_object: URL_SAFE_NO_PAD
                    .encode(serde_json::to_vec(&attestation).unwrap()),
                client_data_json: URL_SAFE_NO_PAD.encode(serde_json::to_vec(&client_data).unwrap()),
                label: None,
            },
        )
        .unwrap();

        // Start second registration — should have exclude_credentials
        let (creation2, _) = mgr.start_registration("user1", "Alice").unwrap();
        assert_eq!(creation2.exclude_credentials.len(), 1);
        assert_eq!(creation2.exclude_credentials[0].id, first_cred_id);
    }

    #[test]
    fn encode_p256_spki_produces_correct_length() {
        let point = [0x04u8; 65];
        let spki = encode_p256_spki(&point);
        // DER prefix is 26 bytes + 65 byte point = 91 bytes
        assert_eq!(spki.len(), 91);
        // First byte should be SEQUENCE tag
        assert_eq!(spki[0], 0x30);
    }

    #[test]
    fn default_config_has_sane_values() {
        let config = WebAuthnConfig::default();
        assert!(!config.enabled);
        assert_eq!(config.rp_id, "localhost");
        assert_eq!(config.rp_name, "ZeroClaw");
    }
