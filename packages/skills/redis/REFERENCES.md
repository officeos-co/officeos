# References

## Source SDK/CLI
- **Repository**: [redis/node-redis](https://github.com/redis/node-redis)
- **License**: MIT
- **npm package**: `redis`
- **Documentation**: [https://redis.js.org](https://redis.js.org)

## API Coverage
- Connection management (connect with TLS and database selection)
- String operations (get, set, mget, mset, incr, decr, append)
- Hash operations (hget, hset, hgetall, hdel, hkeys, hvals)
- List operations (lpush, rpush, lpop, rpop, lrange, llen)
- Set operations (sadd, srem, smembers, sismember, scard, sunion, sinter, sdiff)
- Sorted set operations (zadd, zrem, zrange, zrangebyscore, zscore, zcard)
- Key management (keys, del, exists, expire, ttl, type, rename)
- Pub/Sub (publish, subscribe)
- Server diagnostics (info, dbsize, flushdb, ping)
- RedisJSON (json_set, json_get)
