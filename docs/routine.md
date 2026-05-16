```yaml
apiVersion: officeos.io/v1
kind: Credential
metadata:
  name: github
spec:
  provider: github
  authKind: personal_access_token
  credentials:
    GITHUB_PERSONAL_ACCESS_TOKEN: ${GITHUB_PERSONAL_ACCESS_TOKEN}
---
apiVersion: officeos.io/v1
kind: Routine
metadata:
  name: daily-support-summary
spec:
  agentRef: support-agent
  prompt: Summarize open support work and list blockers.
  scheduleTriggers:
    - name: Weekday morning
      expression: "0 9 * * 1-5"
  apiTriggers:
    - name: Manual run
  githubTriggers:
    - name: Pull request events
      repo: https://github.com/acme/platform.git
      authRef: github
      events:
        - pull_request
      pollIntervalSeconds: 60
```

# Routine

A `Routine` runs an agent prompt from schedules, API triggers, or GitHub repository activity.

Required fields: `apiVersion`, `kind`, `metadata.name`, `spec.agentRef`, `spec.prompt`, and at least one trigger.

`scheduleTriggers[].expression` is cron syntax.

`githubTriggers[].repo` accepts `https://github.com/owner/repo.git`, `https://github.com/owner/repo`, `git@github.com:owner/repo.git`, or `owner/repo`. Prefer the HTTPS clone URL form in manifests.

GitHub trigger modes: `poll`, `webhook`. Polling is the default and requires `githubTriggers[].authRef` to point at a `Credential` resource. Webhook mode requires a GitHub webhook that can reach the backend.

Before using a polling GitHub trigger, connect GitHub for the workspace:

```bash
officeos credential auth github
```

Or define a GitHub token declaratively in the same manifest:

```yaml
apiVersion: officeos.io/v1
kind: Credential
metadata:
  name: github
spec:
  provider: github
  authKind: personal_access_token
  credentials:
    GITHUB_PERSONAL_ACCESS_TOKEN: ${GITHUB_PERSONAL_ACCESS_TOKEN}
---
apiVersion: officeos.io/v1
kind: Routine
metadata:
  name: pr-summary
spec:
  agentRef: support-agent
  prompt: Summarize the new pull request activity.
  githubTriggers:
    - name: Pull request events
      repo: https://github.com/acme/platform.git
      authRef: github
      events:
        - pull_request
      pollIntervalSeconds: 60
```

The first poll initializes the cursor at the current time. New matching repository activity after that cursor triggers the routine and is included in the agent prompt as the trigger payload.
