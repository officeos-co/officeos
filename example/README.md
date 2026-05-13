# Declarative Workspace Example

This folder contains a complete declarative deployment example for `eaos`.

It covers every currently supported manifest kind:

- `Integration`: one built-in GitHub integration and one custom MCP integration.
- `Channel`: Slack and Telegram token-based channels.
- `MemoryStore`: support and engineering knowledge stores with initial entries.
- `Browser`: a shared operations browser resource.
- `Agent`: support and incident agents with integrations, channels, memory, browser access, and tool policies.
- `Routine`: schedule, API, and GitHub-triggered routines.

Apply it with:

```bash
eaos validate -f example/declarative-workspace.yaml
eaos diff -f example/declarative-workspace.yaml
eaos apply -f example/declarative-workspace.yaml
```

Replace the `${...}` placeholders before applying. The CLI sends the manifest as-is; it does not interpolate environment variables.

## Slack Agent Fleet

`slack-agent-fleet.yaml` defines an 11-agent deployment:

- ten specialist agents, each attached to one dedicated Slack channel
- one `main-coordinator-agent`
- one internal channel, `agent-mesh-internal`, shared by all 11 agents
- a routing memory store that tells the coordinator which specialist owns which domain

The internal channel path is implemented in the backend through the `internal_channel_send` tool. One current limitation is that the tool requires the internal channel connection UUID, so after `eaos diff` or `eaos apply`, copy the `resourceId` for `Channel/agent-mesh-internal` into the coordinator task/prompt when it needs to send internal questions.
