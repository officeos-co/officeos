"use client";

// Settings → Providers: one card per supported LLM provider with
// a password input + save button. Mirrors the Skills page styling.
// Hits /api/providers without going through the orval client so
// it works the moment the backend ships.

import { useCallback, useEffect, useState } from "react";

import { getSessionToken } from "@/auth/clerk";
import { getApiBaseUrl } from "@/lib/api-base";

export const dynamic = "force-dynamic";

type Provider = {
  name: string;
  title: string;
  description: string;
  placeholder: string;
  requires_key: boolean;
  configured: boolean;
};

async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getSessionToken();
  const res = await fetch(`${getApiBaseUrl()}${path}`, {
    ...init,
    headers: {
      ...(init?.headers || {}),
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`${res.status} ${res.statusText}: ${text}`);
  }
  return (await res.json()) as T;
}

export default function ProvidersPage() {
  const [providers, setProviders] = useState<Provider[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await api<{ providers: Provider[] }>("/api/providers");
      setProviders(data.providers);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  return (
    <main className="mx-auto max-w-4xl px-6 py-10">
      <header className="mb-8">
        <h1 className="text-2xl font-semibold">LLM Providers</h1>
        <p className="mt-1 text-sm text-muted">
          Configure one API key per provider. Each key is stored on your
          organization and reused by every agent you launch that uses the
          matching provider. Enter a new value to rotate a key.
        </p>
      </header>

      {error && (
        <div className="mb-4 rounded-md border border-red-500/40 bg-red-500/10 px-4 py-3 text-sm text-red-400">
          {error}
        </div>
      )}

      {loading && <p className="text-sm text-muted">Loading…</p>}

      <div className="grid gap-4">
        {providers.map((p) => (
          <ProviderCard
            key={p.name}
            provider={p}
            onSaved={reload}
            onError={setError}
          />
        ))}
      </div>
    </main>
  );
}

function ProviderCard({
  provider,
  onSaved,
  onError,
}: {
  provider: Provider;
  onSaved: () => void | Promise<void>;
  onError: (m: string) => void;
}) {
  const [value, setValue] = useState("");
  const [saving, setSaving] = useState(false);

  const save = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      await api(`/api/providers/${provider.name}`, {
        method: "PUT",
        body: JSON.stringify({ api_key: value }),
      });
      setValue("");
      await onSaved();
    } catch (err) {
      onError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  const remove = async () => {
    setSaving(true);
    try {
      await api(`/api/providers/${provider.name}`, { method: "DELETE" });
      setValue("");
      await onSaved();
    } catch (err) {
      onError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <article className="rounded-2xl border border-[color:var(--border)] bg-[color:var(--surface)] p-5">
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <h2 className="text-lg font-medium">{provider.title}</h2>
            <span
              className={`rounded px-2 py-0.5 text-xs ${
                provider.configured
                  ? "bg-emerald-500/15 text-emerald-400"
                  : provider.requires_key
                    ? "bg-amber-500/15 text-amber-400"
                    : "bg-neutral-500/15 text-neutral-400"
              }`}
            >
              {provider.configured
                ? "Configured"
                : provider.requires_key
                  ? "Not configured"
                  : "No key required"}
            </span>
          </div>
          <p className="mt-1 text-sm text-muted">{provider.description}</p>
        </div>
      </div>

      {provider.requires_key ? (
        <form onSubmit={save} className="mt-4 flex flex-col gap-3 sm:flex-row">
          <input
            type="password"
            className="flex-1 rounded-md border border-[color:var(--border)] bg-transparent px-3 py-2 text-sm"
            placeholder={
              provider.configured
                ? "Enter a new key to rotate"
                : provider.placeholder
            }
            value={value}
            onChange={(e) => setValue(e.target.value)}
            disabled={saving}
          />
          <div className="flex gap-2">
            <button
              type="submit"
              disabled={saving || !value.trim()}
              className="rounded-md border border-[color:var(--border)] bg-white/5 px-4 py-2 text-sm disabled:opacity-50"
            >
              {saving ? "Saving…" : "Save"}
            </button>
            {provider.configured && (
              <button
                type="button"
                disabled={saving}
                onClick={remove}
                className="rounded-md border border-red-500/40 bg-red-500/10 px-3 py-2 text-sm text-red-400 disabled:opacity-50"
              >
                Remove
              </button>
            )}
          </div>
        </form>
      ) : (
        <p className="mt-4 text-xs text-muted">
          This provider doesn&apos;t use an API key. Select it in the agent
          creation form to use it.
        </p>
      )}
    </article>
  );
}
