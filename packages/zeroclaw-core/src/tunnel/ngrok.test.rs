    use super::*;

    #[test]
    fn constructor_stores_domain() {
        let tunnel = NgrokTunnel::new("ngrok-token".into(), Some("my.ngrok.app".into()));
        assert_eq!(tunnel.domain.as_deref(), Some("my.ngrok.app"));
    }

    #[test]
    fn public_url_is_none_before_start() {
        let tunnel = NgrokTunnel::new("ngrok-token".into(), None);
        assert!(tunnel.public_url().is_none());
    }

    #[tokio::test]
    async fn stop_without_started_process_is_ok() {
        let tunnel = NgrokTunnel::new("ngrok-token".into(), None);
        let result = tunnel.stop().await;
        assert!(result.is_ok());
    }

    #[tokio::test]
    async fn health_check_is_false_before_start() {
        let tunnel = NgrokTunnel::new("ngrok-token".into(), None);
        assert!(!tunnel.health_check().await);
    }
