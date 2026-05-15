```yaml
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
      events:
        - pull_request
      pollIntervalSeconds: 60
```

# Routine

A `Routine` runs an agent prompt from schedules, API triggers, or GitHub repository activity.

Required fields: `apiVersion`, `kind`, `metadata.name`, `spec.agentRef`, `spec.prompt`, and at least one trigger.

`scheduleTriggers[].expression` is cron syntax.

`githubTriggers[].repo` accepts `https://github.com/owner/repo.git`, `https://github.com/owner/repo`, `git@github.com:owner/repo.git`, or `owner/repo`. Prefer the HTTPS clone URL form in manifests.

GitHub trigger modes: `poll`, `webhook`. Polling is the default and uses the workspace GitHub OAuth credential. Webhook mode requires `secret` and a GitHub webhook that can reach the backend.
