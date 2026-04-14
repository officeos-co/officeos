# Excel

Create, read, write, format, and export Excel workbooks (.xlsx) with full support for formulas, tables, charts, and data operations via ExcelJS.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Workbook

### Create a new workbook

```
excel create_workbook
```

Returns: `workbook_id` handle for subsequent operations.

### Open an existing workbook

```
excel open_workbook --file_path "/data/reports/q2-revenue.xlsx"
```

| Argument    | Type   | Required | Description               |
|-------------|--------|----------|---------------------------|
| `file_path` | string | yes      | Path to .xlsx file        |

Returns: `workbook_id`, `sheet_count`, `sheet_names`.

### Save workbook

```
excel save_workbook --file_path "/data/reports/q2-revenue-updated.xlsx"
```

| Argument    | Type   | Required | Description                        |
|-------------|--------|----------|------------------------------------|
| `file_path` | string | yes      | Output path for the .xlsx file     |

Returns: confirmation with `file_path` and `file_size`.

### Get workbook info

```
excel get_workbook_info
```

Returns: `sheet_count`, `sheet_names`, `creator`, `created_at`, `modified_at`.

## Sheets

### List sheets

```
excel list_sheets
```

Returns: list of sheets with `name`, `index`, `row_count`, `column_count`, `state` (visible/hidden).

### Add a sheet

```
excel add_sheet --name "Revenue" --position 0
```

| Argument   | Type   | Required | Default    | Description                        |
|------------|--------|----------|------------|------------------------------------|
| `name`     | string | yes      |            | Sheet name                         |
| `position` | int    | no       | end        | Zero-based index to insert at      |

Returns: `name`, `index`.

### Remove a sheet

```
excel remove_sheet --name "OldData"
```

| Argument | Type   | Required | Description          |
|----------|--------|----------|----------------------|
| `name`   | string | yes      | Sheet name to remove |

Returns: confirmation of removal.

### Rename a sheet

```
excel rename_sheet --old_name "Sheet1" --new_name "Summary"
```

| Argument   | Type   | Required | Description          |
|------------|--------|----------|----------------------|
| `old_name` | string | yes      | Current sheet name   |
| `new_name` | string | yes      | New sheet name       |

Returns: confirmation with old and new names.

### Duplicate a sheet

```
excel duplicate_sheet --name "Template" --new_name "January"
```

| Argument   | Type   | Required | Default              | Description                 |
|------------|--------|----------|----------------------|-----------------------------|
| `name`     | string | yes      |                      | Sheet to duplicate          |
| `new_name` | string | no       | `<name> (Copy)`      | Name for the duplicated sheet|

Returns: `name`, `index` of the new sheet.

### Get sheet info

```
excel get_sheet_info --name "Revenue"
```

| Argument | Type   | Required | Description          |
|----------|--------|----------|----------------------|
| `name`   | string | yes      | Sheet name           |

Returns: `name`, `row_count`, `column_count`, `state`, `merged_cells`, `has_autofilter`.

## Read

### Get a single cell value

```
excel get_cell --sheet "Revenue" --cell "B5"
```

| Argument | Type   | Required | Description              |
|----------|--------|----------|--------------------------|
| `sheet`  | string | yes      | Sheet name               |
| `cell`   | string | yes      | Cell reference (e.g. B5) |

Returns: `value`, `type` (string, number, date, boolean, formula), `formula` (if applicable), `formatted_value`.

### Get a range of cells

```
excel get_range --sheet "Revenue" --range "A1:D10"
```

| Argument | Type   | Required | Description                      |
|----------|--------|----------|----------------------------------|
| `sheet`  | string | yes      | Sheet name                       |
| `range`  | string | yes      | Cell range (e.g. A1:D10)        |

Returns: 2D array of cell values with `rows` and `columns` metadata.

### Get a full row

```
excel get_row --sheet "Revenue" --row 1
```

| Argument | Type   | Required | Description       |
|----------|--------|----------|-------------------|
| `sheet`  | string | yes      | Sheet name        |
| `row`    | int    | yes      | Row number (1-based) |

Returns: list of cell values in the row.

### Get a full column

```
excel get_column --sheet "Revenue" --column "B"
```

| Argument | Type   | Required | Description                  |
|----------|--------|----------|------------------------------|
| `sheet`  | string | yes      | Sheet name                   |
| `column` | string | yes      | Column letter (e.g. B)       |

Returns: list of cell values in the column.

### Get all data from a sheet

```
excel get_all_data --sheet "Revenue"
```

| Argument | Type   | Required | Description       |
|----------|--------|----------|-------------------|
| `sheet`  | string | yes      | Sheet name        |

Returns: 2D array of all cell values, `row_count`, `column_count`.

## Write

### Set a single cell

```
excel set_cell --sheet "Revenue" --cell "B5" --value 42500 --type number
```

| Argument | Type   | Required | Default  | Description                                    |
|----------|--------|----------|----------|------------------------------------------------|
| `sheet`  | string | yes      |          | Sheet name                                     |
| `cell`   | string | yes      |          | Cell reference (e.g. B5)                       |
| `value`  | any    | yes      |          | Value to set                                   |
| `type`   | string | no       | `auto`   | Value type: `string`, `number`, `date`, `boolean`, `auto` |

Returns: confirmation with `cell`, `value`, `type`.

### Set a range of cells

```
excel set_range --sheet "Revenue" --range "A1:C2" --values '[["Name","Q1","Q2"],["Product A",1000,1500]]'
```

| Argument | Type     | Required | Description                       |
|----------|----------|----------|-----------------------------------|
| `sheet`  | string   | yes      | Sheet name                        |
| `range`  | string   | yes      | Target range (e.g. A1:C2)        |
| `values` | any[][]  | yes      | 2D array of values                |

Returns: confirmation with `range`, `rows_written`, `columns_written`.

### Insert a row

```
excel insert_row --sheet "Revenue" --row 3 --values '["Product C", 2000, 2500]'
```

| Argument | Type  | Required | Default | Description                        |
|----------|-------|----------|---------|------------------------------------|
| `sheet`  | string| yes      |         | Sheet name                         |
| `row`    | int   | yes      |         | Position to insert at (1-based)    |
| `values` | any[] | no       |         | Values for the new row             |

Returns: confirmation with inserted row number.

### Insert a column

```
excel insert_column --sheet "Revenue" --column "C" --values '["Q3", 1200, 1800]'
```

| Argument | Type   | Required | Default | Description                     |
|----------|--------|----------|---------|---------------------------------|
| `sheet`  | string | yes      |         | Sheet name                      |
| `column` | string | yes      |         | Column letter to insert at      |
| `values` | any[]  | no       |         | Values for the new column       |

Returns: confirmation with inserted column letter.

### Delete a row

```
excel delete_row --sheet "Revenue" --row 5
```

| Argument | Type   | Required | Description              |
|----------|--------|----------|--------------------------|
| `sheet`  | string | yes      | Sheet name               |
| `row`    | int    | yes      | Row number to delete     |

Returns: confirmation of deletion.

### Delete a column

```
excel delete_column --sheet "Revenue" --column "D"
```

| Argument | Type   | Required | Description              |
|----------|--------|----------|--------------------------|
| `sheet`  | string | yes      | Sheet name               |
| `column` | string | yes      | Column letter to delete  |

Returns: confirmation of deletion.

## Format

### Format a cell

```
excel format_cell --sheet "Revenue" --cell "A1" --bold true --font_size 14 --font_color "#FFFFFF" --bg_color "#4472C4" --number_format "#,##0.00" --alignment "center"
```

| Argument        | Type    | Required | Default | Description                                    |
|-----------------|---------|----------|---------|------------------------------------------------|
| `sheet`         | string  | yes      |         | Sheet name                                     |
| `cell`          | string  | yes      |         | Cell reference                                 |
| `bold`          | boolean | no       |         | Bold text                                      |
| `italic`        | boolean | no       |         | Italic text                                    |
| `font_size`     | int     | no       |         | Font size in points                            |
| `font_color`    | string  | no       |         | Font color (hex, e.g. #FF0000)                 |
| `bg_color`      | string  | no       |         | Background fill color (hex)                    |
| `number_format` | string  | no       |         | Number format string (e.g. #,##0.00, 0%, mm/dd/yyyy) |
| `alignment`     | string  | no       |         | Horizontal alignment: `left`, `center`, `right`|

Returns: confirmation with applied formats.

### Format a range

```
excel format_range --sheet "Revenue" --range "A1:D1" --bold true --bg_color "#4472C4" --font_color "#FFFFFF"
```

| Argument        | Type    | Required | Default | Description                                    |
|-----------------|---------|----------|---------|------------------------------------------------|
| `sheet`         | string  | yes      |         | Sheet name                                     |
| `range`         | string  | yes      |         | Cell range (e.g. A1:D1)                        |
| `bold`          | boolean | no       |         | Bold text                                      |
| `italic`        | boolean | no       |         | Italic text                                    |
| `font_size`     | int     | no       |         | Font size in points                            |
| `font_color`    | string  | no       |         | Font color (hex)                               |
| `bg_color`      | string  | no       |         | Background fill color (hex)                    |
| `number_format` | string  | no       |         | Number format string                           |
| `alignment`     | string  | no       |         | Horizontal alignment                           |

Returns: confirmation with applied range and formats.

### Set column width

```
excel set_column_width --sheet "Revenue" --column "A" --width 25
```

| Argument | Type   | Required | Description                     |
|----------|--------|----------|---------------------------------|
| `sheet`  | string | yes      | Sheet name                      |
| `column` | string | yes      | Column letter                   |
| `width`  | number | yes      | Width in character units        |

Returns: confirmation with column and width.

### Set row height

```
excel set_row_height --sheet "Revenue" --row 1 --height 30
```

| Argument | Type   | Required | Description                     |
|----------|--------|----------|---------------------------------|
| `sheet`  | string | yes      | Sheet name                      |
| `row`    | int    | yes      | Row number (1-based)            |
| `height` | number | yes      | Height in points                |

Returns: confirmation with row and height.

### Merge cells

```
excel merge_cells --sheet "Revenue" --range "A1:D1"
```

| Argument | Type   | Required | Description              |
|----------|--------|----------|--------------------------|
| `sheet`  | string | yes      | Sheet name               |
| `range`  | string | yes      | Range to merge           |

Returns: confirmation with merged range.

### Unmerge cells

```
excel unmerge_cells --sheet "Revenue" --range "A1:D1"
```

| Argument | Type   | Required | Description              |
|----------|--------|----------|--------------------------|
| `sheet`  | string | yes      | Sheet name               |
| `range`  | string | yes      | Range to unmerge         |

Returns: confirmation with unmerged range.

## Formulas

### Set a formula

```
excel set_formula --sheet "Revenue" --cell "D2" --formula "=SUM(B2:C2)"
```

| Argument  | Type   | Required | Description                       |
|-----------|--------|----------|-----------------------------------|
| `sheet`   | string | yes      | Sheet name                        |
| `cell`    | string | yes      | Cell reference                    |
| `formula` | string | yes      | Excel formula (e.g. =SUM(A1:A10))|

Returns: confirmation with `cell`, `formula`.

### Recalculate workbook

```
excel recalculate
```

Returns: confirmation that all formulas have been recalculated.

## Tables

### Create a table

```
excel create_table --sheet "Revenue" --range "A1:D5" --columns '["Name","Q1","Q2","Total"]' --name "RevenueTable"
```

| Argument  | Type     | Required | Default     | Description                      |
|-----------|----------|----------|-------------|----------------------------------|
| `sheet`   | string   | yes      |             | Sheet name                       |
| `range`   | string   | yes      |             | Data range including headers     |
| `columns` | string[] | yes      |             | Column header names              |
| `name`    | string   | no       | auto-generated | Table name (must be unique)   |

Returns: `name`, `range`, `column_count`, `row_count`.

### Add a row to a table

```
excel add_table_row --sheet "Revenue" --name "RevenueTable" --values '["Product D", 3000, 3500, 6500]'
```

| Argument | Type   | Required | Description                     |
|----------|--------|----------|---------------------------------|
| `sheet`  | string | yes      | Sheet name                      |
| `name`   | string | yes      | Table name                      |
| `values` | any[]  | yes      | Values for the new row          |

Returns: confirmation with `table_name` and new `row_count`.

## Charts

### Create a chart

```
excel create_chart --sheet "Revenue" --type bar --data_range "A1:C5" --title "Quarterly Revenue" --position "E2"
```

| Argument     | Type   | Required | Default | Description                                     |
|--------------|--------|----------|---------|-------------------------------------------------|
| `sheet`      | string | yes      |         | Sheet name                                      |
| `type`       | string | yes      |         | Chart type: `bar`, `line`, `pie`, `scatter`     |
| `data_range` | string | yes      |         | Source data range (e.g. A1:C5)                  |
| `title`      | string | no       |         | Chart title                                     |
| `position`   | string | no       | `E2`    | Top-left cell for chart placement               |

Returns: confirmation with `chart_type`, `data_range`, `position`.

## Export

### Export to CSV

```
excel export_csv --sheet "Revenue" --file_path "/data/exports/revenue.csv" --delimiter ","
```

| Argument    | Type   | Required | Default | Description                      |
|-------------|--------|----------|---------|----------------------------------|
| `sheet`     | string | yes      |         | Sheet name to export             |
| `file_path` | string | yes      |         | Output CSV file path             |
| `delimiter` | string | no       | `,`     | Field delimiter                  |

Returns: confirmation with `file_path`, `row_count`.

### Export to JSON

```
excel export_json --sheet "Revenue" --file_path "/data/exports/revenue.json" --headers true
```

| Argument    | Type    | Required | Default | Description                                  |
|-------------|---------|----------|---------|----------------------------------------------|
| `sheet`     | string  | yes      |         | Sheet name to export                         |
| `file_path` | string  | yes      |         | Output JSON file path                        |
| `headers`   | boolean | no       | true    | Use first row as object keys                 |

Returns: confirmation with `file_path`, `record_count`.

### Export to PDF

```
excel export_pdf --file_path "/data/exports/revenue.pdf" --sheet "Revenue" --orientation landscape
```

| Argument      | Type   | Required | Default      | Description                            |
|---------------|--------|----------|--------------|----------------------------------------|
| `file_path`   | string | yes      |              | Output PDF file path                   |
| `sheet`       | string | no       |              | Sheet to export (omit for all sheets)  |
| `orientation` | string | no       | `portrait`   | Page orientation: `portrait`, `landscape` |

Returns: confirmation with `file_path`, `page_count`.

## Filters

### Add autofilter

```
excel add_autofilter --sheet "Revenue" --range "A1:D1"
```

| Argument | Type   | Required | Description                         |
|----------|--------|----------|-------------------------------------|
| `sheet`  | string | yes      | Sheet name                          |
| `range`  | string | yes      | Header range for autofilter         |

Returns: confirmation with filter range.

### Sort a column

```
excel sort_column --sheet "Revenue" --column "B" --order descending --range "A1:D50"
```

| Argument | Type   | Required | Default      | Description                        |
|----------|--------|----------|--------------|------------------------------------|
| `sheet`  | string | yes      |              | Sheet name                         |
| `column` | string | yes      |              | Column letter to sort by           |
| `order`  | string | no       | `ascending`  | Sort order: `ascending`, `descending` |
| `range`  | string | no       |              | Limit sort to a specific range     |

Returns: confirmation with sorted column and row count.

## Workflow

1. Start with `excel create_workbook` or `excel open_workbook` to get a workbook handle.
2. Use `excel list_sheets` and `excel get_sheet_info` to understand the workbook structure.
3. Read data with `excel get_range` or `excel get_all_data` before making modifications.
4. Write data with `excel set_cell`, `excel set_range`, or `excel insert_row`.
5. Apply formatting with `excel format_range` for headers and `excel set_column_width` for readability.
6. Add formulas with `excel set_formula` for computed values.
7. Create tables with `excel create_table` for structured data with filtering.
8. Visualize data with `excel create_chart`.
9. Always `excel save_workbook` after making changes.
10. Export to CSV, JSON, or PDF for sharing.

## Safety notes

- Always `open_workbook` before reading or modifying an existing file. Operations on an unopened workbook will fail.
- `save_workbook` overwrites the target file. Use a different path to avoid overwriting the original.
- `delete_row` and `delete_column` shift surrounding data. Verify the target before deleting.
- `remove_sheet` is permanent. There is no undo. Confirm with the user before removing sheets with data.
- Large workbooks (100k+ cells) may be slow to read. Use `get_range` on specific regions instead of `get_all_data`.
- Formula syntax follows Excel conventions (e.g. `=SUM()`, `=VLOOKUP()`). Invalid formulas will produce `#ERROR` values.
- Chart types are limited to `bar`, `line`, `pie`, and `scatter`. Subtypes (stacked, 3D) are not supported.
- File paths must be accessible from the agent's workspace. Use absolute paths for reliability.
