# Backend2 Goal

Use this file as the handoff prompt for a fresh Codex `/goal` session.

```text
/goal Build apps/backend2 into the new stripped OfficeOS backend for declarative async agent infrastructure.

Repo root:
/Users/harrokrog/Desktop/EnterpriseAgentOs

Read first:
- AGENTS.md
- TODO.md
- apps/backend2/AGENTS.md
- apps/backend2/goal/GOAL.md
- apps/backend2/goal/agent.yaml
- apps/infra_cli/AGENTS.md
- .agents/skills/vscode-extension/SKILL.md
- scripts/dev2.sh

Use `tree --gitignore` early to understand the repo layout.

Non-negotiables:
- backend2 keeps the same clean architecture rules as apps/backend through apps/backend2/AGENTS.md.
- backend2 keeps and uses the architecture analyzer copied from apps/backend/analyzers.
- The dashboard is ignored.
- The CLI is the primary interface.
- Create the VS Code extension as a separate app, not as dashboard code.
- Prefer a clean big-bang backend2 implementation over keeping legacy compatibility.
- Do not run the application manually during implementation. Build and test only.

Product direction:
OfficeOS is pivoting to declarative infrastructure for async agents: the Kubernetes-style control plane for agent work. It should not replace Cursor, Claude Code, or OpenCode. It should provide runtime isolation, workspaces, secrets, model access, MCP/tools, memory, policy, approvals, logs, audit, schedules, triggers, artifacts, and durable run status.

Initial setup constraint:
The first end-to-end goal must be self-contained. Do not require real external
secrets, GitHub tokens, cloud credentials, or a real MCP server to pass the
acceptance flow. `apps/backend2/goal/agent.yaml` may model Secret and
Integration resources, but the first runnable path should use a stub/builtin
no-auth integration and fake/in-process engine where needed.

Target user flow:
1. `officeos validate -f apps/backend2/goal/agent.yaml`
2. `officeos apply -f apps/backend2/goal/agent.yaml`
3. `officeos run fix-ci --task "inspect the repository and fix the failing build"`
4. `officeos status <run-id>`
5. `officeos logs <run-id>`
6. `officeos result <run-id>`
7. Open `apps/backend2/goal/agent.yaml` in VS Code and get OfficeOS manifest language support: validation diagnostics, hover/tooltips, completion, and command buttons that call the CLI.

The manifest in apps/backend2/goal/agent.yaml is the architecture example that should eventually compile/work end to end. If the manifest shape needs adjustment while implementing, update it deliberately and keep the same intent.

Required backend2 capabilities:
- Parse, validate, diff, apply, and export declarative resources.
- Persist applied resources.
- Start a durable async run from an AgentJob/Routine target.
- Persist run status.
- Store typed logs for the run.
- Store final result and artifacts.
- Use an engine abstraction so the first implementation can be fake/in-process, while OpenCode or the existing OfficeOS loop can be added later.
- Keep database entities decoupled from domain records.
- Use MediatR events for lifecycle facts where possible.
- Validate that optional placeholder secrets do not block the local first-run flow.

Required resources for the first working slice:
- Engine
- WorkspaceRuntime
- MemoryStore
- Browser
- Integration
- Policy
- Secret
- Agent
- AgentJob

VS Code extension app:
- Create it as a separate app under `apps/vscode_extension` unless the repo already has a clearer convention.
- Follow `.agents/skills/vscode-extension/SKILL.md`.
- Use TypeScript.
- Treat the extension as a thin operator/client surface around manifest files and CLI commands.
- Do not make it a dashboard replacement.
- It should support the OfficeOS manifest in `apps/backend2/goal/agent.yaml`.
- Required first capabilities:
  - register an OfficeOS manifest language/file association for `officeos.yaml`, `*.officeos.yaml`, and the goal `agent.yaml` example if practical
  - JSON schema or language-service-backed validation for the resource kinds in `agent.yaml`
  - hover/tooltips for resource kinds and important fields
  - completion for `kind`, `apiVersion`, references, and common spec fields
  - commands wrapping the CLI: validate, apply, run selected AgentJob, show status, show logs, show result
  - status bar item showing the current OfficeOS context/API target when available
  - optional tree view for recent runs/resources if it can be done without delaying the backend2 acceptance flow
- Extension quality gates should use the package scripts the new app defines, for example build/test/lint.

scripts/dev2.sh requirements:
- Reset backend2 runtime data.
- Reset/recreate backend2 database.
- Apply migrations or schema initialization.
- Start whatever backend2 needs for manual CLI use.
- Be the canonical manual backend2 reset/start path.

Acceptance tests:
- The example manifest validates.
- Applying the example manifest creates/reconciles resources.
- Starting the `fix-ci` AgentJob creates a durable run.
- Status can be fetched.
- Logs can be fetched.
- Final result/artifact summary can be fetched.
- The flow does not touch the dashboard.
- The VS Code extension builds and its manifest/schema support covers `apps/backend2/goal/agent.yaml`.

Quality gates:
- `dotnet build apps/backend2`
- `dotnet test apps/backend2`
- the appropriate `apps/infra_cli` build/lint/test command, depending on available scripts
- the appropriate `apps/vscode_extension` build/lint/test command after creating that app
- backend2 architecture analyzer passes
```

## Notes

- Keep `apps/backend2/goal/agent.yaml` as the example manifest and smoke-test input.
- Do not turn this goal into YAML. YAML belongs to the declarative OfficeOS resource example.
