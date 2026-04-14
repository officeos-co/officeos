# PostgreSQL

Full PostgreSQL database management: connect, query, inspect schemas, manage tables, manipulate data, run migrations, and export results via the node-postgres (pg) driver.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Connection

### Connect

```
postgres connect --host localhost --port 5432 --database myapp --user admin --password secret --ssl true
```

| Argument   | Type    | Required | Default     | Description                 |
| ---------- | ------- | -------- | ----------- | --------------------------- |
| `host`     | string  | no       | `localhost` | Database server hostname    |
| `port`     | int     | no       | 5432        | Database server port        |
| `database` | string  | yes      |             | Database name to connect to |
| `user`     | string  | yes      |             | Authentication username     |
| `password` | string  | yes      |             | Authentication password     |
| `ssl`      | boolean | no       | false       | Enable SSL/TLS connection   |

Returns: `connected`, `server_version`, `database`, `user`.

## Querying

### Run a query

```
postgres query --sql "SELECT id, name, email FROM users WHERE active = true LIMIT 10"
```

| Argument | Type   | Required | Default | Description                                           |
| -------- | ------ | -------- | ------- | ----------------------------------------------------- |
| `sql`    | string | yes      |         | SQL statement to execute                              |
| `params` | string | no       |         | JSON array of parameterized values (e.g. `'[1,"a"]'`) |

Returns: `rows` (array of objects), `row_count`, `fields` (column names and types).

### Query single row

```
postgres query_one --sql "SELECT * FROM users WHERE id = $1" --params '[42]'
```

| Argument | Type   | Required | Default | Description                              |
| -------- | ------ | -------- | ------- | ---------------------------------------- |
| `sql`    | string | yes      |         | SQL statement expected to return one row |
| `params` | string | no       |         | JSON array of parameterized values       |

Returns: single row object, or `null` if no match.

## Schema inspection

### List databases

```
postgres list_databases
```

Returns: `name`, `owner`, `encoding`, `size` for each database on the server.

### List tables

```
postgres list_tables --schema public
```

| Argument | Type   | Required | Default  | Description            |
| -------- | ------ | -------- | -------- | ---------------------- |
| `schema` | string | no       | `public` | Schema name to inspect |

Returns: `table_name`, `table_type` (`BASE TABLE` or `VIEW`), `row_estimate`.

### List columns

```
postgres list_columns --table users --schema public
```

| Argument | Type   | Required | Default  | Description |
| -------- | ------ | -------- | -------- | ----------- |
| `table`  | string | yes      |          | Table name  |
| `schema` | string | no       | `public` | Schema name |

Returns: `column_name`, `data_type`, `is_nullable`, `column_default`, `character_maximum_length`.

### List indexes

```
postgres list_indexes --table users --schema public
```

| Argument | Type   | Required | Default  | Description |
| -------- | ------ | -------- | -------- | ----------- |
| `table`  | string | yes      |          | Table name  |
| `schema` | string | no       | `public` | Schema name |

Returns: `index_name`, `is_unique`, `is_primary`, `columns`, `index_type`.

## Table operations

### Create table

```
postgres create_table --table orders --columns '[{"name":"id","type":"SERIAL PRIMARY KEY"},{"name":"user_id","type":"INTEGER NOT NULL"},{"name":"total","type":"NUMERIC(10,2)"},{"name":"created_at","type":"TIMESTAMPTZ DEFAULT NOW()"}]'
```

| Argument        | Type    | Required | Default  | Description                                     |
| --------------- | ------- | -------- | -------- | ----------------------------------------------- |
| `table`         | string  | yes      |          | New table name                                  |
| `columns`       | string  | yes      |          | JSON array of `{name, type}` column definitions |
| `schema`        | string  | no       | `public` | Schema to create in                             |
| `if_not_exists` | boolean | no       | false    | Skip if table already exists                    |

Returns: `created`, `table_name`.

### Drop table

```
postgres drop_table --table temp_data --cascade true
```

| Argument  | Type    | Required | Default  | Description                 |
| --------- | ------- | -------- | -------- | --------------------------- |
| `table`   | string  | yes      |          | Table to drop               |
| `schema`  | string  | no       | `public` | Schema containing the table |
| `cascade` | boolean | no       | false    | Also drop dependent objects |

Returns: `dropped`, `table_name`.

### Alter table

```
postgres alter_table --table users --action add_column --column_name phone --column_type VARCHAR(20)
```

```
postgres alter_table --table users --action drop_column --column_name phone
```

```
postgres alter_table --table users --action rename_column --column_name email --new_name email_address
```

| Argument      | Type   | Required | Default  | Description                                     |
| ------------- | ------ | -------- | -------- | ----------------------------------------------- |
| `table`       | string | yes      |          | Table to alter                                  |
| `schema`      | string | no       | `public` | Schema containing the table                     |
| `action`      | string | yes      |          | `add_column`, `drop_column`, or `rename_column` |
| `column_name` | string | yes      |          | Column to add, drop, or rename                  |
| `column_type` | string | cond.    |          | Column type (required for `add_column`)         |
| `new_name`    | string | cond.    |          | New column name (required for `rename_column`)  |

Returns: `altered`, `table_name`, `action`.

## Data operations

### Insert

```
postgres insert --table users --data '{"name":"Alice","email":"alice@example.com","active":true}'
```

| Argument    | Type   | Required | Default  | Description                              |
| ----------- | ------ | -------- | -------- | ---------------------------------------- |
| `table`     | string | yes      |          | Target table                             |
| `schema`    | string | no       | `public` | Schema containing the table              |
| `data`      | string | yes      |          | JSON object of column-value pairs        |
| `returning` | string | no       | `*`      | Columns to return (e.g. `id,created_at`) |

Returns: the inserted row (columns specified by `returning`).

### Update

```
postgres update --table users --set '{"active":false}' --where "last_login < '2025-01-01'"
```

| Argument    | Type   | Required | Default  | Description                                |
| ----------- | ------ | -------- | -------- | ------------------------------------------ |
| `table`     | string | yes      |          | Target table                               |
| `schema`    | string | no       | `public` | Schema containing the table                |
| `set`       | string | yes      |          | JSON object of columns to update           |
| `where`     | string | yes      |          | WHERE clause (without the `WHERE` keyword) |
| `returning` | string | no       |          | Columns to return from updated rows        |

Returns: `updated_count`, `rows` (if `returning` is set).

### Delete

```
postgres delete --table sessions --where "expires_at < NOW()"
```

| Argument    | Type   | Required | Default  | Description                                |
| ----------- | ------ | -------- | -------- | ------------------------------------------ |
| `table`     | string | yes      |          | Target table                               |
| `schema`    | string | no       | `public` | Schema containing the table                |
| `where`     | string | yes      |          | WHERE clause (without the `WHERE` keyword) |
| `returning` | string | no       |          | Columns to return from deleted rows        |

Returns: `deleted_count`, `rows` (if `returning` is set).

### Upsert

```
postgres upsert --table users --data '{"email":"alice@example.com","name":"Alice B."}' --conflict_columns '["email"]' --update_columns '["name"]'
```

| Argument           | Type   | Required | Default  | Description                                         |
| ------------------ | ------ | -------- | -------- | --------------------------------------------------- |
| `table`            | string | yes      |          | Target table                                        |
| `schema`           | string | no       | `public` | Schema containing the table                         |
| `data`             | string | yes      |          | JSON object of column-value pairs                   |
| `conflict_columns` | string | yes      |          | JSON array of columns forming the unique constraint |
| `update_columns`   | string | yes      |          | JSON array of columns to update on conflict         |
| `returning`        | string | no       | `*`      | Columns to return                                   |

Returns: the upserted row.

## Migrations

### Run migration

```
postgres run_migration --sql "ALTER TABLE users ADD COLUMN avatar_url TEXT; CREATE INDEX idx_users_email ON users (email);" --name "002_add_avatar_and_index"
```

| Argument | Type   | Required | Description                         |
| -------- | ------ | -------- | ----------------------------------- |
| `sql`    | string | yes      | SQL migration statements to execute |
| `name`   | string | yes      | Unique migration name for tracking  |

Returns: `applied`, `migration_name`, `duration_ms`.

### List migrations

```
postgres list_migrations
```

Returns: `name`, `applied_at`, `duration_ms` for each previously applied migration.

## Transactions

### Begin transaction

```
postgres begin_transaction --isolation_level serializable
```

| Argument          | Type   | Required | Default          | Description                                            |
| ----------------- | ------ | -------- | ---------------- | ------------------------------------------------------ |
| `isolation_level` | string | no       | `read_committed` | `read_committed`, `repeatable_read`, or `serializable` |

Returns: `transaction_id`.

### Commit

```
postgres commit --transaction_id txn_abc123
```

| Argument         | Type   | Required | Description           |
| ---------------- | ------ | -------- | --------------------- |
| `transaction_id` | string | yes      | Transaction to commit |

Returns: `committed`, `transaction_id`.

### Rollback

```
postgres rollback --transaction_id txn_abc123
```

| Argument         | Type   | Required | Description              |
| ---------------- | ------ | -------- | ------------------------ |
| `transaction_id` | string | yes      | Transaction to roll back |

Returns: `rolled_back`, `transaction_id`.

## Server info

### Table info

```
postgres table_info --table users --schema public
```

| Argument | Type   | Required | Default  | Description |
| -------- | ------ | -------- | -------- | ----------- |
| `table`  | string | yes      |          | Table name  |
| `schema` | string | no       | `public` | Schema name |

Returns: `table_name`, `row_count`, `total_size`, `index_size`, `toast_size`, `table_size`.

### Database size

```
postgres database_size
```

Returns: `database`, `size`, `pretty_size`.

### Active connections

```
postgres active_connections
```

Returns: `pid`, `user`, `database`, `client_addr`, `state`, `query`, `query_start` for each active connection.

## Export

### Export to CSV

```
postgres export_csv --sql "SELECT * FROM users WHERE active = true" --file_name users_export.csv
```

| Argument    | Type    | Required | Default      | Description            |
| ----------- | ------- | -------- | ------------ | ---------------------- |
| `sql`       | string  | yes      |              | SELECT query to export |
| `file_name` | string  | no       | `export.csv` | Output file name       |
| `delimiter` | string  | no       | `,`          | Column delimiter       |
| `headers`   | boolean | no       | true         | Include column headers |

Returns: `file_path`, `row_count`, `size_bytes`.

### Export to JSON

```
postgres export_json --sql "SELECT * FROM orders WHERE total > 100" --file_name big_orders.json
```

| Argument    | Type    | Required | Default       | Description              |
| ----------- | ------- | -------- | ------------- | ------------------------ |
| `sql`       | string  | yes      |               | SELECT query to export   |
| `file_name` | string  | no       | `export.json` | Output file name         |
| `pretty`    | boolean | no       | false         | Pretty-print JSON output |

Returns: `file_path`, `row_count`, `size_bytes`.

## Workflow

1. **Always start with `postgres connect`** to establish a connection to the target database.
2. Use `list_tables` and `list_columns` to explore the schema before writing queries.
3. Use parameterized `query` with `--params` for any user-supplied values to prevent SQL injection.
4. For bulk schema changes, wrap operations in a transaction: `begin_transaction` -> queries -> `commit` (or `rollback` on failure).
5. Track schema changes with `run_migration` so they can be audited via `list_migrations`.
6. Use `table_info` and `database_size` to monitor storage before large operations.
7. Use `export_csv` or `export_json` to extract data for downstream processing.

## Safety notes

- **Always use parameterized queries.** Pass user-supplied values via `--params`, never by string concatenation into `--sql`.
- `delete` and `update` require a `--where` clause. There is no way to run them without a filter to prevent accidental full-table mutations.
- `drop_table` with `--cascade true` will destroy dependent views, foreign keys, and other objects. Confirm with the user first.
- Credentials are injected by the backend. Agents never see raw connection passwords.
- `run_migration` records each migration name. Re-running an already-applied migration will be rejected.
- Transactions that are not committed or rolled back will be automatically rolled back after a timeout.
- Large `export_csv` or `export_json` results are streamed. For very large tables, add a `LIMIT` or filter to the `--sql` query.
- The `query` action executes arbitrary SQL. Avoid DDL statements outside of `run_migration` for auditability.
