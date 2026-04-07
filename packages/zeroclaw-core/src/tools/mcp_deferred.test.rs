    use super::*;

    fn make_stub(name: &str, desc: &str) -> DeferredMcpToolStub {
        let def = McpToolDef {
            name: name.to_string(),
            description: Some(desc.to_string()),
            input_schema: serde_json::json!({"type": "object", "properties": {}}),
        };
        DeferredMcpToolStub::new(name.to_string(), def)
    }

    #[test]
    fn stub_uses_description_from_def() {
        let stub = make_stub("fs__read", "Read a file");
        assert_eq!(stub.description, "Read a file");
    }

    #[test]
    fn stub_defaults_description_when_none() {
        let def = McpToolDef {
            name: "mystery".into(),
            description: None,
            input_schema: serde_json::json!({}),
        };
        let stub = DeferredMcpToolStub::new("srv__mystery".into(), def);
        assert_eq!(stub.description, "MCP tool");
    }

    #[test]
    fn activated_set_tracks_activation() {
        use crate::tools::traits::ToolResult;
        use async_trait::async_trait;

        struct FakeTool;
        #[async_trait]
        impl Tool for FakeTool {
            fn name(&self) -> &str {
                "fake"
            }
            fn description(&self) -> &str {
                "fake tool"
            }
            fn parameters_schema(&self) -> serde_json::Value {
                serde_json::json!({})
            }
            async fn execute(&self, _: serde_json::Value) -> anyhow::Result<ToolResult> {
                Ok(ToolResult {
                    success: true,
                    output: String::new(),
                    error: None,
                })
            }
        }

        let mut set = ActivatedToolSet::new();
        assert!(!set.is_activated("fake"));
        set.activate("fake".into(), Arc::new(FakeTool));
        assert!(set.is_activated("fake"));
        assert!(set.get("fake").is_some());
        assert_eq!(set.tool_specs().len(), 1);
    }

    #[test]
    fn activated_set_resolves_unique_suffix() {
        use crate::tools::traits::ToolResult;
        use async_trait::async_trait;

        struct FakeTool;
        #[async_trait]
        impl Tool for FakeTool {
            fn name(&self) -> &str {
                "docker-mcp__extract_text"
            }
            fn description(&self) -> &str {
                "fake tool"
            }
            fn parameters_schema(&self) -> serde_json::Value {
                serde_json::json!({})
            }
            async fn execute(&self, _: serde_json::Value) -> anyhow::Result<ToolResult> {
                Ok(ToolResult {
                    success: true,
                    output: String::new(),
                    error: None,
                })
            }
        }

        let mut set = ActivatedToolSet::new();
        set.activate("docker-mcp__extract_text".into(), Arc::new(FakeTool));
        assert!(set.get_resolved("extract_text").is_some());
    }

    #[test]
    fn activated_set_rejects_ambiguous_suffix() {
        use crate::tools::traits::ToolResult;
        use async_trait::async_trait;

        struct FakeTool(&'static str);
        #[async_trait]
        impl Tool for FakeTool {
            fn name(&self) -> &str {
                self.0
            }
            fn description(&self) -> &str {
                "fake tool"
            }
            fn parameters_schema(&self) -> serde_json::Value {
                serde_json::json!({})
            }
            async fn execute(&self, _: serde_json::Value) -> anyhow::Result<ToolResult> {
                Ok(ToolResult {
                    success: true,
                    output: String::new(),
                    error: None,
                })
            }
        }

        let mut set = ActivatedToolSet::new();
        set.activate(
            "docker-mcp__extract_text".into(),
            Arc::new(FakeTool("docker-mcp__extract_text")),
        );
        set.activate(
            "ocr-mcp__extract_text".into(),
            Arc::new(FakeTool("ocr-mcp__extract_text")),
        );
        assert!(set.get_resolved("extract_text").is_none());
    }

    #[test]
    fn build_deferred_section_empty_when_no_stubs() {
        let set = DeferredMcpToolSet {
            stubs: vec![],
            registry: std::sync::Arc::new(
                tokio::runtime::Runtime::new()
                    .unwrap()
                    .block_on(McpRegistry::connect_all(&[]))
                    .unwrap(),
            ),
        };
        assert!(build_deferred_tools_section(&set).is_empty());
    }

    #[test]
    fn build_deferred_section_lists_names() {
        let stubs = vec![
            make_stub("fs__read_file", "Read a file"),
            make_stub("git__status", "Git status"),
        ];
        let set = DeferredMcpToolSet {
            stubs,
            registry: std::sync::Arc::new(
                tokio::runtime::Runtime::new()
                    .unwrap()
                    .block_on(McpRegistry::connect_all(&[]))
                    .unwrap(),
            ),
        };
        let section = build_deferred_tools_section(&set);
        assert!(section.contains("<available-deferred-tools>"));
        assert!(section.contains("fs__read_file - Read a file"));
        assert!(section.contains("git__status - Git status"));
        assert!(section.contains("</available-deferred-tools>"));
    }

    #[test]
    fn build_deferred_section_includes_tool_search_instruction() {
        let stubs = vec![make_stub("fs__read_file", "Read a file")];
        let set = DeferredMcpToolSet {
            stubs,
            registry: std::sync::Arc::new(
                tokio::runtime::Runtime::new()
                    .unwrap()
                    .block_on(McpRegistry::connect_all(&[]))
                    .unwrap(),
            ),
        };
        let section = build_deferred_tools_section(&set);
        assert!(
            section.contains("tool_search"),
            "deferred section must instruct the LLM to use tool_search"
        );
        assert!(
            section.contains("## Deferred Tools"),
            "deferred section must include a heading"
        );
    }

    #[test]
    fn build_deferred_section_multiple_servers() {
        let stubs = vec![
            make_stub("server_a__list", "List items"),
            make_stub("server_a__create", "Create item"),
            make_stub("server_b__query", "Query records"),
        ];
        let set = DeferredMcpToolSet {
            stubs,
            registry: std::sync::Arc::new(
                tokio::runtime::Runtime::new()
                    .unwrap()
                    .block_on(McpRegistry::connect_all(&[]))
                    .unwrap(),
            ),
        };
        let section = build_deferred_tools_section(&set);
        assert!(section.contains("server_a__list"));
        assert!(section.contains("server_a__create"));
        assert!(section.contains("server_b__query"));
        assert!(
            section.contains("tool_search"),
            "section must mention tool_search for multi-server setups"
        );
    }

    #[test]
    fn keyword_search_ranks_by_hits() {
        let stubs = vec![
            make_stub("fs__read_file", "Read a file from disk"),
            make_stub("fs__write_file", "Write a file to disk"),
            make_stub("git__log", "Show git log"),
        ];
        let set = DeferredMcpToolSet {
            stubs,
            registry: std::sync::Arc::new(
                tokio::runtime::Runtime::new()
                    .unwrap()
                    .block_on(McpRegistry::connect_all(&[]))
                    .unwrap(),
            ),
        };

        // "file read" should rank fs__read_file highest (2 hits vs 1)
        let results = set.search("file read", 5);
        assert!(!results.is_empty());
        assert_eq!(results[0].prefixed_name, "fs__read_file");
    }

    #[test]
    fn get_by_name_returns_correct_stub() {
        let stubs = vec![
            make_stub("a__one", "Tool one"),
            make_stub("b__two", "Tool two"),
        ];
        let set = DeferredMcpToolSet {
            stubs,
            registry: std::sync::Arc::new(
                tokio::runtime::Runtime::new()
                    .unwrap()
                    .block_on(McpRegistry::connect_all(&[]))
                    .unwrap(),
            ),
        };
        assert!(set.get_by_name("a__one").is_some());
        assert!(set.get_by_name("nonexistent").is_none());
    }

    #[test]
    fn search_across_multiple_servers() {
        let stubs = vec![
            make_stub("server_a__read_file", "Read a file from disk"),
            make_stub("server_b__read_config", "Read configuration from database"),
        ];
        let set = DeferredMcpToolSet {
            stubs,
            registry: std::sync::Arc::new(
                tokio::runtime::Runtime::new()
                    .unwrap()
                    .block_on(McpRegistry::connect_all(&[]))
                    .unwrap(),
            ),
        };

        // "read" should match stubs from both servers
        let results = set.search("read", 10);
        assert_eq!(results.len(), 2);

        // "file" should match only server_a
        let results = set.search("file", 10);
        assert_eq!(results.len(), 1);
        assert_eq!(results[0].prefixed_name, "server_a__read_file");

        // "config database" should rank server_b highest (2 hits)
        let results = set.search("config database", 10);
        assert!(!results.is_empty());
        assert_eq!(results[0].prefixed_name, "server_b__read_config");
    }
