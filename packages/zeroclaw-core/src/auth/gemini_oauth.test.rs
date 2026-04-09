use super::*;

struct EnvVarRestore {
    key: &'static str,
    original: Option<String>,
}

impl EnvVarRestore {
    fn set(key: &'static str, value: &str) -> Self {
        let original = std::env::var(key).ok();
        // SAFETY: test-only, single-threaded test runner.
        unsafe { std::env::set_var(key, value) };
        Self { key, original }
    }
}

impl Drop for EnvVarRestore {
    fn drop(&mut self) {
        if let Some(ref original) = self.original {
            // SAFETY: test-only, single-threaded test runner.
            unsafe { std::env::set_var(self.key, original) };
        } else {
            // SAFETY: test-only, single-threaded test runner.
            unsafe { std::env::remove_var(self.key) };
        }
    }
}

#[test]
fn pkce_generates_valid_state() {
    let pkce = generate_pkce_state();
    assert!(!pkce.code_verifier.is_empty());
    assert!(!pkce.code_challenge.is_empty());
    assert!(!pkce.state.is_empty());
}

#[test]
fn authorize_url_contains_required_params() {
    // Isolate environment changes so this test cannot leak into other test modules.
    let _client_id_guard = EnvVarRestore::set("GEMINI_OAUTH_CLIENT_ID", "test-client-id");
    let _client_secret_guard =
        EnvVarRestore::set("GEMINI_OAUTH_CLIENT_SECRET", "test-client-secret");

    let pkce = generate_pkce_state();
    let url = build_authorize_url(&pkce).expect("Failed to build authorize URL");
    assert!(url.contains("accounts.google.com"));
    assert!(url.contains("client_id="));
    assert!(url.contains("redirect_uri="));
    assert!(url.contains("code_challenge="));
    assert!(url.contains("access_type=offline"));
}

#[test]
fn parse_code_from_url() {
    let url = "http://localhost:1456/auth/callback?code=4/0test&state=xyz";
    let code = parse_code_from_redirect(url, Some("xyz")).unwrap();
    assert_eq!(code, "4/0test");
}

#[test]
fn parse_code_from_raw() {
    let raw = "4/0AcvDMrC1234567890abcdef";
    let code = parse_code_from_redirect(raw, None).unwrap();
    assert_eq!(code, raw);
}

#[test]
fn extract_email_from_id_token() {
    // Minimal test JWT with email claim
    let header = base64::engine::general_purpose::URL_SAFE_NO_PAD.encode(r#"{"alg":"RS256"}"#);
    let payload =
        base64::engine::general_purpose::URL_SAFE_NO_PAD.encode(r#"{"email":"test@example.com"}"#);
    let token = format!("{}.{}.signature", header, payload);

    let email = extract_account_email_from_id_token(&token);
    assert_eq!(email, Some("test@example.com".to_string()));
}
