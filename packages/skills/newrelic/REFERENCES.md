# New Relic — References

## Source

- **Official Node.js agent**: https://github.com/newrelic/node-newrelic
- **License**: Apache-2.0
- **npm**: `newrelic`

## API Documentation

- **NerdGraph (GraphQL)**: https://docs.newrelic.com/docs/apis/nerdgraph/get-started/introduction-new-relic-nerdgraph/
- **NerdGraph explorer**: https://api.newrelic.com/graphiql
- **REST API v2**: https://rpm.newrelic.com/api/explore
- **NRQL**: https://docs.newrelic.com/docs/nrql/get-started/introduction-nrql-new-relics-query-language/
- **Alerts**: https://docs.newrelic.com/docs/apis/nerdgraph/examples/nerdgraph-api-alerts-policies-conditions/
- **Dashboards**: https://docs.newrelic.com/docs/apis/nerdgraph/examples/nerdgraph-dashboards/
- **Synthetics**: https://docs.newrelic.com/docs/apis/nerdgraph/examples/synthetic-monitoring-mutations/
- **Deployments**: https://docs.newrelic.com/docs/apis/nerdgraph/examples/nerdgraph-changes-tracking-api/

## Auth Method

`Api-Key: <api_key>` header (User API key from New Relic → Profile → API Keys).

## Endpoints

- **NerdGraph**: `POST https://api.newrelic.com/graphql`
- **REST v2** (Applications, Deployments): `https://api.newrelic.com/v2/`

## Key NerdGraph Operations

| Action | NerdGraph entity |
|--------|-----------------|
| NRQL query | `actor.account(id).nrql(query)` |
| List dashboards | `actor.entitySearch` with `type = DASHBOARD` |
| Get dashboard | `actor.entity(guid)` |
| Create dashboard | `dashboardCreate` mutation |
| Alert policies | `actor.account(id).alerts.policiesSearch` |
| Alert conditions | `actor.account(id).alerts.nrqlConditionsSearch` |
| Synthetics monitors | `actor.entitySearch` with `type = MONITOR` |
| Deployments | `changeTrackingCreateDeployment` mutation |
