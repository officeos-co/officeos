export type ChannelPermissions = {
  /** Agent can receive inbound messages from this channel */
  receive: "allow" | "ask" | "deny"
  /** Agent can send outbound messages through this channel */
  send: "allow" | "ask" | "deny"
  /** Agent can initiate conversations (not just reply) */
  initiate: "allow" | "ask" | "deny"
}

export type Channel = {
  name: string
  slug: string
  logo: string
  description: string
  protocol: string
  likes: number
  updatedAgo: string
  capabilities: string[]
  defaultPermissions: ChannelPermissions
  skillMd: string
}

const SOURCE_BASE = "https://github.com/officeos/channels/tree/main/packages"

export function channelSourceUrl(slug: string) {
  return `${SOURCE_BASE}/${slug}`
}

export const channels: Channel[] = [
  {
    name: "Slack",
    slug: "slack",
    logo: "/logos/slack.svg",
    description: "Receive and respond to Slack messages in real time via WebSocket.",
    protocol: "WebSocket (Socket Mode)",
    likes: 412,
    updatedAgo: "2 days ago",
    capabilities: [
      "Receive direct messages and mentions",
      "Reply in threads",
      "Send to any channel the bot is in",
      "Receive slash commands",
      "React to messages with emoji",
    ],
    defaultPermissions: { receive: "allow", send: "allow", initiate: "ask" },
    skillMd: `# Slack Channel

Real-time bidirectional messaging with Slack workspaces via Socket Mode.

## How it works

The agent connects to Slack via WebSocket (Socket Mode). When a user messages the bot or mentions it in a channel, the message is delivered to the agent's session in real time. The agent can reply in-thread, send new messages, or react with emoji.

## Events the agent receives

| Event | Description |
| ----- | ----------- |
| \`message\` | Direct messages to the bot |
| \`app_mention\` | @mentions in channels |
| \`slash_command\` | Slash commands registered to the app |

## Capabilities

- **Receive** — DMs, @mentions, slash commands
- **Reply** — Respond in the same thread or channel
- **Initiate** — Start new conversations in any channel the bot has access to
- **React** — Add emoji reactions to messages

## Authentication

Requires a Slack App with Socket Mode enabled and a Bot Token (\`xoxb-...\`).
`,
  },
  {
    name: "Discord",
    slug: "discord",
    logo: "/logos/discord.svg",
    description: "Connect to Discord servers and respond to messages via Gateway WebSocket.",
    protocol: "WebSocket (Gateway)",
    likes: 156,
    updatedAgo: "2 weeks ago",
    capabilities: [
      "Receive messages in channels",
      "Receive direct messages",
      "Reply in channels and threads",
      "Send to any channel the bot has access to",
    ],
    defaultPermissions: { receive: "allow", send: "allow", initiate: "ask" },
    skillMd: `# Discord Channel

Real-time messaging with Discord servers via the Gateway WebSocket API.

## How it works

The agent maintains a persistent WebSocket connection to Discord's Gateway. It receives message events from channels and DMs, and can reply or initiate new messages.

## Events the agent receives

| Event | Description |
| ----- | ----------- |
| \`MESSAGE_CREATE\` | New message in a channel or DM |
| \`MESSAGE_UPDATE\` | Edited message |

## Authentication

Requires a Discord Bot Token with Message Content intent enabled.
`,
  },
  {
    name: "Telegram",
    slug: "telegram",
    logo: "/logos/telegram.svg",
    description: "Receive and send Telegram messages via long-polling or webhook.",
    protocol: "Long-polling / Webhook",
    likes: 124,
    updatedAgo: "1 week ago",
    capabilities: [
      "Receive messages from users and groups",
      "Reply to messages",
      "Send messages to any chat the bot is in",
      "Receive inline queries",
    ],
    defaultPermissions: { receive: "allow", send: "allow", initiate: "deny" },
    skillMd: `# Telegram Channel

Bidirectional messaging with Telegram users and groups.

## How it works

The agent receives messages via Telegram Bot API long-polling. When a user sends a message to the bot or in a group where the bot is a member, it's delivered to the agent's session.

## Events the agent receives

| Event | Description |
| ----- | ----------- |
| \`message\` | Text messages from users or groups |
| \`callback_query\` | Button presses from inline keyboards |

## Authentication

Requires a Telegram Bot Token from @BotFather.
`,
  },
  {
    name: "WhatsApp",
    slug: "whatsapp",
    logo: "/logos/whatsapp.svg",
    description: "Receive and send WhatsApp messages via the Cloud API webhook.",
    protocol: "Webhook",
    likes: 201,
    updatedAgo: "4 days ago",
    capabilities: [
      "Receive text messages",
      "Reply to messages within 24h window",
      "Send template messages outside window",
    ],
    defaultPermissions: { receive: "allow", send: "ask", initiate: "deny" },
    skillMd: `# WhatsApp Channel

Bidirectional messaging via the WhatsApp Business Cloud API.

## How it works

Messages arrive via webhook. The agent can reply within the 24-hour customer service window. Outside that window, only pre-approved template messages can be sent.

## Events the agent receives

| Event | Description |
| ----- | ----------- |
| \`message\` | Incoming text message |
| \`status\` | Delivery/read receipts |

## Authentication

Requires WhatsApp Business API credentials and a verified phone number.
`,
  },
  {
    name: "Microsoft Teams",
    slug: "teams",
    logo: "/logos/teams.svg",
    description: "Receive and respond to Microsoft Teams messages via Bot Framework.",
    protocol: "Bot Framework (Webhook)",
    likes: 89,
    updatedAgo: "1 week ago",
    capabilities: [
      "Receive messages in team channels",
      "Receive 1:1 chat messages",
      "Reply in conversations",
      "Send proactive messages",
    ],
    defaultPermissions: { receive: "allow", send: "allow", initiate: "ask" },
    skillMd: `# Microsoft Teams Channel

Bidirectional messaging via the Microsoft Bot Framework.

## How it works

The agent registers as a Bot Framework bot. Messages from Teams users are delivered via webhook. The agent can reply in the same conversation or send proactive messages.

## Events the agent receives

| Event | Description |
| ----- | ----------- |
| \`message\` | Messages in channels or 1:1 chats |
| \`conversationUpdate\` | Members added/removed |

## Authentication

Requires a Microsoft Bot Framework app registration with Teams channel enabled.
`,
  },
]
