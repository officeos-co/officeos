# Word

Create and manipulate Microsoft Word documents via a file-proxy service. Supports creating documents from scratch, editing content, managing structure, and exporting to PDF.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Documents

### Create document

```
word create --filename "report.docx" --title "Q1 Report" --author "Jane Doe"
```

| Argument    | Type   | Required | Description                    |
| ----------- | ------ | -------- | ------------------------------ |
| `filename`  | string | yes      | Output filename (e.g. foo.docx)|
| `title`     | string | no       | Document title metadata        |
| `author`    | string | no       | Document author metadata       |

Returns: `file_id`, `filename`, `created_at`.

### Open document

```
word open --file_id "abc123"
```

| Argument  | Type   | Required | Description               |
| --------- | ------ | -------- | ------------------------- |
| `file_id` | string | yes      | ID of previously created file |

Returns: `file_id`, `filename`, `paragraph_count`, `section_count`, `author`, `title`, `created`, `modified`.

### Save document

```
word save --file_id "abc123"
```

| Argument  | Type   | Required | Description         |
| --------- | ------ | -------- | ------------------- |
| `file_id` | string | yes      | File ID to save     |

Returns: `file_id`, `filename`, `size_bytes`, `saved_at`.

### Get text

```
word get_text --file_id "abc123" --include_formatting false
```

| Argument              | Type    | Required | Default | Description                              |
| --------------------- | ------- | -------- | ------- | ---------------------------------------- |
| `file_id`             | string  | yes      |         | File ID                                  |
| `include_formatting`  | boolean | no       | false   | Include style/font info per paragraph    |

Returns: `text` (full document text), `paragraphs` (array of paragraph objects).

### Replace text

```
word replace_text --file_id "abc123" --search "old phrase" --replace "new phrase" --match_case true
```

| Argument      | Type    | Required | Default | Description                           |
| ------------- | ------- | -------- | ------- | ------------------------------------- |
| `file_id`     | string  | yes      |         | File ID                               |
| `search`      | string  | yes      |         | Text to find                          |
| `replace`     | string  | yes      |         | Replacement text                      |
| `match_case`  | boolean | no       | false   | Case-sensitive match                  |

Returns: `replacements_made`, `file_id`.

## Content

### Add paragraph

```
word add_paragraph --file_id "abc123" --text "Introduction text here." --style "Heading1" --position 0
```

| Argument    | Type   | Required | Default   | Description                                                         |
| ----------- | ------ | -------- | --------- | ------------------------------------------------------------------- |
| `file_id`   | string | yes      |           | File ID                                                             |
| `text`      | string | yes      |           | Paragraph text                                                      |
| `style`     | string | no       | `Normal`  | Style name: `Normal`, `Heading1`–`Heading6`, `ListBullet`, `Quote`  |
| `position`  | int    | no       |           | Insert at paragraph index (omit to append)                          |
| `bold`      | boolean| no       | false     | Bold text                                                           |
| `italic`    | boolean| no       | false     | Italic text                                                         |
| `font_size` | int    | no       |           | Font size in points                                                 |

Returns: `paragraph_index`, `file_id`.

### Add table

```
word add_table --file_id "abc123" --rows 3 --cols 4 --headers '["Name","Role","Dept","Start"]' --data '[["Alice","Eng","R&D","2022"],["Bob","PM","Product","2021"]]'
```

| Argument    | Type       | Required | Description                              |
| ----------- | ---------- | -------- | ---------------------------------------- |
| `file_id`   | string     | yes      | File ID                                  |
| `rows`      | int        | yes      | Number of rows (excluding header)        |
| `cols`      | int        | yes      | Number of columns                        |
| `headers`   | string[]   | no       | Header row values                        |
| `data`      | string[][] | no       | 2D array of cell values                  |
| `style`     | string     | no       | Table style name (e.g. `Table Grid`)     |

Returns: `table_index`, `file_id`.

### Add image

```
word add_image --file_id "abc123" --image_url "https://example.com/logo.png" --width_cm 8 --alt_text "Company logo"
```

| Argument      | Type   | Required | Description                       |
| ------------- | ------ | -------- | --------------------------------- |
| `file_id`     | string | yes      | File ID                           |
| `image_url`   | string | yes      | URL or base64 data URI of image   |
| `width_cm`    | number | no       | Image width in centimetres        |
| `height_cm`   | number | no       | Image height in centimetres       |
| `alt_text`    | string | no       | Accessibility alt text            |
| `position`    | int    | no       | Paragraph index to insert before  |

Returns: `image_index`, `file_id`.

### Add heading

```
word add_heading --file_id "abc123" --text "Executive Summary" --level 1
```

| Argument  | Type   | Required | Default | Description               |
| --------- | ------ | -------- | ------- | ------------------------- |
| `file_id` | string | yes      |         | File ID                   |
| `text`    | string | yes      |         | Heading text              |
| `level`   | int    | no       | 1       | Heading level (1–6)       |

Returns: `paragraph_index`, `file_id`.

### Add page break

```
word add_page_break --file_id "abc123"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `file_id` | string | yes      | File ID     |

Returns: `paragraph_index`, `file_id`.

### Delete paragraph

```
word delete_paragraph --file_id "abc123" --index 5
```

| Argument  | Type   | Required | Description              |
| --------- | ------ | -------- | ------------------------ |
| `file_id` | string | yes      | File ID                  |
| `index`   | int    | yes      | Paragraph index to delete|

Returns: `deleted_index`, `paragraph_count`, `file_id`.

## Sections & Styles

### List styles

```
word list_styles --file_id "abc123" --type paragraph
```

| Argument  | Type   | Required | Default     | Description                          |
| --------- | ------ | -------- | ----------- | ------------------------------------ |
| `file_id` | string | yes      |             | File ID                              |
| `type`    | string | no       | `paragraph` | `paragraph`, `character`, `table`    |

Returns: array of `name`, `type`, `base_style`.

### Set document properties

```
word set_properties --file_id "abc123" --title "Final Report" --subject "Finance" --keywords '["Q1","finance"]'
```

| Argument    | Type     | Required | Description             |
| ----------- | -------- | -------- | ----------------------- |
| `file_id`   | string   | yes      | File ID                 |
| `title`     | string   | no       | Document title          |
| `subject`   | string   | no       | Document subject        |
| `author`    | string   | no       | Document author         |
| `keywords`  | string[] | no       | Keyword list            |

Returns: `file_id`, `properties`.

## Export

### Export to PDF

```
word export_pdf --file_id "abc123" --output_filename "report.pdf"
```

| Argument          | Type   | Required | Description                       |
| ----------------- | ------ | -------- | --------------------------------- |
| `file_id`         | string | yes      | Source Word file ID               |
| `output_filename` | string | no       | PDF filename (defaults to docx name)|

Returns: `pdf_file_id`, `filename`, `size_bytes`, `download_url`.

### Download file

```
word download --file_id "abc123"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `file_id` | string | yes      | File ID     |

Returns: `download_url`, `filename`, `size_bytes`, `expires_at`.

## Workflow

1. Use `create` to start a new document or `open` to load an existing one by `file_id`.
2. Add content with `add_heading`, `add_paragraph`, `add_table`, and `add_image`.
3. Use `replace_text` for find-and-replace operations (e.g. filling template placeholders).
4. Call `save` to persist changes before downloading or exporting.
5. Use `export_pdf` to produce a PDF copy, or `download` to retrieve the `.docx`.

## Safety notes

- File IDs are scoped to the authenticated proxy — they are not portable between proxy instances.
- Large images slow down generation. Prefer hosted URLs over base64 data URIs when possible.
- `delete_paragraph` is irreversible without re-opening from a saved state. Save frequently.
- `export_pdf` requires the proxy service to have a PDF renderer (LibreOffice or equivalent) available.
