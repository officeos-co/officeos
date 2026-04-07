    use super::*;

    #[test]
    fn name_is_none() {
        let tunnel = NoneTunnel;
        assert_eq!(tunnel.name(), "none");
    }

    #[tokio::test]
    async fn start_returns_local_url() {
        let tunnel = NoneTunnel;
        let url = tunnel.start("127.0.0.1", 7788).await.unwrap();
        assert_eq!(url, "http://127.0.0.1:7788");
    }

    #[tokio::test]
    async fn stop_is_noop_success() {
        let tunnel = NoneTunnel;
        assert!(tunnel.stop().await.is_ok());
    }

    #[tokio::test]
    async fn health_check_is_always_true() {
        let tunnel = NoneTunnel;
        assert!(tunnel.health_check().await);
    }

    #[test]
    fn public_url_is_always_none() {
        let tunnel = NoneTunnel;
        assert!(tunnel.public_url().is_none());
    }
