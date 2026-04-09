use super::*;

#[test]
fn empty_collector_returns_none_rates() {
    let c = DoraCollector::new();
    let snap = c.snapshot_30d();
    assert_eq!(snap.total_deployments, 0);
    assert_eq!(snap.failed_deployments, 0);
    assert!(snap.change_failure_rate.is_none());
    assert!(snap.mean_lead_time.is_none());
    assert!(snap.mttr.is_none());
}

#[test]
fn deployment_frequency_counts() {
    let c = DoraCollector::new();
    c.record_deployment(true, None);
    c.record_deployment(true, None);
    c.record_deployment(false, None);

    let snap = c.snapshot_30d();
    assert_eq!(snap.total_deployments, 3);
    assert_eq!(snap.failed_deployments, 1);
}

#[test]
fn change_failure_rate_calculation() {
    let c = DoraCollector::new();
    c.record_deployment(true, None);
    c.record_deployment(false, None);
    c.record_deployment(true, None);
    c.record_deployment(false, None);

    let snap = c.snapshot_30d();
    let rate = snap.change_failure_rate.unwrap();
    assert!((rate - 0.5).abs() < f64::EPSILON);
}

#[test]
fn change_failure_rate_zero_failures() {
    let c = DoraCollector::new();
    c.record_deployment(true, None);
    c.record_deployment(true, None);

    let snap = c.snapshot_30d();
    let rate = snap.change_failure_rate.unwrap();
    assert!((rate - 0.0).abs() < f64::EPSILON);
}

#[test]
fn change_failure_rate_all_failures() {
    let c = DoraCollector::new();
    c.record_deployment(false, None);
    c.record_deployment(false, None);

    let snap = c.snapshot_30d();
    let rate = snap.change_failure_rate.unwrap();
    assert!((rate - 1.0).abs() < f64::EPSILON);
}

#[test]
fn lead_time_calculation() {
    let c = DoraCollector::new();
    c.record_deployment(true, Some(Duration::from_secs(100)));
    c.record_deployment(true, Some(Duration::from_secs(200)));
    c.record_deployment(true, Some(Duration::from_secs(300)));

    let snap = c.snapshot_30d();
    let mean = snap.mean_lead_time.unwrap();
    assert_eq!(mean, Duration::from_secs(200));
}

#[test]
fn lead_time_ignores_none_entries() {
    let c = DoraCollector::new();
    c.record_deployment(true, Some(Duration::from_secs(100)));
    c.record_deployment(true, None); // no lead time
    c.record_deployment(true, Some(Duration::from_secs(300)));

    let snap = c.snapshot_30d();
    let mean = snap.mean_lead_time.unwrap();
    assert_eq!(mean, Duration::from_secs(200));
}

#[test]
fn mttr_calculation() {
    let c = DoraCollector::new();
    c.record_recovery(Duration::from_secs(60));
    c.record_recovery(Duration::from_secs(120));
    c.record_recovery(Duration::from_secs(180));

    let snap = c.snapshot_30d();
    let mttr = snap.mttr.unwrap();
    assert_eq!(mttr, Duration::from_secs(120));
}

#[test]
fn mttr_none_when_no_recoveries() {
    let c = DoraCollector::new();
    c.record_deployment(false, None);

    let snap = c.snapshot_30d();
    assert!(snap.mttr.is_none());
}

#[test]
fn record_failure_convenience() {
    let c = DoraCollector::new();
    c.record_failure();

    let snap = c.snapshot_30d();
    assert_eq!(snap.total_deployments, 1);
    assert_eq!(snap.failed_deployments, 1);
    assert!((snap.change_failure_rate.unwrap() - 1.0).abs() < f64::EPSILON);
}

#[test]
fn rapid_deployments() {
    let c = DoraCollector::new();
    for _ in 0..100 {
        c.record_deployment(true, Some(Duration::from_millis(50)));
    }

    let snap = c.snapshot_30d();
    assert_eq!(snap.total_deployments, 100);
    assert_eq!(snap.mean_lead_time.unwrap(), Duration::from_millis(50));
}

#[test]
fn ring_buffer_eviction() {
    let c = DoraCollector::new();
    // Fill beyond MAX_RECORDS
    for i in 0..(MAX_RECORDS + 50) {
        c.record_deployment(i % 2 == 0, Some(Duration::from_secs(i as u64)));
    }

    let state = c.inner.read().unwrap();
    assert_eq!(state.deployments.len(), MAX_RECORDS);
}

#[test]
fn recovery_ring_buffer_eviction() {
    let c = DoraCollector::new();
    for i in 0..(MAX_RECORDS + 50) {
        c.record_recovery(Duration::from_secs(i as u64));
    }

    let state = c.inner.read().unwrap();
    assert_eq!(state.recoveries.len(), MAX_RECORDS);
}

#[test]
fn different_windows_return_correct_window_duration() {
    let c = DoraCollector::new();
    c.record_deployment(true, None);

    assert_eq!(c.snapshot_7d().window, WINDOW_7D);
    assert_eq!(c.snapshot_30d().window, WINDOW_30D);
    assert_eq!(c.snapshot_90d().window, WINDOW_90D);
}

#[test]
fn default_impl_works() {
    let c = DoraCollector::default();
    let snap = c.snapshot();
    assert_eq!(snap.total_deployments, 0);
}

#[test]
fn thread_safety_basic() {
    use std::sync::Arc;
    use std::thread;

    let c = Arc::new(DoraCollector::new());
    let mut handles = vec![];

    for _ in 0..4 {
        let c = Arc::clone(&c);
        handles.push(thread::spawn(move || {
            for _ in 0..25 {
                c.record_deployment(true, Some(Duration::from_secs(10)));
            }
        }));
    }

    for h in handles {
        h.join().unwrap();
    }

    let snap = c.snapshot_30d();
    assert_eq!(snap.total_deployments, 100);
}
