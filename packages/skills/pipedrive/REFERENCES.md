# Pipedrive Skill — References

## Source library
- **Repo**: https://github.com/pipedrive/client-nodejs
- **License**: MIT
- **npm**: `pipedrive` (official Node.js client)

## API reference
- **Base URL**: `https://api.pipedrive.com/v1/`
- **Auth**: Query param `?api_token=<token>` on every request
- **Docs**: https://developers.pipedrive.com/docs/api/v1
- **OpenAPI spec**: https://developers.pipedrive.com/docs/api/v1/openapi.json

## Key endpoints used
| Endpoint | Method | Action |
|---|---|---|
| `/deals` | GET | list_deals |
| `/deals` | POST | create_deal |
| `/deals/{id}` | GET | get_deal |
| `/deals/{id}` | PUT | update_deal |
| `/deals/{id}` | DELETE | delete_deal |
| `/deals/search` | GET | search_deals |
| `/persons` | GET | list_persons |
| `/persons` | POST | create_person |
| `/persons/{id}` | GET | get_person |
| `/persons/{id}` | PUT | update_person |
| `/organizations` | GET | list_organizations |
| `/organizations` | POST | create_organization |
| `/organizations/{id}` | GET | get_organization |
| `/activities` | GET | list_activities |
| `/activities` | POST | create_activity |
| `/activities/{id}` | PUT | update_activity |
| `/pipelines` | GET | list_pipelines |
| `/stages` | GET | list_stages |
| `/itemSearch` | GET | search |
