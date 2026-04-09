use super::*;
use tempfile::TempDir;

fn temp_cache(ttl_minutes: u32) -> (TempDir, ResponseCache) {
    let tmp = TempDir::new().unwrap();
    let cache = ResponseCache::new(tmp.path(), ttl_minutes, 1000).unwrap();
    (tmp, cache)
}

#[test]
fn cache_key_deterministic() {
    let k1 = ResponseCache::cache_key("gpt-4", Some("sys"), "hello");
    let k2 = ResponseCache::cache_key("gpt-4", Some("sys"), "hello");
    assert_eq!(k1, k2);
    assert_eq!(k1.len(), 64); // SHA-256 hex
}

#[test]
fn cache_key_varies_by_model() {
    let k1 = ResponseCache::cache_key("gpt-4", None, "hello");
    let k2 = ResponseCache::cache_key("claude-3", None, "hello");
    assert_ne!(k1, k2);
}

#[test]
fn cache_key_varies_by_system_prompt() {
    let k1 = ResponseCache::cache_key("gpt-4", Some("You are helpful"), "hello");
    let k2 = ResponseCache::cache_key("gpt-4", Some("You are rude"), "hello");
    assert_ne!(k1, k2);
}

#[test]
fn cache_key_varies_by_prompt() {
    let k1 = ResponseCache::cache_key("gpt-4", None, "hello");
    let k2 = ResponseCache::cache_key("gpt-4", None, "goodbye");
    assert_ne!(k1, k2);
}

#[test]
fn put_and_get() {
    let (_tmp, cache) = temp_cache(60);
    let key = ResponseCache::cache_key("gpt-4", None, "What is Rust?");

    cache
        .put(&key, "gpt-4", "Rust is a systems programming language.", 25)
        .unwrap();

    let result = cache.get(&key).unwrap();
    assert_eq!(
        result.as_deref(),
        Some("Rust is a systems programming language.")
    );
}

#[test]
fn miss_returns_none() {
    let (_tmp, cache) = temp_cache(60);
    let result = cache.get("nonexistent_key").unwrap();
    assert!(result.is_none());
}

#[test]
fn expired_entry_returns_none() {
    let (_tmp, cache) = temp_cache(0); // 0-minute TTL → everything is instantly expired
    let key = ResponseCache::cache_key("gpt-4", None, "test");

    cache.put(&key, "gpt-4", "response", 10).unwrap();

    // The entry was created with created_at = now(), but TTL is 0 minutes,
    // so cutoff = now() - 0 = now(). The entry's created_at is NOT > cutoff.
    let result = cache.get(&key).unwrap();
    assert!(result.is_none());
}

#[test]
fn hit_count_incremented() {
    let (_tmp, cache) = temp_cache(60);
    let key = ResponseCache::cache_key("gpt-4", None, "hello");

    cache.put(&key, "gpt-4", "Hi!", 5).unwrap();

    // 3 hits
    for _ in 0..3 {
        let _ = cache.get(&key).unwrap();
    }

    let (_, total_hits, _) = cache.stats().unwrap();
    assert_eq!(total_hits, 3);
}

#[test]
fn tokens_saved_calculated() {
    let (_tmp, cache) = temp_cache(60);
    let key = ResponseCache::cache_key("gpt-4", None, "explain rust");

    cache.put(&key, "gpt-4", "Rust is...", 100).unwrap();

    // 5 cache hits × 100 tokens = 500 tokens saved
    for _ in 0..5 {
        let _ = cache.get(&key).unwrap();
    }

    let (_, _, tokens_saved) = cache.stats().unwrap();
    assert_eq!(tokens_saved, 500);
}

#[test]
fn lru_eviction() {
    let tmp = TempDir::new().unwrap();
    let cache = ResponseCache::new(tmp.path(), 60, 3).unwrap(); // max 3 entries

    for i in 0..5 {
        let key = ResponseCache::cache_key("gpt-4", None, &format!("prompt {i}"));
        cache
            .put(&key, "gpt-4", &format!("response {i}"), 10)
            .unwrap();
    }

    let (count, _, _) = cache.stats().unwrap();
    assert!(count <= 3, "Should have at most 3 entries after eviction");
}

#[test]
fn clear_wipes_all() {
    let (_tmp, cache) = temp_cache(60);

    for i in 0..10 {
        let key = ResponseCache::cache_key("gpt-4", None, &format!("prompt {i}"));
        cache
            .put(&key, "gpt-4", &format!("response {i}"), 10)
            .unwrap();
    }

    let cleared = cache.clear().unwrap();
    assert_eq!(cleared, 10);

    let (count, _, _) = cache.stats().unwrap();
    assert_eq!(count, 0);
}

#[test]
fn stats_empty_cache() {
    let (_tmp, cache) = temp_cache(60);
    let (count, hits, tokens) = cache.stats().unwrap();
    assert_eq!(count, 0);
    assert_eq!(hits, 0);
    assert_eq!(tokens, 0);
}

#[test]
fn overwrite_same_key() {
    let (_tmp, cache) = temp_cache(60);
    let key = ResponseCache::cache_key("gpt-4", None, "question");

    cache.put(&key, "gpt-4", "answer v1", 20).unwrap();
    cache.put(&key, "gpt-4", "answer v2", 25).unwrap();

    let result = cache.get(&key).unwrap();
    assert_eq!(result.as_deref(), Some("answer v2"));

    let (count, _, _) = cache.stats().unwrap();
    assert_eq!(count, 1);
}

#[test]
fn unicode_prompt_handling() {
    let (_tmp, cache) = temp_cache(60);
    let key = ResponseCache::cache_key("gpt-4", None, "日本語のテスト 🦀");

    cache
        .put(&key, "gpt-4", "はい、Rustは素晴らしい", 30)
        .unwrap();

    let result = cache.get(&key).unwrap();
    assert_eq!(result.as_deref(), Some("はい、Rustは素晴らしい"));
}

// ── §4.4 Cache eviction under pressure tests ─────────────

#[test]
fn lru_eviction_keeps_most_recent() {
    let tmp = TempDir::new().unwrap();
    let cache = ResponseCache::new(tmp.path(), 60, 3).unwrap();

    // Insert 3 entries
    for i in 0..3 {
        let key = ResponseCache::cache_key("gpt-4", None, &format!("prompt {i}"));
        cache
            .put(&key, "gpt-4", &format!("response {i}"), 10)
            .unwrap();
    }

    // Access entry 0 to make it recently used
    let key0 = ResponseCache::cache_key("gpt-4", None, "prompt 0");
    let _ = cache.get(&key0).unwrap();

    // Insert entry 3 (triggers eviction)
    let key3 = ResponseCache::cache_key("gpt-4", None, "prompt 3");
    cache.put(&key3, "gpt-4", "response 3", 10).unwrap();

    let (count, _, _) = cache.stats().unwrap();
    assert!(count <= 3, "cache must not exceed max_entries");

    // Entry 0 was recently accessed and should survive
    let entry0 = cache.get(&key0).unwrap();
    assert!(
        entry0.is_some(),
        "recently accessed entry should survive LRU eviction"
    );
}

#[test]
fn cache_handles_zero_max_entries() {
    let tmp = TempDir::new().unwrap();
    let cache = ResponseCache::new(tmp.path(), 60, 0).unwrap();

    let key = ResponseCache::cache_key("gpt-4", None, "test");
    // Should not panic even with max_entries=0
    cache.put(&key, "gpt-4", "response", 10).unwrap();

    let (count, _, _) = cache.stats().unwrap();
    assert_eq!(count, 0, "cache with max_entries=0 should evict everything");
}

#[test]
fn cache_concurrent_reads_no_panic() {
    let tmp = TempDir::new().unwrap();
    let cache = std::sync::Arc::new(ResponseCache::new(tmp.path(), 60, 100).unwrap());

    let key = ResponseCache::cache_key("gpt-4", None, "concurrent");
    cache.put(&key, "gpt-4", "response", 10).unwrap();

    let mut handles = Vec::new();
    for _ in 0..10 {
        let cache = std::sync::Arc::clone(&cache);
        let key = key.clone();
        handles.push(std::thread::spawn(move || {
            let _ = cache.get(&key).unwrap();
        }));
    }

    for handle in handles {
        handle.join().unwrap();
    }

    let (_, hits, _) = cache.stats().unwrap();
    assert_eq!(hits, 10, "all concurrent reads should register as hits");
}
