# References

## Source SDK/CLI
- **Repository**: [supabase/supabase-js](https://github.com/supabase/supabase-js) + [supabase/cli](https://github.com/supabase/cli)
- **License**: MIT
- **npm package**: `@supabase/supabase-js`
- **Documentation**: [https://supabase.com/docs/reference/javascript/](https://supabase.com/docs/reference/javascript/)

## API Coverage
- Database queries (raw SQL via REST, table listing, table inspection)
- CRUD via PostgREST (select, insert, update, delete, upsert with filters, ordering, pagination)
- Auth / user management (list, get, create, delete, invite, update users)
- Storage (buckets CRUD, file upload/download/delete, public URLs, signed URLs)
- Edge Functions (list, get, invoke)
- Realtime (list channels)
- Vector / embeddings (similarity search via pgvector RPC)
- RPC (invoke arbitrary Postgres functions)
