# Environment Management

## Overview

EnterpriseAgentOs follows the 12-Factor App pattern. Code never contains secrets — values are injected at runtime via environment variables.

| Layer          | Dev (self-hosted)                    | Staging / Production                |
| -------------- | ------------------------------------ | ----------------------------------- |
| Secrets        | `.env` file or `doppler run`         | Doppler K8s Operator                |
| Infra config   | `appsettings.json` defaults          | K8s manifest `env:`                 |
| Feature gating | `ASPNETCORE_ENVIRONMENT=Development` | `ASPNETCORE_ENVIRONMENT=Production` |

## Doppler Setup

### Project structure

```
officeos (project)
  ├── dev    — local development
  ├── stg    — staging cluster
  └── prd    — production cluster
```

### What goes in Doppler (secrets only)

```
CONNECTION_STRING          # DB connection with password
GOOGLEOAUTH__CLIENTID      # OAuth credentials
GOOGLEOAUTH__CLIENTSECRET
GOOGLEOAUTH__REDIRECTURI
GOOGLEOAUTH__SKILLOAUTHREDIRECTURI
WORKSPACESTORAGE__ACCESSKEY  # MinIO/S3 workspace credentials
WORKSPACESTORAGE__SECRETKEY
STRIPE__SECRETKEY          # Billing
STRIPE__WEBHOOKSECRET
STRIPE__*PRICEID           # All Stripe price IDs
PLATFORMKEYS__*APIKEY      # LLM provider keys
POSTHOG__APIKEY            # Analytics
DOCKER_INSECURE_REGISTRY   # Private Docker registry host for CI DinD
```

### What goes in K8s manifests (infra config)

```yaml
env:
  - name: ASPNETCORE_ENVIRONMENT
    value: Production
  - name: REDIS
    value: "eaos-redis:6379,defaultDatabase=0"
  - name: FRONTEND_ORIGIN
    value: "https://dashboard.officeos.co"
  - name: DATA_PROTECTION_KEY_PATH
    value: "/data/dp-keys"
  - name: KUBERNETES__NAMESPACE
    value: "default"
  - name: KUBERNETES__IMAGE
    value: "harkro123/eaos-pod-executor:latest"
  - name: WORKSPACESTORAGE__ENDPOINT
    value: "http://eaos-minio-prod:9000"
  - name: WORKSPACESTORAGE__BUCKET
    value: "eaos-workspaces-prod"
  - name: SKILLGATEWAY__URL
    value: "http://eaos-backend-prod:8000"
  - name: SKILLRUNTIME__URL
    value: "http://eaos-skill-runtime.default.svc.cluster.local:3001"
  - name: MINIO__BUCKET
    value: "skills"
  - name: POSTHOG__HOST
    value: "https://eu.i.posthog.com"
```

### Local dev with Doppler CLI

```bash
# One-time setup
doppler setup  # select officeos → dev

# Run backend with secrets injected
doppler run --config dev -- dotnet run --project apps/backend/src/EnterpriseAgentOs.Api

# Generate .env for docker-compose
doppler secrets download --config dev --format env --no-file > .env
docker compose up
```

### K8s operator

The Doppler K8s Operator auto-syncs secrets to K8s Secret objects.

```
k8s/doppler.yaml          # DopplerSecret CRs (committed)
doppler-token-prod         # K8s Secret with service token (created manually)
doppler-token-staging      # K8s Secret with service token (created manually)
eaos-backend-prod-secrets  # Auto-created by operator, referenced in envFrom
eaos-backend-staging-secrets
```

Install:

```bash
helm repo add doppler https://helm.doppler.com
helm install doppler-secrets-operator doppler/doppler-kubernetes-operator \
  --namespace doppler-operator-system --create-namespace
```

Create service tokens:

```bash
doppler configs tokens create --config prd --name k8s-operator --plain
# → dp.st.prd.xxxx

kubectl create secret generic doppler-token-prod \
  --from-literal=serviceToken=dp.st.prd.xxxx

kubectl apply -f k8s/doppler.yaml
```

Add or update a production secret:

```bash
doppler secrets set DOCKER_INSECURE_REGISTRY --project officeos --config prd
```

## Environment Detection

### Backend (.NET)

```csharp
// In Program.cs — available as local variable
var isDevelopment = builder.Environment.IsDevelopment();

// In services — inject IHostEnvironment
public class MyService(IHostEnvironment env)
{
    private readonly bool _isDev = env.IsDevelopment();
}

// In HotChocolate resolvers — resolve from context
if (!context.Service<IWebHostEnvironment>().IsDevelopment())
    throw new GraphQLException(...);
```

Set via `ASPNETCORE_ENVIRONMENT` env var (`Development`, `Staging`, `Production`).

### Frontend (Next.js)

```typescript
import { isDevelopment } from "@/lib/env"

// Gate UI features
...(isDevelopment() ? [{ title: "Providers", url: "/providers" }] : [])
...(!isDevelopment() ? [{ title: "Billing", url: "/billing" }] : [])

// Redirect in dev
useEffect(() => {
  if (isDevelopment()) {
    window.location.href = "https://officeos.co/pricing"
    return
  }
}, [])
```

Set via `APP_ENV` env var (`development`, `staging`, `production`).

## Feature Gating by Environment

### Disabled in Development (self-hosted)

| Feature                 | Backend                                                      | Frontend                                                    |
| ----------------------- | ------------------------------------------------------------ | ----------------------------------------------------------- |
| Stripe billing          | Guards on `string.IsNullOrWhiteSpace(SecretKey)`             | Billing/Cost pages hidden, pricing redirects to officeos.co |
| PostHog analytics       | `!env.IsDevelopment() && !string.IsNullOrWhiteSpace(ApiKey)` | `AnalyticsPageview` no-ops                                  |
| Provider BYOK mutations | Enabled (gated to dev only)                                  | Providers page shown                                        |
| Upgrade Plan menu item  | N/A                                                          | Hidden                                                      |

### Enabled only in Development

| Feature                   | Details                                               |
| ------------------------- | ----------------------------------------------------- |
| Docker agent sandbox      | `appsettings.json` defaults, not in Doppler           |
| Swagger UI                | `app.Environment.IsDevelopment()` check in Program.cs |
| Provider key CRUD         | GraphQL mutations gated behind `IsDevelopment()`      |
| "Try OfficeOS Cloud" link | Sidebar link to dashboard.officeos.co                 |

## Config Classes

All in `Infrastructure/Common/Configuration/`. No default values — everything must be injected.

```csharp
// Good — no defaults
public string Url { get; set; } = string.Empty;

// Bad — hides missing config
public string Url { get; set; } = "http://localhost:3001";
```

Validation happens in `Program.cs` at startup:

```csharp
// Top-level keys (support both PascalCase and UPPER_SNAKE from Doppler)
var redis = Require("Redis", "REDIS");

// Section keys (validated after bind)
var googleOAuthConfig = RequireSection<GoogleOAuthConfig>("GoogleOAuth");
RequireNotEmpty(googleOAuthConfig.ClientId, "GoogleOAuth:ClientId");

// Prod-only validation
if (!isDevelopment)
{
    RequireNotEmpty(stripeConfig.SecretKey, "Stripe:SecretKey");
    RequireNotEmpty(stripeConfig.WebhookSecret, "Stripe:WebhookSecret");
}
```

## Self-Hosting

Self-hosters copy `.env.example` → `.env` and fill in their values. They never interact with Doppler.

```bash
cp apps/backend/.env.example .env
# Edit .env with your values
docker compose up
```

Docker config (image, network, socket path) comes from `appsettings.json` defaults — self-hosters don't need to set these.

Agent runtime sandboxes use the first-party pod executor image. Development uses
the Docker sandbox. Staging and production use the Kubernetes sandbox and must
set `KUBERNETES__NAMESPACE` and `KUBERNETES__IMAGE`. Workspaces are stored as
compressed S3 objects in MinIO through `WORKSPACESTORAGE__*` config; runtime pods
use disposable `emptyDir` storage and are not the durable source of truth.
