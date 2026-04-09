use super::*;

#[tokio::test]
async fn serializes_same_session() {
    let queue = SessionActorQueue::new(8, 5, 600);

    // Acquire and release, then re-acquire should work
    let guard1 = queue.acquire("s1").await.unwrap();
    drop(guard1);
    let _guard2 = queue.acquire("s1").await.unwrap();
}

#[tokio::test]
async fn parallel_different_sessions() {
    let queue = SessionActorQueue::new(8, 5, 600);
    let _guard1 = queue.acquire("s1").await.unwrap();
    let _guard2 = queue.acquire("s2").await.unwrap();
    // Both acquired simultaneously — different sessions don't block each other
}

#[tokio::test]
async fn queue_depth_limit() {
    let queue = Arc::new(SessionActorQueue::new(2, 30, 600));

    // Hold the session lock (pending=1)
    let guard = queue.acquire("s1").await.unwrap();

    // Queue one more (pending=2, will block waiting for permit)
    let queue_clone = queue.clone();
    let handle = tokio::spawn(async move { queue_clone.acquire("s1").await });

    // Give the spawned task time to register
    tokio::time::sleep(Duration::from_millis(50)).await;

    // Third request should be rejected (pending=2 >= max=2)
    let result = queue.acquire("s1").await;
    assert!(matches!(result, Err(SessionQueueError::QueueFull { .. })));

    drop(guard);
    let _ = handle.await;
}

#[tokio::test]
async fn timeout_returns_error() {
    let queue = SessionActorQueue::new(8, 1, 600);
    let _guard = queue.acquire("s1").await.unwrap();

    let start = Instant::now();
    let result = queue.acquire("s1").await;
    assert!(matches!(result, Err(SessionQueueError::Timeout { .. })));
    assert!(start.elapsed() >= Duration::from_millis(900));
}

#[tokio::test]
async fn idle_eviction() {
    let queue = SessionActorQueue::new(8, 5, 0); // 0s TTL
    {
        let _guard = queue.acquire("s1").await.unwrap();
    }
    tokio::time::sleep(Duration::from_millis(10)).await;
    let evicted = queue.evict_idle().await;
    assert_eq!(evicted, 1);
}

#[tokio::test]
async fn queue_depth_reports_correctly() {
    let queue = SessionActorQueue::new(8, 30, 600);
    assert_eq!(queue.queue_depth("s1").await, 0);

    let guard = queue.acquire("s1").await.unwrap();
    assert_eq!(queue.queue_depth("s1").await, 1);

    drop(guard);
    assert_eq!(queue.queue_depth("s1").await, 0);
}
