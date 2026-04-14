# @harro/skill-runner

> Lightweight poll-based skill execution client for EnterpriseAgentOS — registers with the backend, polls for jobs, executes skill actions, and hot-syncs custom skills from the platform.

## Highlights

- **Poll-based execution.** Registers with the backend, then polls `/api/runner/jobs` for work. No inbound ports needed — works behind NATs, in containers, on edge.
- **Hot skill sync.** Every 60s, checks the platform for new/updated skills, downloads zip bundles, builds with esbuild, and hot-loads them into the executor — zero downtime.
- **Same executor as skill-runtime.** Shares `SkillExecutor` + `createSandboxedContext` — Zod validation, credential injection, and sandboxed fetch are identical.
- **Single binary deployment.** `npm run build` produces a self-contained `dist/client.js` + bundled skills. One `node dist/client.js` to run.

## How it differs from skill-runtime

|                     | **skill-runtime**     | **skill-runner**           |
| ------------------- | --------------------- | -------------------------- |
| **Transport**       | HTTP server (`:3001`) | Poll-based client          |
| **Initiated by**    | Backend calls runtime | Runner polls backend       |
| **Deployment**      | Sidecar in K8s        | Anywhere (edge, local, VM) |
| **Skill sync**      | Rebuild + redeploy    | Hot-sync from platform     |
| **Browser support** | Playwright built in   | No browser skill           |

The **skill-runtime** is the primary executor inside the cluster. The **skill-runner** is for remote/edge execution — it can run on a user's machine, a DigitalOcean droplet, or any environment that can reach the backend API.

## Architecture

```
Backend (C#)                          Skill Runner (Node.js)
    │                                      │
    │◀── POST /api/runner/register ────────│  (1) Register on startup
    │                                      │
    │◀── GET  /api/runner/jobs ────────────│  (2) Poll every 3s
    │── job {skill, action, params} ──────▶│
    │                                      │── validate(Zod) ── execute()
    │◀── POST /api/runner/jobs/:id/result ─│  (3) Return result
    │                                      │
    │◀── POST /api/runner/heartbeat ───────│  (4) Heartbeat every 30s
    │                                      │
    │◀── GET  /api/runner/skills ──────────│  (5) Sync skills every 60s
    │── skill list + download ────────────▶│── esbuild ── hot-load
```

## Source layout

```
src/
├── client.ts      Entry point — registration, poll loop, heartbeat, skill sync
├── executor.ts    SkillExecutor — Zod validation + sandboxed execution
├── sandbox.ts     createSandboxedContext — credential injection + fetch scoping
└── builder.ts     buildSkill() — esbuild bundler for hot-synced skill zips
```

## Build

```bash
cd packages/skill-runner
npm install
npm run build    # Bundles client + all skills from packages/skills/
```

The build process:

1. `build.js` — esbuild bundles the runner client (`dist/client.js`) and each skill from `packages/skills/` into `dist/skills/`
2. Skills are discovered automatically — any directory in `packages/skills/` with a `skill.ts` gets bundled

## Run

```bash
PLATFORM_URL=https://api.officeos.co REGISTRATION_TOKEN=<token> node dist/client.js
```

### Environment variables

| Variable             | Required | Description                                      |
| -------------------- | -------- | ------------------------------------------------ |
| `PLATFORM_URL`       | yes      | Backend API URL (e.g. `https://api.officeos.co`) |
| `REGISTRATION_TOKEN` | yes      | Token for runner authentication with the backend |

### Docker

```bash
docker build -f packages/skill-runner/Dockerfile -t eaos-skill-runner .
docker run -e PLATFORM_URL=https://api.officeos.co -e REGISTRATION_TOKEN=<token> eaos-skill-runner
```

The image is published to `harkro123/eaos-skill-runner:latest` via CI.

## Skill sync flow

1. Runner calls `GET /api/runner/skills` to get the list of platform-managed skills with `updatedAt` timestamps
2. Compares against local `.skill-sync-state.json` to find new/updated skills
3. Downloads skill zip from `GET /api/runner/skills/:name/download`
4. Extracts zip, finds `skill.ts`, builds with esbuild via `buildSkill()`
5. Hot-loads the compiled `.js` into the `SkillExecutor` — immediately available for the next job
6. Updates sync state to avoid re-downloading

## Adding skills

Skills are defined in `packages/skills/` using `@harro/skill-sdk`. Each skill is self-contained:

```
packages/skills/postgres/
├── package.json      # Dependencies (bun test, @harro/skill-sdk)
├── SKILL.md          # CLI-style spec for agents
├── REFERENCES.md     # Source OSS repo links
├── skill.test.ts     # Contract tests (bun:test)
└── skill.ts          # Implementation (defineSkill + actions)
```

The runner auto-bundles any skill with a `skill.ts` at build time. No runner code changes needed.

For the SDK reference, see [`@harro/skill-sdk`](../skill-sdk/).

## Feedback and contributing

Found a bug or want a new feature? [Open an issue](https://github.com/harrokrog/EnterpriseAgentOs/issues) on the EnterpriseAgentOS repo.
