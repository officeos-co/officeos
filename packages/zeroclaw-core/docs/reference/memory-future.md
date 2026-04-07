# Memory Future: Centralized Backend Plan

> **Status:** FUTURE SPEC — not yet implemented. This document captures the intended end state for the memory subsystem so the in-flight design survives between sessions.
>
> **Author:** Phase 4 addendum.
> **Relates to:** `STRIP_DOWN.md`, `docs/reference/identity-vault.md` (Phase 3).

## Goal

Every zeroclaw agent writes every memory it produces — conversational context, learned facts, summarized insights, skill outputs, session logs — to a **single centralized backend** that is shared across all agents in the deployment. No per-agent SQLite file, no local disk state for memory, no "lost memories on pod restart without a PVC."

This extends the Phase 3 principle ("vault is source of truth for identity") to memory: the centralized backend becomes the source of truth for memory. The agent pod becomes a pure consumer — it reads and writes via the Memory trait, but the trait implementation talks to an external service over HTTP/gRPC/etc., with no local state to lose.

## Motivation

Phase 4's memory cleanup removed ~5,200 lines of orphan memory code (lucid, qdrant, markdown backends; hygiene/snapshot/audit/policy decorators; battle_tests). What remains is:

- `sqlite.rs` — full-featured local backend (FTS5, vector search, embeddings, hybrid recall). Currently the default in `MemoryConfig::default()` and in the dashboard backend's `k8s_manager.py`.
- `obsidian.rs` — CouchDB-backed vault backend via the `obsctl` CLI. Active if configured but not the default.
- `none.rs` — no-op for tests.
- `namespaced.rs` — decorator for multi-tenant isolation.
- Feature modules: `response_cache`, `retrieval`, `embeddings`, `vector`, `decay`, `importance`, `conflict`, `chunker`, `consolidation`, `cli`.

The remaining architectural split is intentional but inconsistent:

- **Identity** comes from the per-agent Obsidian vault (Phase 3).
- **Memory** still lives in a per-agent local SQLite file (pre-Phase-3 default).

For a deployment with "hundreds of agents" this split has several problems:

1. **Memory is lost on pod restart without a PVC.** Every agent needs a ReadWriteOnce PVC just to keep its memory. That's per-pod persistent storage at scale — expensive and operationally annoying.
2. **No cross-agent memory sharing.** Two agents working on related tasks for the same user cannot see each other's memories. There is no "team brain."
3. **Backups are 1-per-agent.** Every agent's SQLite file is a separate backup target. Central backup of "all agent memory" is not a single snapshot.
4. **The source-of-truth story is muddled.** "Identity lives in the vault, memory lives in SQLite" is harder to explain and audit than "everything lives in the central backend."
5. **Embeddings are expensive per-agent.** Each agent runs its own OpenAI embedding client and caches vectors locally. A shared embedding store would deduplicate and amortize.

## End state

### Architecture

```
┌──────────────────────────────────────────────────────────────┐
│  Centralized Memory Backend Service                          │
│  (e.g. a dashboard-backend-owned FastAPI endpoint, or        │
│   a dedicated memory-service pod, or a CouchDB view,         │
│   or Obsidian vault with richer querying)                    │
│                                                              │
│  - Stores every memory entry for every agent                 │
│  - Per-agent namespace isolation (agent_id)                  │
│  - Full-text search (BM25)                                   │
│  - Vector search (embeddings)                                │
│  - Importance scoring                                        │
│  - Time decay                                                │
│  - Conflict / supersession detection                         │
│  - Response cache (optional)                                 │
│  - Backed by Postgres, CouchDB, or similar                   │
└──────────────────────────────────────────────────────────────┘
                         ▲ ▲ ▲ ▲ ▲
                         │ │ │ │ │
          ┌──────────────┘ │ │ │ └──────────────┐
          │                │ │ │                │
┌─────────┴────┐  ┌───────┴─┴─┴────┐  ┌─────────┴────┐
│ agent pod 1  │  │ agent pod 2    │  │ agent pod N  │
│              │  │                │  │              │
│ RemoteMemory │  │ RemoteMemory   │  │ RemoteMemory │
│ (Memory      │  │ (Memory        │  │ (Memory      │
│  trait impl) │  │  trait impl)   │  │  trait impl) │
└──────────────┘  └────────────────┘  └──────────────┘
```

Each agent pod holds **no memory state on disk**. Every `memory.store(...)`, `memory.recall(...)`, `memory.forget(...)` call is a network request to the centralized service. The pod is stateless with respect to memory.

### New Memory trait implementation

A single new backend:

- **`RemoteMemory`** (file: `src/memory/remote.rs`) — implements the existing `Memory` trait by making HTTP/gRPC/websocket calls to the centralized service. Stateless, no local files, no local SQLite cache (unless we explicitly want a small hot cache, see below).

Optional short-term cache layer inside `RemoteMemory`:

- In-memory LRU hot cache of recent recalls (same pattern `obsidian.rs` already uses).
- TTL of ~60 seconds for hot lookups.
- **Not persisted** — on pod restart the cache is cold. The authoritative store is the remote service.
- This is an optimization, not a correctness-critical layer. The centralized service is the source of truth.

### What gets deleted

When `RemoteMemory` lands and is proven in production:

- **`src/memory/sqlite.rs`** (1,144 LOC) — the full-featured SQLite backend.
- **`src/memory/sqlite.test.rs`** (1,626 LOC) — its tests.
- **`src/memory/obsidian.rs`** (456 LOC) — the vault-CLI backend, superseded by `RemoteMemory`. (Identity files still come from the vault via ConfigMap mount — that path is unaffected.)
- **`src/memory/obsidian.test.rs`** (174 LOC) — its tests.
- **`src/memory/retrieval.rs`** (189 LOC) — the cache/FTS/vector multi-stage pipeline. These stages now belong to the centralized service, not the agent.
- **`src/memory/retrieval.test.rs`** (79 LOC).
- **`src/memory/embeddings.rs`** (196 LOC) — `EmbeddingProvider` trait. Embeddings are computed server-side by the centralized service.
- **`src/memory/embeddings.test.rs`** (163 LOC).
- **`src/memory/vector.rs`** (403 LOC) — cosine similarity + hybrid merge. Belongs server-side.
- **`src/memory/decay.rs`** (50 LOC) — time decay. Belongs server-side.
- **`src/memory/decay.test.rs`** (102 LOC).
- **`src/memory/importance.rs`** (63 LOC) — importance scoring. Belongs server-side.
- **`src/memory/importance.test.rs`** (45 LOC).
- **`src/memory/conflict.rs`** (112 LOC) — conflict / supersession. Belongs server-side.
- **`src/memory/conflict.test.rs`** (62 LOC).
- **`src/memory/chunker.rs`** (183 LOC) — markdown chunking for consolidation.
- **`src/memory/chunker.test.rs`** (195 LOC).
- **`src/memory/consolidation.rs`** (174 LOC) — LLM-driven consolidation. **UNCERTAIN**: this might stay on the agent side if the LLM consolidation is done during the agent turn, OR it moves server-side if the centralized service has its own consolidation worker. Decide during implementation.
- **`src/memory/consolidation.test.rs`** (58 LOC).
- **`src/memory/response_cache.rs`** (279 LOC) — **KEEP for now**. This is a pure in-memory LLM-response dedup cache, not memory state. Unrelated to the centralized backend migration.
- **`src/memory/backend.rs`** (128 LOC) — the backend-selector enum. Collapses to a single remote backend + none; reduces to ~20 LOC or is deleted entirely.
- **`src/memory/cli.rs`** (289 LOC) — `zeroclaw memory` CLI commands. Might survive but gets rewritten to talk to the centralized service instead of a local SQLite.

**Estimated total LOC removed from `zeroclaw-core` when this future spec lands: ~5,500-6,500 lines** (depending on whether consolidation/cli survive).

### What gets added

- **`src/memory/remote.rs`** — the new `RemoteMemory` implementation. Probably 300-600 LOC (Memory trait impl + HTTP client + cache layer + error handling). Much smaller than the combined backends it replaces.
- **Centralized service implementation** — a new pod or new endpoints in the existing dashboard backend. Probably Python (FastAPI + Postgres or CouchDB). This is a separate codebase addition and the bulk of the work. **Not sized here** — it's a "Phase 6 implementation plan" topic.
- **Protocol** — HTTP/JSON is the simplest starting point. If latency becomes an issue, upgrade to gRPC or a persistent websocket connection with server-push invalidation.
- **Config schema changes** — `MemoryConfig.backend` becomes `"remote"` (with "none" still available for tests). New sub-config: `[memory.remote]` with `base_url`, `auth_token`, `timeout_ms`, `cache_size`, `cache_ttl_s`.
- **K8s manifest changes** — the dashboard backend's `k8s_manager.py` injects the `MEMORY_REMOTE_URL` and `MEMORY_REMOTE_TOKEN` env vars into the agent pod. The per-agent PVC for `/zeroclaw-data` can probably be removed entirely or shrunk to a few KB for ephemeral scratch space.

## Open design questions

These are the decisions that need a real conversation before implementation starts. Captured here so the next session doesn't re-derive them.

### 1. Where does the centralized service live?

Three candidates:

**(a) A new endpoint group in the existing dashboard backend.**
- Pros: Reuses the existing FastAPI infra, Postgres database, auth layer. Single deployment.
- Cons: Couples memory throughput to the dashboard backend's resource budget. Every agent turn hits the dashboard API. At hundreds-of-agents scale, the dashboard becomes the bottleneck.

**(b) A dedicated `memory-service` pod.**
- Pros: Independent scaling. Can use a different database (e.g. a vector-native DB like pgvector, Qdrant, Weaviate). Dashboard backend owns provisioning; memory service owns runtime.
- Cons: Another service to deploy, monitor, version. More moving parts.

**(c) Reuse the Obsidian vault.**
- Pros: Already exists. Already multi-agent aware. Already has persistence + backup through CouchDB + obsidian-livesync.
- Cons: CouchDB isn't a great FTS + vector store. obsctl's write throttle (2 seconds) is a huge problem for high-throughput memory writes. Would need a meaningful redesign of the `obsctl` layer to support bulk write + vector fields.

**Recommendation**: **(b)** — dedicated `memory-service` pod, backed by Postgres with the pgvector extension. Reuses the FastAPI + SQLModel stack the dashboard backend already uses, but runs in its own pod with its own resource limits. Postgres + pgvector gives us FTS (via `tsvector`) + vector search + transactions + proven operational story.

### 2. Authentication & per-agent isolation

Every request from `RemoteMemory` needs to be tagged with an `agent_id` so the server can enforce "agents can only read/write their own memories." Options:

**(a) Shared service token + explicit `agent_id` in every request body.**
- Simple. The service trusts that agents don't lie about their agent_id. If they do, it's a security incident.

**(b) Per-agent tokens minted at provisioning time.**
- The dashboard backend already mints per-agent tokens for other purposes (`Agent.agent_token_hash` in Phase 3). Reuse them. The service decodes the token to get the agent_id; the agent never passes agent_id explicitly.
- More secure. The service can revoke a compromised agent without touching the others.

**Recommendation**: **(b)** — reuse the existing agent token infrastructure from Phase 3. The memory service becomes another consumer of the same token the dashboard already issues.

### 3. Cross-agent memory sharing

The motivation section mentioned "team brain" as a benefit. How literal do we make it?

**(a) No sharing.** Every memory is owned by exactly one agent. `RemoteMemory.recall()` only returns entries whose `agent_id` matches the caller.

**(b) Namespace hierarchy.** Each agent has a private namespace + optional shared namespaces it can read from (e.g. a per-board namespace, a per-organization namespace). Similar to the existing `NamespacedMemory` pattern but server-enforced.

**(c) Free-for-all within an organization.** Every agent in the same org can read every other agent's memories. Bad default — information leakage risk.

**Recommendation**: **(b)** — namespace hierarchy. The agent writes only to its private namespace by default. Cross-agent reads are opt-in via an explicit namespace parameter. The dashboard backend configures which namespaces each agent can read at provisioning time.

### 4. Embedding computation

Embeddings need to be computed somewhere:

**(a) Agent side.** Agent calls OpenAI/Cohere/etc., sends the text + vector to the remote service. Each agent pays the embedding API cost.

**(b) Server side.** Agent sends the text, the memory service computes the embedding before storing. Service pays the cost but amortizes (dedup + batch).

**Recommendation**: **(b)** — server-side embeddings. Eliminates the `embeddings.rs` and `EmbeddingProvider` trait from the agent. Simpler agent, better cost story.

### 5. Consolidation

`consolidation.rs` is called from `src/channels/mod.rs:3228` and `src/gateway/ws.rs:476` as `consolidation::consolidate_turn(...)` at the end of each agent turn. It uses an LLM to summarize the turn's messages into structured memory entries.

**(a) Keep on agent side.** Agent decides when to consolidate, calls the LLM (which it's already doing for the main conversation), sends the structured entries to the remote service as normal `store()` calls.

**(b) Move to server side.** Agent sends raw turn transcripts to the service; the service has its own consolidation worker that batches LLM calls.

**Recommendation**: **(a)** — keep consolidation on the agent side. The agent already has an LLM connection and the turn context; server-side consolidation adds a latency hop and needs to duplicate the conversation state. Keep the simple thing.

### 6. Migration strategy

When `RemoteMemory` lands, existing agents have SQLite files full of memories. How do we migrate?

**(a) Hard cutover.** New version of zeroclaw can only talk to the remote service. All existing memories are lost. User accepts data loss.

**(b) One-time migration script.** A `zeroclaw memory migrate-to-remote --service-url ...` command that reads the local SQLite and pushes every entry to the remote service. Run once per agent pod before upgrading.

**(c) Live migration.** `RemoteMemory` opens both the local SQLite and the remote service. Writes go to both. Reads go to remote first, fall back to SQLite. After a grace period, disable the SQLite side. This is complex and brittle.

**Recommendation**: **(b)** — migration script, one-time per agent, before upgrading. The script can run as a K8s Job that the dashboard backend triggers per agent.

## Implementation phases

When the time comes, the work splits into ~3 phases. Each phase is independently buildable.

### Phase 6 — Design spike + centralized service skeleton

- Pick service location (in-dashboard vs. dedicated pod) — lock in decision #1.
- Design the HTTP API (OpenAPI spec for store/recall/forget/list/purge/export, agent_id authentication, namespace query params).
- Stand up the memory-service pod (empty Memory trait endpoints, no real storage).
- Deploy to staging. Agent pods can still use SQLite; nothing is migrated yet.

### Phase 7 — Real storage + RemoteMemory client

- Implement the real database layer in the memory service (Postgres + pgvector or whatever was chosen).
- Write `src/memory/remote.rs` as a new Memory trait implementation.
- Add the new backend to `MemoryConfig.backend` enum and the factory.
- Dashboard backend can now provision agents with `backend = "remote"`.
- **Old backends still live.** Migration script written but not run.
- Run integration tests against the memory service in staging.

### Phase 8 — Cutover + cleanup

- Run migration script per agent to move SQLite → remote.
- Update `MemoryConfig.default().backend` to `"remote"`.
- Update `k8s_manager.py` default.
- Delete `sqlite.rs`, `obsidian.rs`, `retrieval.rs`, `embeddings.rs`, `vector.rs`, `decay.rs`, `importance.rs`, `conflict.rs`, `chunker.rs` per the "what gets deleted" list above.
- Delete the `/zeroclaw-data` PVC from the agent pod spec (or shrink to a tiny ephemeral volume for scratch).
- Update `STRIP_DOWN.md` with a phase entry.

## Success criteria

A fresh agent pod boots with:

- No `/zeroclaw-data` PVC.
- No local SQLite file.
- `MEMORY_REMOTE_URL` + `MEMORY_REMOTE_TOKEN` env vars injected by the dashboard.
- `zeroclaw daemon` starts successfully.
- First memory store round-trips to the remote service and returns OK.
- First memory recall returns previously-stored entries from the remote service.
- Agent restart preserves all memories (because they're on the server, not the pod).
- Two agents in the same namespace can see each other's shared-namespace memories if configured.
- A deleted agent's memories can be GDPR-exported via a single API call to the memory service.

And the code delta in `zeroclaw-core` is:

- `src/memory/` shrunk from ~7,000 production LOC to ~1,000 (`remote.rs` + `traits.rs` + `mod.rs` + `namespaced.rs` + `response_cache.rs` + `none.rs`).
- The Memory trait itself unchanged.
- No Memory trait consumer (agent loop, tools, prompt builder) needs to know the backend switched.

## Non-goals

- **Not moving identity into the memory service.** Identity stays in the Obsidian vault, mounted as a ConfigMap, per Phase 3. The memory service is only for dynamic memory state.
- **Not replacing `obsctl`.** obsctl still drives identity-file provisioning at agent creation. It's a write-once-per-agent path, not a hot path.
- **Not introducing a new LLM connection to the service.** The memory service does NOT call an LLM. If consolidation stays on the agent side (recommendation (a) above), the service only stores structured entries; it never generates them.
- **Not adding real-time pub/sub.** The initial design is request/response only. If live cross-agent memory invalidation becomes a requirement, add server-push invalidation as a later enhancement, not in the first rollout.

## References

- `STRIP_DOWN.md` — Phase 4 memory cleanup (commits `06a6ef9`, `12fc153`, `75e6dac`, `38e70eb`)
- `docs/reference/identity-vault.md` — the Phase 3 vault architecture this memory plan extends
- `src/memory/mod.rs` — current Memory trait factory post-Phase-4
- `src/memory/obsidian.rs` — the existing CouchDB-via-obsctl backend; reference for "how to implement a remote memory backend"
- `apps/dashboard/backend/app/services/zeroclaw/k8s_manager.py` — where the per-agent pod spec is generated; needs updating when `RemoteMemory` lands
