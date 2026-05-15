# OfficeOS CLI

Kubernetes-style control-plane CLI for EnterpriseAgentOs resources.

## Commands

```bash
officeos login [--api-url <url>] [--context <name>]
officeos whoami

officeos config get-contexts
officeos config current-context
officeos config use-context <name>
officeos config set-context <name> --api-url <url> --token <token>

officeos validate -f <file>
officeos diff -f <file>
officeos apply -f <file>

officeos get <kind|kind/name> [-o json|yaml|name]
officeos describe <kind/name>
officeos delete <kind> <name>
officeos delete --all

officeos run <agent> --task <text>
officeos send <agent> --message <text>
officeos logs <kind/name> [--tail <n>] [--since <duration>] [--type <type>] [--severity <level>]

officeos models [-o json|yaml|name]
officeos providers [-o json|yaml|name]
officeos provider auth codex [--no-browser]
officeos integration auth github [--no-browser]
```

## Agent Messaging

```bash
officeos run fix-ci --task "Fix the failing backend tests"
officeos send fix-ci --message "Use the existing provider tests as the reference"
officeos logs agent/fix-ci --tail 100
```

`run` starts manual work through the agent's configured execution path. `send` sends an additional message to an existing agent through the same agent message endpoint and prints the same `agent/<name> work/<id> <status>` output shape.

## GitHub OAuth

```bash
officeos integration auth github
```

This opens the dashboard-backed GitHub OAuth flow (`/api/auth/github`) and stores the GitHub OAuth credential for the selected OfficeOS workspace. GitHub polling routines use that workspace credential.

## VS Code

The VS Code extension shells out to this CLI. Agent nodes support the right-click action `OfficeOS: Send Message`, which prompts for a message, runs `officeos send <agent> --message <text>`, and refreshes the selected agent node.
