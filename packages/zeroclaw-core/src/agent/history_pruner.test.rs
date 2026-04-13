use super::*;

fn msg(role: &str, content: &str) -> ChatMessage {
    ChatMessage {
        role: role.to_string(),
        content: content.to_string(),
    }
}

#[test]
fn prune_under_budget_no_change() {
    let mut messages = vec![
        msg("system", "You are helpful."),
        msg("user", "Hello"),
        msg("assistant", "Hi!"),
    ];
    let config = HistoryPrunerConfig {
        max_tokens: 8192,
        keep_recent: 2,
        collapse_tool_results: false,
    };
    let stats = prune_history(&mut messages, &config);
    assert_eq!(messages.len(), 3);
    assert_eq!(stats.collapsed_pairs, 0);
    assert_eq!(stats.dropped_messages, 0);
}

#[test]
fn prune_collapses_tool_pairs() {
    let tool_result = "a".repeat(160);
    let mut messages = vec![
        msg("system", "sys"),
        msg("assistant", "calling tool X"),
        msg("tool", &tool_result),
        msg("user", "thanks"),
        msg("assistant", "done"),
    ];
    let config = HistoryPrunerConfig {
        max_tokens: 100_000,
        keep_recent: 2,
        collapse_tool_results: true,
    };
    let stats = prune_history(&mut messages, &config);
    assert_eq!(stats.collapsed_pairs, 1);
    assert_eq!(messages.len(), 4);
    assert_eq!(messages[1].role, "assistant");
    assert!(messages[1].content.starts_with("[Tool result: "));
}

#[test]
fn prune_preserves_system_and_recent() {
    let big = "x".repeat(40_000);
    let mut messages = vec![
        msg("system", "system prompt"),
        msg("user", &big),
        msg("assistant", "old reply"),
        msg("user", "recent1"),
        msg("assistant", "recent2"),
    ];
    let config = HistoryPrunerConfig {
        max_tokens: 100,
        keep_recent: 2,
        collapse_tool_results: false,
    };
    let stats = prune_history(&mut messages, &config);
    assert!(messages.iter().any(|m| m.role == "system"));
    assert!(messages.iter().any(|m| m.content == "recent1"));
    assert!(messages.iter().any(|m| m.content == "recent2"));
    assert!(stats.dropped_messages > 0);
}

#[test]
fn prune_drops_oldest_when_over_budget() {
    let filler = "y".repeat(400);
    let mut messages = vec![
        msg("system", "sys"),
        msg("user", &filler),
        msg("assistant", &filler),
        msg("user", "recent-user"),
        msg("assistant", "recent-assistant"),
    ];
    let config = HistoryPrunerConfig {
        max_tokens: 150,
        keep_recent: 2,
        collapse_tool_results: false,
    };
    let stats = prune_history(&mut messages, &config);
    assert!(stats.dropped_messages >= 1);
    assert_eq!(messages[0].role, "system");
    assert!(messages.iter().any(|m| m.content == "recent-user"));
    assert!(messages.iter().any(|m| m.content == "recent-assistant"));
}

#[test]
fn prune_empty_messages() {
    let mut messages: Vec<ChatMessage> = vec![];
    let config = HistoryPrunerConfig::default();
    let stats = prune_history(&mut messages, &config);
    assert_eq!(stats.messages_before, 0);
    assert_eq!(stats.messages_after, 0);
}
