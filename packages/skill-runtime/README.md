# @harro/skill-runtime

> Node.js execution service for EnterpriseAgentOS skills — validates, sandboxes, and runs skill actions behind an HTTP API.

## Highlights

- **Pure executor.** The runtime only runs skill actions. Skill metadata (manifests) are extracted at build time and seeded into the database by CI — the runtime never determines what skills exist.
- **Zero-config skill discovery.** Bundles all skills from `packages/skills/` at build time. Drop in a new `skill.ts`, rebuild, done.
- **Zod-powered validation.** Every action call is validated against the skill's Zod schema before execution. Bad params get a structured error, not a crash.
- **Browser sessions built in.** The `browser` skill gets a managed Playwright session with cookie persistence. Other skills get a sandboxed context with injected credentials.
- **Build-time manifest extraction.** `npm run build` bundles skills AND extracts JSON manifests to `dist/manifests/`. CI seeds these directly into the database.

## Overview

The skill runtime is the bridge between the C# backend and TypeScript skill code. It runs as a sidecar service in Kubernetes and exposes an HTTP API for executing skill actions.

```
CI Pipeline                         Database (Postgres)
    │                                    │
    ├── build → extract manifests ─────▶ │ POST /api/internal/seed-manifests
    │                                    │
    │                                    │
Backend (C#)                        Skill Runtime (Node.js)              Skills (TypeScript)
    │                                    │                                    │
    ├── POST /execute ─────────────────▶ │ ── validate(Zod) ── execute() ──▶ │
    │   {skill, action, params,          │    (sandbox context injected)      │
    │    credentials, sessionContext}    │                                    │
    │◀── {success, result, error} ──────│◀───────────────── return ──────────│
```

The database is the source of truth for skill metadata. The backend reads skill manifests from the database — not from the runtime. CI extracts manifests at build time and seeds them directly via `POST /api/internal/seed-manifests`.

This package is part of [EnterpriseAgentOS](https://github.com/harrokrog/EnterpriseAgentOs).

## API

### `POST /execute`

Execute a skill action with validated parameters and injected credentials.

```json
{
  "skill": "notion",
  "action": "search",
  "params": { "query": "meeting notes" },
  "credentials": { "token": "ntn_..." },
  "sessionContext": { "sessionId": "optional-browser-session-id" }
}
```

Response:

```json
{
  "success": true,
  "result": [{ "id": "abc-123", "title": "Q1 Meeting Notes" }]
}
```

### `POST /build`

Build a custom skill from source files, hot-load it into the runtime, and return its manifest.

### `POST /install` / `POST /uninstall`

Install or unload a registry-published skill at runtime.

### `GET /manifests` (debug only)

Lists all currently loaded skill manifests. Not used by production code — manifests are seeded from build artifacts.

## Build

```bash
cd packages/skill-runtime
npm install
npm run build    # Bundles skills + extracts manifests to dist/manifests/
npm start        # Starts HTTP server on :3001
```

The build process:
1. `build.js` — esbuild bundles the runtime server, each skill, and the manifest extractor
2. `extract-manifests.js` — imports each bundled skill, calls `extractManifest()`, writes JSON to `dist/manifests/`

### Environment variables

| Variable | Default | Description |
| --- | --- | --- |
| `PORT` | `3001` | HTTP server port |
| `BACKEND_URL` | `http://localhost:5000` | Backend URL for fetching the skill registry |

### Docker

```bash
docker build -f packages/skill-runtime/Dockerfile -t eaos-skill-runtime .
docker run -p 3001:3001 eaos-skill-runtime
```

The Dockerfile installs Playwright and Chromium for the browser skill. The image is published to `harkro123/eaos-skill-runtime:latest` via CI.

## Feedback and contributing

Found a bug or want a new feature? [Open an issue](https://github.com/harrokrog/EnterpriseAgentOs/issues) on the EnterpriseAgentOS repo.

To create a new skill, see [`packages/skill-sdk`](../skill-sdk/) and the [skill system docs](https://github.com/harrokrog/EnterpriseAgentOs/blob/main/docs/skills.md).
