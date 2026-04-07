    use super::*;

    #[test]
    fn constructor_stores_token() {
        let tunnel = CloudflareTunnel::new("cf-token".into());
        assert_eq!(tunnel.token, "cf-token");
    }

    #[test]
    fn public_url_is_none_before_start() {
        let tunnel = CloudflareTunnel::new("cf-token".into());
        assert!(tunnel.public_url().is_none());
    }

    #[tokio::test]
    async fn stop_without_started_process_is_ok() {
        let tunnel = CloudflareTunnel::new("cf-token".into());
        let result = tunnel.stop().await;
        assert!(result.is_ok());
    }

    #[tokio::test]
    async fn health_check_is_false_before_start() {
        let tunnel = CloudflareTunnel::new("cf-token".into());
        assert!(!tunnel.health_check().await);
    }

    #[test]
    fn extract_skips_quic_go_github_url() {
        let line = "2024-01-01T00:00:00Z WRN failed to sufficiently increase receive buffer size. See https://github.com/quic-go/quic-go/wiki/UDP-Buffer-Sizes for details.";
        assert_eq!(extract_tunnel_url(line), None);
    }

    #[test]
    fn extract_skips_cloudflare_docs_url() {
        let line = "2024-01-01T00:00:00Z INF For more info see https://cloudflare.com/docs/tunnels";
        assert_eq!(extract_tunnel_url(line), None);
    }

    #[test]
    fn extract_skips_developers_cloudflare_url() {
        let line = "2024-01-01T00:00:00Z INF See https://developers.cloudflare.com/cloudflare-one/connections/connect-apps";
        assert_eq!(extract_tunnel_url(line), None);
    }

    #[test]
    fn extract_captures_trycloudflare_url() {
        let line = "2024-01-01T00:00:00Z INF Visit it at https://my-tunnel-abc.trycloudflare.com";
        assert_eq!(
            extract_tunnel_url(line),
            Some("https://my-tunnel-abc.trycloudflare.com".into())
        );
    }

    #[test]
    fn extract_captures_url_on_visit_it_at_line() {
        let line = "2024-01-01T00:00:00Z INF Visit it at https://some-custom-domain.example.com";
        assert_eq!(
            extract_tunnel_url(line),
            Some("https://some-custom-domain.example.com".into())
        );
    }

    #[test]
    fn extract_captures_url_on_route_at_line() {
        let line = "2024-01-01T00:00:00Z INF Route at https://tunnel.example.com/path";
        assert_eq!(
            extract_tunnel_url(line),
            Some("https://tunnel.example.com/path".into())
        );
    }

    #[test]
    fn extract_returns_none_for_line_without_url() {
        let line = "2024-01-01T00:00:00Z INF Starting tunnel";
        assert_eq!(extract_tunnel_url(line), None);
    }
