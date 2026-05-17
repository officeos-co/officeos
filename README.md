<p align="center" style="margin-bottom: 0;">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="docs/assets/Logo.png" />
    <source media="(prefers-color-scheme: light)" srcset="docs/assets/Logo.png" />
    <img src="apps/website/public/logo.svg" height="100" alt="OfficeOS logo" />
  </picture>
</p>
<h1 align="center">Declarative infrastructure for coding agents.</h1>

<p align="center">
Open-source, self-hosted control plane for defining, running, and operating repo-aware agents on your own cloud.
</p>

<p align="center">
  <a href="https://officeos.co">Website</a> · 
  <a href="https://docs.officeos.co">Docs</a> · 
  <a href="https://docs.officeos.co/quickstart">Getting Started</a> · 
  <a href="https://github.com/officeos-co/officeos/issues">Issues</a> ·
  <a href="https://discord.gg/TyvRBzsQP">Discord</a>
</p>

<br/>

OfficeOS is a declarative framework for managing agents. You commit agent manifests next to your application code, apply them to the OfficeOS control plane, and run agents with isolated workspaces, scoped credentials, model-agnostic providers, MCP tools, persistent memory, attached browsers, and structured execution logs.

The primary product surface is the `officeos` CLI and the manifest files in your repo. The dashboard is an operator surface for inspecting runs, approvals, logs, credentials, and fleet health.

The first-class use case is coding work: clone a repo, understand the task, edit files, run declared verification commands, preserve artifacts, and hand back a patch or pull request with the full typed log trail. OfficeOS is not another chat wrapper around tools; it is the Kubernetes-style control plane for long-running agent work.

Self-host the full stack with Docker Compose or Kubernetes.

## Preview

https://github.com/user-attachments/assets/d7b8f2c9-350a-4058-a95f-994092c588db

| Quickstart                                                                                                                      | Integrations                                                                                           | Launch setup                                                                                                   |
| ------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------- |
| <img src="docs/assets/OfficeOsScreen1.png" alt="OfficeOS quickstart screen with agent name, model, prompt, and integrations" /> | <img src="docs/assets/OfficeScreen2.png" alt="OfficeOS dashboard integrations list for agent tools" /> | <img src="docs/assets/OfficeScreen3.png" alt="OfficeOS quickstart launch setup with available integrations" /> |

## Highlights

- **Manifest-first agents** — define agents, providers, tools, routines, credentials, policies, and runs as `officeos.io/v1` resources
- **Coding work as a contract** — declare repo checkout, workspace behavior, command policy, verification commands, completion rules, and artifacts
- **One control plane** — apply manifests, run agents, inspect logs, manage approvals, and operate fleets across workspaces
- **Fast isolated launch** — each run gets a sandboxed workspace with filesystem and shell access
- **Attached browsers** — every agent has browser capabilities for web workflows and automation
- **Managed tools and MCP** — connect agents to built-in tools and MCP servers for external integrations
- **Persistent memory** — store agent memory, conversations, and operational context centrally
- **Credential management** — give agents scoped access to provider keys and integration secrets
- **Structured logs** — inspect message, tool call, tool result, and agent output timelines
- **Model-agnostic** — bring your own LLM keys for Anthropic, OpenAI, Google, xAI, and compatible providers

## Why OfficeOS

Hosted agent runtimes are useful when you want a provider to run the agent server for you. OfficeOS is for teams that want the agent control plane in their own cloud and want agent behavior to be reviewable, reproducible, and versioned in the same repo as the code it changes.

The important boundary is declarative operation:

- Declare what an agent is allowed to do instead of hand-wiring a one-off harness.
- Declare how coding runs start, verify themselves, and finish.
- Keep prompts, tools, credentials, schedules, channels, memory, and runtime policy under version control.
- Treat every run as an auditable sequence of typed log entries, not an opaque chat transcript.

## Quick Start

For local development, the root `.env` file is the source of truth for runtime
config and secrets. Copy the template once, edit the provider keys, then start
the development infrastructure from the repo root.

```bash
git clone https://github.com/HarKro753/EnterpriseAgentOs.git
cd EnterpriseAgentOs

cp .env.example .env
# Edit .env: add at least one LLM provider key

docker build -t harkro123/eaos-pod-executor:latest packages/pod-executor
docker compose -f docker-compose.infra.yml up -d
```

## Kubernetes

OfficeOS is built for self-hosters who want to run agent infrastructure on their own cloud. Kubernetes manifests are available under `k8s/` for production and staging deployments.

```bash
kubectl apply -f k8s/prod/
```

You can also install the core control plane with Helm:

```bash
cp k8s/helm/examples/values.local.example.yaml values.local.yaml
# Edit values.local.yaml with your database, public URLs, and provider keys.

helm upgrade --install eaos ./k8s/helm \
  --namespace eaos \
  --create-namespace \
  -f values.local.yaml
```

## Development

Run infrastructure in Docker and run product code on the host for fast rebuilds.
The backend creates one pod-executor container per agent and binds it to a random
localhost port, so local `dotnet run` can call agent runtimes directly.

```bash
# Infra: Postgres, Redis, MinIO, channels, browser controller, browser node
docker compose -f docker-compose.infra.yml up -d
# Rebuild this when packages/pod-executor changes
docker build -t harkro123/eaos-pod-executor:latest packages/pod-executor

# Backend
cd apps/backend && dotnet run --project src/EnterpriseAgentOs.Api

# Dashboard
cd apps/dashboard && bun dev
```

## Project Resources

- [Contributing](CONTRIBUTING.md) explains the development workflow, architecture boundaries, and review expectations.
- [Code of Conduct](CODE_OF_CONDUCT.md) defines how contributors are expected to work together.
- [Security Policy](SECURITY.md) explains how to report vulnerabilities privately.
- [License](LICENSE) is Apache-2.0.
- [Bug reports](.github/ISSUE_TEMPLATE/bug.yml), [feature requests](.github/ISSUE_TEMPLATE/feature.yml), and the [pull request template](.github/pull_request_template.md) are configured for GitHub.

## Star History

[![Star History Chart](https://api.star-history.com/chart?repos=officeos-co/officeos&type=date&legend=top-left)](https://www.star-history.com/?repos=officeos-co%2Fofficeos&type=date&legend=top-left)
