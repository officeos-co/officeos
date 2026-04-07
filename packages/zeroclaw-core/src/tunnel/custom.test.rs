    use super::*;

    #[tokio::test]
    async fn start_with_empty_command_returns_error() {
        let tunnel = CustomTunnel::new("   ".into(), None, None);
        let result = tunnel.start("127.0.0.1", 8080).await;

        assert!(result.is_err());
        assert!(
            result
                .unwrap_err()
                .to_string()
                .contains("start_command is empty")
        );
    }

    #[tokio::test]
    async fn start_without_pattern_returns_local_url() {
        let tunnel = CustomTunnel::new("sleep 1".into(), None, None);

        let url = tunnel.start("127.0.0.1", 4455).await.unwrap();
        assert_eq!(url, "http://127.0.0.1:4455");
        assert_eq!(
            tunnel.public_url().as_deref(),
            Some("http://127.0.0.1:4455")
        );

        tunnel.stop().await.unwrap();
    }

    #[tokio::test]
    async fn start_with_pattern_extracts_url() {
        let tunnel = CustomTunnel::new(
            "echo https://public.example".into(),
            None,
            Some("public.example".into()),
        );

        let url = tunnel.start("localhost", 9999).await.unwrap();

        assert_eq!(url, "https://public.example");
        assert_eq!(
            tunnel.public_url().as_deref(),
            Some("https://public.example")
        );

        tunnel.stop().await.unwrap();
    }

    #[tokio::test]
    async fn start_replaces_host_and_port_placeholders() {
        let tunnel = CustomTunnel::new(
            "echo http://{host}:{port}".into(),
            None,
            Some("http://".into()),
        );

        let url = tunnel.start("10.1.2.3", 4321).await.unwrap();

        assert_eq!(url, "http://10.1.2.3:4321");
        tunnel.stop().await.unwrap();
    }

    #[tokio::test]
    async fn health_check_with_unreachable_health_url_returns_false() {
        let tunnel = CustomTunnel::new(
            "sleep 1".into(),
            Some("http://127.0.0.1:9/healthz".into()),
            None,
        );

        assert!(!tunnel.health_check().await);
    }
