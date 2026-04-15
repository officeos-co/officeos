# Web Scraper

Scrape web pages, extract links, text, structured data, and take screenshots via a configurable HTTP proxy or scraping service.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## scrape_url

Fetch the full HTML content of a URL, optionally rendered with JavaScript.

```
web-scraper scrape_url --url "https://example.com" --render_js true --wait_ms 1000
```

| Argument    | Type    | Required | Default | Description                              |
| ----------- | ------- | -------- | ------- | ---------------------------------------- |
| `url`       | string  | yes      |         | URL to scrape                            |
| `render_js` | boolean | no       | false   | Render JavaScript before extracting HTML |
| `wait_ms`   | number  | no       | 0       | Milliseconds to wait after page load     |
| `headers`   | object  | no       |         | Extra HTTP headers to send               |

Returns: `url`, `status_code`, `html`, `title`, `content_type`.

## extract_links

Extract all hyperlinks from a URL.

```
web-scraper extract_links --url "https://example.com" --selector "nav a" --include_external false
```

| Argument           | Type    | Required | Default | Description                             |
| ------------------ | ------- | -------- | ------- | --------------------------------------- |
| `url`              | string  | yes      |         | URL to scrape                           |
| `selector`         | string  | no       |         | CSS selector to scope link extraction   |
| `include_external` | boolean | no       | true    | Include links to external domains       |
| `render_js`        | boolean | no       | false   | Render JavaScript before extracting     |

Returns: array of `{ href, text, title, is_external }`.

## extract_text

Extract clean readable text from a URL, stripping HTML tags and boilerplate.

```
web-scraper extract_text --url "https://blog.example.com/post" --selector "article"
```

| Argument    | Type    | Required | Default | Description                               |
| ----------- | ------- | -------- | ------- | ----------------------------------------- |
| `url`       | string  | yes      |         | URL to scrape                             |
| `selector`  | string  | no       |         | CSS selector to limit extraction scope    |
| `render_js` | boolean | no       | false   | Render JavaScript before extracting       |
| `wait_ms`   | number  | no       | 0       | Milliseconds to wait after page load      |

Returns: `url`, `title`, `text`, `word_count`.

## extract_structured

Extract structured data from a page using CSS selectors to map fields.

```
web-scraper extract_structured --url "https://shop.example.com/product/123" --fields '{"name":".product-title","price":".price","description":".product-desc"}'
```

| Argument    | Type    | Required | Default | Description                                         |
| ----------- | ------- | -------- | ------- | --------------------------------------------------- |
| `url`       | string  | yes      |         | URL to scrape                                       |
| `fields`    | object  | yes      |         | Map of field name → CSS selector                    |
| `render_js` | boolean | no       | false   | Render JavaScript before extracting                 |
| `wait_ms`   | number  | no       | 0       | Milliseconds to wait                                |

Returns: `url`, `data` (object with extracted field values).

## screenshot_url

Take a screenshot of a rendered web page.

```
web-scraper screenshot_url --url "https://example.com" --full_page true --width 1280 --height 800
```

| Argument    | Type    | Required | Default | Description                          |
| ----------- | ------- | -------- | ------- | ------------------------------------ |
| `url`       | string  | yes      |         | URL to screenshot                    |
| `full_page` | boolean | no       | false   | Capture full scrollable page         |
| `width`     | number  | no       | 1280    | Viewport width in pixels             |
| `height`    | number  | no       | 800     | Viewport height in pixels            |
| `wait_ms`   | number  | no       | 1000    | Milliseconds to wait after page load |

Returns: `url`, `image_base64`, `width`, `height`, `format`.

## get_metadata

Fetch page metadata: title, description, OG tags, canonical URL, and HTTP headers.

```
web-scraper get_metadata --url "https://example.com"
```

| Argument | Type   | Required | Description   |
| -------- | ------ | -------- | ------------- |
| `url`    | string | yes      | URL to inspect |

Returns: `url`, `title`, `description`, `og_title`, `og_description`, `og_image`, `canonical`, `status_code`, `content_type`, `headers`.

## check_accessibility

Check if a URL is reachable and return HTTP status, redirect chain, and response time.

```
web-scraper check_accessibility --url "https://example.com" --follow_redirects true
```

| Argument           | Type    | Required | Default | Description                  |
| ------------------ | ------- | -------- | ------- | ---------------------------- |
| `url`              | string  | yes      |         | URL to check                 |
| `follow_redirects` | boolean | no       | true    | Follow HTTP redirects        |
| `timeout_ms`       | number  | no       | 10000   | Request timeout in ms        |

Returns: `url`, `final_url`, `status_code`, `reachable`, `response_time_ms`, `redirect_chain`.

## batch_scrape

Scrape multiple URLs and return results for each.

```
web-scraper batch_scrape --urls '["https://a.com","https://b.com"]' --extract "text"
```

| Argument    | Type         | Required | Default | Description                                       |
| ----------- | ------------ | -------- | ------- | ------------------------------------------------- |
| `urls`      | string array | yes      |         | List of URLs to scrape (max 10)                   |
| `extract`   | string       | no       | html    | What to extract: `html`, `text`, `links`          |
| `render_js` | boolean      | no       | false   | Render JavaScript for all URLs                    |

Returns: array of `{ url, status_code, result, error }`.
