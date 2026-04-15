# Firebase Skill

Interact with Firebase: Firestore (CRUD, queries, collections), Firebase Auth (users), and Cloud Functions (list, describe) via the Firebase REST APIs.

## Credentials
- `project_id` — Firebase/GCP project ID (e.g. `my-app-12345`)
- `api_key` — Firebase Web API Key (found in Project Settings > General > Web API Key)

---

## Firestore — Collections

### `collections_list`
List top-level collections in the Firestore default database.
Params: `parent` (string, optional — parent document path to list sub-collections, default: root)
Returns: `[{ collectionId: string }]`

---

## Firestore — Documents

### `documents_list`
List documents in a Firestore collection.
Params: `collection` (string — collection path, e.g. "users"), `page_size` (number, default: 20), `page_token` (string, optional)
Returns: `{ documents: [{ id, path, fields, createTime, updateTime }], next_page_token }`

### `documents_get`
Get a single Firestore document by path.
Params: `path` (string — e.g. "users/user123")
Returns: `{ id, path, fields, createTime, updateTime }`

### `documents_create`
Create a new Firestore document with an auto-generated ID.
Params: `collection` (string), `fields` (Record<string, unknown> — plain JS values, auto-converted to Firestore types)
Returns: `{ id, path, createTime }`

### `documents_set`
Create or overwrite a Firestore document with a specific ID.
Params: `path` (string — full document path, e.g. "users/user123"), `fields` (Record<string, unknown>)
Returns: `{ id, path, updateTime }`

### `documents_update`
Update specific fields of a Firestore document (partial update using field mask).
Params: `path` (string), `fields` (Record<string, unknown>), `update_mask` (string[], optional — field names to update)
Returns: `{ id, path, updateTime }`

### `documents_delete`
Delete a Firestore document.
Params: `path` (string)
Returns: `{ success: boolean }`

### `documents_query`
Run a structured query against a Firestore collection.
Params:
- `collection` (string) — collection to query
- `where` (optional) — `[{ field: string, op: "==" | "<" | "<=" | ">" | ">=" | "!=" | "array-contains", value: unknown }]`
- `order_by` (optional) — `{ field: string, direction: "ASCENDING" | "DESCENDING" }`
- `limit` (number, default: 20)
Returns: `[{ id, path, fields }]`

---

## Firebase Auth

### `auth_users_list`
List Firebase Auth users (up to 1000 per call).
Params: `max_results` (number, default: 100), `page_token` (string, optional)
Returns: `{ users: [{ uid, email, displayName, disabled, createdAt }], next_page_token }`

### `auth_users_get`
Get a Firebase Auth user by UID or email.
Params: `uid` (string, optional), `email` (string, optional) — one required
Returns: `{ uid, email, displayName, photoUrl, disabled, createdAt, lastSignIn }`

### `auth_users_create`
Create a new Firebase Auth user.
Params: `email` (string), `password` (string), `display_name` (string, optional), `disabled` (boolean, default: false)
Returns: `{ uid, email, displayName }`

### `auth_users_update`
Update a Firebase Auth user.
Params: `uid` (string), `email` (string, optional), `display_name` (string, optional), `disabled` (boolean, optional), `password` (string, optional)
Returns: `{ uid, email, displayName, disabled }`

### `auth_users_delete`
Delete a Firebase Auth user by UID.
Params: `uid` (string)
Returns: `{ success: boolean }`

---

## Cloud Functions

### `functions_list`
List Cloud Functions in the project.
Params: `region` (string, default: "us-central1")
Returns: `[{ name, state, runtime, httpsTrigger, updateTime }]`

### `functions_describe`
Get details of a specific Cloud Function.
Params: `function_name` (string — full name or short name), `region` (string, default: "us-central1")
Returns: `{ name, state, runtime, httpsTrigger, entryPoint, updateTime }`

---

## Firestore Value Types
Fields are automatically converted:
- JS `string` → `stringValue`
- JS `number` (integer) → `integerValue`
- JS `number` (float) → `doubleValue`
- JS `boolean` → `booleanValue`
- JS `null` → `nullValue`
- JS `Array` → `arrayValue`
- JS `Object` → `mapValue`
