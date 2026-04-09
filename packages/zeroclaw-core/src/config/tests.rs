use super::*;

#[test]
fn reexported_config_default_is_constructible() {
    let config = Config::default();

    // No compiled-in provider/model defaults — callers must supply
    // them via config file or env vars. `Agent::from_config` enforces
    // this at boot time.
    assert!(config.default_provider.is_none());
    assert!(config.default_model.is_none());
    assert!(config.default_temperature > 0.0);
}

#[test]
fn reexported_channel_configs_are_constructible() {
    let telegram = TelegramConfig {
        bot_token: "token".into(),
        allowed_users: vec!["alice".into()],
        stream_mode: StreamMode::default(),
        draft_update_interval_ms: 1000,
        interrupt_on_new_message: false,
        mention_only: false,
        ack_reactions: None,
        proxy_url: None,
    };

    let discord = DiscordConfig {
        bot_token: "token".into(),
        guild_id: Some("123".into()),
        allowed_users: vec![],
        listen_to_bots: false,
        interrupt_on_new_message: false,
        mention_only: false,
        proxy_url: None,
        stream_mode: StreamMode::default(),
        draft_update_interval_ms: 1000,
        multi_message_delay_ms: 800,
        stall_timeout_secs: 0,
    };

    let lark = LarkConfig {
        app_id: "app-id".into(),
        app_secret: "app-secret".into(),
        encrypt_key: None,
        verification_token: None,
        allowed_users: vec![],
        mention_only: false,
        use_feishu: false,
        receive_mode: crate::config::schema::LarkReceiveMode::Websocket,
        port: None,
        proxy_url: None,
    };
    let feishu = FeishuConfig {
        app_id: "app-id".into(),
        app_secret: "app-secret".into(),
        encrypt_key: None,
        verification_token: None,
        allowed_users: vec![],
        receive_mode: crate::config::schema::LarkReceiveMode::Websocket,
        port: None,
        proxy_url: None,
    };

    let nextcloud_talk = NextcloudTalkConfig {
        base_url: "https://cloud.example.com".into(),
        app_token: "app-token".into(),
        webhook_secret: None,
        allowed_users: vec!["*".into()],
        proxy_url: None,
        bot_name: None,
    };

    assert_eq!(telegram.allowed_users.len(), 1);
    assert_eq!(discord.guild_id.as_deref(), Some("123"));
    assert_eq!(lark.app_id, "app-id");
    assert_eq!(feishu.app_id, "app-id");
    assert_eq!(nextcloud_talk.base_url, "https://cloud.example.com");
}
