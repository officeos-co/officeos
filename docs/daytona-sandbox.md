# Daytona Sandbox

EAOS uses Daytona as its sandbox backend. The backend keeps all sandbox runtime calls
behind `IAgentSandbox`; Daytona is the only registered implementation.

## Backend Configuration

Set these variables for the backend:

- `DAYTONA_API_URL`: Daytona API URL, including `/api` (for example `http://localhost:3000/api`)
- `DAYTONA_API_KEY`: Daytona API key used by EAOS
- `DAYTONA_TARGET`: optional Daytona target/region, defaulted by Daytona when omitted
- `DAYTONA_SNAPSHOT`: optional Daytona snapshot, defaulted by Daytona when omitted
- `DAYTONA_TIMEOUT_SECONDS`: default `60`
- `DAYTONA_WORKDIR`: default `/workspace`

Do not put `DAYTONA_API_KEY` in `appsettings.json` or plain Kubernetes manifests.
Store it in the backend secret.

## Local Development

Run Daytona separately with its official Docker Compose stack:

```bash
git clone https://github.com/daytonaio/daytona.git
cd daytona
docker compose -f docker/docker-compose.yaml up -d
```

Then create a Daytona API key from the Daytona dashboard and put it in
`apps/backend/.env` as `DAYTONA_API_KEY`. The EAOS root `docker-compose.yml` does
not run Daytona; it only points the backend at the external Daytona API.

## Kubernetes

Apply `k8s/daytona.yaml` as a self-hosting starting point. Daytona internal
secrets are expected to be provided by Doppler as `daytona-internal-secrets`.
After Daytona is running, create an API key inside Daytona and add that key to
the backend secret:

```bash
kubectl apply -f k8s/daytona.yaml
kubectl create secret generic eaos-backend-staging-secrets \
  --from-literal=DAYTONA_API_KEY='replace-with-daytona-api-key' \
  --dry-run=client -o yaml | kubectl apply -f -
```

The backend staging/prod manifests point to:

```text
http://daytona-api.daytona-system.svc.cluster.local:3000/api
```

Daytona also has internal service tokens such as `PROXY_API_KEY`,
`DAYTONA_RUNNER_TOKEN`, and `SSH_GATEWAY_API_KEY`. Those are Daytona service
credentials and are intentionally separate from the backend `DAYTONA_API_KEY`.

## API Usage

EAOS uses Daytona's documented REST APIs:

- `POST /sandbox` to create a sandbox
- returned `id` is stored in `AgentRecord.PodName`
- returned `toolboxProxyUrl` is stored in `AgentRecord.ServiceUrl`
- toolbox calls use `{toolboxProxyUrl}/{sandboxId}/process/execute`
- file calls use `{toolboxProxyUrl}/{sandboxId}/files/download` and `/files/upload`
- `DELETE /sandbox/{id}` to terminate

Daytona is AGPL-licensed. EAOS deploys and calls Daytona as an external service;
do not vendor or fork Daytona server source into this repository.
