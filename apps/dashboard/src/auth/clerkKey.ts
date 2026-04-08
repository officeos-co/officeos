// Legacy publishable-key gating logic.
//
// IMPORTANT: keep this file dependency-free (no `"use client"`, no React)
// so it can be used from both client and server/edge entrypoints.
// This is retained only for backward-compatible env-var checks during migration.

export function isLikelyValidClerkPublishableKey(
  key: string | undefined,
): key is string {
  if (!key) return false;

  // Publishable keys looked like: pk_test_... or pk_live_...
  const m = /^pk_(test|live)_([A-Za-z0-9]+)$/.exec(key);
  if (!m) return false;

  const body = m[2];
  if (body.length < 16) return false;
  if (/^0+$/.test(body)) return false;

  return true;
}
