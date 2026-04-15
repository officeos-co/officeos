# References

## Source SDK/CLI
- **Repository**: [Hopding/pdf-lib](https://github.com/Hopding/pdf-lib)
- **License**: MIT
- **npm package**: `pdf-lib`
- **Documentation**: [pdf-lib.js.org](https://pdf-lib.js.org)

## Proxy Pattern
This skill communicates with a file-proxy service (`proxy_url`) that wraps the `pdf-lib` library. The proxy maintains file state server-side and returns `file_id` handles. This pattern is required because `pdf-lib` operates on in-memory `PDFDocument` objects which must be serialised to bytes for transport.

## API Coverage
- **Documents**: create, open, get info, save
- **Content**: extract text, add page, add text, add image, delete page, rotate pages
- **Multi-file**: merge PDFs, split PDF, copy pages
- **Metadata**: set metadata
- **Export**: download file
