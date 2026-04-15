# GCP Skill — References

## Proxy Pattern
This skill uses a proxy pattern: the skill-runtime sends `{ command: "gcloud", args: [...] }` to the configured `proxy_url`. The proxy is responsible for executing `gcloud` with the appropriate service account credentials and returning the JSON output.

## Google Cloud SDK (gcloud CLI)
- **Source**: https://github.com/google-cloud/google-cloud-go (Go libs) / https://github.com/google-cloud/gcloud-node
- **Docs**: https://cloud.google.com/sdk/gcloud/reference
- **Install**: https://cloud.google.com/sdk/docs/install
- **Auth**: `gcloud auth activate-service-account --key-file=...` or Application Default Credentials

## API References Used
| Service | Reference |
|---------|-----------|
| Compute Engine | https://cloud.google.com/compute/docs/reference/rest/v1 |
| GKE | https://cloud.google.com/kubernetes-engine/docs/reference/rest/v1 |
| Cloud Run | https://cloud.google.com/run/docs/reference/rest/v2 |
| Cloud Functions | https://cloud.google.com/functions/docs/reference/rest/v2 |
| Cloud Storage | https://cloud.google.com/storage/docs/json_api/v1 |
| BigQuery | https://cloud.google.com/bigquery/docs/reference/rest/v2 |
| Cloud SQL | https://cloud.google.com/sql/docs/mysql/admin-api/rest/v1 |
| IAM | https://cloud.google.com/iam/docs/reference/rest/v1 |

## License
Apache-2.0 (Google Cloud SDK)
