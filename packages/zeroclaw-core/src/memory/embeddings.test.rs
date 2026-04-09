use super::*;

#[test]
fn noop_name() {
    let p = NoopEmbedding;
    assert_eq!(p.name(), "none");
    assert_eq!(p.dimensions(), 0);
}

#[tokio::test]
async fn noop_embed_returns_empty() {
    let p = NoopEmbedding;
    let result = p.embed(&["hello"]).await.unwrap();
    assert!(result.is_empty());
}

#[test]
fn factory_none() {
    let p = create_embedding_provider("none", None, "model", 1536);
    assert_eq!(p.name(), "none");
}

#[test]
fn factory_openai() {
    let p = create_embedding_provider("openai", Some("key"), "text-embedding-3-small", 1536);
    assert_eq!(p.name(), "openai");
    assert_eq!(p.dimensions(), 1536);
}

#[test]
fn factory_openrouter() {
    let p = create_embedding_provider(
        "openrouter",
        Some("sk-or-test"),
        "openai/text-embedding-3-small",
        1536,
    );
    assert_eq!(p.name(), "openai"); // uses OpenAiEmbedding internally
    assert_eq!(p.dimensions(), 1536);
}

#[test]
fn factory_custom_url() {
    let p = create_embedding_provider("custom:http://localhost:1234", None, "model", 768);
    assert_eq!(p.name(), "openai"); // uses OpenAiEmbedding internally
    assert_eq!(p.dimensions(), 768);
}

// ── Edge cases ───────────────────────────────────────────────

#[tokio::test]
async fn noop_embed_one_returns_error() {
    let p = NoopEmbedding;
    // embed returns empty vec → pop() returns None → error
    let result = p.embed_one("hello").await;
    assert!(result.is_err());
}

#[tokio::test]
async fn noop_embed_empty_batch() {
    let p = NoopEmbedding;
    let result = p.embed(&[]).await.unwrap();
    assert!(result.is_empty());
}

#[tokio::test]
async fn noop_embed_multiple_texts() {
    let p = NoopEmbedding;
    let result = p.embed(&["a", "b", "c"]).await.unwrap();
    assert!(result.is_empty());
}

#[test]
fn factory_empty_string_returns_noop() {
    let p = create_embedding_provider("", None, "model", 1536);
    assert_eq!(p.name(), "none");
}

#[test]
fn factory_unknown_provider_returns_noop() {
    let p = create_embedding_provider("cohere", None, "model", 1536);
    assert_eq!(p.name(), "none");
}

#[test]
fn factory_custom_empty_url() {
    // "custom:" with no URL — should still construct without panic
    let p = create_embedding_provider("custom:", None, "model", 768);
    assert_eq!(p.name(), "openai");
}

#[test]
fn factory_openai_no_api_key() {
    let p = create_embedding_provider("openai", None, "text-embedding-3-small", 1536);
    assert_eq!(p.name(), "openai");
    assert_eq!(p.dimensions(), 1536);
}

#[test]
fn openai_trailing_slash_stripped() {
    let p = OpenAiEmbedding::new("https://api.openai.com/", "key", "model", 1536);
    assert_eq!(p.base_url, "https://api.openai.com");
}

#[test]
fn openai_dimensions_custom() {
    let p = OpenAiEmbedding::new("http://localhost", "k", "m", 384);
    assert_eq!(p.dimensions(), 384);
}

#[test]
fn embeddings_url_openrouter() {
    let p = OpenAiEmbedding::new(
        "https://openrouter.ai/api",
        "key",
        "openai/text-embedding-3-small",
        1536,
    );
    assert_eq!(p.embeddings_url(), "https://openrouter.ai/api/embeddings");
}

#[test]
fn embeddings_url_standard_openai() {
    let p = OpenAiEmbedding::new("https://api.openai.com", "key", "model", 1536);
    assert_eq!(p.embeddings_url(), "https://api.openai.com/v1/embeddings");
}

#[test]
fn embeddings_url_base_with_v1_no_duplicate() {
    let p = OpenAiEmbedding::new("https://api.example.com/v1", "key", "model", 1536);
    assert_eq!(p.embeddings_url(), "https://api.example.com/v1/embeddings");
}

#[test]
fn embeddings_url_non_v1_api_path_uses_raw_suffix() {
    let p = OpenAiEmbedding::new(
        "https://api.example.com/api/coding/v3",
        "key",
        "model",
        1536,
    );
    assert_eq!(
        p.embeddings_url(),
        "https://api.example.com/api/coding/v3/embeddings"
    );
}

#[test]
fn embeddings_url_custom_full_endpoint() {
    let p = OpenAiEmbedding::new(
        "https://my-api.example.com/api/v2/embeddings",
        "key",
        "model",
        1536,
    );
    assert_eq!(
        p.embeddings_url(),
        "https://my-api.example.com/api/v2/embeddings"
    );
}
