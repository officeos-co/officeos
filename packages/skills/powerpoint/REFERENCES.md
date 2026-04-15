# References

## Source SDK/CLI
- **Repository**: [gitbrent/PptxGenJS](https://github.com/gitbrent/PptxGenJS)
- **License**: MIT
- **npm package**: `pptxgenjs`
- **Documentation**: [gitbrent.github.io/PptxGenJS](https://gitbrent.github.io/PptxGenJS/)

## Proxy Pattern
This skill communicates with a file-proxy service (`proxy_url`) that wraps the `pptxgenjs` library. The proxy holds presentation state server-side and returns `file_id` handles. This is necessary because PptxGenJS generates binary `.pptx` files that require a stateful session for incremental editing.

## API Coverage
- **Presentations**: create, open, save, get info
- **Slides**: add, delete, reorder, duplicate
- **Content**: add text, add image, add chart, add shape, set background, set notes
- **Export**: export to PDF, download file
