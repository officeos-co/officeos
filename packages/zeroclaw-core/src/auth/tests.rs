use super::*;
use crate::auth::profiles::{AuthProfile, AuthProfileKind};

#[test]
fn normalize_provider_aliases() {
    assert_eq!(normalize_provider("codex").unwrap(), "openai-codex");
    assert_eq!(normalize_provider("claude").unwrap(), "anthropic");
    assert_eq!(normalize_provider("openai").unwrap(), "openai");
}

#[test]
fn select_profile_prefers_override_then_active_then_default() {
    let mut data = AuthProfilesData::default();
    let id_active = profile_id("openai-codex", "work");
    let id_default = profile_id("openai-codex", "default");

    data.profiles.insert(
        id_default.clone(),
        AuthProfile {
            id: id_default.clone(),
            provider: "openai-codex".into(),
            profile_name: "default".into(),
            kind: AuthProfileKind::Token,
            account_id: None,
            workspace_id: None,
            token_set: None,
            token: Some("x".into()),
            metadata: std::collections::BTreeMap::default(),
            created_at: chrono::Utc::now(),
            updated_at: chrono::Utc::now(),
        },
    );
    data.profiles.insert(
        id_active.clone(),
        AuthProfile {
            id: id_active.clone(),
            provider: "openai-codex".into(),
            profile_name: "work".into(),
            kind: AuthProfileKind::Token,
            account_id: None,
            workspace_id: None,
            token_set: None,
            token: Some("y".into()),
            metadata: std::collections::BTreeMap::default(),
            created_at: chrono::Utc::now(),
            updated_at: chrono::Utc::now(),
        },
    );

    data.active_profiles
        .insert("openai-codex".into(), id_active.clone());

    assert_eq!(
        select_profile_id(&data, "openai-codex", Some("default")),
        Some(id_default)
    );
    assert_eq!(
        select_profile_id(&data, "openai-codex", None),
        Some(id_active)
    );
}
