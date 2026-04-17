# Agent Lifecycle

> From "Create" button to running pod to deletion — every step.

## Creation flow

```
User clicks "Create"
    ↓
POST /api/agents { name, provider, model }
    ↓
AgentService.CreateAsync:
  1. Validate provider is configured (API key exists in DB)
  2. Default model if none selected (first entry in KnownModels)
  3. Validate model belongs to provider (KnownModels.IsValid)
  4. Insert AgentRecord (status: "pending") — includes the user-supplied systemPrompt
  5. Deploy K8s resources:
     - PVC (1Gi, zeroclaw-data-{short-id})
     - Pod (zeroclaw image, env: ZEROCLAW_AGENT_ID only)
     - ClusterIP Service (port 42617)
  6. Update record: status="running", podName, serviceUrl
    ↓
Frontend polls GET /api/agents every 10s, sees new agent
```

## Pod boot sequence

```
Pod starts with: ZEROCLAW_AGENT_ID=<uuid>
    ↓
gateway_bootstrap.rs:
  - Derives backend URL (http://eaos-backend-prod:8000)
  - Sets provider: custom:{backend_url}/v1 (LLM proxy)
  - Sets API key: agent UUID (bearer token for all backend calls)
  - Sets skills.graphql_url: {backend_url}/api/graphql
  - Sets workspace: /zeroclaw-data/workspace
    ↓
personality_bootstrap.rs:
  - Personality templates (SOUL.md, IDENTITY.md, AGENTS.md, BOOTSTRAP.md, ...) are
    embedded in the zeroclaw-core binary via include_str!
  - On first boot, writes each template to /zeroclaw-data/workspace/ only if the
    file does not already exist (agent edits survive restarts)
  - Substitutes {{prompt}} in BOOTSTRAP.md with the systemPrompt from the bootstrap payload
    ↓
personality.rs:
  - load_personality_strict validates required files exist and are non-empty
  - Fails loudly if missing → pod CrashLoopBackOff → dashboard shows "failed"
    ↓
agent.rs:
  - Registers skill_exec tool (GraphQL-backed)
  - Builds system prompt with personality + skill docs
  - Opens WebSocket gateway on 0.0.0.0:42617
    ↓
Ready to chat
```

## Status lifecycle

| Status      | Meaning                                | How it's set                      |
| ----------- | -------------------------------------- | --------------------------------- |
| `pending`   | Record created, deployment in progress | `AgentService.CreateAsync`        |
| `running`   | Pod deployed successfully              | After K8s resources created       |
| `failed`    | Deployment threw                       | Catch block in CreateAsync        |
| `not_found` | Pod doesn't exist in K8s               | Live refresh via `GetStatusAsync` |
| `stopped`   | Pod phase is "Succeeded"               | Live refresh                      |
| `unknown`   | Can't determine (e.g. local dev)       | `NullAgentDeployer`               |

**Status sync:** `AgentService.GetAsync` and `ListAsync` call `IAgentDeployer.GetStatusAsync(podName)` inline. If the K8s status differs from the DB, the DB is updated via `UpdateStatusAsync`. Frontend polls every 10s — stale status corrects itself within one poll cycle.

## Deletion flow

```
User clicks "Delete" on agent
    ↓
DELETE /api/agents/{id}
    ↓
AgentService.DeleteAsync:
  1. Look up agent record
  2. Remove K8s resources:
     - Delete pod
     - Delete service
     - Delete PVC
  3. Soft-delete record in Postgres (IsDeleted=true)
    ↓
Frontend removes agent from list
```

## WebSocket chat proxy

The dashboard doesn't connect directly to agent pods. Instead:

```
Browser WebSocket → wss://api.officeos.co/api/agents/{id}/ws
    ↓
Backend proxy (AgentProxyEndpoints.cs):
  - Looks up serviceUrl from agent record
  - Opens upstream WebSocket to pod's :42617/ws/chat
  - Bidirectional frame relay
    ↓
Agent pod zeroclaw gateway
```

This means:

- Pods don't need public ingress.
- The backend can validate agent existence before proxying.
- Dashboard code uses `agentWsUrl()` which picks `ws://localhost:5080` or `wss://api.officeos.co` based on hostname.

## Pod resource defaults

| Resource | Request                     | Limit                     |
| -------- | --------------------------- | ------------------------- |
| Memory   | 64Mi                        | 512Mi                     |
| CPU      | 100m                        | 2 cores                   |
| PVC      | 1Gi (ReadWriteOnce)         | —                         |
| Image    | `harkro123/zeroclaw:latest` | `imagePullPolicy: Always` |
