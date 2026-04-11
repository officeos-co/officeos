# MAKE SDK WORK IN ANY LANGUAGE

Extism (extism.org) — WASM-based plugin system. Developers write skills in any
language (Rust, Go, Python, JS, C), compile to .wasm, and your runtime loads
them into a single process. No containers per skill, no build pipelines. SDKs
exist for most languages. You'd get the "upload a zip" DX you have now but
language-agnostic. The host (your skill-runtime) provides host functions for
HTTP, credentials, logging — which maps directly to your SkillContext.

OpenFaaS — if you want the container-per-skill model. Handles build, deploy,
scaling on K8s. But it's a whole platform to operate and overkill for your
scale.

No. You wouldn't sacrifice anything. Extism gives you:

- Same single-process model (no containers per skill)
- Same upload-a-file DX (.wasm instead of .zip)
- Same sandboxing (WASM is more isolated than Node.js eval, actually)
- Same host-controlled I/O (you provide fetch/credentials as host functions,
  just like SkillContext)
- Any language that compiles to WASM

The only cost is time:

1. Rewrite the skill-runtime executor to load .wasm modules instead of esbuild
   bundles
2. Write the host functions (fetch, log, credentials) as WASM imports
3. Build a compile step per language (TS→WASM, Python→WASM, Go→WASM)
4. Rewrite or adapt the three existing skills to target WASM
5. The manifest/defineSkill contract stays the same — it's just how execution
   happens underneath

No architectural trade-off, no new services to operate, no K8s complexity
increase. It's a pure upgrade path. The reason I said "don't do it yet" is only
time — it's weeks of work to replace something that works fine today for a
capability nobody is asking for yet.

# Agent skills in current session

In frontend it should be visible if a skill is like on the newest version

# Agent skill assignment

Skills should be assigned to agents not globally avilable anymore

# Remove internal mysql entirely

# Self-hosted skill runtime (remote skills)

## Problem

Agents run in our cluster, but enterprise customers have internal services
(ERPs, databases, proprietary APIs) behind their firewall that cloud-hosted
skills can't reach. Requiring customers to deploy the full platform on-prem
is too much friction.

## Solution

Follow the GitHub self-hosted runner model: agents stay in our cluster,
customers deploy a lightweight skill-runtime container in their own network
that registers back to our platform.

## Architecture

```
┌─────────────────────────┐         ┌──────────────────────────────┐
│   Our Cluster           │         │   Customer Network           │
│                         │         │                              │
│  Agent Pod              │         │  Self-hosted skill-runtime   │
│    ↓                    │         │    ↓                         │
│  Backend (LLM proxy,    │◄────────│  Outbound tunnel             │
│   skill gateway)        │  tunnel │  (Cloudflare/Tailscale)      │
│    ↓                    │         │    ↓                         │
│  GraphQL introspection  │         │  Custom skills (JS/TS)       │
│  discovers remote skills│         │    ↓                         │
│                         │         │  Internal APIs (ERP, DB...)  │
└─────────────────────────┘         └──────────────────────────────┘
```

- **Agents stay in our cluster** — we manage runtime, LLM proxy, orchestration
- **Customers deploy one container** — a skill-runtime with their custom skills
- **Credentials stay in customer's network** — internal API keys never leave their infra
- **No inbound ports** — skill-runtime connects outbound to our platform

## Components

### 1. Registration / handshake protocol

- Self-hosted runtime starts up, authenticates with our backend using a
  customer-issued token
- Sends its skill manifests (same format as cloud skills)
- Backend registers these as remote skills, tagged to the customer's org
- Heartbeat to track online/offline status

### 2. Secure tunnel

- Customer's skill-runtime establishes an outbound-only tunnel to our backend
- Options: Cloudflare Tunnel, Tailscale, or our own WebSocket-based relay
- No customer firewall changes required — all connections are outbound
- TLS everywhere, mutual auth via the registration token

### 3. Skill execution flow

- Agent discovers remote skills via GraphQL introspection (same as cloud skills)
- Agent calls `skill_exec` → backend routes to the remote skill-runtime
  via the tunnel
- Remote skill-runtime executes the skill against internal APIs
- Result flows back through the tunnel to the agent

### 4. Customer deployment artifact

- Single Docker image: `harkro123/skill-runtime:latest`
- Customer bundles their custom skills into it (or mounts as volume)
- Config: just our platform URL + registration token
- `docker run -e PLATFORM_URL=... -e REGISTRATION_TOKEN=... harkro123/skill-runtime`

## Key design decisions

- **Same skill-sdk** — customers use `@harro/skill-sdk` and `defineSkill` with
  Zod schemas, identical to cloud skills. Zero new abstractions.
- **Same discovery** — remote skills appear in GraphQL introspection alongside
  cloud skills. Agents don't know or care where a skill runs.
- **Outbound-only connectivity** — modeled after GitHub self-hosted runners.
  No VPN, no port forwarding, no firewall changes.
- **Graceful degradation** — if the tunnel drops, remote skills show as
  unavailable. Agent can still use cloud skills.

# Testing strategy: mock agent + integration tests

## Problem

The Rust agent (zeroclaw) is hard to spin up in a test environment — it needs
a K8s pod, CouchDB vault, personality files, and an LLM provider. But the
backend is the critical control plane and needs reliable test coverage. We need
a way to test the full backend flow (auth → agent creation → skill execution →
runner dispatch) without running the Rust binary.

## Key insight: the agent is just an HTTP client

The agent interacts with the backend through exactly two interfaces:

1. `POST /v1/chat/completions` — LLM proxy (Bearer: agent-uuid)
2. `POST /api/agents/me/skill-exec` + `GET /api/agents/me/capabilities`

A mock agent is just a test harness that calls these endpoints with a valid
agent UUID. No Rust, no pods, no vault needed.

## Architecture

```
Test suite (xUnit)
    ↓
WebApplicationFactory<Program>  ← in-memory ASP.NET Core test server
    ↓
Real Postgres (Testcontainers)  ← disposable DB per test class
    ↓
Mock skill-runtime (WireMock)   ← returns canned manifests + execution results
    ↓
Mock agent client (HttpClient)  ← simulates agent pod calling backend
```

## Phase 1: Backend integration tests (C# xUnit + Testcontainers)

### Setup

- `Testcontainers.PostgreSql` — spins up a real Postgres per test class
- `WebApplicationFactory<Program>` — runs the backend in-process
- Override `SkillRuntimeUrl` to point at a WireMock stub
- Seed providers, create agents, configure skills via the API

### Test cases: Auth flow

| Test                                        | What it verifies                                          |
| ------------------------------------------- | --------------------------------------------------------- |
| `GET /api/auth/me` without cookie → 401     | Middleware skips, controller returns unauthorized         |
| `GET /api/auth/google` → redirect to Google | OAuth URL is well-formed with client ID, scopes, state    |
| Full OAuth round-trip (mocked Google)       | Exchange code → upsert user → create session → cookie set |
| `POST /api/auth/logout` → session deleted   | Cookie cleared, subsequent /me returns 401                |
| Session expiry                              | Expired session rejected by middleware                    |

### Test cases: Agent lifecycle

| Test                                    | What it verifies                            |
| --------------------------------------- | ------------------------------------------- |
| `POST /api/agents` → creates agent      | Record in DB, status "pending" or "unknown" |
| `GET /api/agents` → lists agents        | Returns all non-deleted agents              |
| `DELETE /api/agents/{id}` → soft delete | IsDeleted=true, subsequent GET excludes it  |

### Test cases: Skill execution (the big one)

| Test                                                      | What it verifies                                    |
| --------------------------------------------------------- | --------------------------------------------------- |
| Mock agent calls `GET /api/agents/me/capabilities`        | Returns installed+configured skills with tool specs |
| Mock agent calls `POST /api/agents/me/skill-exec` (cloud) | Routes to skill-runtime, returns result             |
| Skill not installed → 409 with message                    | Clear error, suggests configuring or using runner   |
| Skill set to runner target, no runners online → 503       | Error includes "no runners online" + how to fix     |
| Skill set to runner target, runner online → dispatches    | Job created, waiter resolves when result posted     |
| Runner job timeout → 504                                  | TCS times out, meaningful error returned            |

### Test cases: Runner flow

| Test                                                       | What it verifies                              |
| ---------------------------------------------------------- | --------------------------------------------- |
| `POST /api/runners` → creates runner                       | Returns registration token, hash stored       |
| `POST /api/runner/register` with valid token               | Issues auth token, status → online            |
| `POST /api/runner/register` with invalid token → 401       | Rejected                                      |
| `POST /api/runner/register` twice → 409                    | Already registered                            |
| `GET /api/runner/jobs` with no pending jobs → 204          | No content                                    |
| Full round-trip: create job → runner claims → posts result | RunnerJobWaiter completes, skill_exec returns |
| `GET /api/runner/skills` → lists ready custom skills       | Only BuildStatus=ready skills returned        |
| Heartbeat updates timestamp                                | LastHeartbeatAt refreshed                     |
| RunnerJobTimeoutService marks stale runners offline        | Runner with old heartbeat → status "offline"  |

### Test cases: Custom skill upload

| Test                                       | What it verifies                             |
| ------------------------------------------ | -------------------------------------------- |
| Upload valid zip → stored in MinIO + built | CustomSkillRecord created, BuildStatus=ready |
| Upload zip without skill.ts → 400          | Validation catches missing entry point       |
| Upload non-zip → 400                       | File type validation                         |
| Delete custom skill → removed from DB + S3 | Cleanup works                                |

## Phase 2: Mock agent client

A lightweight C# `MockAgentClient` class that simulates what zeroclaw does:

```csharp
public class MockAgentClient
{
    private readonly HttpClient _http;
    private readonly Guid _agentId;

    public MockAgentClient(HttpClient http, Guid agentId)
    {
        _http = http;
        _agentId = agentId;
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", agentId.ToString());
    }

    /// Fetch capabilities (what zeroclaw does on boot + every 30s)
    public Task<CapabilitiesResponse> GetCapabilitiesAsync()
        => GetAsync<CapabilitiesResponse>("/api/agents/me/capabilities");

    /// Execute a skill (what zeroclaw does when LLM returns a tool call)
    public Task<SkillExecResult> SkillExecAsync(string skill, string action, object? @params = null)
        => PostAsync<SkillExecResult>("/api/agents/me/skill-exec",
            new { skill, action, @params });

    /// Simulate an LLM call (what zeroclaw sends every turn)
    public Task<HttpResponseMessage> ChatCompletionAsync(object body)
        => PostRawAsync("/v1/chat/completions", body);
}
```

This client is used in integration tests to simulate real agent behavior:

```csharp
// Arrange: create agent via dashboard API
var agent = await dashboardClient.CreateAgentAsync("test-agent", "openai");

// Act: simulate agent pod booting and calling skill_exec
var mockAgent = new MockAgentClient(server.CreateClient(), agent.Id);
var caps = await mockAgent.GetCapabilitiesAsync();
var result = await mockAgent.SkillExecAsync("notion", "search", new { query = "test" });

// Assert
Assert.True(result.Success);
```

## Phase 3: Mock runner client

Same pattern for the runner side:

```csharp
public class MockRunnerClient
{
    public Task<RegisterResult> RegisterAsync(string registrationToken);
    public Task<RunnerJob?> PollJobAsync();
    public Task PostResultAsync(Guid jobId, bool success, object? result);
    public Task HeartbeatAsync();
    public Task<List<RunnerSkill>> ListSkillsAsync();
}
```

End-to-end runner test:

```csharp
// Dashboard creates runner
var runner = await dashboardClient.CreateRunnerAsync("test-runner");

// Runner registers
var mockRunner = new MockRunnerClient(server.CreateClient());
var auth = await mockRunner.RegisterAsync(runner.RegistrationToken);

// Set skill to runner target
await dashboardClient.SetRunTargetAsync("notion", "runner");

// Agent calls skill_exec (dispatches to runner)
var execTask = mockAgent.SkillExecAsync("notion", "search", new { query = "test" });

// Runner polls and completes the job
var job = await mockRunner.PollJobAsync();
await mockRunner.PostResultAsync(job.Id, true, new { results = new[] { "page-1" } });

// Agent gets the result
var result = await execTask;
Assert.True(result.Success);
```

## Phase 4: CI integration

- Run tests in GitHub Actions on every PR
- Testcontainers handles Postgres lifecycle (no external DB needed)
- WireMock stubs skill-runtime (no Node.js process needed)
- MinIO via Testcontainers for custom skill upload tests
- Target: <60s for full suite

## What NOT to test

- LLM response parsing (non-deterministic, tested in zeroclaw's own cargo tests)
- Frontend components (too much churn, low ROI until design stabilizes)
- K8s pod deployment (use NullAgentDeployer in tests, test real K8s in staging)
- CouchDB vault operations (mock IVaultClient in tests)

## Dependencies to add

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.*" />
<PackageReference Include="Testcontainers.PostgreSql" Version="4.*" />
<PackageReference Include="WireMock.Net" Version="1.*" />
<PackageReference Include="Testcontainers.Minio" Version="4.*" />
```

# Logging and error handling

We want full logging troughout everything. We should find a guide on logging and error handling and how to do that extensivelyt.

# Runner auto authentication

A runner should be more like in github where you install the runner but dont register it in the dashboard. In the runner itself you then login trough the browser and it then authenticates that runner. I believe in github you never even see your runner.
