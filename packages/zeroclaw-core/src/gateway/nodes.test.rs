    use super::*;

    #[test]
    fn node_registry_register_and_unregister() {
        let registry = NodeRegistry::new(10);
        let (tx, _rx) = mpsc::channel(1);

        let info = NodeInfo {
            node_id: "test-node".to_string(),
            capabilities: vec![NodeCapability {
                name: "ping".to_string(),
                description: "Ping test".to_string(),
                parameters: serde_json::json!({"type": "object", "properties": {}}),
            }],
            invoke_tx: tx,
        };

        assert!(registry.register(info));
        assert!(registry.contains("test-node"));
        assert_eq!(registry.len(), 1);

        registry.unregister("test-node");
        assert!(!registry.contains("test-node"));
        assert_eq!(registry.len(), 0);
    }

    #[test]
    fn node_registry_capacity_limit() {
        let registry = NodeRegistry::new(2);

        for i in 0..2 {
            let (tx, _rx) = mpsc::channel(1);
            let info = NodeInfo {
                node_id: format!("node-{i}"),
                capabilities: vec![],
                invoke_tx: tx,
            };
            assert!(registry.register(info));
        }

        let (tx, _rx) = mpsc::channel(1);
        let info = NodeInfo {
            node_id: "node-overflow".to_string(),
            capabilities: vec![],
            invoke_tx: tx,
        };
        assert!(!registry.register(info));
        assert_eq!(registry.len(), 2);
    }

    #[test]
    fn node_registry_re_register_same_id() {
        let registry = NodeRegistry::new(2);
        let (tx1, _rx1) = mpsc::channel(1);
        let (tx2, _rx2) = mpsc::channel(1);

        let info1 = NodeInfo {
            node_id: "node-1".to_string(),
            capabilities: vec![NodeCapability {
                name: "old".to_string(),
                description: "Old cap".to_string(),
                parameters: serde_json::json!({"type": "object", "properties": {}}),
            }],
            invoke_tx: tx1,
        };
        assert!(registry.register(info1));

        let info2 = NodeInfo {
            node_id: "node-1".to_string(),
            capabilities: vec![NodeCapability {
                name: "new".to_string(),
                description: "New cap".to_string(),
                parameters: serde_json::json!({"type": "object", "properties": {}}),
            }],
            invoke_tx: tx2,
        };
        // Re-registering same node_id should succeed (update)
        assert!(registry.register(info2));
        assert_eq!(registry.len(), 1);

        let caps = registry.all_capabilities();
        assert_eq!(caps.len(), 1);
        assert_eq!(caps[0].2.name, "new");
    }

    #[test]
    fn node_registry_all_capabilities() {
        let registry = NodeRegistry::new(10);
        let (tx1, _rx1) = mpsc::channel(1);
        let (tx2, _rx2) = mpsc::channel(1);

        registry.register(NodeInfo {
            node_id: "phone-1".to_string(),
            capabilities: vec![
                NodeCapability {
                    name: "camera.snap".to_string(),
                    description: "Take a photo".to_string(),
                    parameters: serde_json::json!({"type": "object", "properties": {}}),
                },
                NodeCapability {
                    name: "gps.location".to_string(),
                    description: "Get GPS location".to_string(),
                    parameters: serde_json::json!({"type": "object", "properties": {}}),
                },
            ],
            invoke_tx: tx1,
        });

        registry.register(NodeInfo {
            node_id: "sensor-1".to_string(),
            capabilities: vec![NodeCapability {
                name: "temp.read".to_string(),
                description: "Read temperature".to_string(),
                parameters: serde_json::json!({"type": "object", "properties": {}}),
            }],
            invoke_tx: tx2,
        });

        let caps = registry.all_capabilities();
        assert_eq!(caps.len(), 3);
    }

    #[test]
    fn node_registry_is_empty() {
        let registry = NodeRegistry::new(10);
        assert!(registry.is_empty());

        let (tx, _rx) = mpsc::channel(1);
        registry.register(NodeInfo {
            node_id: "n".to_string(),
            capabilities: vec![],
            invoke_tx: tx,
        });
        assert!(!registry.is_empty());
    }

    #[test]
    fn node_capability_deserialize() {
        let json = r#"{"name":"camera.snap","description":"Take a photo"}"#;
        let cap: NodeCapability = serde_json::from_str(json).unwrap();
        assert_eq!(cap.name, "camera.snap");
        assert_eq!(cap.description, "Take a photo");
        // Default parameters
        assert_eq!(cap.parameters["type"], "object");
    }

    #[test]
    fn node_message_register_deserialize() {
        let json = r#"{"type":"register","node_id":"phone-1","capabilities":[{"name":"camera.snap","description":"Take a photo","parameters":{"type":"object","properties":{"resolution":{"type":"string"}}}}]}"#;
        let msg: NodeMessage = serde_json::from_str(json).unwrap();
        match msg {
            NodeMessage::Register {
                node_id,
                capabilities,
            } => {
                assert_eq!(node_id, "phone-1");
                assert_eq!(capabilities.len(), 1);
                assert_eq!(capabilities[0].name, "camera.snap");
            }
            NodeMessage::Result { .. } => panic!("Expected Register message"),
        }
    }

    #[test]
    fn node_message_result_deserialize() {
        let json = r#"{"type":"result","call_id":"abc-123","success":true,"output":"photo taken"}"#;
        let msg: NodeMessage = serde_json::from_str(json).unwrap();
        match msg {
            NodeMessage::Result {
                call_id,
                success,
                output,
                error,
            } => {
                assert_eq!(call_id, "abc-123");
                assert!(success);
                assert_eq!(output, "photo taken");
                assert!(error.is_none());
            }
            NodeMessage::Register { .. } => panic!("Expected Result message"),
        }
    }

    #[test]
    fn gateway_message_serialize() {
        let msg = GatewayMessage::Registered {
            node_id: "phone-1".to_string(),
            capabilities_count: 3,
        };
        let json = serde_json::to_string(&msg).unwrap();
        assert!(json.contains("\"type\":\"registered\""));
        assert!(json.contains("\"node_id\":\"phone-1\""));
        assert!(json.contains("\"capabilities_count\":3"));
    }

    #[test]
    fn gateway_invoke_message_serialize() {
        let msg = GatewayMessage::Invoke {
            call_id: "call-1".to_string(),
            capability: "camera.snap".to_string(),
            args: serde_json::json!({"resolution": "1080p"}),
        };
        let json = serde_json::to_string(&msg).unwrap();
        assert!(json.contains("\"type\":\"invoke\""));
        assert!(json.contains("\"capability\":\"camera.snap\""));
    }

    #[test]
    fn extract_node_ws_token_from_header() {
        let mut headers = HeaderMap::new();
        headers.insert("authorization", "Bearer node_tok_123".parse().unwrap());
        assert_eq!(extract_node_ws_token(&headers, None), Some("node_tok_123"));
    }

    #[test]
    fn extract_node_ws_token_from_query() {
        let headers = HeaderMap::new();
        assert_eq!(
            extract_node_ws_token(&headers, Some("node_tok_456")),
            Some("node_tok_456")
        );
    }

    #[test]
    fn extract_node_ws_token_none_when_empty() {
        let headers = HeaderMap::new();
        assert_eq!(extract_node_ws_token(&headers, None), None);
    }
