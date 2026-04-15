# PagerDuty — References

## Source

- **Official JS client**: https://github.com/PagerDuty/pdjs
- **License**: Apache-2.0
- **npm**: `@pagerduty/pdjs`

## API Documentation

- **API Reference**: https://developer.pagerduty.com/api-reference/
- **Authentication**: https://developer.pagerduty.com/docs/authentication
- **Incidents**: https://developer.pagerduty.com/api-reference/b3A6Mjc0ODIzMw-list-incidents
- **Services**: https://developer.pagerduty.com/api-reference/b3A6Mjc0ODI3MQ-list-services
- **Users**: https://developer.pagerduty.com/api-reference/b3A6Mjc0ODMxMw-list-users
- **On-Calls**: https://developer.pagerduty.com/api-reference/b3A6Mjc0ODE2NA-list-on-calls
- **Schedules**: https://developer.pagerduty.com/api-reference/b3A6Mjc0ODI2MQ-list-schedules
- **Escalation Policies**: https://developer.pagerduty.com/api-reference/b3A6Mjc0ODE0OA-list-escalation-policies

## Auth Method

`Authorization: Token token=<api_key>` header on every request.
Also requires `Accept: application/vnd.pagerduty+json;version=2`.

## Base URL

`https://api.pagerduty.com`

## Key Endpoints Used

| Action | Method | Path |
|--------|--------|------|
| List incidents | GET | `/incidents` |
| Get incident | GET | `/incidents/{id}` |
| Create incident | POST | `/incidents` |
| Update incident | PUT | `/incidents/{id}` |
| Resolve incident | PUT | `/incidents/{id}` |
| List services | GET | `/services` |
| Get service | GET | `/services/{id}` |
| List users | GET | `/users` |
| Get user | GET | `/users/{id}` |
| List on-calls | GET | `/oncalls` |
| List schedules | GET | `/schedules` |
| Get schedule | GET | `/schedules/{id}` |
| List escalation policies | GET | `/escalation_policies` |
| Get escalation policy | GET | `/escalation_policies/{id}` |
| List alerts for incident | GET | `/incidents/{id}/alerts` |
| List notes for incident | GET | `/incidents/{id}/notes` |
| Create note | POST | `/incidents/{id}/notes` |
