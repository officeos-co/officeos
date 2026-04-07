    use super::*;

    #[test]
    fn constructor_stores_fields() {
        let tunnel = OpenVpnTunnel::new(
            "/etc/openvpn/client.ovpn".into(),
            Some("/etc/openvpn/auth.txt".into()),
            Some("10.8.0.2:42617".into()),
            45,
            vec!["--verb".into(), "3".into()],
        );
        assert_eq!(tunnel.config_file, "/etc/openvpn/client.ovpn");
        assert_eq!(tunnel.auth_file.as_deref(), Some("/etc/openvpn/auth.txt"));
        assert_eq!(tunnel.advertise_address.as_deref(), Some("10.8.0.2:42617"));
        assert_eq!(tunnel.connect_timeout_secs, 45);
        assert_eq!(tunnel.extra_args, vec!["--verb", "3"]);
    }

    #[test]
    fn build_args_basic() {
        let tunnel = OpenVpnTunnel::new("client.ovpn".into(), None, None, 30, vec![]);
        let args = tunnel.build_args();
        assert_eq!(args, vec!["--config", "client.ovpn"]);
    }

    #[test]
    fn build_args_with_auth_and_extras() {
        let tunnel = OpenVpnTunnel::new(
            "client.ovpn".into(),
            Some("auth.txt".into()),
            None,
            30,
            vec!["--verb".into(), "5".into()],
        );
        let args = tunnel.build_args();
        assert_eq!(
            args,
            vec![
                "--config",
                "client.ovpn",
                "--auth-user-pass",
                "auth.txt",
                "--verb",
                "5"
            ]
        );
    }

    #[test]
    fn public_url_is_none_before_start() {
        let tunnel = OpenVpnTunnel::new("client.ovpn".into(), None, None, 30, vec![]);
        assert!(tunnel.public_url().is_none());
    }

    #[tokio::test]
    async fn health_check_is_false_before_start() {
        let tunnel = OpenVpnTunnel::new("client.ovpn".into(), None, None, 30, vec![]);
        assert!(!tunnel.health_check().await);
    }

    #[tokio::test]
    async fn stop_without_started_process_is_ok() {
        let tunnel = OpenVpnTunnel::new("client.ovpn".into(), None, None, 30, vec![]);
        let result = tunnel.stop().await;
        assert!(result.is_ok());
    }

    #[tokio::test]
    async fn start_with_missing_config_file_errors() {
        let tunnel = OpenVpnTunnel::new(
            "/nonexistent/path/to/client.ovpn".into(),
            None,
            None,
            30,
            vec![],
        );
        let result = tunnel.start("127.0.0.1", 8080).await;
        assert!(result.is_err());
        assert!(
            result
                .unwrap_err()
                .to_string()
                .contains("config file not found")
        );
    }
