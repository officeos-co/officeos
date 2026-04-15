import { describe, it } from "bun:test";

describe("firebase skill", () => {
  describe("Firestore value conversion", () => {
    it.todo("should convert string to { stringValue }");
    it.todo("should convert integer to { integerValue: string }");
    it.todo("should convert float to { doubleValue }");
    it.todo("should convert boolean to { booleanValue }");
    it.todo("should convert null to { nullValue: null }");
    it.todo("should convert array to { arrayValue: { values: [...] } }");
    it.todo("should convert nested object to { mapValue: { fields: {...} } }");
    it.todo("should round-trip convert fromFirestoreValue(toFirestoreValue(v)) === v for primitives");
  });

  describe("collections_list", () => {
    it.todo("should POST to /{project}/databases/(default)/documents:listCollectionIds");
    it.todo("should include ?key=api_key in URL");
    it.todo("should return array of { collectionId } objects");
    it.todo("should use parent path for sub-collections when parent param is provided");
  });

  describe("documents_list", () => {
    it.todo("should GET /{project}/databases/(default)/documents/{collection}");
    it.todo("should include pageSize and pageToken query params");
    it.todo("should parse document name into id and path");
    it.todo("should convert Firestore field types to plain JS values");
    it.todo("should return next_page_token from response");
  });

  describe("documents_get", () => {
    it.todo("should GET the document at the specified path");
    it.todo("should return id extracted from document name");
    it.todo("should throw on 404 with Firestore error message");
  });

  describe("documents_create", () => {
    it.todo("should POST to collection path with fields body");
    it.todo("should convert plain JS fields to Firestore typed fields");
    it.todo("should return id and createTime");
  });

  describe("documents_set", () => {
    it.todo("should PATCH the document at specified path (full overwrite)");
    it.todo("should return updateTime");
  });

  describe("documents_update", () => {
    it.todo("should PATCH with updateMask.fieldPaths query params");
    it.todo("should default update_mask to keys of fields param");
  });

  describe("documents_delete", () => {
    it.todo("should DELETE the document at specified path");
    it.todo("should return { success: true }");
  });

  describe("documents_query", () => {
    it.todo("should POST :runQuery with structuredQuery body");
    it.todo("should map == operator to EQUAL");
    it.todo("should map array-contains to ARRAY_CONTAINS");
    it.todo("should use compositeFilter AND for multiple where conditions");
    it.todo("should include single fieldFilter directly for one where condition");
    it.todo("should include orderBy when order_by param is provided");
    it.todo("should filter out results without document field");
  });

  describe("auth_users_list", () => {
    it.todo("should POST to :query endpoint with ?key=api_key");
    it.todo("should include maxResults and optional nextPageToken in body");
    it.todo("should convert createdAt millisecond string to ISO date");
    it.todo("should return next_page_token from response");
  });

  describe("auth_users_get", () => {
    it.todo("should POST to :lookup with localId when uid is provided");
    it.todo("should POST to :lookup with email when email is provided");
    it.todo("should throw when neither uid nor email is provided");
    it.todo("should throw when user is not found in response");
  });

  describe("auth_users_create", () => {
    it.todo("should POST to :signup endpoint (empty path)");
    it.todo("should include email, password, disabled in body");
    it.todo("should include displayName when display_name is provided");
  });

  describe("auth_users_update", () => {
    it.todo("should POST to :update with localId and provided fields");
    it.todo("should use disableUser field for disabled param");
  });

  describe("auth_users_delete", () => {
    it.todo("should POST to :delete with localId");
    it.todo("should return { success: true }");
  });

  describe("functions_list", () => {
    it.todo("should GET cloudfunctions.googleapis.com/v2/projects/{project}/locations/{region}/functions");
    it.todo("should extract short function name from full resource name");
    it.todo("should return runtime from buildConfig.runtime");
  });

  describe("functions_describe", () => {
    it.todo("should GET the full function resource path");
    it.todo("should accept short name and construct full path");
    it.todo("should return entryPoint from buildConfig.entryPoint");
  });
});
