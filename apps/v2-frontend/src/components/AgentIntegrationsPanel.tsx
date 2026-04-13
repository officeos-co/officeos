"use client";

import { useState } from "react";
import { Plug, Plus, RefreshCw, Trash2, Power, PowerOff, Settings } from "lucide-react";
import type { Agent } from "@/hooks/useAgents";
import { useAgentChannels, type AgentChannelConfig } from "@/hooks/useAgentChannels";
import {
  useChannelConnections,
  type ChannelConnection,
} from "@/hooks/useChannelConnections";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const channelEmoji: Record<string, string> = {
  slack: "#",
  telegram: "@",
  discord: "D",
  whatsapp: "W",
  teams: "T",
  "google-chat": "G",
  webchat: "C",
};

function BindChannelForm({
  availableConnections,
  onBind,
  onCancel,
}: {
  availableConnections: ChannelConnection[];
  onBind: (connectionId: string, config: AgentChannelConfig) => Promise<void>;
  onCancel: () => void;
}) {
  const [selectedId, setSelectedId] = useState("");
  const [config, setConfig] = useState<AgentChannelConfig>({
    dmPolicy: "open",
    groupPolicy: "mention",
    requireMention: true,
    historyLimit: 10,
    streamingMode: "off",
  });
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async () => {
    if (!selectedId) return;
    setSubmitting(true);
    try {
      await onBind(selectedId, config);
    } finally {
      setSubmitting(false);
    }
  };

  if (availableConnections.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-border bg-card px-4 py-8 text-center">
        <div className="text-sm text-muted-foreground">
          No available channel connections.
        </div>
        <div className="mt-1 text-xs text-muted-foreground">
          Add channels in Org Settings first.
        </div>
        <Button variant="outline" size="sm" className="mt-3" onClick={onCancel}>
          Close
        </Button>
      </div>
    );
  }

  return (
    <div className="rounded-xl border border-border bg-card p-4">
      <div className="mb-3 text-sm font-medium">Connect Channel</div>

      <div className="flex flex-col gap-3">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="channel-select">Channel</Label>
          <select
            id="channel-select"
            value={selectedId}
            onChange={(e) => setSelectedId(e.target.value)}
            className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
          >
            <option value="">Select a channel...</option>
            {availableConnections.map((c) => (
              <option key={c.id} value={c.id}>
                {c.displayName} ({c.channelType})
              </option>
            ))}
          </select>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="dm-policy">DM Policy</Label>
          <select
            id="dm-policy"
            value={config.dmPolicy}
            onChange={(e) => setConfig({ ...config, dmPolicy: e.target.value })}
            className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
          >
            <option value="open">Open (anyone can DM)</option>
            <option value="allowlist">Allowlist only</option>
            <option value="disabled">Disabled</option>
          </select>
        </div>

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="require-mention"
            checked={config.requireMention}
            onChange={(e) =>
              setConfig({ ...config, requireMention: e.target.checked })
            }
            className="h-4 w-4 rounded border-input"
          />
          <Label htmlFor="require-mention" className="text-sm font-normal">
            Require @mention in group chats
          </Label>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="history-limit">History Limit</Label>
          <Input
            id="history-limit"
            type="number"
            value={config.historyLimit}
            onChange={(e) =>
              setConfig({ ...config, historyLimit: parseInt(e.target.value) || 10 })
            }
            min={1}
            max={100}
          />
          <p className="text-[11px] text-muted-foreground">
            Number of previous messages to include as context.
          </p>
        </div>

        <div className="flex items-center gap-2 pt-1">
          <Button
            size="sm"
            onClick={handleSubmit}
            disabled={submitting || !selectedId}
          >
            {submitting ? "Binding..." : "Bind Channel"}
          </Button>
          <Button variant="outline" size="sm" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </div>
    </div>
  );
}

function BindingCard({
  binding,
  onToggle,
  onUnbind,
}: {
  binding: {
    id: string;
    channelType: string;
    channelDisplayName: string;
    enabled: boolean;
    config: AgentChannelConfig | null;
  };
  onToggle: () => void;
  onUnbind: () => void;
}) {
  const [deleting, setDeleting] = useState(false);
  const [showConfig, setShowConfig] = useState(false);

  return (
    <div className="rounded-xl border border-border bg-card p-4">
      <div className="flex items-center gap-3">
        <div className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-muted text-sm font-bold text-muted-foreground">
          {channelEmoji[binding.channelType] ?? "?"}
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium">{binding.channelDisplayName}</span>
            <span className="rounded border border-border px-1.5 py-0.5 text-[10px] text-muted-foreground">
              {binding.channelType}
            </span>
          </div>
          <div className="mt-0.5 flex items-center gap-2 text-xs text-muted-foreground">
            <span
              className={`h-1.5 w-1.5 rounded-full ${binding.enabled ? "bg-emerald-500" : "bg-muted-foreground/40"}`}
            />
            {binding.enabled ? "Active" : "Disabled"}
            {binding.config && (
              <>
                {" \u00B7 "}DM: {binding.config.dmPolicy}
                {binding.config.requireMention && " \u00B7 mention required"}
              </>
            )}
          </div>
        </div>
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => setShowConfig(!showConfig)}
            title="Settings"
            className="rounded-md p-2 text-muted-foreground hover:bg-accent hover:text-foreground"
          >
            <Settings className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={onToggle}
            title={binding.enabled ? "Disable" : "Enable"}
            className="rounded-md p-2 text-muted-foreground hover:bg-accent hover:text-foreground"
          >
            {binding.enabled ? (
              <Power className="h-4 w-4" />
            ) : (
              <PowerOff className="h-4 w-4" />
            )}
          </button>
          <button
            type="button"
            onClick={async () => {
              setDeleting(true);
              await onUnbind();
            }}
            disabled={deleting}
            className="rounded-md p-2 text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      </div>
      {showConfig && binding.config && (
        <div className="mt-3 border-t border-border pt-3">
          <div className="grid grid-cols-2 gap-2 text-xs">
            <div>
              <span className="text-muted-foreground">DM Policy:</span>{" "}
              {binding.config.dmPolicy}
            </div>
            <div>
              <span className="text-muted-foreground">Group Policy:</span>{" "}
              {binding.config.groupPolicy}
            </div>
            <div>
              <span className="text-muted-foreground">Mention Required:</span>{" "}
              {binding.config.requireMention ? "Yes" : "No"}
            </div>
            <div>
              <span className="text-muted-foreground">History Limit:</span>{" "}
              {binding.config.historyLimit}
            </div>
            <div>
              <span className="text-muted-foreground">Streaming:</span>{" "}
              {binding.config.streamingMode}
            </div>
            {binding.config.allowedUsers && binding.config.allowedUsers.length > 0 && (
              <div className="col-span-2">
                <span className="text-muted-foreground">Allowed Users:</span>{" "}
                {binding.config.allowedUsers.join(", ")}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

export function AgentIntegrationsPanel({ agent }: { agent: Agent }) {
  const { bindings, loading, error, refresh, bind, update, unbind } =
    useAgentChannels(agent.id);
  const {
    connections,
    loading: connectionsLoading,
  } = useChannelConnections();
  const [showBindForm, setShowBindForm] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  // Filter out connections the agent is already bound to
  const boundConnectionIds = new Set(bindings.map((b) => b.channelConnectionId));
  const availableConnections = connections.filter(
    (c) => c.enabled && !boundConnectionIds.has(c.id),
  );

  const handleBind = async (connectionId: string, config: AgentChannelConfig) => {
    setActionError(null);
    try {
      await bind(connectionId, config);
      setShowBindForm(false);
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Failed to bind channel");
    }
  };

  return (
    <div className="mx-8 my-6">
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Plug className="h-4 w-4" />
          {bindings.length} channel{bindings.length === 1 ? "" : "s"} bound
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={refresh}
            className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5 text-xs hover:bg-accent"
          >
            <RefreshCw className="h-3 w-3" />
            Refresh
          </button>
          {!showBindForm && (
            <Button size="sm" onClick={() => setShowBindForm(true)}>
              <Plus className="mr-1.5 h-3 w-3" />
              Connect Channel
            </Button>
          )}
        </div>
      </div>

      {(error || actionError) && (
        <div className="mb-3 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-2 text-xs text-destructive">
          {error || actionError}
        </div>
      )}

      {showBindForm && (
        <div className="mb-4">
          <BindChannelForm
            availableConnections={availableConnections}
            onBind={handleBind}
            onCancel={() => setShowBindForm(false)}
          />
        </div>
      )}

      {loading || connectionsLoading ? (
        <div className="py-8 text-center text-sm text-muted-foreground">
          Loading channels...
        </div>
      ) : bindings.length === 0 && !showBindForm ? (
        <div className="rounded-xl border border-dashed border-border bg-card px-6 py-12 text-center text-sm text-muted-foreground">
          No channels bound to this agent.
          <div className="mt-1 text-xs">
            Connect a channel to let users interact with this agent via Slack, Telegram,
            Discord, and more.
          </div>
        </div>
      ) : (
        <div className="flex flex-col gap-3">
          {bindings.map((b) => (
            <BindingCard
              key={b.id}
              binding={b}
              onToggle={() => update(b.id, { enabled: !b.enabled })}
              onUnbind={() => unbind(b.id)}
            />
          ))}
        </div>
      )}
    </div>
  );
}
