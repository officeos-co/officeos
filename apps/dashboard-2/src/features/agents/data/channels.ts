export type ChannelPermissions = {
  /** Agent can receive inbound messages from this channel */
  receive: "allow" | "ask" | "deny"
  /** Agent can send outbound messages through this channel */
  send: "allow" | "ask" | "deny"
  /** Agent can initiate conversations (not just reply) */
  initiate: "allow" | "ask" | "deny"
}

export type OnboardingStep = {
  title: string
  description: string
  action: "url" | "qr" | "input" | "copy"
  value?: string
  inputKey?: string
  inputLabel?: string
  inputPlaceholder?: string
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
  added: boolean
  onboarding: OnboardingStep[]
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
    added: true,
    defaultPermissions: { receive: "allow", send: "allow", initiate: "ask" },
    onboarding: [
      { title: "Create a Slack App", description: "Go to api.slack.com/apps and create a new app with Socket Mode enabled.", action: "url", value: "https://api.slack.com/apps" },
      { title: "Install to workspace", description: "Install the app to your Slack workspace and copy the Bot Token.", action: "copy", value: "https://api.slack.com/apps/{app_id}/install-on-team" },
      { title: "Enter Bot Token", description: "Paste the Bot Token (xoxb-...) from your Slack App settings.", action: "input", inputKey: "SLACK_BOT_TOKEN", inputLabel: "Bot Token", inputPlaceholder: "xoxb-..." },
    ],
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
    added: false,
    defaultPermissions: { receive: "allow", send: "allow", initiate: "ask" },
    onboarding: [
      { title: "Create a Discord Bot", description: "Go to the Discord Developer Portal and create a new application with a Bot user.", action: "url", value: "https://discord.com/developers/applications" },
      { title: "Enable intents", description: "Enable Message Content Intent in the Bot settings.", action: "url", value: "https://discord.com/developers/applications" },
      { title: "Enter Bot Token", description: "Copy the Bot Token from the Bot settings page.", action: "input", inputKey: "DISCORD_BOT_TOKEN", inputLabel: "Bot Token", inputPlaceholder: "" },
    ],
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
    added: false,
    defaultPermissions: { receive: "allow", send: "allow", initiate: "deny" },
    onboarding: [
      { title: "Create a Telegram Bot", description: "Message @BotFather on Telegram and use /newbot to create a bot.", action: "url", value: "https://t.me/BotFather" },
      { title: "Copy the Bot Token", description: "BotFather will give you a token. Copy it below.", action: "input", inputKey: "TELEGRAM_BOT_TOKEN", inputLabel: "Bot Token", inputPlaceholder: "" },
      { title: "Scan QR code", description: "Share this QR code with users to start messaging your bot.", action: "qr", value: "https://t.me/your_bot_name" },
    ],
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
    added: false,
    defaultPermissions: { receive: "allow", send: "ask", initiate: "deny" },
    onboarding: [
      { title: "Set up WhatsApp Business API", description: "Create a Meta Business account and set up the WhatsApp Cloud API.", action: "url", value: "https://business.facebook.com" },
      { title: "Configure webhook", description: "Set this URL as your webhook endpoint in the WhatsApp API settings.", action: "copy", value: "https://api.officeos.co/webhooks/whatsapp/{agent_id}" },
      { title: "Enter credentials", description: "Enter your WhatsApp Business API credentials.", action: "input", inputKey: "WHATSAPP_TOKEN", inputLabel: "Access Token", inputPlaceholder: "" },
    ],
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
    added: false,
    defaultPermissions: { receive: "allow", send: "allow", initiate: "ask" },
    onboarding: [
      { title: "Register a Bot Framework app", description: "Create a Bot registration in the Azure Portal.", action: "url", value: "https://portal.azure.com/#create/Microsoft.BotServiceConnectivity" },
      { title: "Enable Teams channel", description: "In the Bot Channels Registration, add Microsoft Teams as a channel.", action: "url", value: "https://portal.azure.com" },
      { title: "Enter App credentials", description: "Enter the Microsoft App ID and password.", action: "input", inputKey: "TEAMS_APP_ID", inputLabel: "App ID", inputPlaceholder: "" },
    ],
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
