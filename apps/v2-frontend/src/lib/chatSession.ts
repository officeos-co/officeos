/**
 * Per-agent chat-session state. Each agent gets its own stable session id in
 * localStorage so reloads hydrate from the pod's persisted transcript. A
 * secondary localStorage mirror bounded to MAX_MESSAGES keeps the UI snappy
 * when the network path is slow or the pod has session persistence disabled.
 */

const SESSION_ID_PREFIX = "eaos_chat_session:";
const HISTORY_PREFIX = "eaos_chat_history:";
const MAX_MESSAGES = 100;

export type ChatRole = "user" | "agent";

export type ChatToolCall = {
  name: string;
  args?: unknown;
  output?: string;
};

export type PersistedChatMessage = {
  id: string;
  role: ChatRole;
  content: string;
  thinking?: string;
  markdown?: boolean;
  toolCall?: ChatToolCall;
  timestamp: string;
};

export type ChatMessage = Omit<PersistedChatMessage, "timestamp"> & {
  timestamp: Date;
};

export type ServerSessionMessageRow = {
  role: string;
  content: string;
};

export type ServerSessionMessagesResponse = {
  session_id: string;
  messages: ServerSessionMessageRow[];
  session_persistence: boolean;
};

function uuid(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  return `id-${Math.random().toString(36).slice(2)}-${Date.now()}`;
}

export function getOrCreateSessionId(agentId: string): string {
  if (typeof window === "undefined") return uuid();
  const key = `${SESSION_ID_PREFIX}${agentId}`;
  const existing = localStorage.getItem(key);
  if (existing) return existing;
  const id = uuid();
  localStorage.setItem(key, id);
  return id;
}

export function resetSessionId(agentId: string): string {
  if (typeof window === "undefined") return uuid();
  const key = `${SESSION_ID_PREFIX}${agentId}`;
  const id = uuid();
  localStorage.setItem(key, id);
  localStorage.removeItem(`${HISTORY_PREFIX}${id}`);
  return id;
}

export function loadLocalHistory(sessionId: string): PersistedChatMessage[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = localStorage.getItem(`${HISTORY_PREFIX}${sessionId}`);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as { messages?: PersistedChatMessage[] };
    return parsed.messages ?? [];
  } catch {
    return [];
  }
}

export function saveLocalHistory(
  sessionId: string,
  messages: PersistedChatMessage[],
): void {
  if (typeof window === "undefined") return;
  try {
    const slice = messages.slice(-MAX_MESSAGES);
    localStorage.setItem(
      `${HISTORY_PREFIX}${sessionId}`,
      JSON.stringify({ messages: slice }),
    );
  } catch {
    /* QuotaExceeded or private mode */
  }
}

export function persistedToUi(rows: PersistedChatMessage[]): ChatMessage[] {
  return rows.map((m) => ({ ...m, timestamp: new Date(m.timestamp) }));
}

export function uiToPersisted(messages: ChatMessage[]): PersistedChatMessage[] {
  return messages.map((m) => ({ ...m, timestamp: m.timestamp.toISOString() }));
}

export function mapServerRowsToPersisted(
  rows: ServerSessionMessageRow[],
): PersistedChatMessage[] {
  const base = Date.now() - rows.length * 1000;
  const out: PersistedChatMessage[] = [];
  let idx = 0;
  for (const row of rows) {
    if (row.role === "system") continue;
    const ts = new Date(base + idx * 1000).toISOString();
    idx += 1;
    out.push({
      id: uuid(),
      role: row.role === "user" ? "user" : "agent",
      content: row.content,
      markdown: row.role === "assistant",
      timestamp: ts,
    });
  }
  return out;
}

export function newMessageId(): string {
  return uuid();
}
