"use client";

// Compatibility shim — local auth mode no longer exists.
// These functions are kept so existing imports don't break,
// but they always return false/null.

export function isLocalAuthMode(): boolean {
  return false;
}

export function setLocalAuthToken(_token: string): void {
  // no-op
}

export function getLocalAuthToken(): string | null {
  return null;
}

export function clearLocalAuthToken(): void {
  // no-op
}
