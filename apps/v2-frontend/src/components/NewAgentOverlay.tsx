"use client";

import { useEffect, useState } from "react";
import { Modal } from "./Modal";
import { useAgents } from "@/hooks/useAgents";
import { useProviders } from "@/hooks/useProviders";
import { useProviderModels } from "@/hooks/useProviderModels";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

type NewAgentOverlayProps = {
  open: boolean;
  onClose: () => void;
};

export function NewAgentOverlay({ open, onClose }: NewAgentOverlayProps) {
  const { create } = useAgents();
  const { providers } = useProviders();
  const configured = providers.filter((p) => p.configured);

  const [name, setName] = useState("");
  const [provider, setProvider] = useState("");
  const [model, setModel] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const { models, loading: modelsLoading, error: modelsError } = useProviderModels(
    provider || null,
  );

  useEffect(() => {
    if (open && !provider && configured.length > 0) {
      setProvider(configured[0].name);
    }
  }, [open, provider, configured]);

  useEffect(() => {
    setModel("");
  }, [provider]);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!name.trim()) {
      setError("Name is required");
      return;
    }
    if (!provider) {
      setError("Select a configured provider");
      return;
    }
    setSubmitting(true);
    try {
      await create({ name: name.trim(), provider, model: model.trim() || undefined });
      setName("");
      setModel("");
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create agent");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal open={open} title="New agent" onClose={onClose}>
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        <div className="space-y-2">
          <Label htmlFor="agent-name">Name</Label>
          <Input
            id="agent-name"
            autoFocus
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="my-agent"
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="agent-provider">Provider</Label>
          <select
            id="agent-provider"
            value={provider}
            onChange={(e) => setProvider(e.target.value)}
            className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
          >
            <option value="" disabled>
              Select a configured provider
            </option>
            {configured.map((p) => (
              <option key={p.id} value={p.name}>
                {p.displayName}
              </option>
            ))}
          </select>
          {configured.length === 0 && (
            <p className="text-xs text-amber-400">
              No providers configured yet. Set one up on the Providers page first.
            </p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="agent-model">Model (optional)</Label>
          <select
            id="agent-model"
            value={model}
            onChange={(e) => setModel(e.target.value)}
            disabled={!provider || modelsLoading || (!!modelsError) || models.length === 0}
            className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
          >
            <option value="">(provider default)</option>
            {models.map((m) => (
              <option key={m} value={m}>
                {m}
              </option>
            ))}
          </select>
          {modelsLoading && (
            <p className="text-xs text-muted-foreground">Loading models...</p>
          )}
          {modelsError && (
            <p className="text-xs text-destructive">{modelsError}</p>
          )}
        </div>

        {error && <p className="text-sm text-destructive">{error}</p>}

        <div className="flex justify-end gap-2">
          <Button type="button" variant="outline" size="sm" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" size="sm" disabled={submitting || configured.length === 0}>
            {submitting ? "Creating..." : "Create"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
