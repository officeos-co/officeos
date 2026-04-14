# Redis

Full Redis data store management: connect, manipulate strings, hashes, lists, sets, sorted sets, manage keys, publish/subscribe, and work with RedisJSON via the node-redis driver.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Connection

### Connect

```
redis connect --host localhost --port 6379 --password secret --db 0 --tls true
```

| Argument   | Type    | Required | Default     | Description                          |
|------------|---------|----------|-------------|--------------------------------------|
| `host`     | string  | no       | `localhost` | Redis server hostname                |
| `port`     | int     | no       | 6379        | Redis server port                    |
| `password` | string  | no       |             | Authentication password              |
| `db`       | int     | no       | 0           | Database index (0-15)                |
| `tls`      | boolean | no       | false       | Enable TLS connection                |

Returns: `connected`, `redis_version`, `db`.

## String operations

### Get

```
redis get --key "user:42:name"
```

| Argument | Type   | Required | Description   |
|----------|--------|----------|---------------|
| `key`    | string | yes      | Key to read   |

Returns: `value` (string or `null` if key does not exist).

### Set

```
redis set --key "user:42:name" --value "Alice" --ex 3600 --nx true
```

| Argument | Type    | Required | Default | Description                                    |
|----------|---------|----------|---------|------------------------------------------------|
| `key`    | string  | yes      |         | Key to set                                     |
| `value`  | string  | yes      |         | Value to store                                 |
| `ex`     | int     | no       |         | Expire after N seconds                         |
| `px`     | int     | no       |         | Expire after N milliseconds                    |
| `nx`     | boolean | no       | false   | Only set if key does not exist                 |
| `xx`     | boolean | no       | false   | Only set if key already exists                 |

Returns: `ok` (boolean), `key`.

### Multi-get

```
redis mget --keys '["user:1:name","user:2:name","user:3:name"]'
```

| Argument | Type   | Required | Description                       |
|----------|--------|----------|-----------------------------------|
| `keys`   | string | yes      | JSON array of keys to retrieve    |

Returns: array of `{key, value}` pairs (`value` is `null` for missing keys).

### Multi-set

```
redis mset --entries '{"user:1:name":"Alice","user:2:name":"Bob"}'
```

| Argument  | Type   | Required | Description                           |
|-----------|--------|----------|---------------------------------------|
| `entries` | string | yes      | JSON object of key-value pairs to set |

Returns: `ok`, `count`.

### Increment

```
redis incr --key "page:views" --by 1
```

| Argument | Type   | Required | Default | Description                    |
|----------|--------|----------|---------|--------------------------------|
| `key`    | string | yes      |         | Key to increment               |
| `by`     | int    | no       | 1       | Increment amount               |

Returns: `value` (new integer value after increment).

### Decrement

```
redis decr --key "stock:item:99" --by 1
```

| Argument | Type   | Required | Default | Description                    |
|----------|--------|----------|---------|--------------------------------|
| `key`    | string | yes      |         | Key to decrement               |
| `by`     | int    | no       | 1       | Decrement amount               |

Returns: `value` (new integer value after decrement).

### Append

```
redis append --key "log:entry" --value " additional text"
```

| Argument | Type   | Required | Description                      |
|----------|--------|----------|----------------------------------|
| `key`    | string | yes      | Key to append to                 |
| `value`  | string | yes      | Value to append                  |

Returns: `length` (new string length after append).

## Hash operations

### Hash get

```
redis hget --key "user:42" --field "email"
```

| Argument | Type   | Required | Description        |
|----------|--------|----------|--------------------|
| `key`    | string | yes      | Hash key           |
| `field`  | string | yes      | Field to retrieve  |

Returns: `value` (string or `null`).

### Hash set

```
redis hset --key "user:42" --fields '{"name":"Alice","email":"alice@example.com","age":"30"}'
```

| Argument | Type   | Required | Description                              |
|----------|--------|----------|------------------------------------------|
| `key`    | string | yes      | Hash key                                 |
| `fields` | string | yes      | JSON object of field-value pairs to set  |

Returns: `added_count` (number of new fields created).

### Hash get all

```
redis hgetall --key "user:42"
```

| Argument | Type   | Required | Description   |
|----------|--------|----------|---------------|
| `key`    | string | yes      | Hash key      |

Returns: JSON object of all field-value pairs in the hash.

### Hash delete

```
redis hdel --key "user:42" --fields '["temp_field","old_field"]'
```

| Argument | Type   | Required | Description                          |
|----------|--------|----------|--------------------------------------|
| `key`    | string | yes      | Hash key                             |
| `fields` | string | yes      | JSON array of fields to delete       |

Returns: `removed_count`.

### Hash keys

```
redis hkeys --key "user:42"
```

| Argument | Type   | Required | Description   |
|----------|--------|----------|---------------|
| `key`    | string | yes      | Hash key      |

Returns: array of field names.

### Hash values

```
redis hvals --key "user:42"
```

| Argument | Type   | Required | Description   |
|----------|--------|----------|---------------|
| `key`    | string | yes      | Hash key      |

Returns: array of field values.

## List operations

### Left push

```
redis lpush --key "queue:jobs" --values '["job3","job2","job1"]'
```

| Argument | Type   | Required | Description                        |
|----------|--------|----------|------------------------------------|
| `key`    | string | yes      | List key                           |
| `values` | string | yes      | JSON array of values to push left  |

Returns: `length` (list length after push).

### Right push

```
redis rpush --key "queue:jobs" --values '["job4","job5"]'
```

| Argument | Type   | Required | Description                         |
|----------|--------|----------|-------------------------------------|
| `key`    | string | yes      | List key                            |
| `values` | string | yes      | JSON array of values to push right  |

Returns: `length` (list length after push).

### Left pop

```
redis lpop --key "queue:jobs" --count 1
```

| Argument | Type   | Required | Default | Description                  |
|----------|--------|----------|---------|------------------------------|
| `key`    | string | yes      |         | List key                     |
| `count`  | int    | no       | 1       | Number of elements to pop    |

Returns: `values` (array of popped elements).

### Right pop

```
redis rpop --key "queue:jobs" --count 1
```

| Argument | Type   | Required | Default | Description                  |
|----------|--------|----------|---------|------------------------------|
| `key`    | string | yes      |         | List key                     |
| `count`  | int    | no       | 1       | Number of elements to pop    |

Returns: `values` (array of popped elements).

### List range

```
redis lrange --key "queue:jobs" --start 0 --stop -1
```

| Argument | Type   | Required | Default | Description                              |
|----------|--------|----------|---------|------------------------------------------|
| `key`    | string | yes      |         | List key                                 |
| `start`  | int    | no       | 0       | Start index (0-based, negative from end) |
| `stop`   | int    | no       | -1      | Stop index (inclusive, -1 = end)         |

Returns: array of elements in the specified range.

### List length

```
redis llen --key "queue:jobs"
```

| Argument | Type   | Required | Description   |
|----------|--------|----------|---------------|
| `key`    | string | yes      | List key      |

Returns: `length`.

## Set operations

### Add members

```
redis sadd --key "tags:article:1" --members '["redis","database","nosql"]'
```

| Argument  | Type   | Required | Description                       |
|-----------|--------|----------|-----------------------------------|
| `key`     | string | yes      | Set key                           |
| `members` | string | yes      | JSON array of members to add      |

Returns: `added_count` (number of new members added).

### Remove members

```
redis srem --key "tags:article:1" --members '["nosql"]'
```

| Argument  | Type   | Required | Description                       |
|-----------|--------|----------|-----------------------------------|
| `key`     | string | yes      | Set key                           |
| `members` | string | yes      | JSON array of members to remove   |

Returns: `removed_count`.

### List members

```
redis smembers --key "tags:article:1"
```

| Argument | Type   | Required | Description   |
|----------|--------|----------|---------------|
| `key`    | string | yes      | Set key       |

Returns: array of all members.

### Is member

```
redis sismember --key "tags:article:1" --member "redis"
```

| Argument | Type   | Required | Description        |
|----------|--------|----------|--------------------|
| `key`    | string | yes      | Set key            |
| `member` | string | yes      | Member to check    |

Returns: `is_member` (boolean).

### Set cardinality

```
redis scard --key "tags:article:1"
```

| Argument | Type   | Required | Description   |
|----------|--------|----------|---------------|
| `key`    | string | yes      | Set key       |

Returns: `count`.

### Set union

```
redis sunion --keys '["tags:article:1","tags:article:2"]'
```

| Argument | Type   | Required | Description                    |
|----------|--------|----------|--------------------------------|
| `keys`   | string | yes      | JSON array of set keys         |

Returns: array of members present in any of the sets.

### Set intersection

```
redis sinter --keys '["tags:article:1","tags:article:2"]'
```

| Argument | Type   | Required | Description                    |
|----------|--------|----------|--------------------------------|
| `keys`   | string | yes      | JSON array of set keys         |

Returns: array of members present in all of the sets.

### Set difference

```
redis sdiff --keys '["tags:article:1","tags:article:2"]'
```

| Argument | Type   | Required | Description                                        |
|----------|--------|----------|----------------------------------------------------|
| `keys`   | string | yes      | JSON array of set keys (first set minus the rest)  |

Returns: array of members in the first set that are not in any other set.

## Sorted set operations

### Add members

```
redis zadd --key "leaderboard" --members '[{"score":100,"value":"alice"},{"score":85,"value":"bob"}]'
```

| Argument  | Type   | Required | Description                                          |
|-----------|--------|----------|------------------------------------------------------|
| `key`     | string | yes      | Sorted set key                                       |
| `members` | string | yes      | JSON array of `{score, value}` objects               |
| `nx`      | boolean| no       | Only add new elements, do not update existing scores |
| `xx`      | boolean| no       | Only update existing elements, do not add new ones   |

Returns: `added_count`.

### Remove members

```
redis zrem --key "leaderboard" --members '["bob"]'
```

| Argument  | Type   | Required | Description                            |
|-----------|--------|----------|----------------------------------------|
| `key`     | string | yes      | Sorted set key                         |
| `members` | string | yes      | JSON array of members to remove        |

Returns: `removed_count`.

### Range by rank

```
redis zrange --key "leaderboard" --start 0 --stop 9 --rev true --withscores true
```

| Argument     | Type    | Required | Default | Description                                   |
|--------------|---------|----------|---------|-----------------------------------------------|
| `key`        | string  | yes      |         | Sorted set key                                |
| `start`      | int     | no       | 0       | Start rank (0-based)                          |
| `stop`       | int     | no       | -1      | Stop rank (inclusive, -1 = end)               |
| `rev`        | boolean | no       | false   | Reverse order (highest score first)           |
| `withscores` | boolean | no       | false   | Include scores in output                      |

Returns: array of members (with optional scores).

### Range by score

```
redis zrangebyscore --key "leaderboard" --min 50 --max 100 --withscores true --limit 10
```

| Argument     | Type    | Required | Default | Description                                        |
|--------------|---------|----------|---------|----------------------------------------------------|
| `key`        | string  | yes      |         | Sorted set key                                     |
| `min`        | string  | no       | `-inf`  | Minimum score (use `-inf` for no lower bound)      |
| `max`        | string  | no       | `+inf`  | Maximum score (use `+inf` for no upper bound)      |
| `withscores` | boolean | no       | false   | Include scores in output                           |
| `offset`     | int     | no       | 0       | Number of results to skip                          |
| `limit`      | int     | no       |         | Maximum number of results                          |

Returns: array of members (with optional scores).

### Get score

```
redis zscore --key "leaderboard" --member "alice"
```

| Argument | Type   | Required | Description        |
|----------|--------|----------|--------------------|
| `key`    | string | yes      | Sorted set key     |
| `member` | string | yes      | Member to look up  |

Returns: `score` (number or `null`).

### Sorted set cardinality

```
redis zcard --key "leaderboard"
```

| Argument | Type   | Required | Description        |
|----------|--------|----------|--------------------|
| `key`    | string | yes      | Sorted set key     |

Returns: `count`.

## Key operations

### Find keys

```
redis keys --pattern "user:*:name"
```

| Argument  | Type   | Required | Default | Description                |
|-----------|--------|----------|---------|----------------------------|
| `pattern` | string | no       | `*`     | Glob-style key pattern     |

Returns: array of matching key names.

### Delete keys

```
redis del --keys '["temp:1","temp:2","temp:3"]'
```

| Argument | Type   | Required | Description                    |
|----------|--------|----------|--------------------------------|
| `keys`   | string | yes      | JSON array of keys to delete   |

Returns: `deleted_count`.

### Key exists

```
redis exists --key "user:42"
```

| Argument | Type   | Required | Description      |
|----------|--------|----------|------------------|
| `key`    | string | yes      | Key to check     |

Returns: `exists` (boolean).

### Set expiry

```
redis expire --key "session:abc" --seconds 1800
```

| Argument  | Type   | Required | Description                     |
|-----------|--------|----------|---------------------------------|
| `key`     | string | yes      | Key to set expiry on            |
| `seconds` | int    | yes      | Time-to-live in seconds         |

Returns: `ok` (boolean -- false if key does not exist).

### Get TTL

```
redis ttl --key "session:abc"
```

| Argument | Type   | Required | Description     |
|----------|--------|----------|-----------------|
| `key`    | string | yes      | Key to check    |

Returns: `ttl` (seconds remaining, `-1` if no expiry, `-2` if key does not exist).

### Get key type

```
redis type --key "user:42"
```

| Argument | Type   | Required | Description     |
|----------|--------|----------|-----------------|
| `key`    | string | yes      | Key to check    |

Returns: `type` (`string`, `list`, `set`, `zset`, `hash`, `stream`, or `none`).

### Rename key

```
redis rename --key "old:key" --new_key "new:key"
```

| Argument  | Type   | Required | Description      |
|-----------|--------|----------|------------------|
| `key`     | string | yes      | Current key name |
| `new_key` | string | yes      | New key name     |

Returns: `ok` (boolean).

## Pub/Sub

### Publish

```
redis publish --channel "notifications" --message '{"type":"alert","text":"Deploy complete"}'
```

| Argument  | Type   | Required | Description                  |
|-----------|--------|----------|------------------------------|
| `channel` | string | yes      | Channel to publish to        |
| `message` | string | yes      | Message payload (string)     |

Returns: `receivers` (number of clients that received the message).

### Subscribe

```
redis subscribe --channels '["notifications","events"]' --timeout 30
```

| Argument   | Type   | Required | Default | Description                                     |
|------------|--------|----------|---------|-------------------------------------------------|
| `channels` | string | yes      |         | JSON array of channels to subscribe to          |
| `timeout`  | int    | no       | 10      | Max seconds to listen before returning messages |

Returns: array of `{channel, message}` objects received during the timeout window.

## Server

### Server info

```
redis info --section memory
```

| Argument  | Type   | Required | Default | Description                                                 |
|-----------|--------|----------|---------|-------------------------------------------------------------|
| `section` | string | no       |         | Info section: `server`, `memory`, `stats`, `clients`, etc.  |

Returns: parsed key-value pairs from the requested info section (or all sections).

### Database size

```
redis dbsize
```

Returns: `key_count` (total number of keys in the current database).

### Flush database

```
redis flushdb --async true
```

| Argument | Type    | Required | Default | Description                        |
|----------|---------|----------|---------|------------------------------------|
| `async`  | boolean | no       | false   | Flush asynchronously in background |

Returns: `ok`.

### Ping

```
redis ping
```

Returns: `pong` (string -- confirms server is reachable).

## RedisJSON operations

### JSON set

```
redis json_set --key "user:42" --path "$" --value '{"name":"Alice","scores":[95,87,92]}'
```

| Argument | Type   | Required | Default | Description                                      |
|----------|--------|----------|---------|--------------------------------------------------|
| `key`    | string | yes      |         | Key to store JSON document                       |
| `path`   | string | no       | `$`     | JSONPath expression for nested set               |
| `value`  | string | yes      |         | JSON value to store                              |
| `nx`     | boolean| no       | false   | Only set if path does not exist                  |
| `xx`     | boolean| no       | false   | Only set if path already exists                  |

Returns: `ok` (boolean).

### JSON get

```
redis json_get --key "user:42" --path "$.scores[0]"
```

| Argument | Type   | Required | Default | Description                          |
|----------|--------|----------|---------|--------------------------------------|
| `key`    | string | yes      |         | Key containing the JSON document     |
| `path`   | string | no       | `$`     | JSONPath expression to retrieve      |

Returns: `value` (the JSON value at the specified path).

## Workflow

1. **Always start with `redis connect`** to establish a connection to the target Redis instance.
2. Use `keys` with a pattern to discover existing key structures before reading or writing.
3. Use `type` to determine the data structure stored at a key before calling type-specific commands.
4. For caching patterns: `set` with `--ex` for TTL, `get` to read, `exists` to check before expensive operations.
5. For queues: `rpush` to enqueue, `lpop` to dequeue, `llen` to check depth.
6. For real-time features: `publish` to broadcast events, `subscribe` to listen.
7. For structured data: prefer hashes (`hset`/`hgetall`) over serialized strings for individual field access.
8. For complex documents: use `json_set`/`json_get` if the RedisJSON module is available.
9. Use `info --section memory` to monitor memory usage before large bulk operations.

## Safety notes

- **`keys` with broad patterns (e.g. `*`) is expensive on large databases.** Use specific prefixes to limit scope.
- **`flushdb` is destructive and irrecoverable.** Always confirm with the user before flushing.
- `del` is permanent. There is no undo.
- Credentials are injected by the backend. Agents never see raw connection passwords.
- `subscribe` blocks for up to `--timeout` seconds. Keep timeouts short to avoid stalling the agent.
- Redis is single-threaded. Long-running commands (e.g. `keys *` on millions of keys) block all other operations.
- Pub/sub messages are fire-and-forget. If no subscriber is listening, the message is lost.
- RedisJSON commands require the RedisJSON module to be loaded on the server. They will fail on vanilla Redis.
- All values in Redis are strings. Numeric operations (`incr`, `decr`, `zscore`) parse and return numbers but store strings internally.
