use super::*;

#[test]
fn resolve_aliases_to_duckduckgo() {
    let ddg_aliases = ["duckduckgo", "ddg", "duck-duck-go", "duck_duck_go"];
    for alias in ddg_aliases {
        let resolved = resolve_web_search_provider(alias);
        assert_eq!(resolved.route, WebSearchProviderRoute::DuckDuckGo);
        assert_eq!(resolved.canonical_provider, DEFAULT_WEB_SEARCH_PROVIDER);
        assert!(!resolved.used_fallback);
    }
}

#[test]
fn resolve_aliases_to_brave() {
    let brave_aliases = ["brave", "brave-search", "brave_search"];
    for alias in brave_aliases {
        let resolved = resolve_web_search_provider(alias);
        assert_eq!(resolved.route, WebSearchProviderRoute::Brave);
        assert_eq!(resolved.canonical_provider, BRAVE_PROVIDER);
        assert!(!resolved.used_fallback);
    }
}

#[test]
fn resolve_aliases_to_searxng() {
    let searxng_aliases = ["searxng", "searx", "searx-ng", "searx_ng"];
    for alias in searxng_aliases {
        let resolved = resolve_web_search_provider(alias);
        assert_eq!(resolved.route, WebSearchProviderRoute::SearXNG);
        assert_eq!(resolved.canonical_provider, SEARXNG_PROVIDER);
        assert!(!resolved.used_fallback);
    }
}

#[test]
fn resolve_unknown_provider_falls_back_to_default() {
    let resolved = resolve_web_search_provider("bing");
    assert_eq!(resolved.route, WebSearchProviderRoute::DuckDuckGo);
    assert_eq!(resolved.canonical_provider, DEFAULT_WEB_SEARCH_PROVIDER);
    assert!(resolved.used_fallback);

    let resolved2 = resolve_web_search_provider("searxng-plus");
    assert_eq!(resolved2.route, WebSearchProviderRoute::DuckDuckGo);
    assert_eq!(resolved2.canonical_provider, DEFAULT_WEB_SEARCH_PROVIDER);
    assert!(resolved2.used_fallback);
}
