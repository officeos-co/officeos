# GCP Skill

Interact with Google Cloud Platform via the `gcloud` CLI proxy. All actions send `{ command: "gcloud", args: [...] }` to the proxy and return parsed JSON output.

## Credentials
- `proxy_url` — URL of the gcloud proxy endpoint (e.g. `https://gcloud-proxy.internal/exec`)

---

## Compute Engine

### `instances_list`
List VM instances in a project and zone.
```
gcloud compute instances list --project=PROJECT --zone=ZONE --format=json
```
Params: `project` (string), `zone` (string, optional), `filter` (string, optional)

### `instances_describe`
Get details of a specific VM instance.
```
gcloud compute instances describe INSTANCE --project=PROJECT --zone=ZONE --format=json
```
Params: `project` (string), `zone` (string), `instance` (string)

### `instances_create`
Create a new VM instance.
```
gcloud compute instances create NAME --project=PROJECT --zone=ZONE --machine-type=TYPE --image-family=FAMILY --image-project=IMAGE_PROJECT --format=json
```
Params: `project`, `zone`, `name`, `machine_type` (default: "e2-micro"), `image_family` (default: "debian-12"), `image_project` (default: "debian-cloud")

### `instances_delete`
Delete a VM instance.
```
gcloud compute instances delete INSTANCE --project=PROJECT --zone=ZONE --quiet --format=json
```
Params: `project`, `zone`, `instance`

### `instances_start` / `instances_stop`
Start or stop a VM instance.
```
gcloud compute instances start|stop INSTANCE --project=PROJECT --zone=ZONE --format=json
```
Params: `project`, `zone`, `instance`

---

## GKE (Kubernetes Engine)

### `clusters_list`
List GKE clusters in a project.
```
gcloud container clusters list --project=PROJECT --format=json
```
Params: `project` (string), `region` (string, optional)

### `clusters_describe`
Get details of a GKE cluster.
```
gcloud container clusters describe CLUSTER --project=PROJECT --region=REGION --format=json
```
Params: `project`, `region`, `cluster`

### `clusters_get_credentials`
Fetch kubeconfig credentials for a cluster.
```
gcloud container clusters get-credentials CLUSTER --project=PROJECT --region=REGION
```
Params: `project`, `region`, `cluster`

---

## Cloud Run

### `services_list`
List Cloud Run services.
```
gcloud run services list --project=PROJECT --region=REGION --format=json
```
Params: `project`, `region`

### `services_describe`
Describe a Cloud Run service.
```
gcloud run services describe SERVICE --project=PROJECT --region=REGION --format=json
```
Params: `project`, `region`, `service`

### `services_deploy`
Deploy or update a Cloud Run service.
```
gcloud run deploy SERVICE --image=IMAGE --project=PROJECT --region=REGION --platform=managed --format=json
```
Params: `project`, `region`, `service`, `image`, `allow_unauthenticated` (boolean, optional)

---

## Cloud Functions

### `functions_list`
List Cloud Functions.
```
gcloud functions list --project=PROJECT --format=json
```
Params: `project`, `region` (optional)

### `functions_describe`
Describe a Cloud Function.
```
gcloud functions describe FUNCTION --project=PROJECT --region=REGION --format=json
```
Params: `project`, `region`, `function`

### `functions_deploy`
Deploy a Cloud Function from source.
```
gcloud functions deploy FUNCTION --project=PROJECT --region=REGION --runtime=RUNTIME --trigger-http --source=SOURCE --format=json
```
Params: `project`, `region`, `function`, `runtime`, `source`, `entry_point` (optional)

---

## Cloud Storage

### `buckets_list`
List GCS buckets.
```
gcloud storage buckets list --project=PROJECT --format=json
```
Params: `project`

### `objects_list`
List objects in a bucket.
```
gcloud storage objects list gs://BUCKET/PREFIX --format=json
```
Params: `bucket`, `prefix` (optional)

---

## BigQuery

### `datasets_list`
List BigQuery datasets.
```
gcloud bq datasets list --project=PROJECT --format=json
```
Params: `project`

### `query`
Run a BigQuery SQL query.
```
bq query --project_id=PROJECT --use_legacy_sql=false --format=json 'QUERY'
```
Params: `project`, `query` (SQL string), `max_results` (number, default: 100)

---

## Exit Codes & Errors
All actions throw on non-zero exit codes with the stderr output as the error message.
JSON output is parsed and returned directly.
