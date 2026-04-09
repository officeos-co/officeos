use super::*;

#[test]
fn token_validation_mode_from_str() {
    assert_eq!(
        TokenValidationMode::from_str_config("local").unwrap(),
        TokenValidationMode::Local
    );
    assert_eq!(
        TokenValidationMode::from_str_config("REMOTE").unwrap(),
        TokenValidationMode::Remote
    );
    assert!(TokenValidationMode::from_str_config("invalid").is_err());
}

#[test]
fn local_mode_requires_jwks_url() {
    let result = NevisAuthProvider::new(
        "https://nevis.example.com".into(),
        "master".into(),
        "zeroclaw-client".into(),
        None,
        "local",
        None, // no JWKS URL
        false,
        3600,
    );
    assert!(result.is_err());
    assert!(result.unwrap_err().to_string().contains("jwks_url"));
}

#[test]
fn remote_mode_works_without_jwks_url() {
    let provider = NevisAuthProvider::new(
        "https://nevis.example.com".into(),
        "master".into(),
        "zeroclaw-client".into(),
        None,
        "remote",
        None,
        false,
        3600,
    );
    assert!(provider.is_ok());
}

#[test]
fn provider_stores_config_correctly() {
    let provider = NevisAuthProvider::new(
        "https://nevis.example.com".into(),
        "test-realm".into(),
        "zeroclaw-client".into(),
        Some("test-secret".into()),
        "remote",
        None,
        true,
        7200,
    )
    .unwrap();

    assert_eq!(provider.instance_url(), "https://nevis.example.com");
    assert_eq!(provider.realm(), "test-realm");
    assert!(provider.require_mfa);
    assert_eq!(provider.session_timeout, Duration::from_secs(7200));
}

#[test]
fn debug_redacts_client_secret() {
    let provider = NevisAuthProvider::new(
        "https://nevis.example.com".into(),
        "test-realm".into(),
        "zeroclaw-client".into(),
        Some("super-secret-value".into()),
        "remote",
        None,
        false,
        3600,
    )
    .unwrap();

    let debug_output = format!("{:?}", provider);
    assert!(
        !debug_output.contains("super-secret-value"),
        "Debug output must not contain the raw client_secret"
    );
    assert!(
        debug_output.contains("[REDACTED]"),
        "Debug output must show [REDACTED] for client_secret"
    );
}

#[tokio::test]
async fn validate_token_rejects_empty() {
    let provider = NevisAuthProvider::new(
        "https://nevis.example.com".into(),
        "master".into(),
        "zeroclaw-client".into(),
        None,
        "remote",
        None,
        false,
        3600,
    )
    .unwrap();

    let err = provider.validate_token("").await.unwrap_err();
    assert!(err.to_string().contains("empty bearer token"));
}

#[tokio::test]
async fn validate_session_rejects_empty() {
    let provider = NevisAuthProvider::new(
        "https://nevis.example.com".into(),
        "master".into(),
        "zeroclaw-client".into(),
        None,
        "remote",
        None,
        false,
        3600,
    )
    .unwrap();

    let err = provider.validate_session("").await.unwrap_err();
    assert!(err.to_string().contains("empty session token"));
}

#[test]
fn nevis_identity_serde_roundtrip() {
    let identity = NevisIdentity {
        user_id: "zeroclaw_user".into(),
        roles: vec!["admin".into(), "operator".into()],
        scopes: vec!["openid".into(), "profile".into()],
        mfa_verified: true,
        session_expiry: 1_700_000_000,
    };

    let json = serde_json::to_string(&identity).unwrap();
    let parsed: NevisIdentity = serde_json::from_str(&json).unwrap();
    assert_eq!(parsed.user_id, "zeroclaw_user");
    assert_eq!(parsed.roles.len(), 2);
    assert!(parsed.mfa_verified);
}

#[tokio::test]
async fn local_validation_rejects_malformed_jwt() {
    let provider = NevisAuthProvider::new(
        "https://nevis.example.com".into(),
        "master".into(),
        "zeroclaw-client".into(),
        None,
        "local",
        Some("https://nevis.example.com/.well-known/jwks.json".into()),
        false,
        3600,
    )
    .unwrap();

    let err = provider.validate_token("not-a-jwt").await.unwrap_err();
    assert!(err.to_string().contains("Invalid JWT structure"));
}

#[tokio::test]
async fn local_validation_errors_instead_of_silent_fallback() {
    let provider = NevisAuthProvider::new(
        "https://nevis.example.com".into(),
        "master".into(),
        "zeroclaw-client".into(),
        None,
        "local",
        Some("https://nevis.example.com/.well-known/jwks.json".into()),
        false,
        3600,
    )
    .unwrap();

    // A well-formed JWT structure should hit the "not yet implemented" error
    // instead of silently falling back to remote introspection.
    let err = provider
        .validate_token("header.payload.signature")
        .await
        .unwrap_err();
    assert!(err.to_string().contains("not yet implemented"));
}
