# IMAP/SMTP Email

Read, search, manage, and send email via IMAP (reading) and SMTP (sending) through a proxy service. Supports any standard IMAP/SMTP server including Gmail, Outlook, Fastmail, and self-hosted.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Folders

### List folders

```
imap-smtp-email list_folders
```

No arguments required.

Returns: array of `{name, delimiter, flags, message_count, unseen_count}`.

### Get folder info

```
imap-smtp-email get_folder --folder "INBOX"
```

| Argument | Type   | Required | Description         |
| -------- | ------ | -------- | ------------------- |
| `folder` | string | yes      | Folder/mailbox name |

Returns: `name`, `flags`, `message_count`, `recent_count`, `unseen_count`, `uid_validity`, `uid_next`.

## Messages

### List messages

```
imap-smtp-email list_messages --folder "INBOX" --limit 20 --unseen_only true
```

| Argument     | Type    | Required | Default  | Description                             |
| ------------ | ------- | -------- | -------- | --------------------------------------- |
| `folder`     | string  | no       | `INBOX`  | Folder to list messages from            |
| `limit`      | int     | no       | 20       | Maximum messages to return (newest first)|
| `offset`     | int     | no       | 0        | Message offset for pagination           |
| `unseen_only`| boolean | no       | false    | Return only unread messages             |

Returns: array of `{uid, subject, from, to, date, flags, size_bytes, has_attachments}`.

### Get message

```
imap-smtp-email get_message --folder "INBOX" --uid 1234 --include_body true
```

| Argument       | Type    | Required | Default  | Description                              |
| -------------- | ------- | -------- | -------- | ---------------------------------------- |
| `folder`       | string  | no       | `INBOX`  | Folder name                              |
| `uid`          | int     | yes      |          | Message UID                              |
| `include_body` | boolean | no       | true     | Include message body                     |
| `format`       | string  | no       | `text`   | Body format: `text` or `html`            |

Returns: `uid`, `message_id`, `subject`, `from`, `to`, `cc`, `bcc`, `date`, `flags`, `text_body`, `html_body`, `attachments` (array of `{filename, content_type, size_bytes}`).

### Search messages

```
imap-smtp-email search --folder "INBOX" --query "from:alice@example.com subject:invoice" --since "2026-01-01" --limit 50
```

| Argument  | Type   | Required | Default | Description                                                                 |
| --------- | ------ | -------- | ------- | --------------------------------------------------------------------------- |
| `folder`  | string | no       | `INBOX` | Folder to search in                                                         |
| `query`   | string | no       |         | Free-text search (from:, to:, subject:, body: prefixes supported)           |
| `since`   | string | no       |         | Return messages after this date (ISO 8601, e.g. 2026-01-01)                |
| `before`  | string | no       |         | Return messages before this date (ISO 8601)                                 |
| `unseen`  | boolean| no       |         | Filter by unseen flag                                                       |
| `flagged` | boolean| no       |         | Filter by flagged status                                                    |
| `limit`   | int    | no       | 50      | Maximum results                                                             |

Returns: array of `{uid, subject, from, to, date, flags, size_bytes, has_attachments}`.

## Sending

### Send email

```
imap-smtp-email send --to '["bob@example.com"]' --subject "Report attached" --body "Please find the Q1 report attached." --attachments '[{"filename":"report.pdf","content_base64":"...","content_type":"application/pdf"}]'
```

| Argument      | Type     | Required | Description                                              |
| ------------- | -------- | -------- | -------------------------------------------------------- |
| `to`          | string[] | yes      | Recipient email addresses                                |
| `subject`     | string   | yes      | Message subject                                          |
| `body`        | string   | yes      | Plain-text body                                          |
| `html_body`   | string   | no       | HTML body (sent as alternative part)                     |
| `cc`          | string[] | no       | CC recipients                                            |
| `bcc`         | string[] | no       | BCC recipients                                           |
| `reply_to`    | string   | no       | Reply-To address                                         |
| `attachments` | array    | no       | Array of `{filename, content_base64, content_type}`      |

Returns: `message_id`, `accepted` (addresses), `rejected` (addresses), `sent_at`.

### Reply to email

```
imap-smtp-email reply --folder "INBOX" --uid 1234 --body "Thanks, confirmed." --reply_all false
```

| Argument    | Type    | Required | Default | Description                                  |
| ----------- | ------- | -------- | ------- | -------------------------------------------- |
| `folder`    | string  | no       | `INBOX` | Folder containing the original message       |
| `uid`       | int     | yes      |         | UID of the message to reply to               |
| `body`      | string  | yes      |         | Reply body text                              |
| `html_body` | string  | no       |         | HTML reply body                              |
| `reply_all` | boolean | no       | false   | Include all original recipients in reply     |
| `attachments` | array | no       |         | Array of `{filename, content_base64, content_type}` |

Returns: `message_id`, `accepted`, `sent_at`.

### Forward email

```
imap-smtp-email forward --folder "INBOX" --uid 1234 --to '["manager@example.com"]' --note "FYI see below."
```

| Argument  | Type     | Required | Description                            |
| --------- | -------- | -------- | -------------------------------------- |
| `folder`  | string   | no       | Folder containing the original message |
| `uid`     | int      | yes      | UID of the message to forward          |
| `to`      | string[] | yes      | Forward-to addresses                   |
| `note`    | string   | no       | Prepend note above forwarded content   |

Returns: `message_id`, `accepted`, `sent_at`.

## Message Management

### Move message

```
imap-smtp-email move --folder "INBOX" --uid 1234 --destination "Archive"
```

| Argument      | Type   | Required | Description                         |
| ------------- | ------ | -------- | ----------------------------------- |
| `folder`      | string | yes      | Source folder                       |
| `uid`         | int    | yes      | Message UID                         |
| `destination` | string | yes      | Destination folder name             |

Returns: `uid`, `new_folder`, `success`.

### Copy message

```
imap-smtp-email copy --folder "INBOX" --uid 1234 --destination "Work/Projects"
```

| Argument      | Type   | Required | Description         |
| ------------- | ------ | -------- | ------------------- |
| `folder`      | string | yes      | Source folder       |
| `uid`         | int    | yes      | Message UID         |
| `destination` | string | yes      | Destination folder  |

Returns: `uid`, `destination`, `success`.

### Delete message

```
imap-smtp-email delete_message --folder "INBOX" --uid 1234 --expunge true
```

| Argument  | Type    | Required | Default | Description                                      |
| --------- | ------- | -------- | ------- | ------------------------------------------------ |
| `folder`  | string  | yes      |         | Folder name                                      |
| `uid`     | int     | yes      |         | Message UID                                      |
| `expunge` | boolean | no       | true    | Permanently expunge (true) or just mark deleted  |

Returns: `uid`, `deleted`, `expunged`.

### Set flags

```
imap-smtp-email set_flags --folder "INBOX" --uid 1234 --flags '["\\Seen","\\Flagged"]' --action add
```

| Argument  | Type     | Required | Description                                                     |
| --------- | -------- | -------- | --------------------------------------------------------------- |
| `folder`  | string   | yes      | Folder name                                                     |
| `uid`     | int      | yes      | Message UID                                                     |
| `flags`   | string[] | yes      | IMAP flags: `\\Seen`, `\\Flagged`, `\\Answered`, `\\Draft`      |
| `action`  | string   | yes      | `add` or `remove`                                               |

Returns: `uid`, `flags` (updated flag list).

## Workflow

1. Use `list_folders` to discover available mailboxes (INBOX, Sent, Archive, etc.).
2. Use `list_messages` or `search` to find relevant emails.
3. Use `get_message` to read the full body and attachments of a specific message.
4. Use `send` for new messages, `reply` for responses, and `forward` to re-route.
5. Use `move` to organise messages into folders, `set_flags` to mark as read/flagged.
6. Use `delete_message` to remove messages (with `expunge: true` for permanent deletion).

## Safety notes

- `delete_message` with `expunge: true` is **permanent**. Set `expunge: false` to only mark as deleted and allow recovery by unsetting the `\\Deleted` flag.
- `reply_all` will send to all original recipients (To + CC). Verify the recipient list before sending.
- Attachments are passed as base64-encoded `content_base64`. Large attachments should be chunked or referenced by URL if the proxy supports it.
- Credentials (`imap_host`, `smtp_host`, `email`, `password`) are passed to the proxy and never leave the backend — the proxy authenticates on behalf of the agent.
- IMAP UIDs are per-folder and per-session (`uid_validity`). Always pair UIDs with a specific `folder`.
