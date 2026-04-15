# Perplexity

AI-powered search and chat using Perplexity's online LLMs with real-time web access.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## search

Perform a web search using a Perplexity online model. Returns a grounded answer with citations.

```
perplexity search --query "latest developments in fusion energy" --model "sonar" --search_domain_filter "nature.com,science.org"
```

| Argument               | Type    | Required | Default  | Description                                            |
| ---------------------- | ------- | -------- | -------- | ------------------------------------------------------ |
| `query`                | string  | yes      |          | Search query                                           |
| `model`                | string  | no       | sonar    | Perplexity model: `sonar`, `sonar-pro`, `sonar-reasoning`, `sonar-reasoning-pro` |
| `search_domain_filter` | string  | no       |          | Comma-separated list of domains to restrict search to  |
| `return_images`        | boolean | no       | false    | Include image results                                  |
| `return_related`       | boolean | no       | false    | Include related questions                              |
| `recency`              | string  | no       |          | Filter by recency: `month`, `week`, `day`, `hour`      |

Returns: `answer`, `citations` (array of `{ url, title }`), `model`, `usage` (`prompt_tokens`, `completion_tokens`).

## chat

Multi-turn conversation with a Perplexity model. Supports system prompts and conversation history.

```
perplexity chat --messages '[{"role":"user","content":"Explain quantum entanglement"}]' --model "sonar-pro"
```

| Argument      | Type   | Required | Default     | Description                                                |
| ------------- | ------ | -------- | ----------- | ---------------------------------------------------------- |
| `messages`    | array  | yes      |             | Array of `{ role: "system"\|"user"\|"assistant", content: string }` |
| `model`       | string | no       | sonar       | Perplexity model to use                                    |
| `temperature` | number | no       | 0.2         | Sampling temperature (0–2)                                 |
| `max_tokens`  | number | no       |             | Maximum tokens in response                                 |

Returns: `content`, `role`, `model`, `citations` (array of URLs), `usage` (`prompt_tokens`, `completion_tokens`).

## search_news

Search recent news articles on a topic.

```
perplexity search_news --query "AI regulation Europe 2025" --recency "week"
```

| Argument   | Type   | Required | Default | Description                                         |
| ---------- | ------ | -------- | ------- | --------------------------------------------------- |
| `query`    | string | yes      |         | News search query                                   |
| `recency`  | string | no       | week    | Filter by recency: `month`, `week`, `day`, `hour`   |
| `model`    | string | no       | sonar   | Perplexity model to use                             |

Returns: `answer`, `citations` (array of `{ url, title }`), `model`.

## search_academic

Search academic papers and research on a topic.

```
perplexity search_academic --query "CRISPR cancer treatment meta-analysis" --search_domain_filter "pubmed.ncbi.nlm.nih.gov,scholar.google.com"
```

| Argument               | Type   | Required | Default  | Description                               |
| ---------------------- | ------ | -------- | -------- | ----------------------------------------- |
| `query`                | string | yes      |          | Academic search query                     |
| `search_domain_filter` | string | no       |          | Comma-separated domains to restrict to    |
| `model`                | string | no       | sonar    | Perplexity model to use                   |

Returns: `answer`, `citations` (array of `{ url, title }`), `model`.
