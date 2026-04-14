# Self-Hosted Runners

> How customers execute skills inside their own network using a polling-based runner.

## Overview

Runners let customers execute skills against internal APIs (ERPs, databases, proprietary services) that are behind their firewall. The runner is a Docker container that polls our backend for jobs — no tunnels, no firewall changes, no inbound ports.

```
Customer Network                    EAOS Platform
┌──────────────────┐               ┌──────────────────┐
│ skill-runner     │  HTTPS poll   │ backend       │
│ (Docker)         │ ═══════════>  │                  │
│                  │  POST result  │ Job Queue (PG)   │
│ executes skills  │ ═══════════>  │                  │
│ against internal │               │ RunnerJobWaiter  │
│ APIs             │               │ (TCS in-memory)  │
└──────────────────┘               └──────────────────┘
```

## Why polling (not tunnels)

| Approach          | Downside                                            |
| ----------------- | --------------------------------------------------- |
| Cloudflare Tunnel | Extra dependency, customer must install + configure |
| Tailscale         | VPN complexity, network-level access is overkill    |
| WebSocket relay   | Persistent connection to maintain, reconnect logic  |
| **Polling**       | Simple, stateless, works behind any firewall        |

The runner makes outbound HTTPS requests to our API every 3 seconds. That's it. No special network config, no persistent connections, no third-party dependencies.

## Customer experience

Two ways to register a runner:

### Option A: Device authorization flow (recommended)

GitHub-style browser-based authentication. No manual token copy-paste.

```bash
docker run \
  -e PLATFORM_URL=https://api.officeos.co \
  harkro123/skill-runner
```

The runner displays a URL and a short code. The user opens the URL in their browser, confirms the code, and clicks Approve. The runner is authenticated automatically.

### Option B: Registration token (legacy)

```bash
docker run \
  -e PLATFORM_URL=https://api.officeos.co \
  -e REGISTRATION_TOKEN=sr_xxxx \
  harkro123/skill-runner
```

Two env vars. Customer bundles their custom skills into the image or mounts them as a volume.

## Runner lifecycle

### Registration (device flow — recommended)

Follows RFC 8628 (OAuth 2.0 Device Authorization Grant), similar to GitHub CLI's `gh auth login`.

```
Runner starts: POST /api/runner/device/code { runnerName? }
    ↓
Backend generates device_code (UUID) + user_code (XXXX-XXXX)
Returns: { device_code, user_code, verification_uri, expires_in, interval }
    ↓
Runner displays: "Go to https://api.../device/verify?code=ABCD-1234"
    ↓
User opens URL in browser (must be logged in to dashboard)
    ↓
Browser shows code confirmation page, user clicks "Approve"
    ↓
POST /api/runner/device/approve (session-authenticated)
    ↓
Runner's next poll to POST /api/runner/device/token { device_code }
    ↓
Backend: approved! Creates RunnerRecord, generates auth token
Returns: { auth_token, runner_id, name }
    ↓
Runner stores auth token locally, starts polling for jobs
```

**Security:**

- Device codes expire after 15 minutes
- User codes are 8 characters (XXXX-XXXX), ambiguity-free charset (no 0/O/1/I/L)
- Rate limiting: 428 `slow_down` if runner polls faster than every 5 seconds
- Device code is high-entropy (UUID)

**Database model (`DeviceCodeRecord`):**

| Column         | Description                               |
| -------------- | ----------------------------------------- |
| `DeviceCode`   | UUID, unique, high-entropy                |
| `UserCode`     | 8-char human-readable code (unique index) |
| `UserId`       | FK to UserRecord (null until approved)    |
| `Status`       | `pending`, `approved`, `expired`          |
| `RunnerName`   | Optional runner name from initial request |
| `ExpiresAt`    | 15 minutes from creation                  |
| `LastPolledAt` | Rate limiting tracker                     |

### Registration (token — legacy)

```
Dashboard: "Create Runner" → POST /api/runners
    ↓
Backend generates registration token (shown once), stores SHA-256 hash
    ↓
Customer copies docker run command
    ↓
Runner starts: POST /api/runner/register { registrationToken }
    ↓
Backend validates hash, issues long-lived auth token
Runner status: pending → online
```

### Polling loop

```
Runner client.ts main loop:
  1. GET /api/runner/jobs (Bearer: auth-token)
     → 204 No Content = no jobs, sleep 3s
     → 200 = job claimed, execute it
  2. Execute skill via local SkillExecutor
  3. POST /api/runner/jobs/{id}/result { success, result, error }
  4. POST /api/runner/heartbeat (every 30s)
  5. Sleep 3s if idle, loop
```

### Status tracking

| Status    | Meaning                                                   |
| --------- | --------------------------------------------------------- |
| `pending` | Created in dashboard, not yet registered                  |
| `online`  | Registered, heartbeat within 90s                          |
| `offline` | Heartbeat stale (>90s), marked by RunnerJobTimeoutService |

## Job dispatch (backend)

When an agent calls `skill_exec` for a skill that isn't locally configured:

1. `AgentSkillsController.SkillExec` checks for local credentials → none found
2. Queries for online runners → if one exists, dispatch to it
3. Creates `RunnerJobRecord` (status: `pending`, claim deadline: 60s)
4. Registers a `TaskCompletionSource` in `RunnerJobWaiter` singleton
5. Awaits the TCS with 30s timeout
6. Runner polls → claims job (status: `running`) → executes → posts result
7. `RunnerApiController.PostResult` completes the TCS
8. `AgentSkillsController` returns the result to the agent

The agent doesn't know whether a skill ran locally or on a remote runner. The interface is identical.

### RunnerJobWaiter

Singleton service with `ConcurrentDictionary<Guid, TaskCompletionSource<RunnerJobResult>>`. Bridges the gap between synchronous `skill_exec` and asynchronous runner polling.

### RunnerJobTimeoutService

`BackgroundService` running every 30s:

- Fails pending jobs past their claim deadline
- Fails running jobs older than 120s
- Marks runners offline if heartbeat > 90s stale

## Skill sync

Runners automatically pull custom skills uploaded via the dashboard. No manual skill installation needed.

### Flow

```
Runner boots → POST /api/runner/register
    ↓
GET /api/runner/skills → list of { name, updatedAt }
    ↓
For each skill where local updatedAt != remote updatedAt:
    ↓
GET /api/runner/skills/{name}/download → zip from MinIO
    ↓
Extract zip → build with esbuild → hot-load into executor
    ↓
Skill is now available for job execution
```

### Sync schedule

- **On startup**: full sync before entering the poll loop
- **Every 60s**: check for new/updated skills
- **State tracking**: `.skill-sync-state.json` stores `{ name: updatedAt }` so unchanged skills are skipped

### End-to-end user experience

1. User creates a runner in the dashboard → copies the `docker run` command
2. User writes a custom skill with `defineSkill()` and zips it
3. User uploads the zip in the dashboard → stored in MinIO, built on cloud skill-runtime
4. Runner picks up the new skill within 60s → downloads zip, builds locally, hot-loads
5. Agent calls `skill_exec` → backend routes to runner → runner executes against internal APIs
6. Result flows back to agent — seamless, agent doesn't know where the skill ran

## API endpoints

### Dashboard-facing (session auth)

| Method                     | Path                                                   | Description |
| -------------------------- | ------------------------------------------------------ | ----------- |
| `POST /api/runners`        | Create runner, returns registration token (shown once) |
| `GET /api/runners`         | List runners with status/heartbeat                     |
| `GET /api/runners/{id}`    | Runner detail + recent jobs                            |
| `DELETE /api/runners/{id}` | Delete runner                                          |

### Runner-facing (token auth)

| Method                                   | Path                                                       | Description |
| ---------------------------------------- | ---------------------------------------------------------- | ----------- |
| `POST /api/runner/register`              | Exchange registration token for auth token (legacy)        |
| `GET /api/runner/jobs`                   | Poll for pending job (claims atomically, returns 1 or 204) |
| `POST /api/runner/jobs/{id}/result`      | Post execution result                                      |
| `POST /api/runner/heartbeat`             | Update heartbeat timestamp                                 |
| `GET /api/runner/skills`                 | List available custom skills with updatedAt timestamps     |
| `GET /api/runner/skills/{name}/download` | Download skill zip from MinIO                              |

### Device authorization (no auth / session auth)

| Method                            | Path    | Auth                                                 | Description |
| --------------------------------- | ------- | ---------------------------------------------------- | ----------- |
| `POST /api/runner/device/code`    | None    | Generate device_code + user_code                     |
| `POST /api/runner/device/token`   | None    | Poll for approval (returns auth token when approved) |
| `GET /api/runner/device/verify`   | Session | Browser page showing code for user to approve        |
| `POST /api/runner/device/approve` | Session | User approves the device code                        |

## Database models

### `RunnerRecord`

| Column                  | Description                            |
| ----------------------- | -------------------------------------- |
| `Id`                    | Primary key                            |
| `OwnerId`               | FK to UserRecord                       |
| `Name`                  | Human-readable name                    |
| `Status`                | `pending`, `online`, `offline`         |
| `RegistrationTokenHash` | SHA-256 of one-time registration token |
| `AuthTokenHash`         | SHA-256 of long-lived auth token       |
| `LastHeartbeatAt`       | Last heartbeat timestamp               |
| `Version`               | Runner version string                  |

### `RunnerJobRecord`

| Column          | Description                                 |
| --------------- | ------------------------------------------- |
| `Id`            | Primary key                                 |
| `RunnerId`      | FK to RunnerRecord                          |
| `Status`        | `pending`, `running`, `completed`, `failed` |
| `Payload`       | JSON: `{ skill, action, params }`           |
| `Result`        | JSON: `{ success, result, error }`          |
| `ClaimDeadline` | Job must be claimed before this time        |

## Runner container (`packages/skill-runner/`)

Based on the skill-runtime architecture but runs as a polling client instead of an HTTP server.

| File              | Purpose                                                            |
| ----------------- | ------------------------------------------------------------------ |
| `src/client.ts`   | Main polling loop (register → poll → execute → result → heartbeat) |
| `src/executor.ts` | SkillExecutor (reused from skill-runtime)                          |
| `src/sandbox.ts`  | Sandboxed context creation                                         |
| `build.js`        | esbuild config (bundles client + skills)                           |
| `Dockerfile`      | Production image (`harkro123/skill-runner`)                        |

Skills are bundled into the runner image at build time (same esbuild process as skill-runtime). Customers can add their own skills by extending the Dockerfile or mounting a volume.

## Key files

| File                                          | Purpose                             |
| --------------------------------------------- | ----------------------------------- |
| `Entities/Runners/RunnersController.cs`       | Dashboard runner CRUD               |
| `Entities/Runners/RunnerApiController.cs`     | Runner polling API                  |
| `Entities/Runners/RunnerJobWaiter.cs`         | TCS bridge for synchronous dispatch |
| `Entities/Runners/RunnerJobTimeoutService.cs` | Background cleanup                  |
| `Entities/Runners/RunnerAuthAttribute.cs`     | Runner bearer token validation      |
| `Entities/Runners/DeviceAuthController.cs`    | RFC 8628 device authorization flow  |
| `Entities/Skills/AgentSkillsController.cs`    | Runner dispatch in SkillExec        |
| `packages/skill-runner/src/client.ts`         | Runner polling client               |
| `apps/dashboard/src/app/runners/page.tsx`     | Runner management page              |
