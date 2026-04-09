"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { Agent } from "@/hooks/useAgents";

type WsIncoming = {
  type:
    | "message"
    | "chunk"
    | "chunk_reset"
    | "thinking"
    | "tool_call"
    | "tool_result"
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
};

type ChatMessage = {
  id: string;
  role: "user" | "agent";
  content: string;
  pending?: boolean;
};

function buildWsUrl(agentId: string): string {
  if (typeof window === "undefined") return "";
  const isLocalhost =
    window.location.hostname === "localhost" || window.location.hostname === "127.0.0.1";
  if (isLocalhost) {
    return `ws://localhost:5080/api/agents/${agentId}/ws`;
  }
  return `wss://api.harrokrog.com/api/agents/${agentId}/ws`;
}

export function AgentChatPanel({ agent }: { agent: Agent }) {
  const wsUrl = useMemo(() => buildWsUrl(agent.id), [agent.id]);
  const wsRef = useRef<WebSocket | null>(null);
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const [connected, setConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState("");

  const appendAgentChunk = useCallback((chunk: string) => {
    setMessages((prev) => {
      const last = prev[prev.length - 1];
      if (last && last.role === "agent" && last.pending) {
        const updated = { ...last, content: last.content + chunk };
        return [...prev.slice(0, -1), updated];
      }
      return [
        ...prev,
        {
          id: crypto.randomUUID(),
          role: "agent",
          content: chunk,
          pending: true,
        },
      ];
    });
  }, []);

  const finalizePending = useCallback((finalContent?: string) => {
    setMessages((prev) => {
      const last = prev[prev.length - 1];
      if (!last || last.role !== "agent" || !last.pending) return prev;
      const updated = {
        ...last,
        content: finalContent ?? last.content,
        pending: false,
      };
      return [...prev.slice(0, -1), updated];
    });
  }, []);

  useEffect(() => {
    if (!wsUrl) return;

    const ws = new WebSocket(wsUrl, ["zeroclaw.v1"]);
    wsRef.current = ws;

    ws.onopen = () => {
      setConnected(true);
      setError(null);
    };

    ws.onmessage = (ev) => {
      try {
        const msg = JSON.parse(ev.data) as WsIncoming;
        switch (msg.type) {
          case "chunk":
            if (msg.content) appendAgentChunk(msg.content);
            break;
          case "chunk_reset":
            setMessages((prev) => {
              const last = prev[prev.length - 1];
              if (last?.role === "agent" && last.pending) {
                return [...prev.slice(0, -1), { ...last, content: "" }];
              }
              return prev;
            });
            break;
          case "message":
            if (msg.content) {
              setMessages((prev) => [
                ...prev,
                {
                  id: crypto.randomUUID(),
                  role: "agent",
                  content: msg.content!,
                },
              ]);
            }
            break;
          case "done":
            finalizePending(msg.full_response);
            break;
          case "error":
            setError(msg.message ?? "Agent returned an error");
            finalizePending();
            break;
          default:
            break;
        }
      } catch {
        /* ignore non-JSON frames */
      }
    };

    ws.onerror = () => {
      setError("WebSocket error");
    };

    ws.onclose = () => {
      setConnected(false);
    };

    return () => {
      wsRef.current = null;
      if (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING) {
        ws.close();
      }
    };
  }, [wsUrl, appendAgentChunk, finalizePending]);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight });
  }, [messages]);

  const onSend = (e: React.FormEvent) => {
    e.preventDefault();
    const text = input.trim();
    if (!text || !wsRef.current || wsRef.current.readyState !== WebSocket.OPEN) {
      return;
    }
    setMessages((prev) => [
      ...prev,
      { id: crypto.randomUUID(), role: "user", content: text },
    ]);
    wsRef.current.send(JSON.stringify({ type: "message", content: text }));
    setInput("");
  };

  return (
    <div className="mx-8 my-6 flex h-[calc(100vh-260px)] flex-col rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)]">
      <div className="flex items-center justify-between border-b border-[var(--eaos-border)] px-4 py-2 text-xs">
        <span className="text-[var(--eaos-text-muted)]">
          {agent.podName ?? "no pod"}
        </span>
        <span
          className={
            connected
              ? "rounded-full border border-emerald-500/40 px-2 py-0.5 text-emerald-300"
              : "rounded-full border border-[var(--eaos-border)] px-2 py-0.5 text-[var(--eaos-text-muted)]"
          }
        >
          {connected ? "connected" : "connecting..."}
        </span>
      </div>

      {error && (
        <div className="border-b border-red-500/30 bg-red-500/10 px-4 py-2 text-xs text-red-300">
          {error}
        </div>
      )}

      <div ref={scrollRef} className="flex-1 overflow-y-auto px-4 py-4">
        {messages.length === 0 && (
          <div className="mt-10 text-center text-sm text-[var(--eaos-text-muted)]">
            Send a message to start chatting with the agent.
          </div>
        )}
        <div className="flex flex-col gap-3">
          {messages.map((m) => (
            <div
              key={m.id}
              className={[
                "max-w-[80%] whitespace-pre-wrap rounded-lg px-3 py-2 text-sm",
                m.role === "user"
                  ? "self-end bg-white text-black"
                  : "self-start border border-[var(--eaos-border)] bg-black/30",
              ].join(" ")}
            >
              {m.content || (m.pending ? "…" : "")}
            </div>
          ))}
        </div>
      </div>

      <form
        onSubmit={onSend}
        className="flex gap-2 border-t border-[var(--eaos-border)] p-3"
      >
        <input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          disabled={!connected}
          placeholder={connected ? "Message…" : "Waiting for connection…"}
          className="flex-1 rounded-md border border-[var(--eaos-border)] bg-black/40 px-3 py-2 text-sm outline-none focus:border-white disabled:opacity-50"
        />
        <button
          type="submit"
          disabled={!connected || !input.trim()}
          className="rounded-md bg-white px-4 py-2 text-sm font-medium text-black disabled:opacity-50"
        >
          Send
        </button>
      </form>
    </div>
  );
}
