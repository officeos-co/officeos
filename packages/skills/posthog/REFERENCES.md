# PostHog — References

## Source

- **Official JS SDK**: https://github.com/PostHog/posthog-js
- **License**: MIT
- **npm**: `posthog-js` (browser), `posthog-node` (server)

## API Documentation

- **API Reference**: https://posthog.com/docs/api
- **Events**: https://posthog.com/docs/api/events
- **Persons**: https://posthog.com/docs/api/persons
- **Feature Flags**: https://posthog.com/docs/api/feature-flags
- **Insights**: https://posthog.com/docs/api/insights
- **Dashboards**: https://posthog.com/docs/api/dashboards
- **Cohorts**: https://posthog.com/docs/api/cohorts
- **Annotations**: https://posthog.com/docs/api/annotations

## Auth Method

`Authorization: Bearer <api_key>` (personal API key from PostHog → Settings → Personal API Keys)

## Base URL

`${host}/api/` — default host is `https://app.posthog.com`. Self-hosted instances use their own URL.

## Key Endpoints Used

| Action | Method | Path |
|--------|--------|------|
| List events | GET | `/api/projects/{project_id}/events/` |
| Get event | GET | `/api/projects/{project_id}/events/{id}/` |
| List persons | GET | `/api/projects/{project_id}/persons/` |
| Get person | GET | `/api/projects/{project_id}/persons/{id}/` |
| Delete person | DELETE | `/api/projects/{project_id}/persons/{id}/` |
| List feature flags | GET | `/api/projects/{project_id}/feature_flags/` |
| Get feature flag | GET | `/api/projects/{project_id}/feature_flags/{id}/` |
| Create feature flag | POST | `/api/projects/{project_id}/feature_flags/` |
| Update feature flag | PATCH | `/api/projects/{project_id}/feature_flags/{id}/` |
| List insights | GET | `/api/projects/{project_id}/insights/` |
| Get insight | GET | `/api/projects/{project_id}/insights/{id}/` |
| Create insight | POST | `/api/projects/{project_id}/insights/` |
| List dashboards | GET | `/api/projects/{project_id}/dashboards/` |
| Get dashboard | GET | `/api/projects/{project_id}/dashboards/{id}/` |
| List cohorts | GET | `/api/projects/{project_id}/cohorts/` |
| Get cohort | GET | `/api/projects/{project_id}/cohorts/{id}/` |
| Create cohort | POST | `/api/projects/{project_id}/cohorts/` |
| List annotations | GET | `/api/projects/{project_id}/annotations/` |
| Create annotation | POST | `/api/projects/{project_id}/annotations/` |
