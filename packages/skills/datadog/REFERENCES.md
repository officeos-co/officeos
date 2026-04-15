# Datadog — References

## Source

- **Official API client**: https://github.com/DataDog/datadog-api-client-typescript
- **License**: Apache-2.0
- **npm**: `@datadog/datadog-api-client`

## API Documentation

- **API Reference**: https://docs.datadoghq.com/api/latest/
- **Authentication**: https://docs.datadoghq.com/api/latest/#authentication
- **Metrics**: https://docs.datadoghq.com/api/latest/metrics/
- **Monitors**: https://docs.datadoghq.com/api/latest/monitors/
- **Dashboards**: https://docs.datadoghq.com/api/latest/dashboards/
- **Logs**: https://docs.datadoghq.com/api/latest/logs/
- **Hosts**: https://docs.datadoghq.com/api/latest/hosts/
- **Events**: https://docs.datadoghq.com/api/latest/events/
- **Downtimes**: https://docs.datadoghq.com/api/latest/downtimes/

## Auth Method

Two headers required on every request:
- `DD-API-KEY`: API Key
- `DD-APPLICATION-KEY`: Application Key

Site-aware base URL: `https://api.{{site}}/api/v1/` (default site: `datadoghq.com`)

## Key Endpoints Used

| Action | Method | Path |
|--------|--------|------|
| Query metrics | GET | `/api/v1/query` |
| Submit metrics | POST | `/api/v2/series` |
| List metrics | GET | `/api/v1/metrics` |
| List monitors | GET | `/api/v1/monitor` |
| Get monitor | GET | `/api/v1/monitor/{monitor_id}` |
| Create monitor | POST | `/api/v1/monitor` |
| Update monitor | PUT | `/api/v1/monitor/{monitor_id}` |
| Mute monitor | POST | `/api/v1/monitor/{monitor_id}/mute` |
| List dashboards | GET | `/api/v1/dashboard` |
| Get dashboard | GET | `/api/v1/dashboard/{dashboard_id}` |
| Search logs | POST | `/api/v2/logs/events/search` |
| List hosts | GET | `/api/v1/hosts` |
| Host totals | GET | `/api/v1/hosts/totals` |
| List events | GET | `/api/v1/events` |
| Create event | POST | `/api/v1/events` |
| List downtimes | GET | `/api/v1/downtime` |
| Create downtime | POST | `/api/v1/downtime` |
| Cancel downtime | DELETE | `/api/v1/downtime/{downtime_id}` |
