# MySQL

Full MySQL database management: connect, query, inspect schemas, manage tables, manipulate data, handle transactions, and export results via the mysql2 driver.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Connection

### Connect

```
mysql connect --host localhost --port 3306 --database myapp --user admin --password secret --ssl true
```

| Argument   | Type    | Required | Default     | Description                 |
| ---------- | ------- | -------- | ----------- | --------------------------- |
| `host`     | string  | no       | `localhost` | Database server hostname    |
| `port`     | int     | no       | 3306        | Database server port        |
| `database` | string  | yes      |             | Database name to connect to |
| `user`     | string  | yes      |             | Authentication username     |
| `password` | string  | yes      |             | Authentication password     |
| `ssl`      | boolean | no       | false       | Enable SSL/TLS connection   |

Returns: `connected`, `server_version`, `database`, `user`.

## Querying

### Run a query

```
mysql query --sql "SELECT id, name, email FROM users WHERE active = 1 LIMIT 10"
```

| Argument | Type   | Required | Default | Description                                           |
| -------- | ------ | -------- | ------- | ----------------------------------------------------- |
| `sql`    | string | yes      |         | SQL statement to execute                              |
| `params` | string | no       |         | JSON array of parameterized values (e.g. `'[1,"a"]'`) |

Returns: `rows` (array of objects), `row_count`, `fields` (column names and types).

### Query single row

```
mysql query_one --sql "SELECT * FROM users WHERE id = ?" --params '[42]'
```

| Argument | Type   | Required | Default | Description                              |
| -------- | ------ | -------- | ------- | ---------------------------------------- |
| `sql`    | string | yes      |         | SQL statement expected to return one row |
| `params` | string | no       |         | JSON array of parameterized values       |

Returns: single row object, or `null` if no match.

## Schema inspection

### List databases

```
mysql list_databases
```

Returns: `name`, `size` for each database on the server.

### List tables

```
mysql list_tables --database myapp
```

| Argument   | Type   | Required | Default            | Description              |
| ---------- | ------ | -------- | ------------------ | ------------------------ |
| `database` | string | no       | configured default | Database name to inspect |

Returns: `table_name`, `table_type` (`BASE TABLE` or `VIEW`), `row_estimate`, `engine`.

### List columns

```
mysql list_columns --table users --database myapp
```

| Argument   | Type   | Required | Default            | Description   |
| ---------- | ------ | -------- | ------------------ | ------------- |
| `table`    | string | yes      |                    | Table name    |
| `database` | string | no       | configured default | Database name |

Returns: `column_name`, `data_type`, `is_nullable`, `column_default`, `column_key`, `extra`, `character_maximum_length`.

### List indexes

```
mysql list_indexes --table users --database myapp
```

| Argument   | Type   | Required | Default            | Description |
| ---------- | ------ | -------- | ------------------ | ----------- |
| `table`    | string | yes      |                    | Table name  |
| `database` | string | no       | configured default | Database name |

Returns: `index_name`, `is_unique`, `columns`, `index_type`.

## Table operations

### Create table

```
mysql create_table --table orders --columns '[{"name":"id","type":"INT AUTO_INCREMENT PRIMARY KEY"},{"name":"user_id","type":"INT NOT NULL"},{"name":"total","type":"DECIMAL(10,2)"},{"name":"created_at","type":"TIMESTAMP DEFAULT CURRENT_TIMESTAMP"}]' --engine InnoDB
```

| Argument        | Type    | Required | Default            | Description                                     |
| --------------- | ------- | -------- | ------------------ | ----------------------------------------------- |
| `table`         | string  | yes      |                    | New table name                                  |
| `columns`       | string  | yes      |                    | JSON array of `{name, type}` column definitions |
| `database`      | string  | no       | configured default | Database to create in                           |
| `engine`        | string  | no       | `InnoDB`           | Storage engine                                  |
| `if_not_exists` | boolean | no       | false              | Skip if table already exists                    |

Returns: `created`, `table_name`.

### Drop table

```
mysql drop_table --table temp_data
```

| Argument        | Type    | Required | Default            | Description                 |
| --------------- | ------- | -------- | ------------------ | --------------------------- |
| `table`         | string  | yes      |                    | Table to drop               |
| `database`      | string  | no       | configured default | Database containing the table |
| `if_exists`     | boolean | no       | false              | Skip if table does not exist |

Returns: `dropped`, `table_name`.

### Alter table

```
mysql alter_table --table users --action add_column --column_name phone --column_type VARCHAR(20)
```

```
mysql alter_table --table users --action drop_column --column_name phone
```

```
mysql alter_table --table users --action rename_column --column_name email --new_name email_address
```

| Argument      | Type   | Required | Default            | Description                                     |
| ------------- | ------ | -------- | ------------------ | ----------------------------------------------- |
| `table`       | string | yes      |                    | Table to alter                                  |
| `database`    | string | no       | configured default | Database containing the table                   |
| `action`      | string | yes      |                    | `add_column`, `drop_column`, or `rename_column` |
| `column_name` | string | yes      |                    | Column to add, drop, or rename                  |
| `column_type` | string | cond.    |                    | Column type (required for `add_column`)         |
| `new_name`    | string | cond.    |                    | New column name (required for `rename_column`)  |

Returns: `altered`, `table_name`, `action`.

## Data operations

### Insert

```
mysql insert --table users --data '{"name":"Alice","email":"alice@example.com","active":1}'
```

| Argument   | Type   | Required | Default            | Description                       |
| ---------- | ------ | -------- | ------------------ | --------------------------------- |
| `table`    | string | yes      |                    | Target table                      |
| `database` | string | no       | configured default | Database containing the table     |
| `data`     | string | yes      |                    | JSON object of column-value pairs |

Returns: `inserted_id`, `affected_rows`.

### Update

```
mysql update --table users --set '{"active":0}' --where "last_login < '2025-01-01'"
```

| Argument   | Type   | Required | Default            | Description                                |
| ---------- | ------ | -------- | ------------------ | ------------------------------------------ |
| `table`    | string | yes      |                    | Target table                               |
| `database` | string | no       | configured default | Database containing the table              |
| `set`      | string | yes      |                    | JSON object of columns to update           |
| `where`    | string | yes      |                    | WHERE clause (without the `WHERE` keyword) |

Returns: `affected_rows`, `changed_rows`.

### Delete

```
mysql delete --table sessions --where "expires_at < NOW()"
```

| Argument   | Type   | Required | Default            | Description                                |
| ---------- | ------ | -------- | ------------------ | ------------------------------------------ |
| `table`    | string | yes      |                    | Target table                               |
| `database` | string | no       | configured default | Database containing the table              |
| `where`    | string | yes      |                    | WHERE clause (without the `WHERE` keyword) |

Returns: `affected_rows`.

### Upsert

```
mysql upsert --table users --data '{"email":"alice@example.com","name":"Alice B."}' --update_columns '["name"]'
```

| Argument         | Type   | Required | Default            | Description                                 |
| ---------------- | ------ | -------- | ------------------ | ------------------------------------------- |
| `table`          | string | yes      |                    | Target table                                |
| `database`       | string | no       | configured default | Database containing the table               |
| `data`           | string | yes      |                    | JSON object of column-value pairs           |
| `update_columns` | string | yes      |                    | JSON array of columns to update on conflict |

Returns: `inserted_id`, `affected_rows`.

## Transactions

### Begin transaction

```
mysql begin_transaction --isolation_level serializable
```

| Argument          | Type   | Required | Default           | Description                                                          |
| ----------------- | ------ | -------- | ----------------- | -------------------------------------------------------------------- |
| `isolation_level` | string | no       | `repeatable_read` | `read_uncommitted`, `read_committed`, `repeatable_read`, or `serializable` |

Returns: `transaction_id`.

### Commit

```
mysql commit --transaction_id txn_abc123
```

| Argument         | Type   | Required | Description           |
| ---------------- | ------ | -------- | --------------------- |
| `transaction_id` | string | yes      | Transaction to commit |

Returns: `committed`, `transaction_id`.

### Rollback

```
mysql rollback --transaction_id txn_abc123
```

| Argument         | Type   | Required | Description              |
| ---------------- | ------ | -------- | ------------------------ |
| `transaction_id` | string | yes      | Transaction to roll back |

Returns: `rolled_back`, `transaction_id`.

## Server info

### Table info

```
mysql table_info --table users
```

| Argument   | Type   | Required | Default            | Description   |
| ---------- | ------ | -------- | ------------------ | ------------- |
| `table`    | string | yes      |                    | Table name    |
| `database` | string | no       | configured default | Database name |

Returns: `table_name`, `engine`, `row_count`, `data_size`, `index_size`, `auto_increment`.

### Database size

```
mysql database_size
```

Returns: `database`, `size_bytes`, `pretty_size`.

### Active connections

```
mysql active_connections
```

Returns: `id`, `user`, `host`, `database`, `command`, `time`, `state`, `info` for each active connection.

### Show process list

```
mysql show_processlist
```

Returns: `id`, `user`, `host`, `database`, `command`, `time`, `state`, `info` for each running process.

## Export

### Export to CSV

```
mysql export_csv --sql "SELECT * FROM users WHERE active = 1" --file_name users_export.csv
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
mysql export_json --sql "SELECT * FROM orders WHERE total > 100" --file_name big_orders.json
```

| Argument    | Type    | Required | Default       | Description              |
| ----------- | ------- | -------- | ------------- | ------------------------ |
| `sql`       | string  | yes      |               | SELECT query to export   |
| `file_name` | string  | no       | `export.json` | Output file name         |
| `pretty`    | boolean | no       | false         | Pretty-print JSON output |

Returns: `file_path`, `row_count`, `size_bytes`.

## User management

### List users

```
mysql list_users
```

Returns: `user`, `host`, `authentication_string` for each user.

### Create user

```
mysql create_user --username newuser --password secret123 --host "%"
```

| Argument   | Type   | Required | Default | Description                  |
| ---------- | ------ | -------- | ------- | ---------------------------- |
| `username` | string | yes      |         | New username                 |
| `password` | string | yes      |         | User password                |
| `host`     | string | no       | `%`     | Allowed connection host      |

Returns: `created`, `user`, `host`.

### Grant privileges

```
mysql grant_privileges --username newuser --database myapp --privileges '["SELECT","INSERT","UPDATE"]' --host "%"
```

| Argument     | Type   | Required | Default | Description                                     |
| ------------ | ------ | -------- | ------- | ----------------------------------------------- |
| `username`   | string | yes      |         | Username to grant to                            |
| `database`   | string | yes      |         | Database to grant on (use `*` for all)          |
| `privileges` | string | yes      |         | JSON array of privileges (e.g. `["SELECT"]`)    |
| `host`       | string | no       | `%`     | User host                                       |
| `table`      | string | no       | `*`     | Table to grant on (use `*` for all)             |

Returns: `granted`, `user`, `database`, `privileges`.

## Workflow

1. **Always start with `mysql connect`** to establish a connection to the target database.
2. Use `list_tables` and `list_columns` to explore the schema before writing queries.
3. Use parameterized `query` with `--params` for any user-supplied values to prevent SQL injection.
4. For bulk schema changes, wrap operations in a transaction: `begin_transaction` -> queries -> `commit` (or `rollback` on failure).
5. Use `table_info` and `database_size` to monitor storage before large operations.
6. Use `export_csv` or `export_json` to extract data for downstream processing.

## Safety notes

- **Always use parameterized queries.** Pass user-supplied values via `--params`, never by string concatenation into `--sql`.
- `delete` and `update` require a `--where` clause. There is no way to run them without a filter to prevent accidental full-table mutations.
- `drop_table` will permanently destroy the table and all its data. Confirm with the user first.
- Credentials are injected by the backend. Agents never see raw connection passwords.
- Transactions that are not committed or rolled back will be automatically rolled back after a timeout.
- Large `export_csv` or `export_json` results are streamed. For very large tables, add a `LIMIT` or filter to the `--sql` query.
- The `query` action executes arbitrary SQL. Avoid DDL statements for auditability.
- MySQL uses `?` as the parameter placeholder, not `$1`.
- The `upsert` action uses MySQL's `INSERT ... ON DUPLICATE KEY UPDATE` syntax.
