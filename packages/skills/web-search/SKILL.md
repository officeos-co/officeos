# Web Search

Self-hosted meta-search engine powered by SearXNG. Searches across multiple engines (Google, Bing, DuckDuckGo, etc.) without tracking. Returns structured JSON results.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Search

### Search the web

```
web-search search --query "kubernetes autoscaling" --categories general --language en --page 1 --limit 10
```

| Argument     | Type   | Required | Default   | Description                                              |
|--------------|--------|----------|-----------|----------------------------------------------------------|
| `query`      | string | yes      |           | Search query                                             |
| `categories` | string | no       | `general` | Comma-separated: `general`, `images`, `news`, `videos`, `music`, `files`, `it`, `science`, `social media` |
| `engines`    | string | no       |           | Comma-separated engine names (e.g. `google,bing,duckduckgo`) |
| `language`   | string | no       | `en`      | Search language (BCP 47 code)                            |
| `page`       | int    | no       | 1         | Page number (1-based)                                    |
| `limit`      | int    | no       | 10        | Max results to return                                    |
| `time_range` | string | no       |           | Time filter: `day`, `week`, `month`, `year`              |
| `safe_search`| int    | no       | 0         | Safe search: 0 (off), 1 (moderate), 2 (strict)          |

Returns: array of `{ title, url, content, engine, score, category, publishedDate? }`.

### Search images

```
web-search search_images --query "golden retriever" --limit 5
```

| Argument | Type   | Required | Default | Description        |
|----------|--------|----------|---------|--------------------|
| `query`  | string | yes      |         | Image search query |
| `limit`  | int    | no       | 10      | Max results        |

Returns: array of `{ title, url, img_src, thumbnail_src, engine }`.

### Search news

```
web-search search_news --query "AI regulation" --time_range week --limit 10
```

| Argument     | Type   | Required | Default | Description                              |
|--------------|--------|----------|---------|------------------------------------------|
| `query`      | string | yes      |         | News search query                        |
| `time_range` | string | no       |         | Time filter: `day`, `week`, `month`, `year` |
| `limit`      | int    | no       | 10      | Max results                              |

Returns: array of `{ title, url, content, publishedDate, engine }`.

## Engine management

### Get available engines

```
web-search get_engines
```

No arguments. Returns: array of `{ name, enabled, shortcut, categories, language, paging, time_range }`.

### Get available categories

```
web-search get_categories
```

No arguments. Returns: array of category name strings.

## Autocomplete

### Get search suggestions

```
web-search autocomplete --query "kube"
```

| Argument | Type   | Required | Description              |
|----------|--------|----------|--------------------------|
| `query`  | string | yes      | Partial query for suggestions |

Returns: array of suggestion strings.
