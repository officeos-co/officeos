# @harro/skill-runtime

> Node.js execution service for EnterpriseAgentOS skills — loads, validates, and runs skill actions behind an HTTP API.

## Highlights

- **Zero-config skill discovery.** Bundles all skills from `packages/skills/` at build time. Drop in a new `skill.ts`, rebuild, done.
- **Dynamic registry.** Can also load skills at runtime from the backend's skill registry — no Docker rebuild needed for registry-published skills.
- **Zod-powered validation.** Every action call is validated against the skill's Zod schema before execution. Bad params get a structured error, not a crash.
- **Browser sessions built in.** The `browser` skill gets a managed Playwright session with cookie persistence. Other skills get a sandboxed context with injected credentials.
- **GraphQL-ready manifests.** Exposes skill metadata as JSON manifests that the backend's `SkillTypeModule` converts into a dynamic GraphQL schema.

## Overview

The skill runtime is the bridge between the C# backend and TypeScript skill code. It runs as a sidecar service in Kubernetes, exposes two HTTP endpoints (`/manifest` and `/execute`), and manages the lifecycle of skill execution.

```
Backend (C#)                    Skill Runtime (Node.js)              Skills (TypeScript)
    │                                 │                                    │
    ├── GET /manifest ──────────────▶ │ ── loadSkills() ─────────────────▶ │
    │   (GraphQL schema generation)   │    (reads dist/skills/*.js)        │
    │                                 │                                    │
    ├── POST /execute ──────────────▶ │ ── validate(Zod) ── execute() ──▶ │
    │   {skill, action, params,       │    (sandbox context injected)      │
    │    credentials, sessionContext} │                                    │
    │◀── {success, result, error} ───│◀───────────────── return ──────────│
```

The backend never runs skill code directly. It sends an execute request with decrypted credentials, the runtime validates params, creates a sandboxed context, runs the action, and returns the result.

This package is part of [EnterpriseAgentOS](https://github.com/harrokrog/EnterpriseAgentOs).

## API

### `GET /manifest`

Returns all loaded skill manifests. Used by the backend at startup to seed the skill registry and generate GraphQL types.

```json
[
  {
    "name": "notion",
    "title": "Notion",
    "emoji": "📝",
    "description": "Read and write Notion pages and databases.",
    "doc": "# Notion Skill\n...",
    "actions": {
      "search": {
        "description": "Search across all pages",
        "parameters": { "type": "object", "properties": { "query": { "type": "string" } } },
        "returns": { "type": "array" }
      }
    }
  }
]
```

### `GET /manifest/:name`

Returns the manifest for a single skill by name.

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

### `POST /install`

Install a skill from npm at runtime (registry skills).

### `POST /uninstall`

Unload a registry-installed skill.

## Installation

```bash
cd packages/skill-runtime
npm install
npm run build    # Bundles all skills + runtime into dist/
npm start        # Starts HTTP server on :3001
```

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
