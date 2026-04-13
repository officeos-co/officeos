"use client";

import { useEffect, useState } from "react";
import { Modal } from "@/components/shared/Modal";
import { useProviders } from "@/hooks/useProviders";
import type { Provider } from "@/types/provider";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

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
        <div className="space-y-2">
          <Label htmlFor="api-key">API key</Label>
          <Input
            id="api-key"
            autoFocus
            type="password"
            value={apiKey}
            onChange={(e) => setApiKey(e.target.value)}
            placeholder={provider.configured ? "\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022 (stored)" : "sk-..."}
            className="font-mono"
          />
          {provider.configured && (
            <p className="text-xs text-muted-foreground">
              Key is already stored. Enter a new one to replace it.
            </p>
          )}
        </div>

        {error && <p className="text-sm text-destructive">{error}</p>}

        <div className="flex justify-between gap-2">
          <div>
            {provider.configured && (
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={onClear}
                disabled={submitting}
                className="text-destructive hover:text-destructive"
              >
                Clear key
              </Button>
            )}
          </div>
          <div className="flex gap-2">
            <Button type="button" variant="outline" size="sm" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={submitting}>
              {submitting ? "Saving..." : "Save"}
            </Button>
          </div>
        </div>
      </form>
    </Modal>
  );
}
