# References

## Source SDK/CLI
- **Repository**: [Airtable/airtable.js](https://github.com/Airtable/airtable.js)
- **License**: MIT
- **npm package**: `airtable`
- **Documentation**: [airtable.com/developers/web/api](https://airtable.com/developers/web/api/introduction)

## API
- **Base URL**: `https://api.airtable.com/v0/`
- **Authentication**: Bearer token (`Authorization: Bearer <api_key>`)
- **Rate limit**: 5 requests/second per base

## API Coverage
- **Bases**: list bases, get base schema
- **Tables**: list tables, create table, update table
- **Fields**: create field, update field
- **Records**: list (with filtering, sorting, pagination), get, create, update (PATCH and PUT), delete
- **Batch operations**: batch create, batch update, batch delete (auto-batched in groups of 10)
