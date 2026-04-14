# Google Sheets

Create, read, write, format, and manage spreadsheets, sheets, and cell data via the Google Sheets API v4.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Spreadsheets

### Create spreadsheet

```
google-sheets create_spreadsheet --title "Q2 Budget Tracker"
```

| Argument | Type   | Required | Description          |
|----------|--------|----------|----------------------|
| `title`  | string | yes      | Spreadsheet title    |

Returns: `spreadsheet_id`, `title`, `url`, `sheets` (list of default sheet names).

### Get spreadsheet

```
google-sheets get_spreadsheet --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ"
```

| Argument         | Type   | Required | Description       |
|------------------|--------|----------|-------------------|
| `spreadsheet_id` | string | yes      | Spreadsheet ID    |

Returns: `spreadsheet_id`, `title`, `url`, `locale`, `time_zone`, `sheets` (list of `sheet_id`, `title`, `index`, `row_count`, `column_count`).

### List sheets

```
google-sheets list_sheets --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ"
```

| Argument         | Type   | Required | Description       |
|------------------|--------|----------|-------------------|
| `spreadsheet_id` | string | yes      | Spreadsheet ID    |

Returns: list of `sheet_id`, `title`, `index`, `row_count`, `column_count`.

## Read

### Get values

```
google-sheets get_values --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --range "Sheet1!A1:D10"
```

| Argument         | Type   | Required | Description                          |
|------------------|--------|----------|--------------------------------------|
| `spreadsheet_id` | string | yes      | Spreadsheet ID                       |
| `range`          | string | yes      | A1 notation range (e.g. `Sheet1!A1:D10`) |

Returns: `range`, `major_dimension`, `values` (2D array of cell values).

### Batch get values

```
google-sheets batch_get_values --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --ranges '["Sheet1!A1:B5","Sheet2!A1:C3"]'
```

| Argument         | Type     | Required | Description                      |
|------------------|----------|----------|----------------------------------|
| `spreadsheet_id` | string   | yes      | Spreadsheet ID                   |
| `ranges`         | string[] | yes      | List of A1 notation ranges       |

Returns: list of `range`, `major_dimension`, `values` for each requested range.

## Write

### Update values

```
google-sheets update_values --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --range "Sheet1!A1:B2" --values '[["Name","Score"],["Alice","95"]]' --value_input_option "USER_ENTERED"
```

| Argument             | Type     | Required | Default        | Description                                                |
|----------------------|----------|----------|----------------|------------------------------------------------------------|
| `spreadsheet_id`     | string   | yes      |                | Spreadsheet ID                                             |
| `range`              | string   | yes      |                | A1 notation range to write to                              |
| `values`             | string   | yes      |                | 2D JSON array of values                                    |
| `value_input_option` | string   | no       | `USER_ENTERED` | `RAW` (literal) or `USER_ENTERED` (parsed like UI input)  |

Returns: `updated_range`, `updated_rows`, `updated_columns`, `updated_cells`.

### Append values

```
google-sheets append_values --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --range "Sheet1!A:B" --values '[["Bob","88"]]'
```

| Argument             | Type   | Required | Default        | Description                                                |
|----------------------|--------|----------|----------------|------------------------------------------------------------|
| `spreadsheet_id`     | string | yes      |                | Spreadsheet ID                                             |
| `range`              | string | yes      |                | A1 notation range (rows appended after last data row)      |
| `values`             | string | yes      |                | 2D JSON array of values to append                          |
| `value_input_option` | string | no       | `USER_ENTERED` | `RAW` or `USER_ENTERED`                                   |

Returns: `updated_range`, `updated_rows`, `updated_columns`, `updated_cells`.

### Batch update values

```
google-sheets batch_update_values --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --data '[{"range":"Sheet1!A1:B1","values":[["Name","Score"]]},{"range":"Sheet1!A2:B2","values":[["Alice","95"]]}]'
```

| Argument             | Type   | Required | Default        | Description                                     |
|----------------------|--------|----------|----------------|-------------------------------------------------|
| `spreadsheet_id`     | string | yes      |                | Spreadsheet ID                                  |
| `data`               | string | yes      |                | JSON array of `{range, values}` objects          |
| `value_input_option` | string | no       | `USER_ENTERED` | `RAW` or `USER_ENTERED`                         |

Returns: `total_updated_cells`, `total_updated_rows`, `total_updated_columns`, `responses` (per-range results).

## Clear

### Clear values

```
google-sheets clear_values --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --range "Sheet1!A2:D100"
```

| Argument         | Type   | Required | Description               |
|------------------|--------|----------|---------------------------|
| `spreadsheet_id` | string | yes      | Spreadsheet ID            |
| `range`          | string | yes      | A1 notation range to clear|

Returns: `cleared_range`.

### Batch clear values

```
google-sheets batch_clear_values --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --ranges '["Sheet1!A2:D100","Sheet2!A:Z"]'
```

| Argument         | Type     | Required | Description                  |
|------------------|----------|----------|------------------------------|
| `spreadsheet_id` | string   | yes      | Spreadsheet ID               |
| `ranges`         | string[] | yes      | List of A1 notation ranges   |

Returns: list of `cleared_range`.

## Sheets (tabs)

### Add sheet

```
google-sheets add_sheet --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --title "Q2 Data"
```

| Argument         | Type   | Required | Description        |
|------------------|--------|----------|--------------------|
| `spreadsheet_id` | string | yes      | Spreadsheet ID     |
| `title`          | string | yes      | New sheet name     |

Returns: `sheet_id`, `title`, `index`.

### Delete sheet

```
google-sheets delete_sheet --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --sheet_id 123456789
```

| Argument         | Type   | Required | Description             |
|------------------|--------|----------|-------------------------|
| `spreadsheet_id` | string | yes      | Spreadsheet ID          |
| `sheet_id`       | int    | yes      | Sheet ID (not name)     |

Returns: confirmation status.

### Rename sheet

```
google-sheets rename_sheet --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --sheet_id 123456789 --title "Renamed Tab"
```

| Argument         | Type   | Required | Description          |
|------------------|--------|----------|----------------------|
| `spreadsheet_id` | string | yes      | Spreadsheet ID       |
| `sheet_id`       | int    | yes      | Sheet ID to rename   |
| `title`          | string | yes      | New sheet name       |

Returns: `sheet_id`, `title`.

### Duplicate sheet

```
google-sheets duplicate_sheet --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --sheet_id 123456789 --new_title "Q2 Data (Copy)"
```

| Argument         | Type   | Required | Description                      |
|------------------|--------|----------|----------------------------------|
| `spreadsheet_id` | string | yes      | Spreadsheet ID                   |
| `sheet_id`       | int    | yes      | Sheet ID to duplicate            |
| `new_title`      | string | no       | Title for the duplicate sheet    |

Returns: `sheet_id`, `title`, `index`.

## Format

### Format cells

```
google-sheets format_cells --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --range "Sheet1!A1:D1" --bold true --background_color "#4285F4" --font_size 12
```

| Argument           | Type    | Required | Description                               |
|--------------------|---------|----------|-------------------------------------------|
| `spreadsheet_id`   | string  | yes      | Spreadsheet ID                            |
| `range`            | string  | yes      | A1 notation range to format               |
| `bold`             | boolean | no       | Bold text                                 |
| `italic`           | boolean | no       | Italic text                               |
| `font_size`        | int     | no       | Font size in points                       |
| `background_color` | string  | no       | Hex color code (e.g. `#4285F4`)           |

Returns: confirmation status.

### Auto resize columns

```
google-sheets auto_resize --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --sheet_id 0 --start_column 0 --end_column 5
```

| Argument         | Type   | Required | Description                    |
|------------------|--------|----------|--------------------------------|
| `spreadsheet_id` | string | yes      | Spreadsheet ID                 |
| `sheet_id`       | int    | yes      | Sheet ID                       |
| `start_column`   | int    | yes      | Start column index (0-based)   |
| `end_column`     | int    | yes      | End column index (exclusive)   |

Returns: confirmation status.

### Merge cells

```
google-sheets merge_cells --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --range "Sheet1!A1:D1"
```

| Argument         | Type   | Required | Description                |
|------------------|--------|----------|----------------------------|
| `spreadsheet_id` | string | yes      | Spreadsheet ID             |
| `range`          | string | yes      | A1 notation range to merge |

Returns: confirmation status.

### Unmerge cells

```
google-sheets unmerge_cells --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --range "Sheet1!A1:D1"
```

| Argument         | Type   | Required | Description                  |
|------------------|--------|----------|------------------------------|
| `spreadsheet_id` | string | yes      | Spreadsheet ID               |
| `range`          | string | yes      | A1 notation range to unmerge |

Returns: confirmation status.

## Sort / Filter

### Sort range

```
google-sheets sort_range --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --range "Sheet1!A1:D100" --sort_column 1 --ascending true
```

| Argument         | Type    | Required | Default | Description                        |
|------------------|---------|----------|---------|------------------------------------|
| `spreadsheet_id` | string  | yes      |         | Spreadsheet ID                     |
| `range`          | string  | yes      |         | A1 notation range to sort          |
| `sort_column`    | int     | yes      |         | Column index to sort by (0-based)  |
| `ascending`      | boolean | no       | true    | Sort direction                     |

Returns: confirmation status.

### Add filter

```
google-sheets add_filter --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --range "Sheet1!A1:D100"
```

| Argument         | Type   | Required | Description                       |
|------------------|--------|----------|-----------------------------------|
| `spreadsheet_id` | string | yes      | Spreadsheet ID                    |
| `range`          | string | yes      | A1 notation range for the filter  |

Returns: confirmation status.

### Remove filter

```
google-sheets remove_filter --spreadsheet_id "1aBcDeFgHiJkLmNoPqRsTuVwXyZ" --sheet_id 0
```

| Argument         | Type   | Required | Description           |
|------------------|--------|----------|-----------------------|
| `spreadsheet_id` | string | yes      | Spreadsheet ID        |
| `sheet_id`       | int    | yes      | Sheet ID              |

Returns: confirmation status.

## Workflow

1. **Start with `google-sheets get_spreadsheet` or `google-sheets list_sheets`** to understand the structure.
2. Use `get_values` to read data from specific ranges.
3. Use `update_values` to write data and `append_values` to add rows at the end.
4. Use `batch_get_values` and `batch_update_values` when working with multiple ranges for efficiency.
5. Use `add_sheet` to create new tabs for organizing data.
6. Apply formatting with `format_cells` after writing data.
7. Use `sort_range` and `add_filter` to organize and explore data.

## Safety notes

- Spreadsheet IDs are opaque strings from the URL. **Never fabricate them** -- get them from Google Drive operations or the user.
- **`delete_sheet` is permanent.** All data in the sheet tab is lost. Confirm with the user before deleting.
- `clear_values` removes cell contents but preserves formatting. It cannot be undone.
- Ranges use A1 notation: `Sheet1!A1:D10`, `Sheet1!A:A` (full column), `Sheet1!1:1` (full row).
- `value_input_option` controls how values are interpreted: `USER_ENTERED` parses numbers, dates, and formulas (like typing in the UI); `RAW` stores everything as literal strings.
- **Formulas are passed as string values starting with `=`** (e.g. `=SUM(A1:A10)`). Use `USER_ENTERED` for formulas to be evaluated.
- `sheet_id` (integer) and sheet title (string) are different. Use `list_sheets` to find the `sheet_id` for a named tab.
- Sheets API has per-user rate limits (100 requests per 100 seconds). Batch operations when possible.
- Only spreadsheets accessible to the authenticated account are visible.
