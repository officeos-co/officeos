use super::*;

#[test]
fn loopback_is_exempt() {
    let limiter = AuthRateLimiter::new();
    for _ in 0..20 {
        assert!(limiter.check_rate_limit("127.0.0.1").is_ok());
        limiter.record_attempt("127.0.0.1");
    }
    assert!(!limiter.is_locked_out("127.0.0.1"));

    for _ in 0..20 {
        assert!(limiter.check_rate_limit("::1").is_ok());
        limiter.record_attempt("::1");
    }
}

#[test]
fn lockout_after_max_attempts() {
    let limiter = AuthRateLimiter::new();
    let key = "192.168.1.100";

    for _ in 0..MAX_ATTEMPTS {
        assert!(limiter.check_rate_limit(key).is_ok());
        limiter.record_attempt(key);
    }

    // Next check should fail — lockout triggered.
    let err = limiter.check_rate_limit(key).unwrap_err();
    assert!(err.retry_after_secs > 0);
    assert!(limiter.is_locked_out(key));
}

#[test]
fn under_limit_is_ok() {
    let limiter = AuthRateLimiter::new();
    let key = "10.0.0.1";

    for _ in 0..(MAX_ATTEMPTS - 1) {
        assert!(limiter.check_rate_limit(key).is_ok());
        limiter.record_attempt(key);
    }
    // Still under the limit.
    assert!(limiter.check_rate_limit(key).is_ok());
}
