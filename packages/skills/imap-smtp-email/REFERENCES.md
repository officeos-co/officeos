# References

## Source SDKs
- **Sending**: [nodemailer/nodemailer](https://github.com/nodemailer/nodemailer) — License: MIT — npm: `nodemailer` — Docs: [nodemailer.com](https://nodemailer.com)
- **Receiving**: [mscdex/node-imap](https://github.com/mscdex/node-imap) — License: MIT — npm: `imap` — Docs: [github.com/mscdex/node-imap](https://github.com/mscdex/node-imap)

## Proxy Pattern
This skill communicates with an email-proxy service (`proxy_url`) that wraps both `nodemailer` (SMTP) and `node-imap` (IMAP). The proxy manages persistent IMAP connections and handles SMTP authentication. Credentials (`imap_host`, `smtp_host`, `email`, `password`) are forwarded to the proxy at request time and injected into the underlying connections.

## Supported Email Providers
- Gmail (IMAP: imap.gmail.com:993, SMTP: smtp.gmail.com:587 or 465)
- Outlook/Hotmail (IMAP: outlook.office365.com:993, SMTP: smtp.office365.com:587)
- Fastmail (IMAP: imap.fastmail.com:993, SMTP: smtp.fastmail.com:465 or 587)
- Any IMAP/SMTP-compliant server

## API Coverage
- **Folders**: list folders, get folder info
- **Messages**: list messages, get full message with body/attachments, search
- **Sending**: send new email, reply, forward
- **Management**: move, copy, delete (with expunge), set flags (Seen, Flagged, Answered, Draft)
