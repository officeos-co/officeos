# References

## Source SDK/CLI
- **Repository**: [mholt/PapaParse](https://github.com/mholt/PapaParse)
- **License**: MIT
- **npm package**: `papaparse`
- **Documentation**: [papaparse.com/docs](https://www.papaparse.com/docs)

## Proxy Pattern
This skill communicates with a file-proxy service (`proxy_url`) that wraps the `papaparse` library. The proxy maintains parsed datasets in server-side memory and returns `file_id` handles. Transformation operations are immutable — they return new `file_id`s rather than mutating the original dataset.

## API Coverage
- **Parsing & Serialising**: parse CSV text, parse from URL, stringify to CSV
- **Inspection**: get columns, get rows (paginated), compute statistics
- **Transformation**: filter rows, sort, add column, rename column, drop columns, transform values, deduplicate
- **Combining**: merge datasets (union / inner join / left join)
- **Export**: convert to JSON (records or column orientation), download CSV file
