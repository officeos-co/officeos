# CSV

Parse, transform, and analyse CSV data via a file-proxy service. Supports parsing CSV text or files, filtering and sorting rows, adding columns, merging datasets, and computing statistics.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Parsing & Serialising

### Parse CSV

```
csv parse --content "name,age\nAlice,30\nBob,25" --delimiter "," --has_header true
```

| Argument      | Type    | Required | Default | Description                                        |
| ------------- | ------- | -------- | ------- | -------------------------------------------------- |
| `content`     | string  | yes      |         | Raw CSV text to parse                              |
| `delimiter`   | string  | no       | `,`     | Field delimiter character                          |
| `has_header`  | boolean | no       | true    | Treat first row as column headers                  |
| `skip_empty`  | boolean | no       | true    | Skip empty rows                                    |
| `trim`        | boolean | no       | true    | Trim whitespace from values                        |

Returns: `file_id`, `row_count`, `column_count`, `columns` (array of header names).

### Parse file

```
csv parse_file --file_url "https://example.com/data.csv" --delimiter ","
```

| Argument     | Type    | Required | Default | Description                     |
| ------------ | ------- | -------- | ------- | ------------------------------- |
| `file_url`   | string  | yes      |         | URL of CSV file to download     |
| `delimiter`  | string  | no       | `,`     | Field delimiter character       |
| `has_header` | boolean | no       | true    | Treat first row as headers      |
| `skip_empty` | boolean | no       | true    | Skip empty rows                 |

Returns: `file_id`, `row_count`, `column_count`, `columns`.

### Stringify

```
csv stringify --file_id "abc123" --delimiter "," --include_header true
```

| Argument         | Type    | Required | Default | Description                          |
| ---------------- | ------- | -------- | ------- | ------------------------------------ |
| `file_id`        | string  | yes      |         | File ID of in-memory dataset         |
| `delimiter`      | string  | no       | `,`     | Output field delimiter               |
| `include_header` | boolean | no       | true    | Include header row in output         |
| `quote_all`      | boolean | no       | false   | Force quote all fields               |

Returns: `content` (CSV string), `row_count`, `column_count`.

## Inspection

### Get columns

```
csv get_columns --file_id "abc123"
```

| Argument  | Type   | Required | Description         |
| --------- | ------ | -------- | ------------------- |
| `file_id` | string | yes      | File ID             |

Returns: `columns` (array of column names), `column_count`, `row_count`.

### Get rows

```
csv get_rows --file_id "abc123" --offset 0 --limit 50
```

| Argument  | Type   | Required | Default | Description                  |
| --------- | ------ | -------- | ------- | ---------------------------- |
| `file_id` | string | yes      |         | File ID                      |
| `offset`  | int    | no       | 0       | Row offset (0-based)         |
| `limit`   | int    | no       | 100     | Maximum rows to return       |

Returns: `rows` (array of row objects), `total_rows`, `offset`, `limit`.

### Stats

```
csv stats --file_id "abc123" --columns '["age","salary"]'
```

| Argument   | Type     | Required | Description                                          |
| ---------- | -------- | -------- | ---------------------------------------------------- |
| `file_id`  | string   | yes      | File ID                                              |
| `columns`  | string[] | no       | Columns to compute stats for (omit for all numeric)  |

Returns: per-column `min`, `max`, `mean`, `median`, `stddev`, `null_count`, `unique_count`.

## Transformation

### Filter rows

```
csv filter_rows --file_id "abc123" --column "age" --operator ">=" --value "25"
```

| Argument   | Type   | Required | Description                                                                 |
| ---------- | ------ | -------- | --------------------------------------------------------------------------- |
| `file_id`  | string | yes      | File ID                                                                     |
| `column`   | string | yes      | Column name to filter on                                                    |
| `operator` | string | yes      | `=`, `!=`, `>`, `>=`, `<`, `<=`, `contains`, `starts_with`, `ends_with`, `is_empty`, `is_not_empty` |
| `value`    | string | no       | Comparison value (omit for `is_empty`/`is_not_empty`)                       |

Returns: new `file_id`, `row_count`, `column_count`.

### Sort

```
csv sort --file_id "abc123" --column "age" --direction desc
```

| Argument    | Type   | Required | Default | Description                    |
| ----------- | ------ | -------- | ------- | ------------------------------ |
| `file_id`   | string | yes      |         | File ID                        |
| `column`    | string | yes      |         | Column to sort by              |
| `direction` | string | no       | `asc`   | `asc` or `desc`                |
| `numeric`   | boolean| no       | false   | Sort as numbers instead of strings |

Returns: new `file_id`, `row_count`.

### Add column

```
csv add_column --file_id "abc123" --name "full_name" --expression "{{first_name}} {{last_name}}"
```

| Argument     | Type   | Required | Description                                                            |
| ------------ | ------ | -------- | ---------------------------------------------------------------------- |
| `file_id`    | string | yes      | File ID                                                                |
| `name`       | string | yes      | New column name                                                        |
| `expression` | string | no       | Template using `{{col_name}}` placeholders or a constant value        |
| `values`     | string[]| no      | Array of values, one per row (must match row count)                   |
| `default`    | string | no       | Default value for all rows (if neither expression nor values given)   |

Returns: new `file_id`, `columns`, `row_count`.

### Rename column

```
csv rename_column --file_id "abc123" --old_name "first_name" --new_name "firstName"
```

| Argument   | Type   | Required | Description         |
| ---------- | ------ | -------- | ------------------- |
| `file_id`  | string | yes      | File ID             |
| `old_name` | string | yes      | Existing column name|
| `new_name` | string | yes      | New column name     |

Returns: `file_id`, `columns`.

### Drop columns

```
csv drop_columns --file_id "abc123" --columns '["ssn","internal_id"]'
```

| Argument   | Type     | Required | Description              |
| ---------- | -------- | -------- | ------------------------ |
| `file_id`  | string   | yes      | File ID                  |
| `columns`  | string[] | yes      | Column names to remove   |

Returns: new `file_id`, `columns`, `row_count`.

### Transform

```
csv transform --file_id "abc123" --column "email" --operation lowercase
```

| Argument    | Type   | Required | Description                                                               |
| ----------- | ------ | -------- | ------------------------------------------------------------------------- |
| `file_id`   | string | yes      | File ID                                                                   |
| `column`    | string | yes      | Column to transform                                                       |
| `operation` | string | yes      | `uppercase`, `lowercase`, `trim`, `number`, `boolean`, `date_iso`, `replace` |
| `find`      | string | no       | For `replace`: string to find                                             |
| `replace`   | string | no       | For `replace`: replacement string                                         |

Returns: new `file_id`, `row_count`.

### Deduplicate

```
csv deduplicate --file_id "abc123" --columns '["email"]'
```

| Argument   | Type     | Required | Description                                         |
| ---------- | -------- | -------- | --------------------------------------------------- |
| `file_id`  | string   | yes      | File ID                                             |
| `columns`  | string[] | no       | Columns to consider for deduplication (omit = all)  |
| `keep`     | string   | no       | `first` or `last` (default: `first`)                |

Returns: new `file_id`, `row_count`, `removed_count`.

## Combining

### Merge

```
csv merge --file_ids '["abc123","def456"]' --how union
```

| Argument   | Type     | Required | Default  | Description                                          |
| ---------- | -------- | -------- | -------- | ---------------------------------------------------- |
| `file_ids` | string[] | yes      |          | File IDs of datasets to merge                        |
| `how`      | string   | no       | `union`  | `union` (stack rows), `join_inner`, `join_left`      |
| `on`       | string   | no       |          | Column name to join on (required for join operations)|

Returns: new `file_id`, `row_count`, `column_count`, `columns`.

## Export

### To JSON

```
csv to_json --file_id "abc123" --orientation records
```

| Argument      | Type   | Required | Default   | Description                                             |
| ------------- | ------ | -------- | --------- | ------------------------------------------------------- |
| `file_id`     | string | yes      |           | File ID                                                 |
| `orientation` | string | no       | `records` | `records` (array of objects) or `columns` (col arrays) |

Returns: `data` (JSON array or object), `row_count`, `column_count`.

### Download

```
csv download --file_id "abc123"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `file_id` | string | yes      | File ID     |

Returns: `download_url`, `filename`, `size_bytes`, `expires_at`.

## Workflow

1. Use `parse` to load inline CSV text or `parse_file` to fetch from a URL.
2. Use `get_columns` and `stats` to understand the dataset structure.
3. Clean data with `filter_rows`, `deduplicate`, `transform`, and `drop_columns`.
4. Enrich with `add_column` using template expressions.
5. Sort with `sort` before exporting.
6. Combine multiple datasets with `merge`.
7. Export with `stringify`, `to_json`, or `download`.

## Safety notes

- All transformation operations return a **new `file_id`** — they do not mutate the source. Chain operations by using the returned `file_id` as input to the next step.
- `file_id` handles are scoped to the proxy session — they are not persistent across proxy restarts.
- Very large CSV files (> 100 MB) may time out or be rejected by the proxy. Split them first if needed.
- `stats` only computes meaningful numeric statistics for columns that parse as numbers; string columns will have `null` for `min`/`max`/`mean`/`median`/`stddev`.
