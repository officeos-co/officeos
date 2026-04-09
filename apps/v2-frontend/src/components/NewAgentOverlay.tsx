"use client";

import { useEffect, useState } from "react";
import { Modal } from "./Modal";
import { useAgents } from "@/hooks/useAgents";
import { useProviders } from "@/hooks/useProviders";
import { useProviderModels } from "@/hooks/useProviderModels";

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
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-[var(--eaos-text-muted)]">Name</span>
          <input
            autoFocus
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="rounded-md border border-[var(--eaos-border)] bg-black/40 px-3 py-2 outline-none focus:border-white"
            placeholder="my-agent"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm">
          <span className="text-[var(--eaos-text-muted)]">Provider</span>
          <select
            value={provider}
            onChange={(e) => setProvider(e.target.value)}
            className="rounded-md border border-[var(--eaos-border)] bg-black/40 px-3 py-2 outline-none focus:border-white"
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
            <span className="text-xs text-yellow-400">
              No providers configured yet. Set one up on the Providers page first.
            </span>
          )}
        </label>

        <label className="flex flex-col gap-1 text-sm">
          <span className="text-[var(--eaos-text-muted)]">Model (optional)</span>
          <select
            value={model}
            onChange={(e) => setModel(e.target.value)}
            disabled={!provider || modelsLoading || (!!modelsError) || models.length === 0}
            className="rounded-md border border-[var(--eaos-border)] bg-black/40 px-3 py-2 outline-none focus:border-white disabled:opacity-50"
          >
            <option value="">(provider default)</option>
            {models.map((m) => (
              <option key={m} value={m}>
                {m}
              </option>
            ))}
          </select>
          {modelsLoading && (
            <span className="text-xs text-[var(--eaos-text-muted)]">Loading models…</span>
          )}
          {modelsError && (
            <span className="text-xs text-red-400">{modelsError}</span>
          )}
          {!modelsLoading && !modelsError && provider && models.length === 0 && (
            <span className="text-xs text-yellow-400">
              No models configured for this provider.
            </span>
          )}
        </label>

        {error && <div className="text-sm text-red-400">{error}</div>}

        <div className="mt-2 flex justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded-md border border-[var(--eaos-border)] px-4 py-2 text-sm hover:bg-black/40"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={submitting || configured.length === 0}
            className="rounded-md bg-white px-4 py-2 text-sm font-medium text-black disabled:opacity-50"
          >
            {submitting ? "Creating..." : "Create"}
          </button>
        </div>
      </form>
    </Modal>
  );
}
