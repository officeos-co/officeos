```yaml
apiVersion: officeos.io/v1
kind: Agent
metadata:
  name: support-agent
spec:
  provider: anthropic
  model: claude-sonnet-4-6
  description: Answers support questions from approved sources.
  system: Answer clearly and cite the source material.
  tools:
    builtin:
      permissionPolicy:
        type: always_allow
    browser:
      permissionPolicy:
        type: allow_list
        tools:
          - browser.open
          - browser.screenshot
  integrations:
    - ref: github
      permissionPolicy:
        type: always_allow
  channels:
    - ref: support-slack
      config:
        mode: mentions
  memoryStores:
    - ref: product-docs
      accessMode: read_only
      instructions: Use as source material.
  browsers:
    - ref: qa-browser
      accessMode: read_write
      instructions: Verify customer-visible pages.
  metadata:
    owner: support
```

# Agent

An `Agent` is the runnable worker definition: provider, model, prompt, tools, and attached resources.

Required fields: `apiVersion`, `kind`, `metadata.name`, `spec.provider`, `spec.model`.

The referenced `Provider`, integrations, channels, memory stores, and browsers must already exist or appear in the same manifest.

Permission policy types: `always_allow`, `always_deny`, `allow_list`, `deny_list`.

Resource access modes: `read_only`, `read_write`.
