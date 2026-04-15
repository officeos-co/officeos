# References

## Source SDK/CLI
- **Repository**: [dolanmiu/docx](https://github.com/dolanmiu/docx)
- **License**: MIT
- **npm package**: `docx`
- **Documentation**: [docx.js.org](https://docx.js.org)

## Proxy Pattern
This skill communicates with a file-proxy service (`proxy_url`) that wraps the `docx` library. The proxy holds files in server-side state and returns `file_id` handles. This pattern is used because `docx` generates binary `.docx` files that cannot be streamed through a simple JSON API without a proxy layer.

## API Coverage
- **Documents**: create, open, save, get text, replace text
- **Content**: add paragraph, add table, add image, add heading, add page break, delete paragraph
- **Sections & Styles**: list styles, set document properties
- **Export**: export to PDF, download file
