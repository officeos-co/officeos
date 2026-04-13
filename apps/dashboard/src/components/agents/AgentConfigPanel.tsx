"use client";

import { useEffect, useState } from "react";
import { Save, RefreshCw, AlertTriangle } from "lucide-react";
import type { Agent } from "@/types/agent";
import { agentProxyUrl } from "@/lib/agentProxy";

/**
 * Raw config.toml viewer/editor. The old embedded-agent had a full
 * form-based view with field-by-field bindings — that's deferred; power
 * users can edit the TOML directly which is what the old dashboard's
 * TOML editor mode did anyway. Field-form mode can be added in a later
 * pass if demand exists.
 */
export function AgentConfigPanel({ agent }: { agent: Agent }) {
  const [content, setContent] = useState<string>("");
  const [draft, setDraft] = useState<string>("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch(agentProxyUrl(agent.id, "/api/config"));
      if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
      const ct = res.headers.get("content-type") ?? "";
      let text: string;
      if (ct.includes("application/json")) {
        const parsed = (await res.json()) as { content?: string } | string;
        text = typeof parsed === "string" ? parsed : parsed.content ?? "";
      } else {
        text = await res.text();
      }
      setContent(text);
      setDraft(text);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load config");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [agent.id]);

  const save = async () => {
    setSaving(true);
    setError(null);
    setSaved(false);
    try {
      const res = await fetch(agentProxyUrl(agent.id, "/api/config"), {
        method: "PUT",
        headers: { "content-type": "application/toml" },
        body: draft,
      });
      if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(`${res.status} ${res.statusText}${text ? `: ${text}` : ""}`);
      }
      setContent(draft);
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save config");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="mx-8 my-6">
      <div className="mb-3 flex items-start gap-2 rounded-md border border-yellow-200 bg-yellow-50 px-3 py-2 text-xs text-yellow-700">
        <AlertTriangle className="mt-0.5 h-3.5 w-3.5 flex-shrink-0" />
        <div>
          Editing config.toml may require a pod restart to take effect. Changes are written
          directly to the agent pod.
        </div>
      </div>

      <div className="mb-4 flex items-center justify-between">
        <div className="text-sm text-muted-foreground">config.toml</div>
        <div className="flex gap-2">
          {saved && <span className="text-xs text-emerald-700">Saved.</span>}
          <button
            type="button"
            onClick={load}
            disabled={loading}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5 text-xs hover:bg-accent disabled:opacity-50"
          >
            <RefreshCw className={`h-3 w-3 ${loading ? "animate-spin" : ""}`} />
            Reload
          </button>
          <button
            type="button"
            onClick={save}
            disabled={saving || draft === content}
            className="flex items-center gap-1 rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-primary-foreground disabled:opacity-50"
          >
            <Save className="h-3 w-3" />
            {saving ? "Saving…" : "Save"}
          </button>
        </div>
      </div>

      {error && (
        <div className="mb-3 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-2 text-xs text-destructive">
          {error}
        </div>
      )}

      {loading ? (
        <div className="text-sm text-muted-foreground">Loading…</div>
      ) : (
        <textarea
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          spellCheck={false}
          className="h-[calc(100vh-340px)] w-full resize-none rounded-md border border-border bg-accent p-3 font-mono text-xs outline-none focus:border-primary"
        />
      )}
    </div>
  );
}
