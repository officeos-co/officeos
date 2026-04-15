# PDF

Create, read, and manipulate PDF documents via a file-proxy service. Supports creating PDFs from scratch, extracting text, merging and splitting files, adding pages and text, rotating pages, and managing page structure.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Documents

### Create PDF

```
pdf create --filename "report.pdf"
```

| Argument   | Type   | Required | Description                         |
| ---------- | ------ | -------- | ----------------------------------- |
| `filename` | string | yes      | Output filename (e.g. report.pdf)   |

Returns: `file_id`, `filename`, `created_at`.

### Open PDF

```
pdf open --file_id "abc123"
```

| Argument  | Type   | Required | Description               |
| --------- | ------ | -------- | ------------------------- |
| `file_id` | string | yes      | ID of a previously stored file |

Returns: `file_id`, `filename`, `page_count`, `title`, `author`, `creator`, `producer`, `creation_date`, `modification_date`, `encrypted`.

### Get info

```
pdf get_info --file_id "abc123"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `file_id` | string | yes      | File ID     |

Returns: `file_id`, `page_count`, `title`, `author`, `subject`, `keywords`, `creator`, `producer`, `creation_date`, `modification_date`, `page_sizes`, `encrypted`.

### Save PDF

```
pdf save --file_id "abc123"
```

| Argument  | Type   | Required | Description     |
| --------- | ------ | -------- | --------------- |
| `file_id` | string | yes      | File ID to save |

Returns: `file_id`, `filename`, `size_bytes`, `saved_at`.

## Content

### Extract text

```
pdf extract_text --file_id "abc123" --pages "1-5,8,10-12"
```

| Argument  | Type   | Required | Description                                          |
| --------- | ------ | -------- | ---------------------------------------------------- |
| `file_id` | string | yes      | File ID                                              |
| `pages`   | string | no       | Page range expression: `1-3,5,7-9` (1-indexed, omit for all) |

Returns: `text` (full extracted text), `pages` (array of `{page, text}`).

### Add page

```
pdf add_page --file_id "abc123" --width 595 --height 842 --position 2
```

| Argument   | Type   | Required | Default | Description                                                 |
| ---------- | ------ | -------- | ------- | ----------------------------------------------------------- |
| `file_id`  | string | yes      |         | File ID                                                     |
| `width`    | int    | no       | 595     | Page width in points (595 = A4)                             |
| `height`   | int    | no       | 842     | Page height in points (842 = A4)                            |
| `size`     | string | no       |         | Named size: `A4`, `A3`, `Letter`, `Legal` (overrides width/height) |
| `position` | int    | no       |         | Insert at page index (0-based, omit to append)              |

Returns: `page_index`, `page_count`, `file_id`.

### Add text

```
pdf add_text --file_id "abc123" --page_index 0 --text "Confidential" --x 50 --y 750 --font_size 12 --color "#FF0000"
```

| Argument      | Type   | Required | Default   | Description                               |
| ------------- | ------ | -------- | --------- | ----------------------------------------- |
| `file_id`     | string | yes      |           | File ID                                   |
| `page_index`  | int    | yes      |           | Zero-based page index                     |
| `text`        | string | yes      |           | Text to add                               |
| `x`           | int    | no       | 50        | X position in points from left            |
| `y`           | int    | no       | 750       | Y position in points from bottom          |
| `font_size`   | int    | no       | 12        | Font size in points                       |
| `font`        | string | no       | `Helvetica` | Font family: `Helvetica`, `Times-Roman`, `Courier` |
| `color`       | string | no       | `#000000` | Hex text color                            |
| `bold`        | boolean| no       | false     | Bold text                                 |
| `italic`      | boolean| no       | false     | Italic text                               |
| `opacity`     | number | no       | 1.0       | Opacity (0.0–1.0)                         |

Returns: `text_element_id`, `page_index`, `file_id`.

### Add image to page

```
pdf add_image --file_id "abc123" --page_index 0 --image_url "https://example.com/logo.png" --x 50 --y 700 --width 150
```

| Argument      | Type   | Required | Description                          |
| ------------- | ------ | -------- | ------------------------------------ |
| `file_id`     | string | yes      | File ID                              |
| `page_index`  | int    | yes      | Zero-based page index                |
| `image_url`   | string | yes      | URL or base64 data URI               |
| `x`           | int    | no       | X position in points from left       |
| `y`           | int    | no       | Y position in points from bottom     |
| `width`       | int    | no       | Image width in points                |
| `height`      | int    | no       | Image height in points               |

Returns: `image_element_id`, `page_index`, `file_id`.

### Delete page

```
pdf delete_page --file_id "abc123" --page_index 3
```

| Argument     | Type   | Required | Description                |
| ------------ | ------ | -------- | -------------------------- |
| `file_id`    | string | yes      | File ID                    |
| `page_index` | int    | yes      | Zero-based page index      |

Returns: `deleted_index`, `page_count`, `file_id`.

### Rotate pages

```
pdf rotate --file_id "abc123" --pages "1,3-5" --degrees 90
```

| Argument  | Type   | Required | Description                                         |
| --------- | ------ | -------- | --------------------------------------------------- |
| `file_id` | string | yes      | File ID                                             |
| `pages`   | string | yes      | Page range: `1-3,5` (1-indexed), or `all`           |
| `degrees` | int    | yes      | Rotation angle: `90`, `180`, or `270`               |

Returns: `rotated_pages`, `file_id`.

## Multi-file Operations

### Merge PDFs

```
pdf merge --file_ids '["abc123","def456","ghi789"]' --output_filename "merged.pdf"
```

| Argument          | Type     | Required | Description                        |
| ----------------- | -------- | -------- | ---------------------------------- |
| `file_ids`        | string[] | yes      | Ordered list of file IDs to merge  |
| `output_filename` | string   | no       | Output filename for merged PDF     |

Returns: `file_id`, `filename`, `page_count`, `size_bytes`.

### Split PDF

```
pdf split --file_id "abc123" --ranges '["1-3","4-6","7"]'
```

| Argument   | Type     | Required | Description                                                            |
| ---------- | -------- | -------- | ---------------------------------------------------------------------- |
| `file_id`  | string   | yes      | Source file ID                                                         |
| `ranges`   | string[] | yes      | Array of page range expressions (1-indexed): `["1-3","4-6","7"]`      |

Returns: array of `{file_id, filename, page_count, range}`.

### Copy pages

```
pdf copy_pages --source_file_id "abc123" --target_file_id "def456" --pages "2-4" --insert_at 5
```

| Argument          | Type   | Required | Description                             |
| ----------------- | ------ | -------- | --------------------------------------- |
| `source_file_id`  | string | yes      | Source file ID                          |
| `target_file_id`  | string | yes      | Destination file ID                     |
| `pages`           | string | yes      | Page range from source (1-indexed)      |
| `insert_at`       | int    | no       | Page index in target to insert before   |

Returns: `page_count`, `target_file_id`.

## Metadata

### Set metadata

```
pdf set_metadata --file_id "abc123" --title "Annual Report 2025" --author "Finance Team" --subject "Finance"
```

| Argument   | Type   | Required | Description         |
| ---------- | ------ | -------- | ------------------- |
| `file_id`  | string | yes      | File ID             |
| `title`    | string | no       | Document title      |
| `author`   | string | no       | Document author     |
| `subject`  | string | no       | Document subject    |
| `keywords` | string | no       | Comma-separated keywords |
| `creator`  | string | no       | Creating application|

Returns: `file_id`, `metadata`.

## Export

### Download file

```
pdf download --file_id "abc123"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `file_id` | string | yes      | File ID     |

Returns: `download_url`, `filename`, `size_bytes`, `expires_at`.

## Workflow

1. Use `create` to start a blank PDF or `open` to load an existing one by `file_id`.
2. Use `get_info` to inspect page count and metadata before editing.
3. Add content with `add_text` and `add_image`, or new blank pages with `add_page`.
4. Use `extract_text` to read content from existing PDFs.
5. Use `merge` to combine multiple PDFs into one, or `split` to break a PDF into parts.
6. Use `rotate` to fix page orientation.
7. Call `save` and then `download` to retrieve the final file.

## Safety notes

- PDF coordinates use **points from the bottom-left corner** (1 point = 1/72 inch). For A4, valid range is 0–595 x (width) and 0–842 y (height).
- `delete_page` is irreversible without re-opening from a saved state. Call `save` first.
- `extract_text` works only on PDFs with embedded text layers. Scanned image PDFs require OCR (not supported by this skill).
- `merge` and `split` create new file IDs — the source files are not modified.
- Encrypted PDFs require decryption before editing; this skill does not decrypt password-protected files.
