```yaml
apiVersion: officeos.io/v1
kind: Channel
metadata:
  name: support-slack
spec:
  type: slack
  displayName: Support Slack
  enabled: true
  permissionPolicy:
    type: allow_list
    tools:
      - internal_channel_send
  credentials:
    botToken: ${SLACK_BOT_TOKEN}
    signingSecret: ${SLACK_SIGNING_SECRET}
```

# Channel

A `Channel` registers an external or internal message surface that agents can attach to.

Required fields: `apiVersion`, `kind`, `metadata.name`, `spec.type`.

Non-`internal` channels also need `spec.token` or at least one credential value.

Supported types: `internal`, `slack`, `telegram`.

`spec.permissionPolicy` controls channel-owned agent harness tools for agents attached to this channel. Supported policy types are `always_allow`, `always_deny`, `allow_list`, and `deny_list`.

Attach a channel from an `Agent` with `spec.channels[].ref`.
