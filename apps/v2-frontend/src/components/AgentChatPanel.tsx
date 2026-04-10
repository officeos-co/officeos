"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { Bot, User, AlertCircle, Copy, Check, Send } from "lucide-react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { Agent } from "@/hooks/useAgents";
import { agentFetch, agentWsUrl } from "@/lib/agentProxy";
import {
  type ChatMessage,
  type ServerSessionMessagesResponse,
  getOrCreateSessionId,
  loadLocalHistory,
  mapServerRowsToPersisted,
  newMessageId,
  persistedToUi,
  saveLocalHistory,
  uiToPersisted,
} from "@/lib/chatSession";

type WsIncoming = {
  type:
    | "message"
    | "chunk"
    | "chunk_reset"
    | "thinking"
    | "tool_call"
    | "tool_result"
    | "cron_result"
    | "done"
    | "error"
    | "session_start"
    | "connected";
  content?: string;
  full_response?: string;
  name?: string;
  args?: unknown;
  output?: string;
  message?: string;
  code?: string;
  timestamp?: string;
};

export function AgentChatPanel({ agent }: { agent: Agent }) {
  const sessionIdRef = useRef<string>(getOrCreateSessionId(agent.id));
  const [messages, setMessages] = useState<ChatMessage[]>(() =>
    persistedToUi(loadLocalHistory(sessionIdRef.current)),
  );
  const [historyReady, setHistoryReady] = useState(false);
  const [input, setInput] = useState("");
  const [typing, setTyping] = useState(false);
  const [connected, setConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const [streamingContent, setStreamingContent] = useState("");
  const [streamingThinking, setStreamingThinking] = useState("");

  const [reconnecting, setReconnecting] = useState(false);

  const wsRef = useRef<WebSocket | null>(null);
  const messagesEndRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLTextAreaElement | null>(null);
  const pendingContentRef = useRef("");
  const pendingThinkingRef = useRef("");
  const capturedThinkingRef = useRef("");
  const reconnectTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const reconnectDelayRef = useRef(1000);
  const retriesRef = useRef(0);
  const intentionalCloseRef = useRef(false);
  const MAX_RECONNECT_DELAY = 30_000;
  const MAX_RETRIES = 10;

  // Hydrate from the pod's persisted transcript; fall back to localStorage.
  useEffect(() => {
    const sid = sessionIdRef.current;
    let cancelled = false;

    (async () => {
      try {
        const res = await agentFetch<ServerSessionMessagesResponse>(
          agent.id,
          `/api/sessions/${encodeURIComponent(sid)}/messages`,
        );
        if (cancelled) return;
        if (res.session_persistence && res.messages.length > 0) {
          setMessages((prev) =>
            prev.length > 0 ? prev : persistedToUi(mapServerRowsToPersisted(res.messages)),
          );
        }
      } catch {
        // Pod may be unreachable or session not yet created — localStorage
        // hydration from the initial state already provided best-effort.
      } finally {
        if (!cancelled) setHistoryReady(true);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [agent.id]);

  // Mirror transcript to localStorage once we've finished the initial hydrate.
  useEffect(() => {
    if (!historyReady) return;
    saveLocalHistory(sessionIdRef.current, uiToPersisted(messages));
  }, [messages, historyReady]);

  // Open a WebSocket scoped to this session id, with reconnect.
  useEffect(() => {
    intentionalCloseRef.current = false;
    reconnectDelayRef.current = 1000;
    retriesRef.current = 0;

    function connectWs() {
      const url = agentWsUrl(agent.id, { session_id: sessionIdRef.current });
      if (!url) return;

      const ws = new WebSocket(url, ["zeroclaw.v1"]);
      wsRef.current = ws;

      ws.onopen = () => {
        setConnected(true);
        setReconnecting(false);
        setError(null);
        reconnectDelayRef.current = 1000;
        retriesRef.current = 0;
      };

      ws.onclose = (ev) => {
        setConnected(false);
        if (intentionalCloseRef.current) return;
        if (ev.code !== 1000 && ev.code !== 1001 && retriesRef.current < MAX_RETRIES) {
          setReconnecting(true);
          const delay = reconnectDelayRef.current;
          reconnectTimerRef.current = setTimeout(() => {
            reconnectDelayRef.current = Math.min(delay * 2, MAX_RECONNECT_DELAY);
            retriesRef.current += 1;
            connectWs();
          }, delay);
        } else if (retriesRef.current >= MAX_RETRIES) {
          setReconnecting(false);
          setError("Connection lost. Refresh the page to reconnect.");
        }
      };

      ws.onerror = () => {
        // onclose fires after onerror — reconnect is handled there
      };

    ws.onmessage = (ev) => {
      let msg: WsIncoming;
      try {
        msg = JSON.parse(ev.data) as WsIncoming;
      } catch {
        return;
      }
      switch (msg.type) {
        case "session_start":
        case "connected":
          break;

        case "thinking":
          setTyping(true);
          pendingThinkingRef.current += msg.content ?? "";
          setStreamingThinking(pendingThinkingRef.current);
          break;

        case "chunk":
          setTyping(true);
          pendingContentRef.current += msg.content ?? "";
          setStreamingContent(pendingContentRef.current);
          break;

        case "chunk_reset":
          capturedThinkingRef.current = pendingThinkingRef.current;
          pendingContentRef.current = "";
          pendingThinkingRef.current = "";
          setStreamingContent("");
          setStreamingThinking("");
          break;

        case "message":
        case "done": {
          const content = msg.full_response ?? msg.content ?? pendingContentRef.current;
          const thinking =
            capturedThinkingRef.current || pendingThinkingRef.current || undefined;
          if (content) {
            setMessages((prev) => [
              ...prev,
              {
                id: newMessageId(),
                role: "agent",
                content,
                thinking,
                markdown: true,
                timestamp: new Date(),
              },
            ]);
          }
          pendingContentRef.current = "";
          pendingThinkingRef.current = "";
          capturedThinkingRef.current = "";
          setStreamingContent("");
          setStreamingThinking("");
          setTyping(false);
          break;
        }

        case "tool_call": {
          const toolName = msg.name ?? "unknown";
          const toolArgs = msg.args;
          setMessages((prev) => {
            const argsKey = JSON.stringify(toolArgs ?? {});
            const duplicate = prev.some(
              (m) =>
                m.toolCall &&
                m.toolCall.output === undefined &&
                m.toolCall.name === toolName &&
                JSON.stringify(m.toolCall.args ?? {}) === argsKey,
            );
            if (duplicate) return prev;
            return [
              ...prev,
              {
                id: newMessageId(),
                role: "agent",
                content: `↳ ${toolName}(${argsKey})`,
                toolCall: { name: toolName, args: toolArgs },
                timestamp: new Date(),
              },
            ];
          });
          break;
        }

        case "tool_result": {
          setMessages((prev) => {
            const idx = prev.findIndex((m) => m.toolCall && m.toolCall.output === undefined);
            if (idx !== -1) {
              const updated = [...prev];
              const existing = prev[idx]!;
              updated[idx] = {
                ...existing,
                toolCall: { ...existing.toolCall!, output: msg.output ?? "" },
              };
              return updated;
            }
            return [
              ...prev,
              {
                id: newMessageId(),
                role: "agent",
                content: `← ${msg.output ?? ""}`,
                toolCall: { name: msg.name ?? "unknown", output: msg.output ?? "" },
                timestamp: new Date(),
              },
            ];
          });
          break;
        }

        case "cron_result": {
          const output = msg.output ?? "";
          if (output) {
            setMessages((prev) => [
              ...prev,
              {
                id: newMessageId(),
                role: "agent",
                content: output,
                markdown: true,
                timestamp: new Date(msg.timestamp ?? Date.now()),
              },
            ]);
          }
          break;
        }

        case "error":
          setMessages((prev) => [
            ...prev,
            {
              id: newMessageId(),
              role: "agent",
              content: `⚠ ${msg.message ?? "Unknown error"}`,
              timestamp: new Date(),
            },
          ]);
          setError(msg.message ?? "Agent returned an error");
          setTyping(false);
          pendingContentRef.current = "";
          pendingThinkingRef.current = "";
          setStreamingContent("");
          setStreamingThinking("");
          break;
      }
    };

    } // end connectWs

    connectWs();

    return () => {
      intentionalCloseRef.current = true;
      if (reconnectTimerRef.current) clearTimeout(reconnectTimerRef.current);
      const ws = wsRef.current;
      wsRef.current = null;
      if (ws && (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING)) {
        ws.close(1000, "component unmount");
      }
    };
  }, [agent.id]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, typing, streamingContent]);

  const handleSend = useCallback(() => {
    const trimmed = input.trim();
    if (!trimmed || !wsRef.current || wsRef.current.readyState !== WebSocket.OPEN) return;

    setMessages((prev) => [
      ...prev,
      {
        id: newMessageId(),
        role: "user",
        content: trimmed,
        timestamp: new Date(),
      },
    ]);
    try {
      wsRef.current.send(JSON.stringify({ type: "message", content: trimmed }));
      setTyping(true);
      pendingContentRef.current = "";
      pendingThinkingRef.current = "";
    } catch {
      setError("Failed to send message");
    }
    setInput("");
    if (inputRef.current) {
      inputRef.current.style.height = "auto";
      inputRef.current.focus();
    }
  }, [input]);

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleCopy = useCallback((msgId: string, content: string) => {
    const onSuccess = () => {
      setCopiedId(msgId);
      setTimeout(() => setCopiedId((prev) => (prev === msgId ? null : prev)), 2000);
    };
    if (navigator.clipboard?.writeText) {
      navigator.clipboard.writeText(content).then(onSuccess).catch(() => {
        /* ignore */
      });
    }
  }, []);

  return (
    <div className="mx-8 my-6 flex h-[calc(100vh-260px)] flex-col rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)]">
      <div className="flex items-center justify-between border-b border-[var(--eaos-border)] px-4 py-2 text-xs">
        <div className="flex items-center gap-3">
          <span className="text-[var(--eaos-text-muted)]">{agent.podName ?? "no pod"}</span>
          <span className="font-mono text-[10px] text-[var(--eaos-text-muted)]">
            session: {sessionIdRef.current.slice(0, 8)}
          </span>
        </div>
        <span
          className={
            connected
              ? "rounded-full border border-emerald-500/40 px-2 py-0.5 text-emerald-300"
              : reconnecting
                ? "rounded-full border border-yellow-500/40 px-2 py-0.5 text-yellow-300"
                : "rounded-full border border-[var(--eaos-border)] px-2 py-0.5 text-[var(--eaos-text-muted)]"
          }
        >
          {connected ? "connected" : reconnecting ? "reconnecting…" : "disconnected"}
        </span>
      </div>

      {error && (
        <div className="flex items-center gap-2 border-b border-red-500/30 bg-red-500/10 px-4 py-2 text-xs text-red-300">
          <AlertCircle className="h-3.5 w-3.5" />
          {error}
        </div>
      )}

      <div className="flex-1 space-y-4 overflow-y-auto p-4">
        {messages.length === 0 && !typing && (
          <div className="flex h-full flex-col items-center justify-center text-center text-sm text-[var(--eaos-text-muted)]">
            <Bot className="mb-3 h-8 w-8" />
            Send a message to start chatting with {agent.name}.
          </div>
        )}

        {messages.map((m) => (
          <div
            key={m.id}
            className={`group flex items-start gap-3 ${
              m.role === "user" ? "flex-row-reverse" : ""
            }`}
          >
            <div
              className={`flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-xl border ${
                m.role === "user"
                  ? "border-white bg-white text-black"
                  : "border-[var(--eaos-border)] bg-black/30"
              }`}
            >
              {m.role === "user" ? <User className="h-4 w-4" /> : <Bot className="h-4 w-4" />}
            </div>
            <div className="relative max-w-[75%]">
              <div
                className={`rounded-xl px-3 py-2 text-sm ${
                  m.role === "user"
                    ? "bg-white text-black"
                    : "border border-[var(--eaos-border)] bg-black/30 text-white"
                }`}
              >
                {m.thinking && (
                  <details className="mb-2">
                    <summary className="cursor-pointer text-[11px] text-[var(--eaos-text-muted)]">
                      Thinking
                    </summary>
                    <pre className="mt-1 max-h-60 overflow-auto whitespace-pre-wrap break-words rounded bg-black/30 p-2 text-[11px] text-[var(--eaos-text-muted)]">
                      {m.thinking}
                    </pre>
                  </details>
                )}
                {m.toolCall ? (
                  <div className="font-mono text-[11px]">
                    <div className="text-[var(--eaos-text-muted)]">
                      tool: {m.toolCall.name}
                    </div>
                    {m.toolCall.args !== undefined && (
                      <pre className="mt-1 max-h-40 overflow-auto whitespace-pre-wrap break-words rounded bg-black/40 p-2">
                        {JSON.stringify(m.toolCall.args, null, 2)}
                      </pre>
                    )}
                    {m.toolCall.output !== undefined && (
                      <pre className="mt-1 max-h-40 overflow-auto whitespace-pre-wrap break-words rounded bg-black/40 p-2">
                        {m.toolCall.output}
                      </pre>
                    )}
                  </div>
                ) : m.markdown ? (
                  <div className="break-words text-sm [&>*]:my-1 [&_code]:rounded [&_code]:bg-black/40 [&_code]:px-1 [&_pre]:overflow-auto [&_pre]:rounded [&_pre]:bg-black/40 [&_pre]:p-2">
                    <ReactMarkdown remarkPlugins={[remarkGfm]}>{m.content}</ReactMarkdown>
                  </div>
                ) : (
                  <p className="whitespace-pre-wrap break-words text-sm">{m.content}</p>
                )}
                <p className="mt-1 text-[10px] opacity-70">
                  {m.timestamp.toLocaleTimeString()}
                </p>
              </div>
              <button
                type="button"
                onClick={() => handleCopy(m.id, m.content)}
                className="absolute right-1 top-1 rounded border border-[var(--eaos-border)] bg-black/60 p-1 opacity-0 transition-opacity group-hover:opacity-100"
                aria-label="Copy message"
              >
                {copiedId === m.id ? (
                  <Check className="h-3 w-3 text-emerald-400" />
                ) : (
                  <Copy className="h-3 w-3" />
                )}
              </button>
            </div>
          </div>
        ))}

        {typing && (streamingContent || streamingThinking) && (
          <div className="flex items-start gap-3">
            <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-xl border border-[var(--eaos-border)] bg-black/30">
              <Bot className="h-4 w-4" />
            </div>
            <div className="max-w-[75%] rounded-xl border border-[var(--eaos-border)] bg-black/30 px-3 py-2 text-sm">
              {streamingThinking && (
                <details className="mb-2" open={!streamingContent}>
                  <summary className="cursor-pointer text-[11px] text-[var(--eaos-text-muted)]">
                    Thinking{!streamingContent && "…"}
                  </summary>
                  <pre className="mt-1 max-h-60 overflow-auto whitespace-pre-wrap break-words rounded bg-black/30 p-2 text-[11px] text-[var(--eaos-text-muted)]">
                    {streamingThinking}
                  </pre>
                </details>
              )}
              {streamingContent && (
                <p className="whitespace-pre-wrap break-words">{streamingContent}</p>
              )}
            </div>
          </div>
        )}

        {typing && !streamingContent && !streamingThinking && (
          <div className="flex items-start gap-3">
            <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-xl border border-[var(--eaos-border)] bg-black/30">
              <Bot className="h-4 w-4" />
            </div>
            <div className="rounded-xl border border-[var(--eaos-border)] bg-black/30 px-3 py-2 text-xs text-[var(--eaos-text-muted)]">
              …
            </div>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      <div className="flex gap-2 border-t border-[var(--eaos-border)] p-3">
        <textarea
          ref={inputRef}
          rows={1}
          value={input}
          onChange={(e) => {
            setInput(e.target.value);
            e.currentTarget.style.height = "auto";
            e.currentTarget.style.height = `${Math.min(e.currentTarget.scrollHeight, 200)}px`;
          }}
          onKeyDown={handleKeyDown}
          disabled={!connected}
          placeholder={connected ? "Message…" : "Waiting for connection…"}
          className="flex-1 resize-none rounded-md border border-[var(--eaos-border)] bg-black/40 px-3 py-2 text-sm outline-none focus:border-white disabled:opacity-50"
          style={{ minHeight: "40px", maxHeight: "200px" }}
        />
        <button
          type="button"
          onClick={handleSend}
          disabled={!connected || !input.trim()}
          className="flex items-center gap-1 rounded-md bg-white px-4 py-2 text-sm font-medium text-black disabled:opacity-50"
        >
          <Send className="h-4 w-4" />
          Send
        </button>
      </div>
    </div>
  );
}
