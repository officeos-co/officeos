    use super::*;

    #[tokio::test]
    async fn logs_tool_calls() {
        let hook = CommandLoggerHook::new();
        let result = ToolResult {
            success: true,
            output: "ok".into(),
            error: None,
        };
        hook.on_after_tool_call("shell", &result, Duration::from_millis(42))
            .await;
        let entries = hook.entries();
        assert_eq!(entries.len(), 1);
        assert!(entries[0].contains("shell"));
        assert!(entries[0].contains("42ms"));
        assert!(entries[0].contains("success=true"));
    }
