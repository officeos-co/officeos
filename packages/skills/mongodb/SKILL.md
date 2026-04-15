# MongoDB

Full MongoDB database management: connect, manage collections, CRUD documents, run aggregation pipelines, manage indexes, and inspect databases via a backend proxy.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Connection

### Connect

```
mongodb connect --uri mongodb://localhost:27017 --database myapp
```

| Argument   | Type   | Required | Default                      | Description              |
| ---------- | ------ | -------- | ---------------------------- | ------------------------ |
| `uri`      | string | no       | `mongodb://localhost:27017`  | MongoDB connection URI   |
| `database` | string | yes      |                              | Database name to use     |

Returns: `connected`, `version`, `database`.

## Collections

### List collections

```
mongodb list_collections
```

Returns: `name`, `type` for each collection in the database.

### Create collection

```
mongodb create_collection --name orders
```

| Argument | Type   | Required | Description          |
| -------- | ------ | -------- | -------------------- |
| `name`   | string | yes      | New collection name  |

Returns: `created`, `name`.

### Drop collection

```
mongodb drop_collection --collection users
```

| Argument     | Type   | Required | Description              |
| ------------ | ------ | -------- | ------------------------ |
| `collection` | string | yes      | Collection name to drop  |

Returns: `dropped`, `collection`.

### Collection stats

```
mongodb collection_stats --collection users
```

| Argument     | Type   | Required | Description             |
| ------------ | ------ | -------- | ----------------------- |
| `collection` | string | yes      | Collection to inspect   |

Returns: `ns`, `count`, `size`, `avgObjSize`, `storageSize`, `nindexes`, `totalIndexSize`.

## CRUD operations

### Find documents

```
mongodb find --collection users --filter '{"active": true}' --projection '{"name": 1, "email": 1}' --sort '{"created_at": -1}' --limit 10 --skip 0
```

| Argument     | Type   | Required | Default | Description                                   |
| ------------ | ------ | -------- | ------- | --------------------------------------------- |
| `collection` | string | yes      |         | Collection to query                           |
| `filter`     | string | no       | `{}`    | JSON query filter                             |
| `projection` | string | no       |         | JSON projection (fields to include/exclude)   |
| `sort`       | string | no       |         | JSON sort specification (e.g. `{"name": 1}`)  |
| `limit`      | int    | no       | 20      | Maximum documents to return                   |
| `skip`       | int    | no       | 0       | Number of documents to skip                   |

Returns: `documents` (array), `count`.

### Find one document

```
mongodb find_one --collection users --filter '{"email": "alice@example.com"}'
```

| Argument     | Type   | Required | Default | Description                                 |
| ------------ | ------ | -------- | ------- | ------------------------------------------- |
| `collection` | string | yes      |         | Collection to query                         |
| `filter`     | string | yes      |         | JSON query filter                           |
| `projection` | string | no       |         | JSON projection (fields to include/exclude) |

Returns: single document object, or `null` if no match.

### Insert one document

```
mongodb insert_one --collection users --document '{"name": "Alice", "email": "alice@example.com", "active": true}'
```

| Argument     | Type   | Required | Description                    |
| ------------ | ------ | -------- | ------------------------------ |
| `collection` | string | yes      | Target collection              |
| `document`   | string | yes      | JSON document to insert        |

Returns: `inserted_id`, `acknowledged`.

### Insert many documents

```
mongodb insert_many --collection users --documents '[{"name": "Alice"}, {"name": "Bob"}]'
```

| Argument     | Type   | Required | Description                          |
| ------------ | ------ | -------- | ------------------------------------ |
| `collection` | string | yes      | Target collection                    |
| `documents`  | string | yes      | JSON array of documents to insert    |

Returns: `inserted_ids` (array), `inserted_count`, `acknowledged`.

### Update one document

```
mongodb update_one --collection users --filter '{"email": "alice@example.com"}' --update '{"$set": {"active": false}}'
```

| Argument     | Type    | Required | Default | Description                                 |
| ------------ | ------- | -------- | ------- | ------------------------------------------- |
| `collection` | string  | yes      |         | Target collection                           |
| `filter`     | string  | yes      |         | JSON query filter to match one document     |
| `update`     | string  | yes      |         | JSON update operators (e.g. `{"$set": {}}`) |
| `upsert`     | boolean | no       | false   | Insert if no document matches               |

Returns: `matched_count`, `modified_count`, `upserted_id`, `acknowledged`.

### Update many documents

```
mongodb update_many --collection users --filter '{"active": false}' --update '{"$set": {"archived": true}}'
```

| Argument     | Type    | Required | Default | Description                                 |
| ------------ | ------- | -------- | ------- | ------------------------------------------- |
| `collection` | string  | yes      |         | Target collection                           |
| `filter`     | string  | yes      |         | JSON query filter                           |
| `update`     | string  | yes      |         | JSON update operators (e.g. `{"$set": {}}`) |
| `upsert`     | boolean | no       | false   | Insert if no document matches               |

Returns: `matched_count`, `modified_count`, `upserted_id`, `acknowledged`.

### Replace one document

```
mongodb replace_one --collection users --filter '{"_id": "abc123"}' --replacement '{"name": "Alice B.", "email": "alice@example.com"}'
```

| Argument      | Type    | Required | Default | Description                             |
| ------------- | ------- | -------- | ------- | --------------------------------------- |
| `collection`  | string  | yes      |         | Target collection                       |
| `filter`      | string  | yes      |         | JSON query filter to match one document |
| `replacement` | string  | yes      |         | JSON replacement document (no operators)|
| `upsert`      | boolean | no       | false   | Insert if no document matches           |

Returns: `matched_count`, `modified_count`, `upserted_id`, `acknowledged`.

### Delete one document

```
mongodb delete_one --collection users --filter '{"email": "alice@example.com"}'
```

| Argument     | Type   | Required | Description                             |
| ------------ | ------ | -------- | --------------------------------------- |
| `collection` | string | yes      | Target collection                       |
| `filter`     | string | yes      | JSON query filter to match one document |

Returns: `deleted_count`, `acknowledged`.

### Delete many documents

```
mongodb delete_many --collection sessions --filter '{"expires_at": {"$lt": "2025-01-01T00:00:00Z"}}'
```

| Argument     | Type   | Required | Description                   |
| ------------ | ------ | -------- | ----------------------------- |
| `collection` | string | yes      | Target collection             |
| `filter`     | string | yes      | JSON query filter             |

Returns: `deleted_count`, `acknowledged`.

## Aggregation

### Aggregate

```
mongodb aggregate --collection orders --pipeline '[{"$match": {"status": "completed"}}, {"$group": {"_id": "$customer_id", "total": {"$sum": "$amount"}}}, {"$sort": {"total": -1}}, {"$limit": 10}]'
```

| Argument     | Type   | Required | Description                         |
| ------------ | ------ | -------- | ----------------------------------- |
| `collection` | string | yes      | Collection to aggregate             |
| `pipeline`   | string | yes      | JSON array of aggregation stages    |

Returns: `documents` (array), `count`.

## Indexes

### List indexes

```
mongodb list_indexes --collection users
```

| Argument     | Type   | Required | Description        |
| ------------ | ------ | -------- | ------------------ |
| `collection` | string | yes      | Collection to list |

Returns: array of `name`, `key`, `unique`, `sparse` for each index.

### Create index

```
mongodb create_index --collection users --keys '{"email": 1}' --unique true --name idx_users_email
```

| Argument     | Type    | Required | Default | Description                                |
| ------------ | ------- | -------- | ------- | ------------------------------------------ |
| `collection` | string  | yes      |         | Collection to index                        |
| `keys`       | string  | yes      |         | JSON index key specification               |
| `unique`     | boolean | no       | false   | Create a unique index                      |
| `sparse`     | boolean | no       | false   | Only index documents with the field        |
| `name`       | string  | no       |         | Custom index name                          |
| `ttl`        | int     | no       |         | TTL in seconds (for expiring documents)    |

Returns: `created`, `index_name`.

### Drop index

```
mongodb drop_index --collection users --name idx_users_email
```

| Argument     | Type   | Required | Description        |
| ------------ | ------ | -------- | ------------------ |
| `collection` | string | yes      | Collection name    |
| `name`       | string | yes      | Index name to drop |

Returns: `dropped`, `index_name`.

## Database operations

### List databases

```
mongodb list_databases
```

Returns: `name`, `sizeOnDisk`, `empty` for each database on the server.

### Database stats

```
mongodb db_stats
```

Returns: `db`, `collections`, `views`, `objects`, `avgObjSize`, `dataSize`, `storageSize`, `indexes`, `indexSize`.

### Drop database

```
mongodb drop_database
```

Returns: `dropped`, `database`.

## Count

### Count documents

```
mongodb count_documents --collection users --filter '{"active": true}'
```

| Argument     | Type   | Required | Default | Description       |
| ------------ | ------ | -------- | ------- | ----------------- |
| `collection` | string | yes      |         | Collection name   |
| `filter`     | string | no       | `{}`    | JSON query filter |

Returns: `count`.

### Estimated count

```
mongodb estimated_count --collection users
```

| Argument     | Type   | Required | Description     |
| ------------ | ------ | -------- | --------------- |
| `collection` | string | yes      | Collection name |

Returns: `count`.

## Distinct

### Distinct values

```
mongodb distinct --collection users --field country --filter '{"active": true}'
```

| Argument     | Type   | Required | Default | Description                    |
| ------------ | ------ | -------- | ------- | ------------------------------ |
| `collection` | string | yes      |         | Collection name                |
| `field`      | string | yes      |         | Field to get distinct values   |
| `filter`     | string | no       | `{}`    | JSON query filter              |

Returns: `values` (array), `count`.

## Bulk operations

### Bulk write

```
mongodb bulk_write --collection users --operations '[{"insertOne": {"document": {"name": "Alice"}}}, {"updateOne": {"filter": {"name": "Bob"}, "update": {"$set": {"active": false}}}}, {"deleteOne": {"filter": {"name": "Charlie"}}}]'
```

| Argument     | Type    | Required | Default | Description                                   |
| ------------ | ------- | -------- | ------- | --------------------------------------------- |
| `collection` | string  | yes      |         | Target collection                             |
| `operations` | string  | yes      |         | JSON array of bulk write operations           |
| `ordered`    | boolean | no       | true    | Execute operations in order (stop on error)   |

Returns: `inserted_count`, `matched_count`, `modified_count`, `deleted_count`, `upserted_count`, `acknowledged`.

## Workflow

1. **Always start with `mongodb connect`** to verify connectivity to the target database.
2. Use `list_collections` and `collection_stats` to explore the database before writing queries.
3. Use `find` with `--filter` for querying. Pass filters as JSON strings.
4. For complex analytics, use `aggregate` with a pipeline of stages.
5. Create indexes with `create_index` before running performance-sensitive queries.
6. Use `bulk_write` for batch operations instead of individual insert/update/delete calls.
7. Use `count_documents` for exact counts and `estimated_count` for fast approximate counts on large collections.

## Safety notes

- **Always use filters.** `delete_many` and `update_many` with an empty filter will affect all documents. Confirm with the user first.
- `drop_collection` and `drop_database` are destructive and irreversible. Confirm with the user first.
- Credentials are injected by the backend. Agents never see raw connection strings.
- `find` defaults to 20 documents. Use `--limit` to control result size.
- Aggregation pipelines can be expensive. Add `$match` stages early to reduce the working set.
- `bulk_write` with `--ordered false` continues on error but may leave partial results.
- The `replace_one` action replaces the entire document (except `_id`). Use `update_one` with `$set` to modify specific fields.
