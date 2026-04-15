# S3 Skill

Interact with AWS S3 using the S3 REST API with AWS Signature Version 4 authentication.

## Credentials
- `access_key_id` — AWS Access Key ID
- `secret_access_key` — AWS Secret Access Key
- `region` — AWS region (e.g. `us-east-1`, `eu-west-1`)

---

## Buckets

### `buckets_list`
List all S3 buckets owned by the authenticated user.
Returns: `[{ name, creationDate }]`

### `buckets_create`
Create a new S3 bucket.
Params: `bucket` (string), `region` (string, optional — overrides credential region)
Returns: `{ bucket, location }`

### `buckets_delete`
Delete an empty bucket.
Params: `bucket` (string)
Returns: `{ success: boolean }`

### `buckets_head`
Check if a bucket exists and you have access to it.
Params: `bucket` (string)
Returns: `{ exists: boolean, region: string }`

---

## Objects

### `objects_list`
List objects in a bucket (up to 1000 per call, with pagination token).
Params: `bucket` (string), `prefix` (string, optional), `max_keys` (number, default: 1000), `continuation_token` (string, optional)
Returns: `{ objects: [{ key, size, lastModified, etag }], next_continuation_token, is_truncated }`

### `objects_get`
Download an object and return its content as text or base64.
Params: `bucket` (string), `key` (string), `encoding` (enum: "text" | "base64", default: "text")
Returns: `{ content: string, content_type: string, content_length: number, etag: string }`

### `objects_put`
Upload an object.
Params: `bucket` (string), `key` (string), `body` (string), `content_type` (string, default: "application/octet-stream"), `encoding` (enum: "text" | "base64", default: "text")
Returns: `{ etag: string }`

### `objects_delete`
Delete a single object.
Params: `bucket` (string), `key` (string)
Returns: `{ success: boolean }`

### `objects_delete_batch`
Delete up to 1000 objects in one request.
Params: `bucket` (string), `keys` (string[])
Returns: `{ deleted: string[], errors: [{ key, code, message }] }`

### `objects_head`
Get metadata for an object without downloading it.
Params: `bucket` (string), `key` (string)
Returns: `{ content_type, content_length, etag, last_modified, metadata: Record<string, string> }`

### `objects_copy`
Server-side copy an object within or between buckets.
Params: `source_bucket` (string), `source_key` (string), `dest_bucket` (string), `dest_key` (string)
Returns: `{ etag, last_modified }`

---

## Presigned URLs

### `presign_get`
Generate a presigned GET URL for temporary object access.
Params: `bucket` (string), `key` (string), `expires_in` (number seconds, default: 3600, max: 604800)
Returns: `{ url: string, expires_at: string }`

### `presign_put`
Generate a presigned PUT URL for direct browser/client uploads.
Params: `bucket` (string), `key` (string), `content_type` (string), `expires_in` (number seconds, default: 3600)
Returns: `{ url: string, expires_at: string }`

---

## Multipart Upload

### `multipart_create`
Initiate a multipart upload and return an upload ID.
Params: `bucket` (string), `key` (string), `content_type` (string, optional)
Returns: `{ upload_id: string }`

### `multipart_complete`
Complete a multipart upload by providing the ETags from all parts.
Params: `bucket` (string), `key` (string), `upload_id` (string), `parts` ([{ part_number: number, etag: string }])
Returns: `{ location: string, etag: string }`

### `multipart_abort`
Abort and clean up a multipart upload.
Params: `bucket` (string), `key` (string), `upload_id` (string)
Returns: `{ success: boolean }`

---

## Error Handling
All actions throw with the S3 error code and message extracted from the XML error response body.
