    use super::*;

    #[test]
    fn parse_kind_from_metadata() {
        assert_eq!(
            AnthropicAuthKind::from_metadata_value("authorization"),
            Some(AnthropicAuthKind::Authorization)
        );
        assert_eq!(
            AnthropicAuthKind::from_metadata_value("x-api-key"),
            Some(AnthropicAuthKind::ApiKey)
        );
        assert_eq!(AnthropicAuthKind::from_metadata_value("nope"), None);
    }

    #[test]
    fn detect_prefers_override() {
        let kind = detect_auth_kind("sk-ant-api-123", Some("authorization"));
        assert_eq!(kind, AnthropicAuthKind::Authorization);
    }

    #[test]
    fn detect_jwt_like_as_authorization() {
        let kind = detect_auth_kind("aaa.bbb.ccc", None);
        assert_eq!(kind, AnthropicAuthKind::Authorization);
    }

    #[test]
    fn detect_default_for_api_prefix() {
        let kind = detect_auth_kind("sk-ant-api-123", None);
        assert_eq!(kind, AnthropicAuthKind::ApiKey);
    }
