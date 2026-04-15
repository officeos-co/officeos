# PowerPoint

Create and manipulate Microsoft PowerPoint presentations via a file-proxy service. Supports creating presentations from scratch, managing slides, adding rich content, and exporting to PDF.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Presentations

### Create presentation

```
powerpoint create --filename "deck.pptx" --title "Q1 Review" --author "Jane Doe" --width_px 1280 --height_px 720
```

| Argument    | Type   | Required | Default | Description                         |
| ----------- | ------ | -------- | ------- | ----------------------------------- |
| `filename`  | string | yes      |         | Output filename (e.g. deck.pptx)    |
| `title`     | string | no       |         | Presentation title metadata         |
| `author`    | string | no       |         | Author metadata                     |
| `width_px`  | int    | no       | 1280    | Slide width in pixels               |
| `height_px` | int    | no       | 720     | Slide height in pixels              |

Returns: `file_id`, `filename`, `created_at`.

### Open presentation

```
powerpoint open --file_id "abc123"
```

| Argument  | Type   | Required | Description                   |
| --------- | ------ | -------- | ----------------------------- |
| `file_id` | string | yes      | ID of a previously saved file |

Returns: `file_id`, `filename`, `slide_count`, `title`, `author`, `created`, `modified`.

### Save presentation

```
powerpoint save --file_id "abc123"
```

| Argument  | Type   | Required | Description       |
| --------- | ------ | -------- | ----------------- |
| `file_id` | string | yes      | File ID to save   |

Returns: `file_id`, `filename`, `size_bytes`, `saved_at`.

### Get presentation info

```
powerpoint get_info --file_id "abc123"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `file_id` | string | yes      | File ID     |

Returns: `file_id`, `slide_count`, `title`, `author`, `subject`, `keywords`, `width_px`, `height_px`, `created`, `modified`.

## Slides

### Add slide

```
powerpoint add_slide --file_id "abc123" --layout "TITLE_AND_CONTENT" --position 2
```

| Argument   | Type   | Required | Description                                                          |
| ---------- | ------ | -------- | -------------------------------------------------------------------- |
| `file_id`  | string | yes      | File ID                                                              |
| `layout`   | string | no       | Layout: `BLANK`, `TITLE_ONLY`, `TITLE_AND_CONTENT`, `TWO_CONTENT`   |
| `position` | int    | no       | Slide index to insert at (0-based, omit to append)                  |

Returns: `slide_index`, `slide_id`, `file_id`.

### Delete slide

```
powerpoint delete_slide --file_id "abc123" --slide_index 3
```

| Argument      | Type   | Required | Description                   |
| ------------- | ------ | -------- | ----------------------------- |
| `file_id`     | string | yes      | File ID                       |
| `slide_index` | int    | yes      | Zero-based slide index        |

Returns: `deleted_index`, `slide_count`, `file_id`.

### Reorder slide

```
powerpoint reorder_slide --file_id "abc123" --from_index 4 --to_index 1
```

| Argument     | Type   | Required | Description                  |
| ------------ | ------ | -------- | ---------------------------- |
| `file_id`    | string | yes      | File ID                      |
| `from_index` | int    | yes      | Current zero-based index     |
| `to_index`   | int    | yes      | Target zero-based index      |

Returns: `slide_count`, `file_id`.

### Duplicate slide

```
powerpoint duplicate_slide --file_id "abc123" --slide_index 0
```

| Argument      | Type   | Required | Description             |
| ------------- | ------ | -------- | ----------------------- |
| `file_id`     | string | yes      | File ID                 |
| `slide_index` | int    | yes      | Index of slide to copy  |

Returns: `new_slide_index`, `slide_count`, `file_id`.

## Content

### Add text

```
powerpoint add_text --file_id "abc123" --slide_index 0 --text "Welcome to Q1 Review" --x 10 --y 10 --w 80 --h 20 --font_size 36 --bold true --align center
```

| Argument      | Type    | Required | Default  | Description                                      |
| ------------- | ------- | -------- | -------- | ------------------------------------------------ |
| `file_id`     | string  | yes      |          | File ID                                          |
| `slide_index` | int     | yes      |          | Zero-based slide index                           |
| `text`        | string  | yes      |          | Text content                                     |
| `x`           | number  | no       | 10       | X position as % of slide width (0–100)           |
| `y`           | number  | no       | 10       | Y position as % of slide height (0–100)          |
| `w`           | number  | no       | 80       | Width as % of slide width (0–100)                |
| `h`           | number  | no       | 20       | Height as % of slide height (0–100)              |
| `font_size`   | int     | no       | 24       | Font size in points                              |
| `bold`        | boolean | no       | false    | Bold text                                        |
| `italic`      | boolean | no       | false    | Italic text                                      |
| `color`       | string  | no       |          | Hex color (e.g. `#FF0000`)                       |
| `align`       | string  | no       | `left`   | `left`, `center`, `right`                        |

Returns: `element_id`, `slide_index`, `file_id`.

### Add image

```
powerpoint add_image --file_id "abc123" --slide_index 1 --image_url "https://example.com/chart.png" --x 20 --y 20 --w 60 --h 50
```

| Argument      | Type   | Required | Description                            |
| ------------- | ------ | -------- | -------------------------------------- |
| `file_id`     | string | yes      | File ID                                |
| `slide_index` | int    | yes      | Zero-based slide index                 |
| `image_url`   | string | yes      | URL or base64 data URI                 |
| `x`           | number | no       | X position as % of slide width         |
| `y`           | number | no       | Y position as % of slide height        |
| `w`           | number | no       | Width as % of slide width              |
| `h`           | number | no       | Height as % of slide height            |
| `alt_text`    | string | no       | Accessibility alt text                 |

Returns: `element_id`, `slide_index`, `file_id`.

### Add chart

```
powerpoint add_chart --file_id "abc123" --slide_index 2 --chart_type bar --title "Revenue by Quarter" --categories '["Q1","Q2","Q3","Q4"]' --series '[{"name":"2025","values":[120,145,130,160]}]'
```

| Argument      | Type   | Required | Description                                                            |
| ------------- | ------ | -------- | ---------------------------------------------------------------------- |
| `file_id`     | string | yes      | File ID                                                                |
| `slide_index` | int    | yes      | Zero-based slide index                                                 |
| `chart_type`  | string | yes      | `bar`, `column`, `line`, `pie`, `doughnut`, `area`, `scatter`          |
| `title`       | string | no       | Chart title                                                            |
| `categories`  | string[] | yes    | X-axis category labels                                                 |
| `series`      | array  | yes      | Array of `{name: string, values: number[]}` objects                    |
| `x`           | number | no       | X position as % of slide width                                         |
| `y`           | number | no       | Y position as % of slide height                                        |
| `w`           | number | no       | Width as % of slide width                                              |
| `h`           | number | no       | Height as % of slide height                                            |

Returns: `element_id`, `slide_index`, `file_id`.

### Add shape

```
powerpoint add_shape --file_id "abc123" --slide_index 0 --shape_type rect --x 40 --y 80 --w 20 --h 10 --fill_color "#4472C4" --text "Key Insight"
```

| Argument      | Type   | Required | Description                                            |
| ------------- | ------ | -------- | ------------------------------------------------------ |
| `file_id`     | string | yes      | File ID                                                |
| `slide_index` | int    | yes      | Zero-based slide index                                 |
| `shape_type`  | string | yes      | `rect`, `ellipse`, `arrow`, `star`, `triangle`         |
| `x`           | number | no       | X position as % of slide width                         |
| `y`           | number | no       | Y position as % of slide height                        |
| `w`           | number | no       | Width as % of slide width                              |
| `h`           | number | no       | Height as % of slide height                            |
| `fill_color`  | string | no       | Hex fill color                                         |
| `line_color`  | string | no       | Hex border color                                       |
| `text`        | string | no       | Text label inside shape                                |

Returns: `element_id`, `slide_index`, `file_id`.

### Set slide background

```
powerpoint set_background --file_id "abc123" --slide_index 0 --color "#1F497D"
```

| Argument      | Type   | Required | Description                           |
| ------------- | ------ | -------- | ------------------------------------- |
| `file_id`     | string | yes      | File ID                               |
| `slide_index` | int    | yes      | Zero-based slide index                |
| `color`       | string | no       | Hex background color                  |
| `image_url`   | string | no       | Background image URL or base64        |

Returns: `slide_index`, `file_id`.

### Set slide notes

```
powerpoint set_notes --file_id "abc123" --slide_index 0 --notes "Speaker notes for this slide."
```

| Argument      | Type   | Required | Description            |
| ------------- | ------ | -------- | ---------------------- |
| `file_id`     | string | yes      | File ID                |
| `slide_index` | int    | yes      | Zero-based slide index |
| `notes`       | string | yes      | Speaker notes text     |

Returns: `slide_index`, `file_id`.

## Export

### Export to PDF

```
powerpoint export_pdf --file_id "abc123" --output_filename "deck.pdf"
```

| Argument          | Type   | Required | Description                           |
| ----------------- | ------ | -------- | ------------------------------------- |
| `file_id`         | string | yes      | Source presentation file ID           |
| `output_filename` | string | no       | PDF filename (defaults to pptx name)  |

Returns: `pdf_file_id`, `filename`, `size_bytes`, `download_url`.

### Download file

```
powerpoint download --file_id "abc123"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `file_id` | string | yes      | File ID     |

Returns: `download_url`, `filename`, `size_bytes`, `expires_at`.

## Workflow

1. Use `create` to start a new presentation or `open` to load an existing one.
2. Add slides with `add_slide`, specifying layout as needed.
3. Populate slides with `add_text`, `add_image`, `add_chart`, and `add_shape`.
4. Set `set_background` for branded slides and `set_notes` for speaker notes.
5. Reorder with `reorder_slide` and remove unwanted slides with `delete_slide`.
6. Call `save` before exporting or downloading.
7. Use `export_pdf` to produce a shareable PDF or `download` to retrieve the `.pptx`.

## Safety notes

- Positions (`x`, `y`, `w`, `h`) are expressed as a **percentage of slide dimensions** (0–100), not pixels.
- `delete_slide` is irreversible without re-opening from a saved state. Save frequently.
- `export_pdf` requires LibreOffice or a compatible PDF renderer on the proxy server.
- Chart `series[].values` must have the same length as `categories`.
