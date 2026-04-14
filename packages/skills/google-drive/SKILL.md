# Google Drive

List, search, upload, download, share, and manage files and folders via the Google Drive API v3.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Files

### List files

```
google-drive list_files --query "mimeType='application/pdf'" --page_size 20 --order_by "modifiedTime desc"
```

| Argument    | Type   | Required | Default            | Description                                         |
|-------------|--------|----------|--------------------|-----------------------------------------------------|
| `query`     | string | no       |                    | Drive search query (Drive query syntax)             |
| `page_size` | int    | no       | 10                 | Results per page (1-1000)                           |
| `order_by`  | string | no       | `modifiedTime desc`| Sort order (e.g. `name`, `modifiedTime desc`)       |
| `fields`    | string | no       |                    | Comma-separated fields to include in response       |

Returns: list of `id`, `name`, `mimeType`, `size`, `modifiedTime`, `parents`, `webViewLink`.

### Get file

```
google-drive get_file --file_id "1aBcDeFgHiJkLmNoPqRs"
```

| Argument  | Type   | Required | Description |
|-----------|--------|----------|-------------|
| `file_id` | string | yes      | File ID     |

Returns: `id`, `name`, `mimeType`, `size`, `createdTime`, `modifiedTime`, `parents`, `owners`, `webViewLink`, `webContentLink`, `shared`.

### Create file

```
google-drive create_file --name "notes.txt" --mime_type "text/plain" --parents '["1aBcDeFgHiJkLmNoPqRs"]' --content "These are my notes."
```

| Argument    | Type     | Required | Description                              |
|-------------|----------|----------|------------------------------------------|
| `name`      | string   | yes      | File name                                |
| `mime_type` | string   | no       | MIME type (auto-detected if omitted)     |
| `parents`   | string[] | no       | Parent folder IDs                        |
| `content`   | string   | no       | Text content for the file                |

Returns: `id`, `name`, `mimeType`, `webViewLink`.

### Update file

```
google-drive update_file --file_id "1aBcDeFgHiJkLmNoPqRs" --name "updated-notes.txt" --content "Updated content."
```

| Argument  | Type   | Required | Description                          |
|-----------|--------|----------|--------------------------------------|
| `file_id` | string | yes      | File ID to update                    |
| `name`    | string | no       | Updated file name                    |
| `content` | string | no       | Updated text content                 |

Returns: `id`, `name`, `modifiedTime`.

### Delete file

```
google-drive delete_file --file_id "1aBcDeFgHiJkLmNoPqRs"
```

| Argument  | Type   | Required | Description            |
|-----------|--------|----------|------------------------|
| `file_id` | string | yes      | File ID to delete      |

Returns: confirmation status.

### Copy file

```
google-drive copy_file --file_id "1aBcDeFgHiJkLmNoPqRs" --name "notes-copy.txt" --parents '["0BxDeFgHiJkLmNoPq"]'
```

| Argument  | Type     | Required | Description                     |
|-----------|----------|----------|---------------------------------|
| `file_id` | string   | yes      | File ID to copy                 |
| `name`    | string   | no       | Name for the copy               |
| `parents` | string[] | no       | Parent folder IDs for the copy  |

Returns: `id`, `name`, `webViewLink`.

### Move file

```
google-drive move_file --file_id "1aBcDeFgHiJkLmNoPqRs" --from_folder "0BxOldFolder" --to_folder "0BxNewFolder"
```

| Argument      | Type   | Required | Description               |
|---------------|--------|----------|---------------------------|
| `file_id`     | string | yes      | File ID to move           |
| `from_folder` | string | yes      | Current parent folder ID  |
| `to_folder`   | string | yes      | Destination folder ID     |

Returns: `id`, `name`, `parents`.

## Upload / Download

### Upload file

```
google-drive upload_file --local_path "/tmp/report.pdf" --parents '["0BxDeFgHiJkLmNoPq"]' --mime_type "application/pdf"
```

| Argument     | Type     | Required | Description                            |
|--------------|----------|----------|----------------------------------------|
| `local_path` | string   | yes      | Local file path to upload              |
| `parents`    | string[] | no       | Parent folder IDs                      |
| `mime_type`  | string   | no       | MIME type (auto-detected if omitted)   |

Returns: `id`, `name`, `mimeType`, `size`, `webViewLink`.

### Download file

```
google-drive download_file --file_id "1aBcDeFgHiJkLmNoPqRs" --output_path "/tmp/downloaded-report.pdf"
```

| Argument      | Type   | Required | Description                    |
|---------------|--------|----------|--------------------------------|
| `file_id`     | string | yes      | File ID to download            |
| `output_path` | string | yes      | Local path to save the file    |

Returns: `file_path`, `size`, `mimeType`.

### Export file

```
google-drive export_file --file_id "1aBcDeFgHiJkLmNoPqRs" --mime_type "application/pdf" --output_path "/tmp/document.pdf"
```

| Argument      | Type   | Required | Description                                           |
|---------------|--------|----------|-------------------------------------------------------|
| `file_id`     | string | yes      | Google Docs/Sheets/Slides file ID                     |
| `mime_type`   | string | yes      | Export MIME type (e.g. `application/pdf`, `text/csv`)  |
| `output_path` | string | yes      | Local path to save the exported file                  |

Returns: `file_path`, `size`, `mimeType`.

## Folders

### Create folder

```
google-drive create_folder --name "Project Assets" --parents '["0BxDeFgHiJkLmNoPq"]'
```

| Argument  | Type     | Required | Description              |
|-----------|----------|----------|--------------------------|
| `name`    | string   | yes      | Folder name              |
| `parents` | string[] | no       | Parent folder IDs        |

Returns: `id`, `name`, `webViewLink`.

### List folder contents

```
google-drive list_folder_contents --folder_id "0BxDeFgHiJkLmNoPq" --page_size 50
```

| Argument    | Type   | Required | Default | Description                |
|-------------|--------|----------|---------|----------------------------|
| `folder_id` | string | yes      |         | Folder ID to list          |
| `page_size` | int    | no       | 10      | Results per page (1-1000)  |

Returns: list of `id`, `name`, `mimeType`, `size`, `modifiedTime`.

## Search

### Search files

```
google-drive search --query "name contains 'report'" --full_text "quarterly revenue" --page_size 10
```

| Argument    | Type   | Required | Default | Description                                         |
|-------------|--------|----------|---------|-----------------------------------------------------|
| `query`     | string | no       |         | Drive query syntax (e.g. `mimeType='...'`)          |
| `full_text` | string | no       |         | Full-text search across file content                |
| `page_size` | int    | no       | 10      | Results per page (1-1000)                           |

Returns: list of `id`, `name`, `mimeType`, `size`, `modifiedTime`, `webViewLink`.

## Sharing

### Share file

```
google-drive share --file_id "1aBcDeFgHiJkLmNoPqRs" --email "alice@example.com" --role "writer" --type "user"
```

| Argument  | Type   | Required | Default | Description                                        |
|-----------|--------|----------|---------|----------------------------------------------------|
| `file_id` | string | yes      |         | File or folder ID to share                         |
| `email`   | string | yes      |         | Email address of the user or group                 |
| `role`    | string | no       | `reader`| `reader`, `writer`, `commenter`, or `owner`        |
| `type`    | string | no       | `user`  | `user`, `group`, `domain`, or `anyone`             |

Returns: `permission_id`, `role`, `type`, `email`.

### List permissions

```
google-drive list_permissions --file_id "1aBcDeFgHiJkLmNoPqRs"
```

| Argument  | Type   | Required | Description |
|-----------|--------|----------|-------------|
| `file_id` | string | yes      | File ID     |

Returns: list of `permission_id`, `role`, `type`, `email_address`, `display_name`.

### Remove permission

```
google-drive remove_permission --file_id "1aBcDeFgHiJkLmNoPqRs" --permission_id "12345678901234567890"
```

| Argument        | Type   | Required | Description              |
|-----------------|--------|----------|--------------------------|
| `file_id`       | string | yes      | File ID                  |
| `permission_id` | string | yes      | Permission ID to remove  |

Returns: confirmation status.

## Trash

### Trash file

```
google-drive trash_file --file_id "1aBcDeFgHiJkLmNoPqRs"
```

| Argument  | Type   | Required | Description         |
|-----------|--------|----------|---------------------|
| `file_id` | string | yes      | File ID to trash    |

Returns: `id`, `name`, `trashed`.

### Untrash file

```
google-drive untrash_file --file_id "1aBcDeFgHiJkLmNoPqRs"
```

| Argument  | Type   | Required | Description           |
|-----------|--------|----------|------------------------|
| `file_id` | string | yes      | File ID to restore    |

Returns: `id`, `name`, `trashed`.

### Empty trash

```
google-drive empty_trash
```

Returns: confirmation status.

## Workflow

1. **Start with `google-drive list_files` or `google-drive search`** to find files. Never guess file IDs.
2. Use `get_file` for detailed metadata about a specific file.
3. Use `list_folder_contents` to browse folder hierarchies.
4. For Google Docs/Sheets/Slides, use `export_file` to convert to standard formats (PDF, CSV, etc.).
5. For binary files, use `download_file` to save locally.
6. Use `upload_file` for local files and `create_file` for text content.
7. Manage access with `share`, `list_permissions`, and `remove_permission`.
8. Prefer `trash_file` over `delete_file` for safer removal.

## Safety notes

- File IDs are opaque strings. **Never fabricate them** -- always discover via search or list operations.
- **`delete_file` is permanent and bypasses trash.** Use `trash_file` unless permanent deletion is explicitly requested.
- **`empty_trash` is irreversible.** All trashed files are permanently deleted. Confirm with the user.
- `export_file` only works with Google Workspace files (Docs, Sheets, Slides). Use `download_file` for other file types.
- Drive query syntax uses operators like `name contains '...'`, `mimeType = '...'`, `modifiedTime > '...'`, `'folder_id' in parents`, and `trashed = false`.
- Only files accessible to the authenticated account are visible.
- Large file uploads/downloads may take significant time. Check file size before operations.
- Sharing with `role: "owner"` transfers ownership and cannot be undone by the original owner.
