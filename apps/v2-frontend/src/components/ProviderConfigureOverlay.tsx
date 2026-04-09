"use client";

import { useEffect, useState } from "react";
import { Modal } from "./Modal";
import { useProviders, type Provider } from "@/hooks/useProviders";

type Props = {
  provider: Provider | null;
  onClose: () => void;
};

export function ProviderConfigureOverlay({ provider, onClose }: Props) {
  const { configure, clear } = useProviders();
  const [apiKey, setApiKey] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    setApiKey("");
    setError(null);
  }, [provider?.id]);

  if (!provider) {
    return null;
  }

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!apiKey.trim()) {
      setError("API key is required");
      return;
    }
    setSubmitting(true);
    try {
      await configure(provider.name, apiKey.trim());
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save key");
    } finally {
      setSubmitting(false);
    }
  };

  const onClear = async () => {
    setSubmitting(true);
    setError(null);
    try {
      await clear(provider.name);
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to clear key");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal open={true} title={`Configure ${provider.displayName}`} onClose={onClose}>
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-[var(--eaos-text-muted)]">API key</span>
          <input
            autoFocus
            type="password"
            value={apiKey}
            onChange={(e) => setApiKey(e.target.value)}
            className="rounded-md border border-[var(--eaos-border)] bg-black/40 px-3 py-2 font-mono outline-none focus:border-white"
            placeholder={provider.configured ? "•••••••• (stored)" : "sk-..."}
          />
          {provider.configured && (
            <span className="text-xs text-[var(--eaos-text-muted)]">
              Key is already stored. Enter a new one to replace it.
            </span>
          )}
        </label>

        {error && <div className="text-sm text-red-400">{error}</div>}

        <div className="mt-2 flex justify-between gap-2">
          <div>
            {provider.configured && (
              <button
                type="button"
                onClick={onClear}
                disabled={submitting}
                className="rounded-md border border-red-500/30 px-4 py-2 text-sm text-red-300 hover:bg-red-500/10 disabled:opacity-50"
              >
                Clear key
              </button>
            )}
          </div>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded-md border border-[var(--eaos-border)] px-4 py-2 text-sm hover:bg-black/40"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="rounded-md bg-white px-4 py-2 text-sm font-medium text-black disabled:opacity-50"
            >
              {submitting ? "Saving..." : "Save"}
            </button>
          </div>
        </div>
      </form>
    </Modal>
  );
}
