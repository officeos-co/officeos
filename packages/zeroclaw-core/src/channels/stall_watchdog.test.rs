    use super::*;
    use std::sync::atomic::AtomicBool;

    #[tokio::test]
    async fn touch_prevents_stall() {
        let wd = StallWatchdog::new(2);
        wd.touch();
        assert!(!wd.is_stalled());
    }

    #[tokio::test]
    async fn is_stalled_after_timeout() {
        let wd = StallWatchdog::new(0); // 0-second timeout → always stalled
        // Force last_event into the past
        wd.last_event.store(0, Ordering::Relaxed);
        assert!(wd.is_stalled());
    }

    #[tokio::test]
    async fn callback_fires_on_stall() {
        let fired = Arc::new(AtomicBool::new(false));
        let fired_clone = Arc::clone(&fired);

        let wd = StallWatchdog::new(1);

        wd.start(move || {
            fired_clone.store(true, Ordering::Relaxed);
        })
        .await;

        // Force last_event far into the past *after* start() so the next poll
        // detects a stall (start() calls touch() which would overwrite an
        // earlier store).
        wd.last_event.store(0, Ordering::Relaxed);

        // Wait long enough for the poll interval (1 / 2 = clamped to 1s) + margin.
        tokio::time::sleep(std::time::Duration::from_secs(2)).await;
        assert!(fired.load(Ordering::Relaxed));
    }

    #[tokio::test]
    async fn stop_prevents_callback() {
        let fired = Arc::new(AtomicBool::new(false));
        let fired_clone = Arc::clone(&fired);

        let wd = StallWatchdog::new(1);

        wd.start(move || {
            fired_clone.store(true, Ordering::Relaxed);
        })
        .await;

        wd.last_event.store(0, Ordering::Relaxed);

        // Stop immediately before the poll task can fire.
        wd.stop().await;

        tokio::time::sleep(std::time::Duration::from_secs(2)).await;
        assert!(!fired.load(Ordering::Relaxed));
    }
