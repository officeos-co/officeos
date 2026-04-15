# Firebase Skill — References

## Firestore REST API
- **Endpoint**: `https://firestore.googleapis.com/v1/projects/{project}/databases/(default)/documents`
- **Docs**: https://firebase.google.com/docs/firestore/reference/rest/v1/projects.databases.documents
- **Auth**: `?key={api_key}` query param for client-accessible rules, or OAuth2 for admin access

## Firebase Auth REST API
- **Endpoint**: `https://identitytoolkit.googleapis.com/v1/accounts`
- **Docs**: https://firebase.google.com/docs/reference/rest/auth
- **Auth**: `?key={api_key}` query param

## Cloud Functions REST API
- **Endpoint**: `https://cloudfunctions.googleapis.com/v2/projects/{project}/locations`
- **Docs**: https://cloud.google.com/functions/docs/reference/rest/v2/projects.locations.functions
- **Note**: Listing/describing functions requires an OAuth2 token, not just the API key. The skill uses `api_key` for Firestore/Auth and service account token for Functions if available.

## Firebase Admin SDK (reference)
- **Source**: https://github.com/firebase/firebase-admin-node
- **License**: Apache-2.0

## Key Firestore Concepts
- Documents are stored as `{ fields: { fieldName: { stringValue|integerValue|booleanValue|... } } }`
- Collection path: `projects/{project}/databases/(default)/documents/{collectionId}`
- Document path: `projects/{project}/databases/(default)/documents/{collectionId}/{documentId}`
- Queries use `runQuery` endpoint with `StructuredQuery`

## Rate Limits
- Firestore: 1 write/second per document, 1 million reads/day (Spark), unlimited (Blaze)
- Auth: 100 requests/second
