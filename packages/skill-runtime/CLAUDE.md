# skill-runtime — Node.js skill execution service

Runs as a separate K8s deployment. Loads bundled TypeScript skills, exposes an HTTP API, and executes skill actions with injected credentials and sandboxed fetch. The backend calls this service — agent pods never call it directly.

## Commands

```bash
npm install
npm run build        # Bundle skills + compile runtime → dist/
npm run dev          # Watch mode
```

## Project structure

```
src/
  server.ts                   HTTP server — all routes, on-demand skill loading
  executor.ts                 SkillExecutor — registers skills, Zod param validation, calls execute()
  manifest.ts                 Extracts SkillManifest from SkillDefinition for backend introspection
  builder.ts                  Builds skill source files via esbuild → single bundled .js
  sandbox.ts                  Sandboxed fetch — scopes outbound HTTP to allowed origins in production
  browser-session-manager.ts  Playwright browser session lifecycle for the browser skill
```

## HTTP API

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/health` | Health check |
| `GET` | `/manifests` | All loaded skill manifests — backend `SkillTypeModule` calls this to generate the GraphQL schema |
| `GET` | `/manifest/:skill` | Single skill manifest |
| `POST` | `/execute` | Execute a skill action — body: `{ skill, action, params, credentials }` |
| `POST` | `/install` | Install skill from `bundleUrl` or `npmPackage` |
| `POST` | `/uninstall` | Remove a skill by name |
| `POST` | `/build` | Build skill from source files (custom skill upload) and hot-load |
| `POST` | `/reload/:name` | Hot-reload a skill from `dist/skills/` |

## Startup loading sequence

1. Load all bundled skills from `dist/skills/*.js` — first-party skills baked in at image build time
2. Fetch active registry skills from `{BACKEND_URL}/api/skill-registry` and install any not already loaded
3. Start Playwright browser session cleanup loop

**On-demand fallback:** if `/execute` receives a request for an unknown skill, the runtime calls `GET {BACKEND_URL}/api/skills/:name/bundle` before returning 422. No restart needed.

## Key rules

- **Credentials are injected here, not in skills.** The executor receives decrypted credentials in the request body (the backend decrypts them) and passes them as `ctx.credentials`. Skills never touch the credential store.
- **`ctx.fetch` is sandboxed.** `sandbox.ts` scopes outbound requests to allowed origins in production. Skills must use `ctx.fetch` — the sandbox does not apply to native `fetch`.
- **Zod validation is the only enforcement layer.** `SkillExecutor` validates params against the action's Zod schema before calling `execute()`. Do not bypass this.
- **Manifests drive the backend's GraphQL schema.** The backend's `SkillTypeModule` calls `GET /manifests` at startup. If this endpoint is wrong, the entire skill GraphQL schema breaks.

## Anti-patterns

- Do not add business logic or skill implementations here. Skills live in `packages/skills/`.
- Do not expose credentials to agent pods — credentials flow backend → skill-runtime per request only.
- Do not skip Zod validation in `SkillExecutor`. It is the contract between the agent's CLI call and the skill's `execute()`.
- Do not add persistent state per skill execution — each call is stateless (except browser sessions which are managed explicitly).
- Do not change the `/manifests` response shape without updating the backend's `SkillTypeModule` deserialization.
