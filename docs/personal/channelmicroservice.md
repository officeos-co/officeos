# Channel Microservice Architecture

## Ownership Split

The channel system is split between the **C# backend** and the **TypeScript channel microservice** (`apps/channels`).

### Backend owns (main Postgres DB)

| Table | What | Why |
|---|---|---|
| `ChannelConnections` | `Id, ChannelType, DisplayName, Enabled, CreatedAt, CreatedById` | Lightweight metadata — dashboard needs to list connections without calling the microservice. No credentials, no platform config. |
| `AgentChannelBindings` | `Id, AgentId, ChannelConnectionId, Enabled, Config` | Which agent listens on which connection. This is an agent concern — agents live in the backend DB, so bindings must too. |

### Microservice owns (own DB — SQLite, Postgres, or in-memory)

| Data | What |
|---|---|
| Platform credentials | Bot tokens, signing secrets, service account keys, OAuth tokens |
| Connection state | WebSocket sessions (WhatsApp), webhook registrations, QR pairing state |
| Message deduplication | Per-platform idempotency tracking |
| Platform-specific config | Webhook URLs, retry policies, rate limits |

## Why this split

- **Agents reference connections by ID.** The backend needs to query "which connections does this agent broadcast to?" without an HTTP call. The binding table must be local.
- **Credentials never enter the backend.** The dashboard sends creds to the backend, which immediately forwards them to the microservice. Backend never stores, decrypts, or inspects platform secrets.
- **Connection metadata is cached locally.** Dashboard lists connections frequently. Having `Id + ChannelType + DisplayName` local avoids constant microservice calls for simple UI rendering.

---

## API Contract — What the microservice must expose

Base URL: configured via `CHANNEL_SERVICE_URL` env var (default `http://localhost:3100`)

### `POST /api/connections/start`

Called when a new channel connection is created in the dashboard.

```json
{ "connectionId": "uuid", "channelType": "slack" }
```

The microservice should:
- Create its internal record for this connection ID
- Begin any platform-specific setup (e.g., WhatsApp QR pairing listener)
- Return `200 OK`

### `POST /api/connections/{connectionId}/creds`

Called when the user submits platform credentials (bot token, signing secret, etc.) during onboarding.

```json
{ "connectionId": "uuid", "credsJson": "{\"botToken\":\"xoxb-...\",\"signingSecret\":\"...\"}" }
```

The microservice should:
- Store credentials in its own DB (encrypted at rest)
- Activate the connection (register webhooks, start listeners, etc.)
- Return `200 OK`

### `POST /api/send`

Called when an agent broadcasts a message to a channel connection.

```json
{ "connectionId": "uuid", "text": "Hello from the agent!" }
```

The microservice should:
- Look up the connection's platform + credentials
- Send the message via the platform API (Slack `chat.postMessage`, Telegram `sendMessage`, etc.)
- Handle chunking, markdown conversion, rate limiting internally
- Return `200 OK` on success, `5xx` on failure

### `DELETE /api/connections/{connectionId}`

Called when a connection is deleted from the dashboard.

The microservice should:
- Deregister webhooks
- Close WebSocket sessions
- Delete credentials from its DB
- Return `200 OK`

### `POST /api/webhooks/{channelType}`

**Not called by the backend.** This is where platforms (Slack, Telegram, Discord, etc.) send inbound messages. The microservice receives them, resolves the `connectionId`, and calls the backend to route the message to the right agent.

The microservice should call the backend:
```
POST http://api.officeos.co/api/channels/inbound
{
  "connectionId": "uuid",
  "senderIdentifier": "U12345",
  "messageText": "Hello agent",
  "isGroupMessage": false,
  "messageId": "platform-msg-id",
  "channelId": "C12345"
}
```

The backend looks up agent bindings for that `connectionId` and dispatches the turn.

---

## Data flow diagrams

### Outbound (agent → platform)

```
Agent turn emits MessageOut event
  → BroadcastToChannelsHandler
    → ChannelService.BroadcastAsync()
      → queries AgentChannelBindings (local DB)
      → for each binding: POST /api/send to microservice
        → microservice sends via platform API
```

### Inbound (platform → agent)

```
Platform webhook hits microservice
  → microservice resolves connectionId
  → POST /api/channels/inbound on backend
    → backend looks up bindings for connectionId
    → dispatches agent turn for each bound agent
    → returns agent response
  → microservice sends response back to platform
```

### Connection setup

```
Dashboard: user clicks "Add Slack"
  → GraphQL mutation: createChannelConnection("slack", "My Workspace")
    → backend saves metadata to ChannelConnections table
    → backend calls POST /api/connections/start on microservice
  → Dashboard shows onboarding steps (bot token, signing secret)
  → User submits credentials
    → GraphQL mutation or REST call
      → backend calls POST /api/connections/{id}/creds on microservice
      → microservice stores creds, registers webhook, activates
```

---

## Backend code removed

- All platform adapters: `SlackAdapter`, `DiscordAdapter`, `TelegramAdapter`, `TeamsAdapter`, `GoogleChatAdapter`, `WhatsAppGatewayService`
- `ChannelAdapterRegistry`, `IChannelAdapter`, `ChannelBroadcastService`, `ChannelMessageRouter`
- `Channels/Common/` (InMemoryMessageDeduplicator, PlatformTextChunker, MarkdownFormatConverter)
- `ChannelWebhooksController`, `WhatsAppInternalController`, `WhatsAppTypes`
- `WhatsAppGatewayConfig`
- `IChannelConfigProtector` + `ChannelConfigProtector` (no creds in backend)
- `EncryptedConfig` and `TestMessageSent` columns dropped from `ChannelConnectionRecord`

## Backend code remaining

- `ChannelConnectionRecord` — lightweight metadata (id, type, name, enabled)
- `AgentChannelBindingRecord` — agent ↔ connection mapping with policy config
- `ChannelRepository` — EF Core CRUD for both tables
- `ChannelService` — orchestration (broadcast, create/delete lifecycle)
- `ChannelMicroserviceGateway` — HTTP client implementing `IChannelGateway`, proxies to microservice
- `ChannelMutations`, `ChannelQueries`, `ChannelTypes` — GraphQL for dashboard
- `ChannelDto`, `ChannelBindingConfig` — domain DTOs
- `ChannelTypes` — static definitions of supported channel types with onboarding steps

## Backend inbound endpoint needed

The microservice needs a backend endpoint to forward inbound platform messages to:

```
POST /api/channels/inbound
```

This replaces the old `ChannelWebhooksController`. The microservice normalizes inbound messages from all platforms into this single format before calling the backend.

## EF Migration

After deploying, create a migration to drop the removed columns:

```bash
cd apps/backend
dotnet ef migrations add RemoveChannelCredsFromBackend --project src/EnterpriseAgentOs.Infrastructure --startup-project src/EnterpriseAgentOs.Api
dotnet ef database update --project src/EnterpriseAgentOs.Infrastructure --startup-project src/EnterpriseAgentOs.Api
```

This drops `EncryptedConfig` and `TestMessageSent` from the `ChannelConnections` table.
