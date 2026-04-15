# Prometheus — References

## Source

- **Official repo**: https://github.com/prometheus/prometheus
- **License**: Apache-2.0

## API Documentation

- **HTTP API**: https://prometheus.io/docs/prometheus/latest/querying/api/
- **Querying basics**: https://prometheus.io/docs/prometheus/latest/querying/basics/
- **Functions**: https://prometheus.io/docs/prometheus/latest/querying/functions/
- **Management API**: https://prometheus.io/docs/prometheus/latest/management_api/

## Auth Method

No built-in auth — expose via reverse proxy with basic auth or bearer token if needed.
The `url` credential is the full base URL (e.g. `http://localhost:9090`).

## Base Path

`${url}/api/v1/`

## Key Endpoints Used

| Action | Method | Path |
|--------|--------|------|
| Instant query | GET | `/api/v1/query` |
| Range query | GET | `/api/v1/query_range` |
| Series metadata | GET | `/api/v1/series` |
| Label names | GET | `/api/v1/labels` |
| Label values | GET | `/api/v1/label/{name}/values` |
| Targets | GET | `/api/v1/targets` |
| Rules | GET | `/api/v1/rules` |
| Alerts | GET | `/api/v1/alerts` |
| Config | GET | `/api/v1/status/config` |
| Flags | GET | `/api/v1/status/flags` |
| Metadata | GET | `/api/v1/metadata` |
| TSDB stats | GET | `/api/v1/status/tsdb` |
