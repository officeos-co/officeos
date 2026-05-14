# Quickstart

Run OfficeOS locally with Docker infrastructure, the backend API, and the dashboard.

## Before you start

Install these tools:

- Docker Desktop or another Docker-compatible runtime
- .NET SDK 9
- Bun

You also need at least one Provider manifest with credentials for the model provider you want to use. OfficeOS does not read model provider keys from backend environment variables.

## Step 1: Clone the repository

```bash
git clone https://github.com/HarKro753/EnterpriseAgentOs.git
cd EnterpriseAgentOs
```

## Step 2: Configure environment variables

Copy the example environment file.

```bash
cp .env.example .env
```

For local development, keep the default Postgres, Redis, MinIO, browser, and Docker settings unless you are running those services elsewhere.

Provider credentials are declared through `officeos.io/v1` Provider manifests. Keep local files that contain real keys in an ignored path such as `example/local-secrets/`.

## Step 3: Build the agent runtime image

Agents run in isolated pod-executor containers. Build the local image before launching agents.

```bash
docker build -t harkro123/eaos-pod-executor:latest packages/pod-executor
```

Rebuild this image whenever you change `packages/pod-executor`.

## Step 4: Start local infrastructure

Start Postgres, Redis, MinIO, channels, and browser services from the repository root.

```bash
docker compose -f docker-compose.infra.yml up -d
```

Check that the containers are running:

```bash
docker compose -f docker-compose.infra.yml ps
```

Useful local service URLs:

| Service            | URL                      |
| ------------------ | ------------------------ |
| MinIO API          | `http://localhost:9000`  |
| MinIO Console      | `http://localhost:9001`  |
| Channels           | `http://localhost:3100`  |
| Browser controller | `http://localhost:18080` |
| Browser view       | `http://localhost:6080`  |

## Step 5: Start the backend

Run the API from the repository root.

```bash
dotnet run --project apps/backend/src/EnterpriseAgentOs.Api
```

The dashboard defaults to `http://localhost:5000` for the backend API in development. If your API binds to a different port, update the dashboard environment configuration before starting the dashboard.

## Step 6: Start the dashboard

Open a second terminal and run:

```bash
cd apps/dashboard
bun install
bun dev
```

Open the dashboard at `http://localhost:3000`.

## Step 7: Launch an agent

In the dashboard:

1. Open Agents.
2. Create a new agent.
3. Select a provider and model that you applied through a Provider manifest.
4. Add a system prompt.
5. Launch the agent.

The backend stores the agent record, starts an isolated runtime container, and proxies chat traffic between the dashboard and the runtime.

## Stop local services

Stop the infrastructure containers when you are done.

```bash
docker compose -f docker-compose.infra.yml down
```

Use this only when you also want to remove local volumes:

```bash
docker compose -f docker-compose.infra.yml down -v
```

## Next steps

- Read [Agent Lifecycle](/agent-lifecycle) to understand how agent records become running runtimes.
- Read [LLM Proxy](/llm-proxy) to see how model calls flow through the backend.
- Read [Environment Management](/environment-management) before wiring production secrets.
