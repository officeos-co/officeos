# Enterprise Cloud Provider UX Flow: Azure

Date: 2026-05-10

## Scenario

A European enterprise wants agents to use Claude through their own Azure tenant instead of sending model traffic through a platform-owned provider key. The example company runs OfficeOS in its own Azure environment, has an Enterprise subscription in OfficeOS, and wants model calls routed to Microsoft Foundry.

## User Roles

- Organization owner/admin: configures provider access in the dashboard.
- Cloud platform team: prepares the Azure resource, RBAC, managed identity, network policy, and optional gateway.
- Agent operators: only see the resulting provider and pinned models in model selection after setup is complete.

Regular users do not see the provider setup flow. The dashboard navigation exposes Providers only when the current organization plan is Enterprise.

## Expected Admin Flow

1. The admin opens `Manage -> Providers`.
2. The page lists cloud providers separately from platform API-key providers.
3. The admin selects `Microsoft Foundry` and clicks `Configure`.
4. The admin chooses one of the supported Claude-style authentication modes:
   - `azure_default_credential`: the backend uses Azure's default credential chain in the deployment environment.
   - `azure_api_key`: the admin enters a Foundry API key.
   - `gateway`: the admin provides an enterprise LLM gateway base URL and OfficeOS skips direct cloud-provider auth.
5. The admin enters the Foundry resource name or a base URL.
6. The admin pins the exact allowed Claude model IDs, for example `claude-sonnet-4-6`.
7. The admin saves the setup.
8. The dashboard shows the provider as connected, displays pinned models, and exposes redacted environment-style status.
9. Optionally, the admin runs a model access check so OfficeOS can verify that the configured provider can actually reach the pinned model.

## Azure Default Credential Flow

This is the preferred enterprise path for a self-hosted Azure deployment.

The cloud platform team assigns the backend workload identity permission to call the Foundry resource. The OfficeOS admin configures:

- Auth kind: `azure_default_credential`
- Resource: for example `acme-ai`
- Pinned models: for example `claude-sonnet-4-6`
- Enabled: true

OfficeOS stores only non-secret setup metadata for this mode:

- Provider slug: `azure-foundry`
- Auth kind: `azure_default_credential`
- Resource or base URL
- Pinned model list
- Enabled/configured timestamps

At runtime, the backend resolves an Azure access token from the process environment using `DefaultAzureCredential`. No user OAuth app is required in OfficeOS for this flow. The credential chain is owned by Azure infrastructure: managed identity, workload identity, environment credentials, or another Azure-supported deployment credential.

## Azure API Key Flow

This exists for enterprises that issue a Foundry API key instead of using managed identity.

The OfficeOS admin configures:

- Auth kind: `azure_api_key`
- Resource or base URL
- API key
- Pinned models
- Enabled: true

OfficeOS stores the API key inside the encrypted provider credential payload. Setup status only shows `<configured>` and never returns the raw key to the dashboard.

At runtime, dispatch sends the API key in the provider request header.

## Gateway Flow

This supports enterprises that already run a central LLM gateway or proxy.

The OfficeOS admin configures:

- Auth kind: `gateway`
- Base URL: for example `https://llm-gateway.eu.example.com/foundry`
- Pinned models
- Enabled: true

OfficeOS stores the gateway URL and a skip-auth marker. Runtime dispatch sends the model request to the gateway without adding Azure auth headers. The gateway owns Azure authentication, policy enforcement, audit logging, and any tenant-specific routing.

## What Agents See

Agents and regular users do not configure Azure credentials. They only see Microsoft Foundry as an available provider after an enterprise admin has enabled it for the organization workspace. Model selection is restricted to the pinned model IDs stored on the provider profile.

If no Foundry profile is configured, if the organization is not Enterprise, or if the requested model is not pinned, the provider is not available for dispatch.

## Storage And Security

Provider setup is stored as an organization provider profile:

- Organization scope, not user scope.
- Provider slug, display name, enabled state, configured timestamp.
- Pinned models as JSON.
- Encrypted credential/config payload.

Secret-bearing modes store secrets encrypted through `CredentialProtector`. Status payloads are redacted before reaching the dashboard.

For `azure_default_credential`, the sensitive trust relationship lives outside OfficeOS in Azure IAM/RBAC. OfficeOS stores only enough metadata to build the Foundry endpoint and know which auth kind to use.

## Operational Checks

After saving a profile, the admin should run the model access check. The backend sends a minimal streaming request to the configured provider and returns either:

- Accessible: the configured credential can reach the pinned model.
- Not accessible: the provider error body is returned so the cloud platform team can fix RBAC, model deployment/access, region, gateway, or networking.

This makes the UX enterprise-friendly without pretending cloud auth is a consumer OAuth connection. The setup mirrors the infrastructure-owned model used by Claude Code for Foundry, Bedrock, and Vertex.
