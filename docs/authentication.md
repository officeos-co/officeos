# Authentication

> How dashboard users authenticate via Google OAuth, and how sessions work.

## Overview

The dashboard is protected by Google OAuth 2.0. The entire OAuth flow is handled server-side by the backend — the frontend just redirects to `/api/auth/google` and reads session state from `/api/auth/me`.

```
Browser                          Backend                          Google
   │                               │                                │
   │  GET /api/auth/google         │                                │
   │ ────────────────────────────▶ │                                │
   │                               │  Set CSRF state cookie         │
   │  ◀── 302 Redirect ────────── │                                │
   │                               │                                │
   │  Consent screen               │                                │
   │ ──────────────────────────────────────────────────────────────▶│
   │                               │                                │
   │  ◀─────── 302 Redirect (code + state) ────────────────────────│
   │                               │                                │
   │  GET /api/auth/callback/google│                                │
   │ ────────────────────────────▶ │                                │
   │                               │  Validate CSRF state           │
   │                               │  Exchange code for tokens      │
   │                               │ ──────────────────────────────▶│
   │                               │  ◀──── access_token ──────────│
   │                               │                                │
   │                               │  Fetch userinfo                │
   │                               │ ──────────────────────────────▶│
   │                               │  ◀──── email, name, avatar ───│
   │                               │                                │
   │                               │  Upsert UserRecord             │
   │                               │  Create SessionRecord          │
   │  ◀── Set eaos-session cookie  │                                │
   │  ◀── 302 Redirect to /        │                                │
   │                               │                                │
   │  Authenticated                │                                │
```

## Session model

Sessions are **server-side in Postgres** — no JWTs. The `eaos-session` cookie contains a random token; the backend stores the SHA-256 hash.

| Property | Value |
|----------|-------|
| Cookie name | `eaos-session` |
| HttpOnly | Yes |
| Secure | Yes (production), No (localhost) |
| SameSite | Lax |
| Expiry | 7 days |
| Storage | `SessionRecord` in Postgres |

### Why not JWT

JWTs can't be revoked without a blocklist, are larger, and add unnecessary complexity for a server-rendered dashboard. Cookie + server session is simpler and more secure.

## Middleware (`SessionAuthMiddleware`)

Runs on every request. Reads the `eaos-session` cookie, hashes it, looks up the `SessionRecord` (with eager-loaded `UserRecord`), and sets `HttpContext.Items["User"]`.

### Skipped paths

These paths use their own auth mechanisms:

| Path prefix | Auth mechanism |
|-------------|---------------|
| `/api/auth/` | Public (OAuth flow) |
| `/api/health`, `/healthz` | Public (health checks) |
| `/api/graphql` | Agent UUID bearer token (`AgentAuthInterceptor`) |
| `/api/agents/me/` | Agent UUID bearer token (`AgentTokenAuthAttribute`) |
| `/api/runner/` | Runner bearer token (`RunnerAuthAttribute`) |

### Unauthenticated requests

If no valid session cookie is found, `HttpContext.Items["User"]` is `null`. Controllers check this and return 401 as needed. The middleware does **not** short-circuit the pipeline — it lets the request through, so unauthenticated paths work normally.

## Frontend

### `useAuth` hook

Calls `GET /api/auth/me` on mount. Returns `{ user, loading, isAuthenticated, logout }`.

### `AuthGuard` component

Wraps the root layout. Redirects to `/login` if not authenticated. Skips the check for the `/login` path itself.

### Login page

Single button: "Sign in with Google" → `window.location.href = "/api/auth/google"`. No client-side OAuth libraries.

### Sidebar

Shows the authenticated user's name, email, and Google avatar. "Sign out" button calls `POST /api/auth/logout` and redirects to `/login`.

## Configuration

Google OAuth credentials in `appsettings.json`:

| Key | Description |
|-----|-------------|
| `GoogleOAuthClientId` | OAuth client ID from Google Cloud Console |
| `GoogleOAuthClientSecret` | OAuth client secret |
| `GoogleOAuthRedirectUri` | Callback URL (must match Google Console) |

Typed config: `Properties/GoogleOAuthConfig.cs`, registered as singleton in `Program.cs`.

## Database models

### `UserRecord`

| Column | Type | Description |
|--------|------|-------------|
| `Id` | Guid | Primary key |
| `Email` | string | Google email (unique) |
| `Name` | string? | Display name |
| `AvatarUrl` | string? | Google profile picture |
| `GoogleSubjectId` | string | Google `sub` claim (unique) |
| `CreatedAt` | DateTime | First login |
| `LastLoginAt` | DateTime | Most recent login |

### `SessionRecord`

| Column | Type | Description |
|--------|------|-------------|
| `Id` | Guid | Primary key |
| `UserId` | Guid | FK to UserRecord |
| `TokenHash` | string | SHA-256 of session cookie (unique) |
| `ExpiresAt` | DateTime | Session expiry |
| `CreatedAt` | DateTime | When created |

## Key files

| File | Purpose |
|------|---------|
| `Entities/Auth/AuthController.cs` | Google OAuth endpoints (login, callback, logout, me) |
| `Entities/Auth/SessionAuthMiddleware.cs` | Cookie-based session validation |
| `Entities/Auth/UserRepository.cs` | User upsert by Google subject ID |
| `Entities/Auth/SessionRepository.cs` | Session CRUD |
| `Properties/GoogleOAuthConfig.cs` | Typed config for OAuth credentials |
| `apps/v2-frontend/src/hooks/useAuth.ts` | Auth state hook |
| `apps/v2-frontend/src/components/AuthGuard.tsx` | Route protection |
| `apps/v2-frontend/src/app/login/page.tsx` | Login page |
