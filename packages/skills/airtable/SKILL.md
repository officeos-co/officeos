# Airtable

Manage Airtable bases, tables, fields, and records via the Airtable REST API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Bases

### List bases

```
airtable list_bases
```

No arguments required.

Returns: array of `{id, name, permission_level}`.

### Get base schema

```
airtable get_schema --base_id "appXXXXXXXXXXXXXX"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `base_id` | string | yes      | Base ID     |

Returns: `id`, `name`, `tables` (array of `{id, name, fields, views}`).

## Tables

### List tables

```
airtable list_tables --base_id "appXXXXXXXXXXXXXX"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `base_id` | string | yes      | Base ID     |

Returns: array of `{id, name, description, fields, views, primary_field_id}`.

### Create table

```
airtable create_table --base_id "appXXXXXXXXXXXXXX" --name "Projects" --description "Active projects" --fields '[{"name":"Name","type":"singleLineText"},{"name":"Status","type":"singleSelect","options":{"choices":[{"name":"Active"},{"name":"Done"}]}}]'
```

| Argument      | Type   | Required | Description                                  |
| ------------- | ------ | -------- | -------------------------------------------- |
| `base_id`     | string | yes      | Base ID                                      |
| `name`        | string | yes      | Table name                                   |
| `description` | string | no       | Table description                            |
| `fields`      | array  | no       | Initial field definitions (JSON array)       |

Returns: `{id, name, description, fields, views, primary_field_id}`.

### Update table

```
airtable update_table --base_id "appXXXXXXXXXXXXXX" --table_id "tblXXXXXXXXXXXXXX" --name "Renamed Projects"
```

| Argument      | Type   | Required | Description          |
| ------------- | ------ | -------- | -------------------- |
| `base_id`     | string | yes      | Base ID              |
| `table_id`    | string | yes      | Table ID             |
| `name`        | string | no       | New table name       |
| `description` | string | no       | Updated description  |

Returns: `{id, name, description}`.

## Fields

### Create field

```
airtable create_field --base_id "appXXXXXXXXXXXXXX" --table_id "tblXXXXXXXXXXXXXX" --name "Due Date" --type date --options '{"dateFormat":{"name":"iso"}}'
```

| Argument   | Type   | Required | Description                                                                      |
| ---------- | ------ | -------- | -------------------------------------------------------------------------------- |
| `base_id`  | string | yes      | Base ID                                                                          |
| `table_id` | string | yes      | Table ID                                                                         |
| `name`     | string | yes      | Field name                                                                       |
| `type`     | string | yes      | Field type: `singleLineText`, `multilineText`, `number`, `currency`, `percent`, `date`, `dateTime`, `checkbox`, `singleSelect`, `multipleSelects`, `email`, `url`, `phoneNumber`, `rating`, `duration`, `autoNumber` |
| `options`  | object | no       | Type-specific options (see Airtable field type docs)                             |
| `description` | string | no    | Field description                                                                |

Returns: `{id, name, type, options, description}`.

### Update field

```
airtable update_field --base_id "appXXXXXXXXXXXXXX" --table_id "tblXXXXXXXXXXXXXX" --field_id "fldXXXXXXXXXXXXXX" --name "Deadline" --description "Hard deadline"
```

| Argument      | Type   | Required | Description         |
| ------------- | ------ | -------- | ------------------- |
| `base_id`     | string | yes      | Base ID             |
| `table_id`    | string | yes      | Table ID            |
| `field_id`    | string | yes      | Field ID            |
| `name`        | string | no       | New field name      |
| `description` | string | no       | Updated description |

Returns: `{id, name, type, description}`.

## Records

### List records

```
airtable list_records --base_id "appXXXXXXXXXXXXXX" --table_id "tblXXXXXXXXXXXXXX" --view "Grid view" --max_records 100
```

| Argument      | Type     | Required | Default | Description                                              |
| ------------- | -------- | -------- | ------- | -------------------------------------------------------- |
| `base_id`     | string   | yes      |         | Base ID                                                  |
| `table_id`    | string   | yes      |         | Table ID or table name                                   |
| `view`        | string   | no       |         | View name or ID to use                                   |
| `fields`      | string[] | no       |         | Field names to include (omit for all)                    |
| `filter`      | string   | no       |         | Airtable formula filter, e.g. `{Status}="Active"`        |
| `sort`        | array    | no       |         | Sort spec: `[{"field":"Name","direction":"asc"}]`         |
| `max_records` | int      | no       | 100     | Maximum records to return (Airtable max 100 per page, auto-paginated up to this limit) |
| `offset`      | string   | no       |         | Pagination cursor from a previous response               |

Returns: `records` (array of `{id, fields, created_time}`), `offset` (pagination cursor if more records exist).

### Get record

```
airtable get_record --base_id "appXXXXXXXXXXXXXX" --table_id "tblXXXXXXXXXXXXXX" --record_id "recXXXXXXXXXXXXXX"
```

| Argument    | Type   | Required | Description |
| ----------- | ------ | -------- | ----------- |
| `base_id`   | string | yes      | Base ID     |
| `table_id`  | string | yes      | Table ID    |
| `record_id` | string | yes      | Record ID   |

Returns: `{id, fields, created_time}`.

### Create record

```
airtable create_record --base_id "appXXXXXXXXXXXXXX" --table_id "tblXXXXXXXXXXXXXX" --fields '{"Name":"New Project","Status":"Active","Due Date":"2026-06-01"}'
```

| Argument   | Type   | Required | Description                               |
| ---------- | ------ | -------- | ----------------------------------------- |
| `base_id`  | string | yes      | Base ID                                   |
| `table_id` | string | yes      | Table ID                                  |
| `fields`   | object | yes      | Field name/value pairs for the new record |

Returns: `{id, fields, created_time}`.

### Update record

```
airtable update_record --base_id "appXXXXXXXXXXXXXX" --table_id "tblXXXXXXXXXXXXXX" --record_id "recXXXXXXXXXXXXXX" --fields '{"Status":"Done"}' --merge true
```

| Argument    | Type    | Required | Default | Description                                         |
| ----------- | ------- | -------- | ------- | --------------------------------------------------- |
| `base_id`   | string  | yes      |         | Base ID                                             |
| `table_id`  | string  | yes      |         | Table ID                                            |
| `record_id` | string  | yes      |         | Record ID                                           |
| `fields`    | object  | yes      |         | Field values to update                              |
| `merge`     | boolean | no       | true    | If true, PATCH (merge); if false, PUT (replace all) |

Returns: `{id, fields, created_time}`.

### Delete record

```
airtable delete_record --base_id "appXXXXXXXXXXXXXX" --table_id "tblXXXXXXXXXXXXXX" --record_id "recXXXXXXXXXXXXXX"
```

| Argument    | Type   | Required | Description |
| ----------- | ------ | -------- | ----------- |
| `base_id`   | string | yes      | Base ID     |
| `table_id`  | string | yes      | Table ID    |
| `record_id` | string | yes      | Record ID   |

Returns: `{id, deleted}`.

## Batch Operations

### Batch create records

```
airtable batch_create --base_id "appXXXXXXXXXXXXXX" --table_id "tblXXXXXXXXXXXXXX" --records '[{"Name":"Alpha","Status":"Active"},{"Name":"Beta","Status":"Active"}]'
```

| Argument   | Type  | Required | Description                                                      |
| ---------- | ----- | -------- | ---------------------------------------------------------------- |
| `base_id`  | string| yes      | Base ID                                                          |
| `table_id` | string| yes      | Table ID                                                         |
| `records`  | array | yes      | Array of field objects (max 10 per request, auto-batched)        |

Returns: array of `{id, fields, created_time}`.

### Batch update records

```
airtable batch_update --base_id "appXXXXXXXXXXXXXX" --table_id "tblXXXXXXXXXXXXXX" --records '[{"id":"recXXXXXXXXXXXXXX","fields":{"Status":"Done"}}]'
```

| Argument   | Type  | Required | Description                                                          |
| ---------- | ----- | -------- | -------------------------------------------------------------------- |
| `base_id`  | string| yes      | Base ID                                                              |
| `table_id` | string| yes      | Table ID                                                             |
| `records`  | array | yes      | Array of `{id, fields}` objects (max 10 per request, auto-batched)   |
| `merge`    | boolean| no      | If true, PATCH; if false, PUT (default: true)                        |

Returns: array of `{id, fields, created_time}`.

### Batch delete records

```
airtable batch_delete --base_id "appXXXXXXXXXXXXXX" --table_id "tblXXXXXXXXXXXXXX" --record_ids '["recXXXXXX","recYYYYYY"]'
```

| Argument     | Type     | Required | Description                                                     |
| ------------ | -------- | -------- | --------------------------------------------------------------- |
| `base_id`    | string   | yes      | Base ID                                                         |
| `table_id`   | string   | yes      | Table ID                                                        |
| `record_ids` | string[] | yes      | Record IDs to delete (max 10 per request, auto-batched)         |

Returns: array of `{id, deleted}`.

## Workflow

1. Use `list_bases` to find your base IDs, then `get_schema` to inspect table and field structure.
2. Use `list_records` with a `filter` formula to find records matching criteria.
3. Use `create_record` for single inserts or `batch_create` for bulk imports.
4. Use `update_record` (with `merge: true`) to patch specific fields without overwriting others.
5. Use `batch_update` for updating many records at once.
6. Use `delete_record` or `batch_delete` to clean up records.

## Safety notes

- All IDs (base, table, field, record) are immutable Airtable-assigned strings starting with `app`, `tbl`, `fld`, `rec`.
- `delete_record` and `batch_delete` are **permanent** — Airtable has no recycle bin.
- `update_record` with `merge: false` (PUT) will **clear all fields not included** in the payload. Use PATCH (`merge: true`) unless a full replacement is intentional.
- Airtable API rate limit: 5 requests/second per base. Batch operations auto-respect this.
- Filter formulas use Airtable formula syntax: `AND({Status}="Active",{Priority}="High")`.
- Field names in `filter` formulas are case-sensitive and must match the table schema exactly.
