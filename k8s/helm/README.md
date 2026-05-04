# EnterpriseAgentOS Helm Chart

This chart installs the core EnterpriseAgentOS control plane on Kubernetes:

- backend API with channel gateway sidecar
- dashboard frontend
- browser controller and browser node
- Redis
- MinIO workspace storage
- RBAC for creating per-agent Kubernetes pods, services, and config maps

The website and docs deployments are included as optional components and are disabled by default.

## Install

Create a values file for secrets and public URLs. You can start from `examples/values.local.example.yaml`:

```yaml
secrets:
  data:
    CONNECTION_STRING: "Host=postgres.example.internal;Port=5432;Database=eaos;Username=eaos;Password=change-me"
    WORKSPACESTORAGE__ACCESSKEY: "eaos"
    WORKSPACESTORAGE__SECRETKEY: "change-me"
    PLATFORMKEYS__ANTHROPICAPIKEY: ""
    PLATFORMKEYS__OPENAIAPIKEY: ""
    PLATFORMKEYS__GEMINIAPIKEY: ""
    PLATFORMKEYS__XAIAPIKEY: ""

backend:
  frontendOrigin: "https://dashboard.example.com"
  browserPublicViewBaseUrl: "https://browser.example.com"
```

Install the chart:

```bash
cp k8s/helm/examples/values.local.example.yaml values.local.yaml
# Edit values.local.yaml with your database, public URLs, and provider keys.

helm upgrade --install eaos ./k8s/helm \
  --namespace eaos \
  --create-namespace \
  -f values.local.yaml
```

## Existing Secret

If you already manage secrets outside Helm, create a Kubernetes Secret containing at least:

- `CONNECTION_STRING`
- `WORKSPACESTORAGE__ACCESSKEY`
- `WORKSPACESTORAGE__SECRETKEY`
- one or more `PLATFORMKEYS__*` provider keys

Then install with:

```yaml
secrets:
  create: false
  existingSecret: eaos-backend-secrets
```

## External Services

Redis and MinIO install by default. To use external services:

```yaml
redis:
  enabled: false
  urlOverride: "redis.example.internal:6379,defaultDatabase=0"

minio:
  enabled: false
  endpointOverride: "https://s3.example.com"

backend:
  workspaceBucket: "eaos-workspaces"
```

## Optional Website And Docs

```yaml
website:
  enabled: true

docs:
  enabled: true
```

## Local Access

```bash
kubectl -n eaos port-forward svc/eaos-enterprise-agent-os-frontend 3000:3000
kubectl -n eaos port-forward svc/eaos-enterprise-agent-os-browser 6080:6080
```
