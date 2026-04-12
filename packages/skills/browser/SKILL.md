# Browser

Control a headless browser — navigate, click, fill forms, take screenshots, and extract page content.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Navigation

### Open a URL

```
browser open --url "https://example.com"
```

| Argument | Type   | Required | Description        |
|----------|--------|----------|--------------------|
| `url`    | string | yes      | URL to open        |

Returns: `title`, `content` (truncated to 20000 chars), `url`.

### Navigate to a URL

```
browser navigate --url "https://example.com/page"
```

| Argument | Type   | Required | Description        |
|----------|--------|----------|--------------------|
| `url`    | string | yes      | URL to navigate to |

Returns: `title`, `content` (truncated to 20000 chars), `url`.

## Interaction

### Click an element

```
browser click --selector "button.submit"
```

| Argument   | Type   | Required | Description  |
|------------|--------|----------|--------------|
| `selector` | string | yes      | CSS selector |

Returns: `clicked` (the selector that was clicked).

### Fill a form field

```
browser fill --selector "input[name=email]" --value "user@example.com"
```

| Argument   | Type   | Required | Description          |
|------------|--------|----------|----------------------|
| `selector` | string | yes      | CSS selector         |
| `value`    | string | yes      | Value to fill        |

Returns: `filled` (the selector that was filled).

### Press a key

```
browser press --key "Enter"
```

| Argument | Type   | Required | Description                              |
|----------|--------|----------|------------------------------------------|
| `key`    | string | yes      | Key to press, e.g. Enter, Tab, Escape    |

Returns: `pressed` (the key that was pressed).

### Scroll the page

```
browser scroll --direction "down" --amount 500
```

| Argument    | Type   | Required | Default | Description          |
|-------------|--------|----------|---------|----------------------|
| `direction` | enum   | yes      |         | `up` or `down`       |
| `amount`    | number | no       | 500     | Pixels to scroll     |

Returns: `scrolled` (direction and amount).

## Content Extraction

### Take a screenshot

```
browser screenshot --full_page true
```

| Argument    | Type    | Required | Default | Description                  |
|-------------|---------|----------|---------|------------------------------|
| `full_page` | boolean | no       | false   | Capture the entire page      |

Returns: `image_base64`, `width`, `height`.

### Get accessibility snapshot

```
browser snapshot
```

No arguments. Returns the full page text content (truncated to 30000 chars).

### Get text from page or element

```
browser get_text --selector "div.main-content"
```

| Argument   | Type   | Required | Description                                |
|------------|--------|----------|--------------------------------------------|
| `selector` | string | no       | CSS selector, or omit for full page text   |

Returns: `text` (truncated to 30000 chars).

## Session

### Close session

```
browser close
```

No arguments. Returns: `closed` (boolean). Signals session cleanup to the runtime layer.

## Workflow

1. **Start with `browser open`** to load a page. This gives you the page title and text content.
2. Use `browser snapshot` or `browser get_text` to read page content before interacting.
3. Use `browser click`, `browser fill`, and `browser press` to interact with forms and buttons.
4. Use `browser scroll` to reveal content below the fold.
5. Use `browser screenshot` to capture the visual state when text extraction is insufficient.
6. Use `browser navigate` to move between pages without opening a new session.
7. Use `browser get_text --selector "..."` to extract content from specific elements.
8. **Always read before clicking** — use `snapshot` or `get_text` to understand the page structure first.

## Safety notes

- The browser runs in **headless mode**. There is no visible browser window.
- Sessions have a **5-minute idle timeout**. After 5 minutes of no actions, the session is reclaimed.
- **File downloads are not supported.** The browser cannot save files to disk.
- Text content is **truncated** to prevent memory issues: `open`/`navigate` at 20000 chars, `snapshot`/`get_text` at 30000 chars.
- CSS selectors must be valid. Use `snapshot` or `get_text` to discover the page structure before targeting elements.
- The browser has no persistent cookies or login state between sessions.
