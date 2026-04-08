"use client";

// Google OAuth session auth — replaces Clerk but keeps the same hook API
// so all 50+ consumer files continue working without changes.

import type { ReactNode } from "react";

const TOKEN_KEY = "session_token";

export function getSessionToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
}

export function setSessionToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

export function clearSessionToken(): void {
  localStorage.removeItem(TOKEN_KEY);
}

function isSignedIn(): boolean {
  return Boolean(getSessionToken());
}

function parseTokenClaims(): { sub?: string; email?: string; name?: string } | null {
  const token = getSessionToken();
  if (!token) return null;
  try {
    const payload = token.split(".")[1];
    return JSON.parse(atob(payload));
  } catch {
    return null;
  }
}

// --- Public API (same shape as the old Clerk wrappers) ---

export function SignedIn(props: { children: ReactNode }) {
  return isSignedIn() ? <>{props.children}</> : null;
}

export function SignedOut(props: { children: ReactNode }) {
  return isSignedIn() ? null : <>{props.children}</>;
}

export function SignInButton(props: {
  children?: ReactNode;
  // Clerk-compat props accepted (and ignored) so callers can stay source-compatible
  // with Clerk's real SignInButton without conditional rendering.
  mode?: "modal" | "redirect";
  forceRedirectUrl?: string;
  signUpForceRedirectUrl?: string;
  fallbackRedirectUrl?: string;
  signUpFallbackRedirectUrl?: string;
}) {
  const handleSignIn = async () => {
    try {
      const baseUrl =
        process.env.NEXT_PUBLIC_API_URL === "auto"
          ? `${window.location.protocol}//${window.location.hostname}:8000`
          : process.env.NEXT_PUBLIC_API_URL || "http://localhost:8000";

      const resp = await fetch(`${baseUrl}/api/auth/google/url`);
      const data = await resp.json();
      if (data.url) {
        window.location.href = data.url;
      }
    } catch (err) {
      console.error("Failed to get Google auth URL:", err);
    }
  };

  return (
    <button onClick={handleSignIn}>
      {props.children || "Sign in with Google"}
    </button>
  );
}

export function SignOutButton(props: { children?: ReactNode }) {
  const handleSignOut = () => {
    clearSessionToken();
    window.location.href = "/";
  };

  return (
    <button onClick={handleSignOut}>
      {props.children || "Sign out"}
    </button>
  );
}

export function useUser() {
  const claims = parseTokenClaims();
  return {
    isLoaded: true,
    isSignedIn: isSignedIn(),
    user: claims
      ? {
          id: claims.sub ?? claims.email ?? "local-user",
          primaryEmailAddress: { emailAddress: claims.email },
          fullName: claims.name,
          imageUrl: null,
        }
      : null,
  } as const;
}

export function useAuth() {
  const token = getSessionToken();
  const claims = parseTokenClaims();
  return {
    isLoaded: true,
    isSignedIn: Boolean(token),
    userId: claims?.sub ?? null,
    sessionId: token ? "session" : null,
    getToken: async () => token,
  } as const;
}

// Always false — Clerk has been fully replaced by Google OAuth.
export function isClerkEnabled(): boolean {
  return false;
}

// Kept for compatibility — no-op wrapper (no external provider needed)
export function ClerkProvider(props: { children: ReactNode; publishableKey?: string; afterSignOutUrl?: string }) {
  return <>{props.children}</>;
}
