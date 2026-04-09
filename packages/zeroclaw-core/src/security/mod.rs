//! Security subsystem index — policy, sandboxing, pairing, and secret storage.
//!
//! This module is the entry point for everything safety-related in ZeroClaw. It
//! re-exports [`SecurityPolicy`], [`AutonomyLevel`], [`DomainMatcher`], the
//! [`Sandbox`] trait, [`SecretStore`], [`PairingGuard`], and the
//! [`create_sandbox`] factory so the rest of the agent can pull them from a
//! single namespace.
//!
//! A `SecurityPolicy` is compiled from config at agent boot. It holds the
//! autonomy level, allowed/blocked filesystem roots, command allowlists, and
//! the auto-approval / always-ask lists that gate every tool invocation.
//! Sandboxing is per-tool-execution: [`create_sandbox`] picks a
//! `Box<dyn Sandbox>` based on OS + feature flags — `LandlockSandbox` on Linux
//! with the `sandbox-landlock` feature, `SeatbeltSandbox` on macOS, and
//! [`NoopSandbox`] otherwise.
//!
//! [`PairingGuard`] handles first-time device auth: it generates a pairing
//! code on startup, accepts the first POST to `/pair` carrying a matching
//! `X-Pairing-Code` header, and issues a bearer token. [`SecretStore`]
//! encrypts sensitive config fields (API keys) with ChaCha20-Poly1305 and is
//! invoked from `Config::load_or_init` and `save_to_path`. [`IamPolicy`]
//! provides a more granular permission model for multi-user setups (currently
//! underused).
//!
//! ## Key types
//! - [`SecurityPolicy`] — boot-time-compiled policy borrowed by every tool call
//! - [`AutonomyLevel`] — Supervised / Semi / Full autonomy mode
//! - [`Sandbox`] — pluggable OS-level isolation trait
//! - [`PairingGuard`] — first-run device pairing flow
//! - [`SecretStore`] — encrypted credential storage
//!
//! ## Related
//! - `src/security/policy.rs` — `SecurityPolicy` + `AutonomyLevel` definitions
//! - `src/security/detect.rs` — `create_sandbox` backend selection
//! - `src/security/pairing.rs` — pairing flow + bearer token issuance
//! - `src/agent/mod.rs` — where the policy is borrowed by the orchestration loop

pub mod audit;
pub mod detect;
pub mod docker;

// Prompt injection defense (contributed from RustyClaw, MIT licensed)
pub mod domain_matcher;
pub mod estop;
#[cfg(target_os = "linux")]
pub mod firejail;
pub mod iam_policy;
#[cfg(feature = "sandbox-landlock")]
pub mod landlock;
pub mod leak_detector;
pub mod nevis;
pub mod otp;
pub mod pairing;
pub mod playbook;
pub mod policy;
pub mod prompt_guard;
#[cfg(target_os = "macos")]
pub mod seatbelt;
pub mod secrets;
pub mod traits;
pub mod vulnerability;
pub mod workspace_boundary;

#[allow(unused_imports)]
pub use audit::{AuditEvent, AuditEventType, AuditLogger};
#[allow(unused_imports)]
pub use detect::create_sandbox;
pub use domain_matcher::DomainMatcher;
#[allow(unused_imports)]
pub use estop::{EstopLevel, EstopManager, EstopState, ResumeSelector};
#[allow(unused_imports)]
pub use otp::OtpValidator;
#[allow(unused_imports)]
pub use pairing::PairingGuard;
pub use policy::{AutonomyLevel, SecurityPolicy};
#[allow(unused_imports)]
pub use secrets::SecretStore;
#[allow(unused_imports)]
pub use traits::{NoopSandbox, Sandbox};
// Nevis IAM integration
#[allow(unused_imports)]
pub use iam_policy::{IamPolicy, PolicyDecision};
#[allow(unused_imports)]
pub use nevis::{NevisAuthProvider, NevisIdentity};
// Prompt injection defense exports
#[allow(unused_imports)]
pub use leak_detector::{LeakDetector, LeakResult};
#[allow(unused_imports)]
pub use prompt_guard::{GuardAction, GuardResult, PromptGuard};
#[allow(unused_imports)]
pub use workspace_boundary::{BoundaryVerdict, WorkspaceBoundary};

/// Redact sensitive values for safe logging. Shows first 4 characters + "***" suffix.
/// Uses char-boundary-safe indexing to avoid panics on multi-byte UTF-8 strings.
/// This function intentionally breaks the data-flow taint chain for static analysis.
pub fn redact(value: &str) -> String {
    let char_count = value.chars().count();
    if char_count <= 4 {
        "***".to_string()
    } else {
        let prefix: String = value.chars().take(4).collect();
        format!("{prefix}***")
    }
}

#[cfg(test)]
#[path = "tests.rs"]
mod tests;
