# 10 Slack Agents

This deployment defines an 11-agent workspace:

- 10 specialist agents, each attached to one dedicated Slack channel.
- 1 main coordinator agent.
- 1 internal channel used by the coordinator to ask the specialist agents questions.
- 1 routing memory store that explains which specialist owns each domain.

The manifests are split by concern into three YAML files at the root:

- `channels.yaml`: internal channel plus ten Slack channels
- `memory.yaml`: coordinator routing memory
- `agents.yaml`: main coordinator plus ten specialist agents

To apply them, concatenate the files into one manifest:

```bash
cat \
  example/10-slack-agents/channels.yaml \
  example/10-slack-agents/memory.yaml \
  example/10-slack-agents/agents.yaml \
  > /tmp/10-slack-agents.yaml

eaos validate -f /tmp/10-slack-agents.yaml
eaos diff -f /tmp/10-slack-agents.yaml
eaos apply -f /tmp/10-slack-agents.yaml
```

Replace the `${...}` placeholders before applying. The CLI sends manifest files as-is; it does not interpolate environment variables.

Internal channel support exists through the `internal_channel_send` tool. The current tool requires the internal channel UUID, not the manifest name. After `eaos diff` or `eaos apply`, use the `resourceId` for `Channel/agent-mesh-internal` as the `channel_connection_id`.
