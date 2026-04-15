# Klaviyo Skill — References

## Source library
- **Repo**: https://github.com/klaviyo/klaviyo-api-node
- **License**: MIT
- **npm**: `klaviyo-api`

## API reference
- **Base URL**: `https://a.klaviyo.com/api/`
- **Auth**: Header `Klaviyo-API-Key: <api_key>`
- **Revision header**: `revision: 2024-10-15` (latest stable)
- **Docs**: https://developers.klaviyo.com/en/reference/api-overview
- **API versioning**: All requests must include `revision` header

## Key endpoints used
| Endpoint | Method | Action |
|---|---|---|
| `/profiles` | GET | list_profiles |
| `/profiles` | POST | create_profile |
| `/profiles/{id}` | GET | get_profile |
| `/profiles/{id}` | PATCH | update_profile |
| `/profiles/?filter=...` | GET | search_profiles |
| `/lists` | GET | list_lists |
| `/lists` | POST | create_list |
| `/lists/{id}` | GET | get_list |
| `/lists/{id}/relationships/profiles` | POST | add_profiles_to_list |
| `/segments` | GET | list_segments |
| `/segments/{id}` | GET | get_segment |
| `/campaigns` | GET | list_campaigns |
| `/campaigns` | POST | create_campaign |
| `/campaigns/{id}` | GET | get_campaign |
| `/flows` | GET | list_flows |
| `/flows/{id}` | GET | get_flow |
| `/templates` | GET | list_templates |
| `/templates/{id}` | GET | get_template |
| `/events` | POST | track_event |
| `/events` | GET | list_events |
| `/metrics` | GET | list_metrics |
