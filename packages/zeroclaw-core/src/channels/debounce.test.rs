use super::*;

#[tokio::test]
async fn passthrough_when_disabled() {
    let debouncer = MessageDebouncer::new(Duration::ZERO);
    assert!(!debouncer.enabled());
    match debouncer.debounce("user1", "hello").await {
        DebounceResult::Passthrough(msg) => assert_eq!(msg, "hello"),
        DebounceResult::Pending(_) => panic!("expected Passthrough"),
    }
}

#[tokio::test]
async fn single_message_fires_after_window() {
    let debouncer = MessageDebouncer::new(Duration::from_millis(50));
    let rx = match debouncer.debounce("user1", "hello").await {
        DebounceResult::Pending(rx) => rx,
        DebounceResult::Passthrough(_) => panic!("expected Pending"),
    };
    let combined = rx.await.unwrap();
    assert_eq!(combined, "hello");
}

#[tokio::test]
async fn multiple_messages_concatenated() {
    let debouncer = MessageDebouncer::new(Duration::from_millis(100));

    // First message
    let _rx1 = match debouncer.debounce("user1", "hello").await {
        DebounceResult::Pending(rx) => rx,
        DebounceResult::Passthrough(_) => panic!("expected Pending"),
    };

    // Second message within window (resets timer)
    tokio::time::sleep(Duration::from_millis(30)).await;
    let rx2 = match debouncer.debounce("user1", "world").await {
        DebounceResult::Pending(rx) => rx,
        DebounceResult::Passthrough(_) => panic!("expected Pending"),
    };

    // The first receiver is dropped (superseded), second gets the combined result
    let combined = rx2.await.unwrap();
    assert_eq!(combined, "hello\nworld");
}

#[tokio::test]
async fn different_senders_independent() {
    let debouncer = MessageDebouncer::new(Duration::from_millis(50));

    let rx_a = match debouncer.debounce("alice", "hi alice").await {
        DebounceResult::Pending(rx) => rx,
        DebounceResult::Passthrough(_) => panic!("expected Pending"),
    };
    let rx_b = match debouncer.debounce("bob", "hi bob").await {
        DebounceResult::Pending(rx) => rx,
        DebounceResult::Passthrough(_) => panic!("expected Pending"),
    };

    assert_eq!(rx_a.await.unwrap(), "hi alice");
    assert_eq!(rx_b.await.unwrap(), "hi bob");
}
