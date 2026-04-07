    use super::*;
    use std::time::Duration;

    #[test]
    fn verbose_name() {
        assert_eq!(VerboseObserver::new().name(), "verbose");
    }

    #[test]
    fn verbose_events_do_not_panic() {
        let obs = VerboseObserver::new();
        obs.record_event(&ObserverEvent::LlmRequest {
            provider: "openrouter".into(),
            model: "claude".into(),
            messages_count: 3,
        });
        obs.record_event(&ObserverEvent::LlmResponse {
            provider: "openrouter".into(),
            model: "claude".into(),
            duration: Duration::from_millis(12),
            success: true,
            error_message: None,
            input_tokens: Some(50),
            output_tokens: Some(25),
        });
        obs.record_event(&ObserverEvent::ToolCallStart {
            tool: "shell".into(),
            arguments: None,
        });
        obs.record_event(&ObserverEvent::ToolCall {
            tool: "shell".into(),
            duration: Duration::from_millis(2),
            success: true,
        });
        obs.record_event(&ObserverEvent::TurnComplete);
    }

    #[test]
    fn verbose_hand_events_do_not_panic() {
        let obs = VerboseObserver::new();
        obs.record_event(&ObserverEvent::HandStarted {
            hand_name: "review".into(),
        });
        obs.record_event(&ObserverEvent::HandCompleted {
            hand_name: "review".into(),
            duration_ms: 1500,
            findings_count: 3,
        });
        obs.record_event(&ObserverEvent::HandFailed {
            hand_name: "review".into(),
            error: "timeout".into(),
            duration_ms: 5000,
        });
    }
