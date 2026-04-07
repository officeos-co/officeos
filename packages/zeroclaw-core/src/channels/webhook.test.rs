    use super::*;

    fn make_channel() -> WebhookChannel {
        WebhookChannel::new(
            8080,
            Some("/webhook".into()),
            Some("https://example.com/callback".into()),
            None,
            None,
            None,
        )
    }

    fn make_channel_with_secret() -> WebhookChannel {
        WebhookChannel::new(
            8080,
            None,
            Some("https://example.com/callback".into()),
            None,
            None,
            Some("mysecret".into()),
        )
    }

    #[test]
    fn default_path() {
        let ch = WebhookChannel::new(8080, None, None, None, None, None);
        assert_eq!(ch.listen_path, "/webhook");
    }

    #[test]
    fn path_normalized() {
        let ch = WebhookChannel::new(8080, Some("hooks/incoming".into()), None, None, None, None);
        assert_eq!(ch.listen_path, "/hooks/incoming");
    }

    #[test]
    fn send_method_default() {
        let ch = make_channel();
        assert_eq!(ch.send_method, "POST");
    }

    #[test]
    fn send_method_put() {
        let ch = WebhookChannel::new(
            8080,
            None,
            Some("https://example.com".into()),
            Some("put".into()),
            None,
            None,
        );
        assert_eq!(ch.send_method, "PUT");
    }

    #[test]
    fn incoming_payload_deserializes_all_fields() {
        let json = r#"{"sender": "zeroclaw_user", "content": "hello", "thread_id": "t1"}"#;
        let payload: IncomingWebhook = serde_json::from_str(json).unwrap();
        assert_eq!(payload.sender, "zeroclaw_user");
        assert_eq!(payload.content, "hello");
        assert_eq!(payload.thread_id.as_deref(), Some("t1"));
    }

    #[test]
    fn incoming_payload_without_thread() {
        let json = r#"{"sender": "bob", "content": "hi"}"#;
        let payload: IncomingWebhook = serde_json::from_str(json).unwrap();
        assert_eq!(payload.sender, "bob");
        assert_eq!(payload.content, "hi");
        assert!(payload.thread_id.is_none());
    }

    #[test]
    fn outgoing_payload_serializes_content() {
        let payload = OutgoingWebhook {
            content: "response".into(),
            thread_id: Some("t1".into()),
            recipient: Some("zeroclaw_user".into()),
        };
        let json = serde_json::to_value(&payload).unwrap();
        assert_eq!(json["content"], "response");
        assert_eq!(json["thread_id"], "t1");
        assert_eq!(json["recipient"], "zeroclaw_user");
    }

    #[test]
    fn outgoing_payload_omits_none_fields() {
        let payload = OutgoingWebhook {
            content: "response".into(),
            thread_id: None,
            recipient: None,
        };
        let json = serde_json::to_value(&payload).unwrap();
        assert_eq!(json["content"], "response");
        assert!(json.get("thread_id").is_none());
        assert!(json.get("recipient").is_none());
    }

    #[test]
    fn verify_signature_no_secret() {
        let ch = make_channel();
        assert!(ch.verify_signature(b"body", None));
    }

    #[test]
    fn verify_signature_missing_header() {
        let ch = make_channel_with_secret();
        assert!(!ch.verify_signature(b"body", None));
    }

    #[test]
    fn verify_signature_valid() {
        use hmac::{Hmac, Mac};
        use sha2::Sha256;
        type HmacSha256 = Hmac<Sha256>;

        let ch = make_channel_with_secret();
        let body = b"test body";

        let mut mac = HmacSha256::new_from_slice(b"mysecret").unwrap();
        mac.update(body);
        let sig = hex::encode(mac.finalize().into_bytes());

        assert!(ch.verify_signature(body, Some(&sig)));
    }

    #[test]
    fn verify_signature_invalid() {
        let ch = make_channel_with_secret();
        assert!(!ch.verify_signature(b"body", Some("badhex")));
    }
