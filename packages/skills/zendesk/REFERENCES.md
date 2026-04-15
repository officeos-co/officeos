# Zendesk Skill — References

## Source library
- **Repo**: https://github.com/blakmatrix/node-zendesk
- **License**: MIT
- **npm**: `node-zendesk`

## API reference
- **Base URL**: `https://{subdomain}.zendesk.com/api/v2/`
- **Auth**: HTTP Basic — username `{email}/token`, password `{api_token}`
- **Docs**: https://developer.zendesk.com/api-reference/
- **Rate limits**: 700 req/min (Enterprise), 200 req/min (others)

## Key endpoints used
| Endpoint | Method | Action |
|---|---|---|
| `/tickets` | GET | list_tickets |
| `/tickets` | POST | create_ticket |
| `/tickets/{id}` | GET | get_ticket |
| `/tickets/{id}` | PUT | update_ticket |
| `/tickets/{id}` | DELETE | delete_ticket |
| `/tickets/search` | GET | search_tickets |
| `/tickets/{id}/comments` | GET | list_comments |
| `/tickets/{id}/comments` | POST | add_comment |
| `/users` | GET | list_users |
| `/users/{id}` | GET | get_user |
| `/users` | POST | create_user |
| `/organizations` | GET | list_organizations |
| `/organizations/{id}` | GET | get_organization |
| `/search` | GET | search |
| `/views` | GET | list_views |
| `/views/{id}/tickets` | GET | execute_view |
| `/macros` | GET | list_macros |
| `/macros/{id}/apply` | POST | apply_macro |
