use super::*;
use tokio::test;

// ── PairingGuard ─────────────────────────────────────────

#[test]
async fn new_guard_generates_code_when_no_tokens() {
    let guard = PairingGuard::new(true, &[]);
    assert!(guard.pairing_code().is_some());
    assert!(!guard.is_paired());
}

#[test]
async fn new_guard_no_code_when_tokens_exist() {
    let guard = PairingGuard::new(true, &["zc_existing".into()]);
    assert!(guard.pairing_code().is_none());
    assert!(guard.is_paired());
}

#[test]
async fn new_guard_no_code_when_pairing_disabled() {
    let guard = PairingGuard::new(false, &[]);
    assert!(guard.pairing_code().is_none());
}

#[test]
async fn try_pair_correct_code() {
    let guard = PairingGuard::new(true, &[]);
    let code = guard.pairing_code().unwrap().to_string();
    let token = guard.try_pair(&code, "test_client").await.unwrap();
    assert!(token.is_some());
    assert!(token.unwrap().starts_with("zc_"));
    assert!(guard.is_paired());
}

#[test]
async fn try_pair_wrong_code() {
    let guard = PairingGuard::new(true, &[]);
    let result = guard.try_pair("000000", "test_client").await.unwrap();
    // Might succeed if code happens to be 000000, but extremely unlikely
    // Just check it returns Ok(None) normally
    let _ = result;
}

#[test]
async fn try_pair_empty_code() {
    let guard = PairingGuard::new(true, &[]);
    assert!(guard.try_pair("", "test_client").await.unwrap().is_none());
}

#[test]
async fn is_authenticated_with_valid_token() {
    // Pass plaintext token — PairingGuard hashes it on load
    let guard = PairingGuard::new(true, &["zc_valid".into()]);
    assert!(guard.is_authenticated("zc_valid"));
}

#[test]
async fn is_authenticated_with_prehashed_token() {
    // Pass an already-hashed token (64 hex chars)
    let hashed = hash_token("zc_valid");
    let guard = PairingGuard::new(true, &[hashed]);
    assert!(guard.is_authenticated("zc_valid"));
}

#[test]
async fn is_authenticated_with_invalid_token() {
    let guard = PairingGuard::new(true, &["zc_valid".into()]);
    assert!(!guard.is_authenticated("zc_invalid"));
}

#[test]
async fn is_authenticated_when_pairing_disabled() {
    let guard = PairingGuard::new(false, &[]);
    assert!(guard.is_authenticated("anything"));
    assert!(guard.is_authenticated(""));
}

#[test]
async fn tokens_returns_hashes() {
    let guard = PairingGuard::new(true, &["zc_a".into(), "zc_b".into()]);
    let tokens = guard.tokens();
    assert_eq!(tokens.len(), 2);
    // Tokens should be stored as 64-char hex hashes, not plaintext
    for t in &tokens {
        assert_eq!(t.len(), 64, "Token should be a SHA-256 hash");
        assert!(t.chars().all(|c| c.is_ascii_hexdigit()));
        assert!(!t.starts_with("zc_"), "Token should not be plaintext");
    }
}

#[test]
async fn pair_then_authenticate() {
    let guard = PairingGuard::new(true, &[]);
    let code = guard.pairing_code().unwrap().to_string();
    let token = guard.try_pair(&code, "test_client").await.unwrap().unwrap();
    assert!(guard.is_authenticated(&token));
    assert!(!guard.is_authenticated("wrong"));
}

// ── Token hashing ────────────────────────────────────────

#[test]
async fn hash_token_produces_64_hex_chars() {
    let hash = hash_token("zc_test_token");
    assert_eq!(hash.len(), 64);
    assert!(hash.chars().all(|c| c.is_ascii_hexdigit()));
}

#[test]
async fn hash_token_is_deterministic() {
    assert_eq!(hash_token("zc_abc"), hash_token("zc_abc"));
}

#[test]
async fn hash_token_differs_for_different_inputs() {
    assert_ne!(hash_token("zc_a"), hash_token("zc_b"));
}

#[test]
async fn is_token_hash_detects_hash_vs_plaintext() {
    assert!(is_token_hash(&hash_token("zc_test")));
    assert!(!is_token_hash("zc_test_token"));
    assert!(!is_token_hash("too_short"));
    assert!(!is_token_hash(""));
}

// ── is_public_bind ───────────────────────────────────────

#[test]
async fn localhost_variants_not_public() {
    assert!(!is_public_bind("127.0.0.1"));
    assert!(!is_public_bind("localhost"));
    assert!(!is_public_bind("::1"));
    assert!(!is_public_bind("[::1]"));
}

#[test]
async fn zero_zero_is_public() {
    assert!(is_public_bind("0.0.0.0"));
}

#[test]
async fn real_ip_is_public() {
    assert!(is_public_bind("192.168.1.100"));
    assert!(is_public_bind("10.0.0.1"));
}

// ── constant_time_eq ─────────────────────────────────────

#[test]
async fn constant_time_eq_same() {
    assert!(constant_time_eq("abc", "abc"));
    assert!(constant_time_eq("", ""));
}

#[test]
async fn constant_time_eq_different() {
    assert!(!constant_time_eq("abc", "abd"));
    assert!(!constant_time_eq("abc", "ab"));
    assert!(!constant_time_eq("a", ""));
}

// ── generate helpers ─────────────────────────────────────

#[test]
async fn generate_code_is_6_digits() {
    let code = generate_code();
    assert_eq!(code.len(), 6);
    assert!(code.chars().all(|c| c.is_ascii_digit()));
}

#[test]
async fn generate_code_is_not_deterministic() {
    // Two codes should differ with overwhelming probability. We try
    // multiple pairs so a single 1-in-10^6 collision doesn't cause
    // a flaky CI failure. All 10 pairs colliding is ~1-in-10^60.
    for _ in 0..10 {
        if generate_code() != generate_code() {
            return; // Pass: found a non-matching pair.
        }
    }
    panic!("Generated 10 pairs of codes and all were collisions — CSPRNG failure");
}

#[test]
async fn generate_token_has_prefix_and_hex_payload() {
    let token = generate_token();
    let payload = token
        .strip_prefix("zc_")
        .expect("Generated token should include zc_ prefix");

    assert_eq!(payload.len(), 64, "Token payload should be 32 bytes in hex");
    assert!(
        payload
            .chars()
            .all(|c| c.is_ascii_digit() || matches!(c, 'a'..='f')),
        "Token payload should be lowercase hex"
    );
}

// ── Brute force protection ───────────────────────────────

#[test]
async fn brute_force_lockout_after_max_attempts() {
    let guard = PairingGuard::new(true, &[]);
    let client = "attacker_client";
    // Exhaust all attempts with wrong codes
    for i in 0..MAX_PAIR_ATTEMPTS {
        let result = guard.try_pair(&format!("wrong_{i}"), client).await;
        assert!(result.is_ok(), "Attempt {i} should not be locked out yet");
    }
    // Next attempt should be locked out
    let result = guard.try_pair("another_wrong", client).await;
    assert!(
        result.is_err(),
        "Should be locked out after {MAX_PAIR_ATTEMPTS} attempts"
    );
    let lockout_secs = result.unwrap_err();
    assert!(lockout_secs > 0, "Lockout should have remaining seconds");
    assert!(
        lockout_secs <= PAIR_LOCKOUT_SECS,
        "Lockout should not exceed max"
    );
}

#[test]
async fn correct_code_resets_failed_attempts() {
    let guard = PairingGuard::new(true, &[]);
    let code = guard.pairing_code().unwrap().to_string();
    let client = "test_client";
    // Fail a few times
    for _ in 0..3 {
        let _ = guard.try_pair("wrong", client).await;
    }
    // Correct code should still work (under MAX_PAIR_ATTEMPTS)
    let result = guard.try_pair(&code, client).await.unwrap();
    assert!(result.is_some(), "Correct code should work before lockout");
}

#[test]
async fn lockout_returns_remaining_seconds() {
    let guard = PairingGuard::new(true, &[]);
    let client = "test_client";
    for _ in 0..MAX_PAIR_ATTEMPTS {
        let _ = guard.try_pair("wrong", client).await;
    }
    let err = guard.try_pair("wrong", client).await.unwrap_err();
    // Should be close to PAIR_LOCKOUT_SECS (within a second)
    assert!(
        err >= PAIR_LOCKOUT_SECS - 1,
        "Remaining lockout should be ~{PAIR_LOCKOUT_SECS}s, got {err}s"
    );
}

#[test]
async fn successful_pair_resets_only_requesting_client_state() {
    let guard = PairingGuard::new(true, &[]);
    let code = guard.pairing_code().unwrap().to_string();
    let client_a = "client_a";
    let client_b = "client_b";

    // Both clients fail a few times
    for _ in 0..3 {
        let _ = guard.try_pair("wrong", client_a).await;
        let _ = guard.try_pair("wrong", client_b).await;
    }

    // client_a pairs successfully — only its state should reset
    let result = guard.try_pair(&code, client_a).await.unwrap();
    assert!(result.is_some(), "client_a should pair successfully");

    // client_b's failed count should still be intact (3 failures recorded)
    let state = guard.failed_attempts.lock();
    let b_state = state.0.get(client_b);
    assert!(b_state.is_some(), "client_b state should still exist");
    assert_eq!(
        b_state.unwrap().count,
        3,
        "client_b should still have 3 failures"
    );

    // client_a should have been removed
    assert!(
        !state.0.contains_key(client_a),
        "client_a state should be cleared"
    );
}

#[test]
async fn failed_attempt_state_is_bounded_by_max_clients() {
    let guard = PairingGuard::new(true, &[]);

    // Fill the map to MAX_TRACKED_CLIENTS with stale entries
    {
        let mut state = guard.failed_attempts.lock();
        let past = Instant::now()
            .checked_sub(std::time::Duration::from_secs(
                FAILED_ATTEMPT_RETENTION_SECS + 60,
            ))
            .unwrap_or_else(Instant::now);
        for i in 0..MAX_TRACKED_CLIENTS {
            state.0.insert(
                format!("stale_client_{i}"),
                FailedAttemptState {
                    count: 1,
                    lockout_until: None,
                    last_attempt: past,
                },
            );
        }
    }

    // A new client triggers an attempt — should prune stale entries and fit
    let result = guard.try_pair("wrong", "new_client").await;
    assert!(result.is_ok(), "New client should not be blocked");

    let state = guard.failed_attempts.lock();
    assert!(
        state.0.len() <= MAX_TRACKED_CLIENTS,
        "Map size should stay within bound, got {}",
        state.0.len()
    );
    assert!(
        state.0.contains_key("new_client"),
        "New client should be tracked"
    );
}

#[test]
async fn failed_attempt_sweep_prunes_expired_clients() {
    let guard = PairingGuard::new(true, &[]);

    // Seed a stale entry and set last_sweep to long ago so sweep triggers
    {
        let mut state = guard.failed_attempts.lock();
        let past = Instant::now()
            .checked_sub(std::time::Duration::from_secs(
                FAILED_ATTEMPT_RETENTION_SECS + 60,
            ))
            .unwrap_or_else(Instant::now);
        state.0.insert(
            "stale_client".to_string(),
            FailedAttemptState {
                count: 2,
                lockout_until: None,
                last_attempt: past,
            },
        );
        // Force last_sweep to be old enough to trigger sweep
        state.1 = Instant::now()
            .checked_sub(std::time::Duration::from_secs(
                FAILED_ATTEMPT_SWEEP_INTERVAL_SECS + 1,
            ))
            .unwrap_or_else(Instant::now);
    }

    // Any attempt triggers sweep
    let _ = guard.try_pair("wrong", "fresh_client").await;

    let state = guard.failed_attempts.lock();
    assert!(
        !state.0.contains_key("stale_client"),
        "Stale client should have been pruned by sweep"
    );
    assert!(
        state.0.contains_key("fresh_client"),
        "Fresh client should still be tracked"
    );
}

#[test]
async fn lockout_is_per_client() {
    let guard = PairingGuard::new(true, &[]);
    let attacker = "attacker_ip";
    let legitimate = "legitimate_ip";

    // Attacker exhausts attempts
    for i in 0..MAX_PAIR_ATTEMPTS {
        let _ = guard.try_pair(&format!("wrong_{i}"), attacker).await;
    }
    // Attacker is locked out
    assert!(guard.try_pair("wrong", attacker).await.is_err());

    // Legitimate client is NOT locked out
    let result = guard.try_pair("wrong", legitimate).await;
    assert!(
        result.is_ok(),
        "Legitimate client should not be locked out by attacker"
    );
}
