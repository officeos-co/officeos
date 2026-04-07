    use super::*;

    #[test]
    fn name_returns_pinggy() {
        let tunnel = PinggyTunnel::new(None, None);
        assert_eq!(tunnel.name(), "pinggy");
    }

    #[test]
    fn constructor_stores_fields() {
        let tunnel = PinggyTunnel::new(Some("test-token".into()), Some("us".into()));
        assert_eq!(tunnel.token.as_deref(), Some("test-token"));
        assert_eq!(tunnel.region.as_deref(), Some("us"));
    }

    #[test]
    fn public_url_is_none_before_start() {
        let tunnel = PinggyTunnel::new(None, None);
        assert!(tunnel.public_url().is_none());
    }

    #[tokio::test]
    async fn stop_before_start_is_ok() {
        let tunnel = PinggyTunnel::new(None, None);
        let result = tunnel.stop().await;
        assert!(result.is_ok());
    }

    #[tokio::test]
    async fn health_check_is_false_before_start() {
        let tunnel = PinggyTunnel::new(None, None);
        assert!(!tunnel.health_check().await);
    }
