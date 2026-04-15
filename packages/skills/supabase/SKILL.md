# Supabase

Full Supabase platform coverage: database, PostgREST CRUD, auth, storage, edge functions, realtime, vector search, and RPC via the Supabase REST APIs.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Database

### Execute SQL query

```
supabase query --sql "SELECT * FROM users WHERE active = true LIMIT 10"
```

| Argument | Type   | Required | Description               |
|----------|--------|----------|---------------------------|
| `sql`    | string | yes      | SQL query to execute      |

Returns the result rows as a JSON array.

### List tables

```
supabase list_tables
```

No arguments. Returns all tables in the `public` schema with `table_name`, `row_count`, and `size`.

### Get table details

```
supabase get_table --table users
```

| Argument | Type   | Required | Description     |
|----------|--------|----------|-----------------|
| `table`  | string | yes      | Table name      |

Returns columns with `column_name`, `data_type`, `is_nullable`, `column_default`.

## CRUD (PostgREST)

### Select rows

```
supabase select --table users --columns "id,name,email" --filter "active=eq.true" --order "created_at.desc" --limit 20 --offset 0
```

| Argument  | Type   | Required | Default | Description                                       |
|-----------|--------|----------|---------|---------------------------------------------------|
| `table`   | string | yes      |         | Table name                                        |
| `columns` | string | no       | `*`     | Comma-separated column names                      |
| `filter`  | string | no       |         | PostgREST filter (e.g. `age=gt.18`, `name=eq.Jo`) |
| `order`   | string | no       |         | Column and direction (e.g. `created_at.desc`)     |
| `limit`   | int    | no       | 100     | Max rows to return                                |
| `offset`  | int    | no       | 0       | Number of rows to skip                            |

### Insert rows

```
supabase insert --table users --records '[{"name":"Alice","email":"alice@example.com"}]'
```

| Argument  | Type   | Required | Description                         |
|-----------|--------|----------|-------------------------------------|
| `table`   | string | yes      | Table name                          |
| `records` | json   | yes      | JSON array of objects to insert     |

Returns inserted rows with `Prefer: return=representation`.

### Update rows

```
supabase update --table users --filter "id=eq.42" --data '{"name":"Bob"}'
```

| Argument | Type   | Required | Description                          |
|----------|--------|----------|--------------------------------------|
| `table`  | string | yes      | Table name                           |
| `filter` | string | yes      | PostgREST filter to match rows       |
| `data`   | json   | yes      | JSON object with columns to update   |

### Delete rows

```
supabase delete --table users --filter "id=eq.42"
```

| Argument | Type   | Required | Description                    |
|----------|--------|----------|--------------------------------|
| `table`  | string | yes      | Table name                     |
| `filter` | string | yes      | PostgREST filter to match rows |

### Upsert rows

```
supabase upsert --table users --records '[{"id":1,"name":"Alice"}]' --on_conflict "id"
```

| Argument      | Type   | Required | Default | Description                              |
|---------------|--------|----------|---------|------------------------------------------|
| `table`       | string | yes      |         | Table name                               |
| `records`     | json   | yes      |         | JSON array of objects to upsert          |
| `on_conflict` | string | no       |         | Comma-separated conflict column names    |

## Auth

### List users

```
supabase list_users --page 1 --per_page 50
```

| Argument   | Type | Required | Default | Description          |
|------------|------|----------|---------|----------------------|
| `page`     | int  | no       | 1       | Page number          |
| `per_page` | int  | no       | 50      | Results per page     |

### Get user

```
supabase get_user --user_id "uuid-here"
```

| Argument  | Type   | Required | Description |
|-----------|--------|----------|-------------|
| `user_id` | string | yes      | User UUID   |

### Create user

```
supabase create_user --email "user@example.com" --password "secret" --email_confirm true
```

| Argument        | Type    | Required | Default | Description                    |
|-----------------|---------|----------|---------|--------------------------------|
| `email`         | string  | yes      |         | User email                     |
| `password`      | string  | yes      |         | User password                  |
| `email_confirm` | boolean | no       | false   | Auto-confirm email             |
| `user_metadata` | json    | no       |         | Custom user metadata object    |

### Delete user

```
supabase delete_user --user_id "uuid-here"
```

| Argument  | Type   | Required | Description |
|-----------|--------|----------|-------------|
| `user_id` | string | yes      | User UUID   |

### Invite user

```
supabase invite_user --email "user@example.com"
```

| Argument | Type   | Required | Description         |
|----------|--------|----------|---------------------|
| `email`  | string | yes      | Email to invite     |

### Update user

```
supabase update_user --user_id "uuid-here" --email "new@example.com" --user_metadata '{"role":"admin"}'
```

| Argument        | Type   | Required | Description                    |
|-----------------|--------|----------|--------------------------------|
| `user_id`       | string | yes      | User UUID                      |
| `email`         | string | no       | New email                      |
| `password`      | string | no       | New password                   |
| `user_metadata` | json   | no       | Updated user metadata object   |

## Storage

### List buckets

```
supabase list_buckets
```

No arguments. Returns all storage buckets with `id`, `name`, `public`, `created_at`.

### Create bucket

```
supabase create_bucket --name "avatars" --public true
```

| Argument | Type    | Required | Default | Description               |
|----------|---------|----------|---------|---------------------------|
| `name`   | string  | yes      |         | Bucket name               |
| `public` | boolean | no       | false   | Whether bucket is public   |

### Delete bucket

```
supabase delete_bucket --id "avatars"
```

| Argument | Type   | Required | Description |
|----------|--------|----------|-------------|
| `id`     | string | yes      | Bucket ID   |

### List files

```
supabase list_files --bucket "avatars" --path "uploads/" --limit 100 --offset 0
```

| Argument | Type   | Required | Default | Description                   |
|----------|--------|----------|---------|-------------------------------|
| `bucket` | string | yes      |         | Bucket name                   |
| `path`   | string | no       | `""`    | Folder path within bucket     |
| `limit`  | int    | no       | 100     | Max files to return           |
| `offset` | int    | no       | 0       | Number of files to skip       |

### Upload file

```
supabase upload_file --bucket "avatars" --path "profile.png" --content "<base64>" --content_type "image/png" --upsert true
```

| Argument       | Type    | Required | Default | Description                         |
|----------------|---------|----------|---------|-------------------------------------|
| `bucket`       | string  | yes      |         | Bucket name                         |
| `path`         | string  | yes      |         | File path within bucket             |
| `content`      | string  | yes      |         | Base64-encoded file content         |
| `content_type` | string  | no       |         | MIME type                           |
| `upsert`       | boolean | no       | false   | Overwrite if file exists            |

### Download file

```
supabase download_file --bucket "avatars" --path "profile.png"
```

| Argument | Type   | Required | Description               |
|----------|--------|----------|---------------------------|
| `bucket` | string | yes      | Bucket name               |
| `path`   | string | yes      | File path within bucket   |

Returns base64-encoded file content and `content_type`.

### Delete file

```
supabase delete_file --bucket "avatars" --paths '["profile.png","old.png"]'
```

| Argument | Type     | Required | Description                    |
|----------|----------|----------|--------------------------------|
| `bucket` | string   | yes      | Bucket name                    |
| `paths`  | string[] | yes      | Array of file paths to delete  |

### Get public URL

```
supabase get_public_url --bucket "avatars" --path "profile.png"
```

| Argument | Type   | Required | Description             |
|----------|--------|----------|-------------------------|
| `bucket` | string | yes      | Bucket name             |
| `path`   | string | yes      | File path within bucket |

Returns `public_url`. Only works for public buckets.

### Create signed URL

```
supabase create_signed_url --bucket "avatars" --path "profile.png" --expires_in 3600
```

| Argument     | Type   | Required | Default | Description                     |
|--------------|--------|----------|---------|---------------------------------|
| `bucket`     | string | yes      |         | Bucket name                     |
| `path`       | string | yes      |         | File path within bucket         |
| `expires_in` | int    | no       | 3600    | Seconds until URL expires       |

## Edge Functions

### List functions

```
supabase list_functions
```

No arguments. Returns deployed edge functions with `slug`, `name`, `status`, `created_at`.

### Get function

```
supabase get_function --slug "hello-world"
```

| Argument | Type   | Required | Description      |
|----------|--------|----------|------------------|
| `slug`   | string | yes      | Function slug    |

### Invoke function

```
supabase invoke_function --slug "hello-world" --body '{"name":"Alice"}'
```

| Argument | Type   | Required | Description                  |
|----------|--------|----------|------------------------------|
| `slug`   | string | yes      | Function slug                |
| `body`   | json   | no       | Request body as JSON         |

## Realtime

### List channels

```
supabase list_channels
```

No arguments. Returns active realtime channels.

## Vector / Embeddings

### Vector search

```
supabase vector_search --table "documents" --query_embedding "[0.1,0.2,0.3]" --match_count 5 --match_threshold 0.8
```

| Argument          | Type   | Required | Default | Description                              |
|-------------------|--------|----------|---------|------------------------------------------|
| `table`           | string | yes      |         | Table with vector column                 |
| `query_embedding` | json   | yes      |         | Query vector as JSON array of floats     |
| `match_count`     | int    | no       | 10      | Max results to return                    |
| `match_threshold` | float  | no       | 0.5     | Minimum similarity threshold             |

Calls a `match_<table>` RPC function. Requires a corresponding Postgres function with pgvector.

## RPC

### Call RPC function

```
supabase rpc --function_name "get_stats" --params '{"user_id":"uuid-here"}'
```

| Argument        | Type   | Required | Default | Description                          |
|-----------------|--------|----------|---------|--------------------------------------|
| `function_name` | string | yes      |         | Postgres function name               |
| `params`        | json   | no       | `{}`    | Arguments to pass to the function    |

## Workflow

1. Start with `supabase list_tables` to discover the schema.
2. Use `supabase select` for reading data with PostgREST filters.
3. Use `supabase insert`, `update`, `delete`, or `upsert` for data manipulation.
4. Use `supabase query` for complex SQL that cannot be expressed with PostgREST.
5. Manage users with `list_users`, `create_user`, `update_user`, etc.
6. Manage file storage: create buckets, upload/download files, generate signed URLs.
7. Invoke edge functions for server-side logic.
8. Use `vector_search` for semantic similarity queries on pgvector-enabled tables.
9. Use `rpc` to invoke any custom Postgres function.

## Safety notes

- Write operations require the `service_role_key`. The `anon_key` respects Row Level Security.
- PostgREST filters use the syntax `column=operator.value` (e.g. `id=eq.42`, `age=gt.18`).
- Storage upload uses base64 encoding for file content.
- The `vector_search` action requires a matching `match_<table>` Postgres function.
- Edge function invocation is subject to the function's configured timeout.
