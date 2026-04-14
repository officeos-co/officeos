# Gmail

Send, search, read, and manage emails, drafts, labels, threads, and attachments via the Gmail API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Messages

### List messages

```
gmail list_messages --query "is:unread" --label INBOX --max_results 20
```

| Argument      | Type   | Required | Default | Description                              |
|---------------|--------|----------|---------|------------------------------------------|
| `query`       | string | no       |         | Gmail search query (same syntax as web)  |
| `label`       | string | no       | `INBOX` | Label ID to filter by                    |
| `max_results` | int    | no       | 10      | Messages to return (1-500)               |

Returns: list of `id`, `thread_id`, `snippet`, `from`, `to`, `subject`, `date`, `label_ids`.

### Get message

```
gmail get_message --message_id "18a1b2c3d4e5f6"
```

| Argument     | Type   | Required | Description       |
|--------------|--------|----------|-------------------|
| `message_id` | string | yes      | Message ID        |

Returns: `id`, `thread_id`, `from`, `to`, `cc`, `bcc`, `subject`, `date`, `body` (plain text and HTML), `label_ids`, `attachments` (list of `filename`, `mime_type`, `size`, `attachment_id`).

### Send message

```
gmail send_message --to "alice@example.com" --subject "Project update" --body "Hi Alice, here is the latest status." --cc "bob@example.com"
```

| Argument      | Type     | Required | Description                              |
|---------------|----------|----------|------------------------------------------|
| `to`          | string   | yes      | Recipient email address                  |
| `subject`     | string   | yes      | Email subject line                       |
| `body`        | string   | yes      | Email body (plain text or HTML)          |
| `cc`          | string   | no       | CC recipients (comma-separated)          |
| `bcc`         | string   | no       | BCC recipients (comma-separated)         |
| `attachments` | string[] | no       | File paths to attach                     |

Returns: `id`, `thread_id`, `label_ids`.

### Reply to message

```
gmail reply_message --message_id "18a1b2c3d4e5f6" --body "Thanks, looks good!"
```

| Argument     | Type     | Required | Description                              |
|--------------|----------|----------|------------------------------------------|
| `message_id` | string   | yes      | Message ID to reply to                   |
| `body`       | string   | yes      | Reply body                               |
| `cc`         | string   | no       | Additional CC recipients                 |
| `bcc`        | string   | no       | BCC recipients                           |
| `attachments`| string[] | no       | File paths to attach                     |

Returns: `id`, `thread_id`.

### Forward message

```
gmail forward_message --message_id "18a1b2c3d4e5f6" --to "charlie@example.com" --body "FYI see below."
```

| Argument     | Type   | Required | Description                    |
|--------------|--------|----------|--------------------------------|
| `message_id` | string | yes      | Message ID to forward          |
| `to`         | string | yes      | Recipient email address        |
| `body`       | string | no       | Optional message to prepend    |

Returns: `id`, `thread_id`.

### Delete message

```
gmail delete_message --message_id "18a1b2c3d4e5f6"
```

| Argument     | Type   | Required | Description                          |
|--------------|--------|----------|--------------------------------------|
| `message_id` | string | yes      | Message ID to permanently delete     |

Returns: confirmation status.

### Trash message

```
gmail trash_message --message_id "18a1b2c3d4e5f6"
```

| Argument     | Type   | Required | Description             |
|--------------|--------|----------|-------------------------|
| `message_id` | string | yes      | Message ID to trash     |

Returns: `id`, `label_ids`.

### Untrash message

```
gmail untrash_message --message_id "18a1b2c3d4e5f6"
```

| Argument     | Type   | Required | Description               |
|--------------|--------|----------|---------------------------|
| `message_id` | string | yes      | Message ID to untrash     |

Returns: `id`, `label_ids`.

## Search

### Search messages

```
gmail search --query "from:alice@example.com after:2026/01/01 has:attachment"
```

| Argument      | Type   | Required | Default | Description                                  |
|---------------|--------|----------|---------|----------------------------------------------|
| `query`       | string | yes      |         | Gmail search query (supports full Gmail syntax) |
| `max_results` | int    | no       | 10      | Results to return (1-500)                    |

Returns: list of `id`, `thread_id`, `snippet`, `from`, `subject`, `date`.

## Drafts

### List drafts

```
gmail list_drafts --max_results 10
```

| Argument      | Type | Required | Default | Description              |
|---------------|------|----------|---------|--------------------------|
| `max_results` | int  | no       | 10      | Drafts to return (1-500) |

Returns: list of `draft_id`, `message_id`, `snippet`, `subject`.

### Get draft

```
gmail get_draft --draft_id "r123456789"
```

| Argument   | Type   | Required | Description   |
|------------|--------|----------|---------------|
| `draft_id` | string | yes      | Draft ID      |

Returns: `draft_id`, `message` (full message object with `to`, `from`, `subject`, `body`).

### Create draft

```
gmail create_draft --to "alice@example.com" --subject "Draft proposal" --body "Here is the draft content."
```

| Argument      | Type     | Required | Description                     |
|---------------|----------|----------|---------------------------------|
| `to`          | string   | yes      | Recipient email address         |
| `subject`     | string   | yes      | Email subject line              |
| `body`        | string   | yes      | Draft body                      |
| `cc`          | string   | no       | CC recipients (comma-separated) |
| `bcc`         | string   | no       | BCC recipients                  |
| `attachments` | string[] | no       | File paths to attach            |

Returns: `draft_id`, `message_id`.

### Update draft

```
gmail update_draft --draft_id "r123456789" --subject "Updated subject" --body "Revised content."
```

| Argument   | Type   | Required | Description                         |
|------------|--------|----------|-------------------------------------|
| `draft_id` | string | yes      | Draft ID to update                  |
| `to`       | string | no       | Updated recipient                   |
| `subject`  | string | no       | Updated subject                     |
| `body`     | string | no       | Updated body                        |
| `cc`       | string | no       | Updated CC recipients               |
| `bcc`      | string | no       | Updated BCC recipients              |

Returns: `draft_id`, `message_id`.

### Send draft

```
gmail send_draft --draft_id "r123456789"
```

| Argument   | Type   | Required | Description        |
|------------|--------|----------|--------------------|
| `draft_id` | string | yes      | Draft ID to send   |

Returns: `id`, `thread_id`, `label_ids`.

### Delete draft

```
gmail delete_draft --draft_id "r123456789"
```

| Argument   | Type   | Required | Description          |
|------------|--------|----------|----------------------|
| `draft_id` | string | yes      | Draft ID to delete   |

Returns: confirmation status.

## Labels

### List labels

```
gmail list_labels
```

Returns: list of `id`, `name`, `type` (`system` or `user`), `message_list_visibility`, `label_list_visibility`.

### Get label

```
gmail get_label --label_id "Label_42"
```

| Argument   | Type   | Required | Description |
|------------|--------|----------|-------------|
| `label_id` | string | yes      | Label ID    |

Returns: `id`, `name`, `type`, `messages_total`, `messages_unread`, `threads_total`, `threads_unread`.

### Create label

```
gmail create_label --name "Projects/AgentOS"
```

| Argument | Type   | Required | Description    |
|----------|--------|----------|----------------|
| `name`   | string | yes      | Label name     |

Returns: `id`, `name`.

### Update label

```
gmail update_label --label_id "Label_42" --name "Projects/Archived"
```

| Argument   | Type   | Required | Description      |
|------------|--------|----------|------------------|
| `label_id` | string | yes      | Label ID         |
| `name`     | string | yes      | New label name   |

Returns: `id`, `name`.

### Delete label

```
gmail delete_label --label_id "Label_42"
```

| Argument   | Type   | Required | Description        |
|------------|--------|----------|--------------------|
| `label_id` | string | yes      | Label ID to delete |

Returns: confirmation status.

### Add label to message

```
gmail add_label --message_id "18a1b2c3d4e5f6" --label_id "Label_42"
```

| Argument     | Type   | Required | Description       |
|--------------|--------|----------|-------------------|
| `message_id` | string | yes      | Message ID        |
| `label_id`   | string | yes      | Label ID to add   |

Returns: `id`, `label_ids`.

### Remove label from message

```
gmail remove_label --message_id "18a1b2c3d4e5f6" --label_id "Label_42"
```

| Argument     | Type   | Required | Description          |
|--------------|--------|----------|----------------------|
| `message_id` | string | yes      | Message ID           |
| `label_id`   | string | yes      | Label ID to remove   |

Returns: `id`, `label_ids`.

## Threads

### List threads

```
gmail list_threads --query "subject:standup" --max_results 10
```

| Argument      | Type   | Required | Default | Description                    |
|---------------|--------|----------|---------|--------------------------------|
| `query`       | string | no       |         | Gmail search query             |
| `max_results` | int    | no       | 10      | Threads to return (1-500)      |

Returns: list of `thread_id`, `snippet`, `history_id`.

### Get thread

```
gmail get_thread --thread_id "18a1b2c3d4e5f6"
```

| Argument    | Type   | Required | Description |
|-------------|--------|----------|-------------|
| `thread_id` | string | yes      | Thread ID   |

Returns: `thread_id`, `messages` (list of full message objects in chronological order).

### Trash thread

```
gmail trash_thread --thread_id "18a1b2c3d4e5f6"
```

| Argument    | Type   | Required | Description           |
|-------------|--------|----------|-----------------------|
| `thread_id` | string | yes      | Thread ID to trash    |

Returns: `thread_id`.

### Untrash thread

```
gmail untrash_thread --thread_id "18a1b2c3d4e5f6"
```

| Argument    | Type   | Required | Description             |
|-------------|--------|----------|-------------------------|
| `thread_id` | string | yes      | Thread ID to untrash    |

Returns: `thread_id`.

## Attachments

### Get attachment metadata

```
gmail get_attachment --message_id "18a1b2c3d4e5f6" --attachment_id "ANGjdJ8..."
```

| Argument        | Type   | Required | Description          |
|-----------------|--------|----------|----------------------|
| `message_id`    | string | yes      | Parent message ID    |
| `attachment_id` | string | yes      | Attachment ID        |

Returns: `attachment_id`, `size`, `data` (base64-encoded content).

### Download attachment

```
gmail download_attachment --message_id "18a1b2c3d4e5f6" --attachment_id "ANGjdJ8..." --output_path "/tmp/report.pdf"
```

| Argument        | Type   | Required | Description                      |
|-----------------|--------|----------|----------------------------------|
| `message_id`    | string | yes      | Parent message ID                |
| `attachment_id` | string | yes      | Attachment ID                    |
| `output_path`   | string | yes      | Local file path to save to       |

Returns: `file_path`, `size`, `mime_type`.

## Workflow

1. **Start with `gmail search` or `gmail list_messages`** to find messages. Never guess message IDs.
2. Use `get_message` to read full content including body and attachment metadata.
3. Reply or forward using the `message_id` from a retrieved message.
4. Manage organization with labels: create labels, then add/remove them from messages.
5. Use threads to follow entire conversations: `list_threads` to find, `get_thread` to read all messages.
6. For drafts: create, review with `get_draft`, update if needed, then `send_draft`.
7. Download attachments to local paths before processing them.

## Safety notes

- **`delete_message` is permanent.** Use `trash_message` unless permanent deletion is explicitly requested.
- Message and thread IDs are opaque strings. **Never fabricate them** -- always discover via search or list operations.
- Gmail API is rate-limited. Avoid rapid loops of send operations.
- Only messages in the authenticated account are accessible.
- Attachments can be large. Check `size` before downloading.
- Gmail search syntax supports operators like `from:`, `to:`, `subject:`, `has:attachment`, `after:`, `before:`, `is:unread`, `label:`, and boolean `OR` / `-` for exclusion.
- Sending emails is irreversible. Confirm recipient and content with the user before calling `send_message`.
