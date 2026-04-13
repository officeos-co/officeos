"use client";

import { useState } from "react";
import { Plus, Trash2, Power, PowerOff, RefreshCw } from "lucide-react";
import { TopBar } from "@/components/shared/TopBar";
import {
  useChannelConnections,
  type ChannelType,
  type ChannelConnection,
} from "@/hooks/useChannelConnections";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";

const channelEmoji: Record<string, string> = {
  slack: "#",
  telegram: "@",
  discord: "D",
  whatsapp: "W",
  teams: "T",
  "google-chat": "G",
  webchat: "C",
};

function ChannelSetupWizard({
  channelTypes,
  onComplete,
  onCancel,
}: {
  channelTypes: ChannelType[];
  onComplete: (type: string, name: string, config: Record<string, string>) => void;
  onCancel: () => void;
}) {
  const [step, setStep] = useState<"type" | "config">("type");
  const [selectedType, setSelectedType] = useState<ChannelType | null>(null);
  const [displayName, setDisplayName] = useState("");
  const [config, setConfig] = useState<Record<string, string>>({});
  const [submitting, setSubmitting] = useState(false);

  const handleSelectType = (t: ChannelType) => {
    setSelectedType(t);
    setDisplayName(t.displayName);
    setConfig({});
    setStep("config");
  };

  const handleSubmit = async () => {
    if (!selectedType) return;
    setSubmitting(true);
    try {
      await onComplete(selectedType.type, displayName, config);
    } finally {
      setSubmitting(false);
    }
  };

  if (step === "type") {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Add Channel Connection</CardTitle>
          <CardDescription>Select a platform to connect.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {channelTypes.map((t) => (
              <button
                key={t.type}
                type="button"
                onClick={() => handleSelectType(t)}
                className="flex items-start gap-3 rounded-xl border border-border bg-card p-4 text-left transition-colors hover:border-primary/40 hover:bg-accent"
              >
                <div className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-muted text-sm font-bold text-muted-foreground">
                  {channelEmoji[t.type] ?? "?"}
                </div>
                <div className="min-w-0">
                  <div className="text-sm font-medium">{t.displayName}</div>
                  <div className="mt-0.5 text-xs text-muted-foreground">
                    {t.description}
                  </div>
                </div>
              </button>
            ))}
          </div>
          <div className="mt-4">
            <Button variant="outline" onClick={onCancel}>
              Cancel
            </Button>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Configure {selectedType?.displayName}</CardTitle>
        <CardDescription>
          Enter the credentials for your {selectedType?.displayName} integration.
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="display-name">Display Name</Label>
          <Input
            id="display-name"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            placeholder="e.g. Production Slack"
          />
        </div>

        {selectedType?.configFields.map((field) => (
          <div key={field.key} className="flex flex-col gap-1.5">
            <Label htmlFor={field.key}>
              {field.label}
              {field.required && <span className="ml-1 text-destructive">*</span>}
            </Label>
            {field.kind === "textarea" ? (
              <textarea
                id={field.key}
                className="flex min-h-[100px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                value={config[field.key] ?? ""}
                onChange={(e) =>
                  setConfig((prev) => ({ ...prev, [field.key]: e.target.value }))
                }
                placeholder={field.placeholder ?? undefined}
              />
            ) : (
              <Input
                id={field.key}
                type={field.kind === "password" ? "password" : "text"}
                value={config[field.key] ?? ""}
                onChange={(e) =>
                  setConfig((prev) => ({ ...prev, [field.key]: e.target.value }))
                }
                placeholder={field.placeholder ?? undefined}
              />
            )}
            {field.help && (
              <p className="text-[11px] text-muted-foreground">{field.help}</p>
            )}
          </div>
        ))}

        <div className="flex items-center gap-3 pt-2">
          <Button onClick={handleSubmit} disabled={submitting || !displayName.trim()}>
            {submitting ? "Connecting..." : "Connect"}
          </Button>
          <Button
            variant="outline"
            onClick={() => {
              setStep("type");
              setSelectedType(null);
            }}
          >
            Back
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

function ConnectionCard({
  connection,
  onToggle,
  onDelete,
}: {
  connection: ChannelConnection;
  onToggle: () => void;
  onDelete: () => void;
}) {
  const [deleting, setDeleting] = useState(false);

  return (
    <div className="flex items-center gap-4 rounded-xl border border-border bg-card p-4">
      <div className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-muted text-sm font-bold text-muted-foreground">
        {channelEmoji[connection.channelType] ?? "?"}
      </div>
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium">{connection.displayName}</span>
          <span className="rounded border border-border px-1.5 py-0.5 text-[10px] text-muted-foreground">
            {connection.channelType}
          </span>
        </div>
        <div className="mt-0.5 flex items-center gap-2 text-xs text-muted-foreground">
          <span
            className={`h-1.5 w-1.5 rounded-full ${connection.enabled ? "bg-emerald-500" : "bg-muted-foreground/40"}`}
          />
          {connection.enabled ? "Active" : "Disabled"}
          {connection.configured && " \u00B7 Configured"}
        </div>
      </div>
      <div className="flex items-center gap-1">
        <button
          type="button"
          onClick={onToggle}
          title={connection.enabled ? "Disable" : "Enable"}
          className="rounded-md p-2 text-muted-foreground hover:bg-accent hover:text-foreground"
        >
          {connection.enabled ? (
            <Power className="h-4 w-4" />
          ) : (
            <PowerOff className="h-4 w-4" />
          )}
        </button>
        <button
          type="button"
          onClick={async () => {
            setDeleting(true);
            await onDelete();
          }}
          disabled={deleting}
          className="rounded-md p-2 text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
        >
          <Trash2 className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}

export default function ChannelsSettingsPage() {
  const { connections, channelTypes, loading, error, refresh, create, update, remove } =
    useChannelConnections();
  const [showWizard, setShowWizard] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const handleCreate = async (
    type: string,
    name: string,
    config: Record<string, string>,
  ) => {
    setActionError(null);
    try {
      await create(type, name, config);
      setShowWizard(false);
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Failed to create connection");
    }
  };

  return (
    <div>
      <TopBar
        title="Channels"
        subtitle="Manage org-wide channel connections. Agents can be bound to these channels individually."
      />
      <div className="max-w-3xl p-6">
        {(error || actionError) && (
          <div className="mb-4 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-2 text-sm text-destructive">
            {error || actionError}
          </div>
        )}

        {showWizard ? (
          <ChannelSetupWizard
            channelTypes={channelTypes}
            onComplete={handleCreate}
            onCancel={() => setShowWizard(false)}
          />
        ) : (
          <>
            <div className="mb-4 flex items-center justify-between">
              <div className="text-sm text-muted-foreground">
                {connections.length} connection{connections.length === 1 ? "" : "s"}
              </div>
              <div className="flex items-center gap-2">
                <Button variant="outline" size="sm" onClick={refresh}>
                  <RefreshCw className="mr-1.5 h-3 w-3" />
                  Refresh
                </Button>
                <Button size="sm" onClick={() => setShowWizard(true)}>
                  <Plus className="mr-1.5 h-3 w-3" />
                  Add Channel
                </Button>
              </div>
            </div>

            {loading ? (
              <div className="flex flex-col gap-3">
                {Array.from({ length: 3 }).map((_, i) => (
                  <div key={i} className="flex items-center gap-4 rounded-xl border border-border bg-card p-4">
                    <Skeleton className="h-10 w-10 rounded-lg shrink-0" />
                    <div className="flex-1 space-y-2">
                      <div className="flex items-center gap-2">
                        <Skeleton className="h-4 w-32" />
                        <Skeleton className="h-4 w-12 rounded" />
                      </div>
                      <Skeleton className="h-3 w-24" />
                    </div>
                    <div className="flex items-center gap-1">
                      <Skeleton className="h-8 w-8 rounded-md" />
                      <Skeleton className="h-8 w-8 rounded-md" />
                    </div>
                  </div>
                ))}
              </div>
            ) : connections.length === 0 ? (
              <div className="rounded-xl border border-dashed border-border bg-card px-6 py-12 text-center">
                <div className="text-sm text-muted-foreground">
                  No channel connections yet.
                </div>
                <div className="mt-1 text-xs text-muted-foreground">
                  Add a Slack, Telegram, Discord, or other channel to get started.
                </div>
                <Button
                  className="mt-4"
                  size="sm"
                  onClick={() => setShowWizard(true)}
                >
                  <Plus className="mr-1.5 h-3 w-3" />
                  Add Channel
                </Button>
              </div>
            ) : (
              <div className="flex flex-col gap-3">
                {connections.map((conn) => (
                  <ConnectionCard
                    key={conn.id}
                    connection={conn}
                    onToggle={() => update(conn.id, { enabled: !conn.enabled })}
                    onDelete={() => remove(conn.id)}
                  />
                ))}
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
