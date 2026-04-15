# Railway Skill — References

## API
- **Endpoint**: `https://backboard.railway.app/graphql/v2`
- **Auth**: `Authorization: Bearer <api_token>`
- **Docs**: https://docs.railway.app/reference/public-api
- **GraphQL Explorer**: https://railway.app/account/api (built-in explorer)
- **API Changelog**: https://docs.railway.app/reference/public-api#changelog

## Railway CLI (reference for operation parity)
- **Source**: https://github.com/railwayapp/cli
- **Docs**: https://docs.railway.app/develop/cli
- **License**: MIT

## Key GraphQL Types
| Type | Description |
|------|-------------|
| `Project` | Top-level container: services, environments, members |
| `Service` | A deployable unit within a project |
| `Deployment` | A specific build/deploy of a service |
| `Environment` | Named environment (production, staging, etc.) |
| `Variable` | Environment variable on a service/environment |
| `Domain` | Custom domain or railway.app subdomain |
| `Log` | Build or deployment log line |

## Rate Limits
- 1000 requests / 15 minutes per token
- Mutations are rate-limited more aggressively (100/15min)
