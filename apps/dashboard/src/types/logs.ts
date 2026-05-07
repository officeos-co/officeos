export type AgentLog = {
  id: string
  time: number
  type: "tool_call" | "tool_result" | "message_in" | "message_out" | "channel_in" | "channel_out" | "system" | "agent_startup" | "agent_shutdown" | "error"
  tool?: string
  integration?: string
  channel?: string
  channelConnectionId?: string
  content: string
  durationMs?: number
  tokens?: { input: number; output: number }
  correlationId?: string
}
