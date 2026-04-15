# Exa

Neural search engine for the web. Finds semantically relevant pages, similar content, and retrieves full page text.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## search

Search the web using Exa's neural or keyword search.

```
exa search --query "recent breakthroughs in battery technology" --num_results 10 --type neural
```

| Argument             | Type         | Required | Default | Description                                                      |
| -------------------- | ------------ | -------- | ------- | ---------------------------------------------------------------- |
| `query`              | string       | yes      |         | Search query                                                     |
| `num_results`        | number       | no       | 10      | Number of results to return (1-100)                              |
| `type`               | string       | no       | neural  | Search type: `neural` or `keyword`                               |
| `include_domains`    | string array | no       |         | Restrict results to these domains                                |
| `exclude_domains`    | string array | no       |         | Exclude results from these domains                               |
| `start_crawl_date`   | string       | no       |         | ISO 8601 date — only return pages crawled after this date        |
| `end_crawl_date`     | string       | no       |         | ISO 8601 date — only return pages crawled before this date       |
| `start_published_date` | string     | no       |         | ISO 8601 date — only return pages published after this date      |
| `end_published_date` | string       | no       |         | ISO 8601 date — only return pages published before this date     |
| `include_text`       | boolean      | no       | false   | Include page text/highlights in results                          |
| `highlights`         | boolean      | no       | false   | Return relevant highlights instead of full text                  |
| `category`           | string       | no       |         | Filter by category: `news`, `research paper`, `tweet`, `github repo`, `movie`, `song`, `personal site`, `pdf` |

Returns: array of `{ id, url, title, score, published_date, author, text?, highlights? }`.

## find_similar

Find pages similar to a given URL.

```
exa find_similar --url "https://openai.com/research/gpt-4" --num_results 5 --exclude_source_domain true
```

| Argument                | Type    | Required | Default | Description                                    |
| ----------------------- | ------- | -------- | ------- | ---------------------------------------------- |
| `url`                   | string  | yes      |         | URL to find similar pages for                  |
| `num_results`           | number  | no       | 10      | Number of results to return                    |
| `exclude_source_domain` | boolean | no       | true    | Exclude results from the same domain as `url`  |
| `include_domains`       | string array | no  |         | Restrict results to these domains              |
| `exclude_domains`       | string array | no  |         | Exclude results from these domains             |
| `include_text`          | boolean | no       | false   | Include page text in results                   |

Returns: array of `{ id, url, title, score, published_date, author, text? }`.

## get_contents

Retrieve the full text content of one or more pages by URL or Exa ID.

```
exa get_contents --ids '["https://example.com/article","https://example.com/other"]' --text true --highlights true
```

| Argument     | Type         | Required | Default | Description                                       |
| ------------ | ------------ | -------- | ------- | ------------------------------------------------- |
| `ids`        | string array | yes      |         | List of Exa result IDs or URLs                    |
| `text`       | boolean      | no       | true    | Return full page text                             |
| `highlights` | boolean      | no       | false   | Return relevant highlights                        |
| `max_chars`  | number       | no       |         | Maximum characters of text to return per document |

Returns: array of `{ id, url, title, text?, highlights?, author, published_date }`.

## search_and_contents

Search the web and immediately return the full text content of results. Combines search + get_contents in one call.

```
exa search_and_contents --query "machine learning papers 2025" --num_results 5 --max_chars 2000
```

| Argument             | Type    | Required | Default | Description                                                |
| -------------------- | ------- | -------- | ------- | ---------------------------------------------------------- |
| `query`              | string  | yes      |         | Search query                                               |
| `num_results`        | number  | no       | 5       | Number of results (1-20 recommended for content retrieval) |
| `type`               | string  | no       | neural  | Search type: `neural` or `keyword`                         |
| `max_chars`          | number  | no       | 3000    | Maximum characters of text per result                      |
| `include_domains`    | string array | no  |         | Restrict results to these domains                          |
| `exclude_domains`    | string array | no  |         | Exclude results from these domains                         |
| `start_published_date` | string | no      |         | ISO 8601 — only pages published after this date            |
| `end_published_date` | string  | no       |         | ISO 8601 — only pages published before this date           |
| `category`           | string  | no       |         | Category filter (see search for options)                   |

Returns: array of `{ id, url, title, score, text, published_date, author }`.
