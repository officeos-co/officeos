"use client";

import { useState } from "react";
import { Plus, Trash2, Power, PowerOff, RefreshCw } from "lucide-react";
import { TopBar } from "@/components/shared/TopBar";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { EmptyState } from "@/components/shared/EmptyState";
import { ChannelIcon } from "@/components/shared/ChannelIcon";
import {
  useChannelConnections,
  type ChannelType,
} from "@/hooks/useChannelConnections";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

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
      <div className="px-6 py-6">
        <div className="mb-4">
          <h2 className="text-sm font-medium">Select a platform</h2>
          <p className="text-[12px] text-muted-foreground mt-0.5">Choose which messaging platform to connect.</p>
        </div>

        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent">
              <TableHead className="text-[12px] font-normal text-muted-foreground w-[32px]" />
              <TableHead className="text-[12px] font-normal text-muted-foreground">Platform</TableHead>
              <TableHead className="text-[12px] font-normal text-muted-foreground">Description</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {channelTypes.map((t) => (
              <TableRow
                key={t.type}
                onClick={() => handleSelectType(t)}
                className="cursor-pointer"
              >
                <TableCell>
                  <ChannelIcon type={t.type} size={18} />
                </TableCell>
                <TableCell className="text-[13px] font-medium">{t.displayName}</TableCell>
                <TableCell className="text-[13px] text-muted-foreground">{t.description}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>

        <div className="mt-4">
          <Button variant="ghost" size="sm" onClick={onCancel} className="h-7 text-[12px]">
            Cancel
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="px-6 py-6">
      <div className="mb-6 flex items-center gap-3">
        <ChannelIcon type={selectedType!.type} size={20} />
        <div>
          <h2 className="text-sm font-medium">Configure {selectedType?.displayName}</h2>
          <p className="text-[12px] text-muted-foreground">Enter the credentials for your integration.</p>
        </div>
      </div>

      <div className="max-w-md space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="display-name" className="text-[12px]">Display name</Label>
          <Input
            id="display-name"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            className="h-8 text-[13px]"
          />
        </div>

        {selectedType?.configFields.map((field) => (
          <div key={field.key} className="space-y-1.5">
            <Label htmlFor={field.key} className="text-[12px]">
              {field.label}
              {field.required && <span className="ml-0.5 text-destructive">*</span>}
            </Label>
            {field.kind === "textarea" ? (
              <Textarea
                id={field.key}
                value={config[field.key] ?? ""}
                onChange={(e) => setConfig((prev) => ({ ...prev, [field.key]: e.target.value }))}
                placeholder={field.placeholder ?? undefined}
                className="text-[13px]"
              />
            ) : (
              <Input
                id={field.key}
                type={field.kind === "password" ? "password" : "text"}
                value={config[field.key] ?? ""}
                onChange={(e) => setConfig((prev) => ({ ...prev, [field.key]: e.target.value }))}
                placeholder={field.placeholder ?? undefined}
                className="h-8 text-[13px]"
              />
            )}
            {field.help && (
              <p className="text-[11px] text-muted-foreground">{field.help}</p>
            )}
          </div>
        ))}

        <div className="flex items-center gap-2 pt-2">
          <Button size="sm" onClick={handleSubmit} disabled={submitting || !displayName.trim()} className="h-7">
            {submitting ? "Connecting..." : "Connect"}
          </Button>
          <Button variant="ghost" size="sm" onClick={() => { setStep("type"); setSelectedType(null); }} className="h-7 text-[12px]">
            Back
          </Button>
        </div>
      </div>
    </div>
  );
}

export default function ChannelsSettingsPage() {
  const { connections, channelTypes, loading, error, refresh, create, update, remove } =
    useChannelConnections();
  const [showWizard, setShowWizard] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const handleCreate = async (type: string, name: string, config: Record<string, string>) => {
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
        action={
          !showWizard ? (
            <div className="flex items-center gap-2">
              <Button variant="ghost" size="sm" onClick={refresh} className="h-7 w-7 p-0">
                <RefreshCw className="h-3.5 w-3.5" />
              </Button>
              <Button size="sm" onClick={() => setShowWizard(true)} className="h-7">
                <Plus className="mr-1 h-3 w-3" />
                Add channel
              </Button>
            </div>
          ) : undefined
        }
      />

      {(error || actionError) && (
        <div className="mx-6 mt-4 rounded-md border border-destructive/20 bg-destructive/5 px-4 py-2.5 text-[13px] text-destructive">
          {error || actionError}
        </div>
      )}

      {showWizard ? (
        <ChannelSetupWizard
          channelTypes={channelTypes}
          onComplete={handleCreate}
          onCancel={() => setShowWizard(false)}
        />
      ) : loading ? (
        <div className="px-6 py-6 space-y-2">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="flex items-center gap-4 py-3">
              <Skeleton className="h-4 w-4 rounded" />
              <Skeleton className="h-4 w-28" />
              <Skeleton className="h-3 w-16" />
              <Skeleton className="h-3 w-12" />
            </div>
          ))}
        </div>
      ) : connections.length === 0 ? (
        <EmptyState
          title="No channels yet"
          description="Connect Slack, Telegram, Discord, or other messaging platforms."
          action={
            <Button size="sm" onClick={() => setShowWizard(true)} className="h-7">
              Add channel
            </Button>
          }
        />
      ) : (
        <div className="px-6 py-4">
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                <TableHead className="text-[12px] font-normal text-muted-foreground w-[32px]" />
                <TableHead className="text-[12px] font-normal text-muted-foreground">Name</TableHead>
                <TableHead className="text-[12px] font-normal text-muted-foreground">Platform</TableHead>
                <TableHead className="text-[12px] font-normal text-muted-foreground">Status</TableHead>
                <TableHead className="w-[80px]" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {connections.map((conn) => (
                <TableRow key={conn.id}>
                  <TableCell>
                    <ChannelIcon type={conn.channelType} size={16} />
                  </TableCell>
                  <TableCell className="text-[13px] font-medium">{conn.displayName}</TableCell>
                  <TableCell className="text-[13px] text-muted-foreground capitalize">{conn.channelType}</TableCell>
                  <TableCell>
                    <StatusBadge status={conn.enabled ? "online" : "offline"} />
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => update(conn.id, { enabled: !conn.enabled })}
                        className="h-7 w-7 p-0"
                        title={conn.enabled ? "Disable" : "Enable"}
                      >
                        {conn.enabled ? <Power className="h-3.5 w-3.5" /> : <PowerOff className="h-3.5 w-3.5" />}
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => remove(conn.id)}
                        className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}
