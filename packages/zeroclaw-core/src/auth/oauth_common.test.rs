use super::*;

#[test]
fn pkce_generation_is_valid() {
    let pkce = generate_pkce_state();
    // Code verifier should be at least 43 chars (base64url of 32 bytes)
    assert!(pkce.code_verifier.len() >= 43);
    assert!(!pkce.code_challenge.is_empty());
    assert!(!pkce.state.is_empty());
}

#[test]
fn pkce_challenge_is_sha256_of_verifier() {
    let pkce = generate_pkce_state();
    let expected = {
        let digest = Sha256::digest(pkce.code_verifier.as_bytes());
        base64::engine::general_purpose::URL_SAFE_NO_PAD.encode(digest)
    };
    assert_eq!(pkce.code_challenge, expected);
}

#[test]
fn url_encode_basic() {
    assert_eq!(url_encode("hello"), "hello");
    assert_eq!(url_encode("hello world"), "hello%20world");
    assert_eq!(url_encode("a=b&c=d"), "a%3Db%26c%3Dd");
}

#[test]
fn url_decode_basic() {
    assert_eq!(url_decode("hello"), "hello");
    assert_eq!(url_decode("hello%20world"), "hello world");
    assert_eq!(url_decode("hello+world"), "hello world");
    assert_eq!(url_decode("a%3Db%26c%3Dd"), "a=b&c=d");
}

#[test]
fn url_encode_decode_roundtrip() {
    let original = "hello world! @#$%^&*()";
    let encoded = url_encode(original);
    let decoded = url_decode(&encoded);
    assert_eq!(decoded, original);
}

#[test]
fn parse_query_params_basic() {
    let params = parse_query_params("code=abc123&state=xyz");
    assert_eq!(params.get("code"), Some(&"abc123".to_string()));
    assert_eq!(params.get("state"), Some(&"xyz".to_string()));
}

#[test]
fn parse_query_params_encoded() {
    let params = parse_query_params("name=hello%20world&value=a%3Db");
    assert_eq!(params.get("name"), Some(&"hello world".to_string()));
    assert_eq!(params.get("value"), Some(&"a=b".to_string()));
}

#[test]
fn parse_query_params_empty() {
    let params = parse_query_params("");
    assert!(params.is_empty());
}

#[test]
fn random_base64url_length() {
    let s = random_base64url(32);
    // base64url encodes 3 bytes to 4 chars, so 32 bytes = ~43 chars
    assert!(s.len() >= 42);
}
