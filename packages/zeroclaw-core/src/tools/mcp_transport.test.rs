    use super::*;

    #[test]
    fn test_transport_default_is_stdio() {
        let config = McpServerConfig::default();
        assert_eq!(config.transport, McpTransport::Stdio);
    }

    #[test]
    fn test_http_transport_requires_url() {
        let config = McpServerConfig {
            name: "test".into(),
            transport: McpTransport::Http,
            ..Default::default()
        };
        assert!(HttpTransport::new(&config).is_err());
    }

    #[test]
    fn test_sse_transport_requires_url() {
        let config = McpServerConfig {
            name: "test".into(),
            transport: McpTransport::Sse,
            ..Default::default()
        };
        assert!(SseTransport::new(&config).is_err());
    }

    #[test]
    fn test_extract_json_from_sse_data_no_space() {
        let input = "data:{\"jsonrpc\":\"2.0\",\"result\":{}}\n\n";
        let extracted = extract_json_from_sse_text(input);
        let _: JsonRpcResponse = serde_json::from_str(extracted.as_ref()).unwrap();
    }

    #[test]
    fn test_extract_json_from_sse_with_event_and_id() {
        let input = "id: 1\nevent: message\ndata: {\"jsonrpc\":\"2.0\",\"result\":{}}\n\n";
        let extracted = extract_json_from_sse_text(input);
        let _: JsonRpcResponse = serde_json::from_str(extracted.as_ref()).unwrap();
    }

    #[test]
    fn test_extract_json_from_sse_multiline_data() {
        let input = "event: message\ndata: {\ndata:   \"jsonrpc\": \"2.0\",\ndata:   \"result\": {}\ndata: }\n\n";
        let extracted = extract_json_from_sse_text(input);
        let _: JsonRpcResponse = serde_json::from_str(extracted.as_ref()).unwrap();
    }

    #[test]
    fn test_extract_json_from_sse_skips_bom_and_leading_whitespace() {
        let input = "\u{feff}\n\n  data: {\"jsonrpc\":\"2.0\",\"result\":{}}\n\n";
        let extracted = extract_json_from_sse_text(input);
        let _: JsonRpcResponse = serde_json::from_str(extracted.as_ref()).unwrap();
    }

    #[test]
    fn test_extract_json_from_sse_uses_last_event_with_data() {
        let input =
            ": keep-alive\n\nid: 1\nevent: message\ndata: {\"jsonrpc\":\"2.0\",\"result\":{}}\n\n";
        let extracted = extract_json_from_sse_text(input);
        let _: JsonRpcResponse = serde_json::from_str(extracted.as_ref()).unwrap();
    }

    #[test]
    fn test_parse_jsonrpc_response_text_handles_plain_json() {
        let parsed = parse_jsonrpc_response_text("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{}}")
            .expect("plain JSON response should parse");
        assert_eq!(parsed.id, Some(serde_json::json!(1)));
        assert!(parsed.error.is_none());
    }

    #[test]
    fn test_parse_jsonrpc_response_text_handles_sse_framed_json() {
        let sse =
            "event: message\ndata: {\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"ok\":true}}\n\n";
        let parsed =
            parse_jsonrpc_response_text(sse).expect("SSE-framed JSON response should parse");
        assert_eq!(parsed.id, Some(serde_json::json!(2)));
        assert_eq!(
            parsed
                .result
                .as_ref()
                .and_then(|v| v.get("ok"))
                .and_then(|v| v.as_bool()),
            Some(true)
        );
    }

    #[test]
    fn test_parse_jsonrpc_response_text_rejects_empty_payload() {
        assert!(parse_jsonrpc_response_text(" \n\t ").is_err());
    }

    #[test]
    fn http_transport_updates_session_id_from_response_headers() {
        let config = McpServerConfig {
            name: "test-http".into(),
            transport: McpTransport::Http,
            url: Some("http://localhost/mcp".into()),
            ..Default::default()
        };
        let mut transport = HttpTransport::new(&config).expect("build transport");

        let mut headers = reqwest::header::HeaderMap::new();
        headers.insert(
            reqwest::header::HeaderName::from_static("mcp-session-id"),
            reqwest::header::HeaderValue::from_static("session-abc"),
        );
        transport.update_session_id_from_headers(&headers);
        assert_eq!(transport.session_id.as_deref(), Some("session-abc"));
    }

    #[test]
    fn http_transport_injects_session_id_header_when_available() {
        let config = McpServerConfig {
            name: "test-http".into(),
            transport: McpTransport::Http,
            url: Some("http://localhost/mcp".into()),
            ..Default::default()
        };
        let mut transport = HttpTransport::new(&config).expect("build transport");
        transport.session_id = Some("session-xyz".to_string());

        let req = transport
            .apply_session_header(reqwest::Client::new().post("http://localhost/mcp"))
            .build()
            .expect("build request");
        assert_eq!(
            req.headers()
                .get(MCP_SESSION_ID_HEADER)
                .and_then(|v| v.to_str().ok()),
            Some("session-xyz")
        );
    }

    // ── derive_message_url tests ──────────────────────────────────────────────

    #[test]
    fn derive_message_url_replaces_sse_segment_with_messages() {
        let url = derive_message_url("http://localhost:3000/mcp/sse", "messages");
        assert_eq!(url, Some("http://localhost:3000/mcp/messages".to_string()));
    }

    #[test]
    fn derive_message_url_appends_when_no_sse_segment() {
        let url = derive_message_url("http://localhost:3000/mcp", "messages");
        assert_eq!(url, Some("http://localhost:3000/mcp/messages".to_string()));
    }

    #[test]
    fn derive_message_url_returns_none_for_invalid_url() {
        let url = derive_message_url("not-a-url", "messages");
        assert!(url.is_none());
    }

    #[test]
    fn derive_message_url_message_path_variant() {
        let url = derive_message_url("http://localhost:3000/mcp/sse", "message");
        assert_eq!(url, Some("http://localhost:3000/mcp/message".to_string()));
    }

    // ── parse_endpoint_from_data tests ───────────────────────────────────────

    #[test]
    fn parse_endpoint_absolute_http_url_returned_as_is() {
        let result = parse_endpoint_from_data("http://base/sse", "http://other/messages");
        assert_eq!(result, Some("http://other/messages".to_string()));
    }

    #[test]
    fn parse_endpoint_absolute_https_url_returned_as_is() {
        let result = parse_endpoint_from_data("https://base/sse", "https://other/messages");
        assert_eq!(result, Some("https://other/messages".to_string()));
    }

    #[test]
    fn parse_endpoint_relative_path_resolved_against_base() {
        let result = parse_endpoint_from_data("http://localhost:3000/sse", "/messages");
        assert_eq!(result, Some("http://localhost:3000/messages".to_string()));
    }

    #[test]
    fn parse_endpoint_json_object_with_endpoint_key() {
        let json_data = r#"{"endpoint":"/messages"}"#;
        let result = parse_endpoint_from_data("http://localhost:3000/sse", json_data);
        assert_eq!(result, Some("http://localhost:3000/messages".to_string()));
    }

    // ── looks_like_sse_text tests ─────────────────────────────────────────────

    #[test]
    fn looks_like_sse_text_detects_data_prefix() {
        assert!(looks_like_sse_text("data:{\"jsonrpc\":\"2.0\"}"));
    }

    #[test]
    fn looks_like_sse_text_detects_event_prefix() {
        assert!(looks_like_sse_text("event: message\ndata: {}"));
    }

    #[test]
    fn looks_like_sse_text_detects_embedded_data_line() {
        assert!(looks_like_sse_text("id: 1\ndata:{\"x\":1}"));
    }

    #[test]
    fn looks_like_sse_text_plain_json_is_not_sse() {
        assert!(!looks_like_sse_text(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{}}"
        ));
    }

    // ── extract_json_from_sse_text edge cases ─────────────────────────────────

    #[test]
    fn extract_json_skips_comment_lines() {
        let input = ": keep-alive\ndata: {\"jsonrpc\":\"2.0\",\"result\":{}}\n\n";
        let extracted = extract_json_from_sse_text(input);
        let v: serde_json::Value = serde_json::from_str(extracted.as_ref()).unwrap();
        assert_eq!(v["jsonrpc"], "2.0");
    }

    #[test]
    fn extract_json_empty_input_returns_empty_trimmed() {
        let result = extract_json_from_sse_text("   ");
        assert!(result.as_ref().trim().is_empty());
    }

    #[test]
    fn extract_json_plain_json_returned_unchanged() {
        let input = "{\"jsonrpc\":\"2.0\",\"result\":{}}";
        let extracted = extract_json_from_sse_text(input);
        // No SSE framing, extracted as-is (trimmed)
        assert_eq!(extracted.as_ref(), input);
    }

    // ── parse_jsonrpc_response_text edge cases ────────────────────────────────

    #[test]
    fn parse_jsonrpc_response_rejects_whitespace_only() {
        assert!(parse_jsonrpc_response_text("   \n\t  ").is_err());
    }

    #[test]
    fn parse_jsonrpc_response_with_error_result() {
        let json = r#"{"jsonrpc":"2.0","id":1,"error":{"code":-32601,"message":"not found"}}"#;
        let resp = parse_jsonrpc_response_text(json).unwrap();
        assert!(resp.error.is_some());
        assert_eq!(resp.error.unwrap().code, -32601);
    }

    // ── create_transport factory ──────────────────────────────────────────────

    #[test]
    fn create_transport_stdio_fails_without_valid_command() {
        // Spawning a non-existent binary should fail
        let config = McpServerConfig {
            name: "test-stdio".into(),
            transport: McpTransport::Stdio,
            command: "/usr/bin/zeroclaw_nonexistent_binary_abc123".into(),
            ..Default::default()
        };
        let result = create_transport(&config);
        assert!(result.is_err());
    }

    #[test]
    fn create_transport_http_without_url_fails() {
        let config = McpServerConfig {
            name: "test-http".into(),
            transport: McpTransport::Http,
            ..Default::default()
        };
        assert!(create_transport(&config).is_err());
    }

    #[test]
    fn create_transport_sse_without_url_fails() {
        let config = McpServerConfig {
            name: "test-sse".into(),
            transport: McpTransport::Sse,
            ..Default::default()
        };
        assert!(create_transport(&config).is_err());
    }

    #[test]
    fn create_transport_http_with_url_succeeds() {
        let config = McpServerConfig {
            name: "test-http".into(),
            transport: McpTransport::Http,
            url: Some("http://localhost:9999/mcp".into()),
            ..Default::default()
        };
        // Build should succeed even if server isn't running
        assert!(create_transport(&config).is_ok());
    }

    #[test]
    fn create_transport_sse_with_url_succeeds() {
        let config = McpServerConfig {
            name: "test-sse".into(),
            transport: McpTransport::Sse,
            url: Some("http://localhost:9999/sse".into()),
            ..Default::default()
        };
        assert!(create_transport(&config).is_ok());
    }

    // ── HTTP session id whitespace handling ───────────────────────────────────

    #[test]
    fn http_transport_ignores_empty_session_id_header() {
        let config = McpServerConfig {
            name: "test-http".into(),
            transport: McpTransport::Http,
            url: Some("http://localhost/mcp".into()),
            ..Default::default()
        };
        let mut transport = HttpTransport::new(&config).expect("build transport");
        let mut headers = reqwest::header::HeaderMap::new();
        headers.insert(
            reqwest::header::HeaderName::from_static("mcp-session-id"),
            reqwest::header::HeaderValue::from_static("   "),
        );
        transport.update_session_id_from_headers(&headers);
        // Whitespace-only session id should not be stored
        assert!(transport.session_id.is_none());
    }

    #[test]
    fn http_transport_no_session_header_leaves_none() {
        let config = McpServerConfig {
            name: "test-http".into(),
            transport: McpTransport::Http,
            url: Some("http://localhost/mcp".into()),
            ..Default::default()
        };
        let transport = HttpTransport::new(&config).expect("build transport");
        assert!(transport.session_id.is_none());
    }

    #[test]
    fn http_transport_apply_session_header_noop_when_no_session() {
        let config = McpServerConfig {
            name: "test-http".into(),
            transport: McpTransport::Http,
            url: Some("http://localhost/mcp".into()),
            ..Default::default()
        };
        let transport = HttpTransport::new(&config).expect("build transport");
        let req = transport
            .apply_session_header(reqwest::Client::new().post("http://localhost/mcp"))
            .build()
            .expect("build request");
        assert!(req.headers().get(MCP_SESSION_ID_HEADER).is_none());
    }
