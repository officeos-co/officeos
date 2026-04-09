"use client";

import { useState } from "react";
import { TopBar } from "@/components/TopBar";
import { ProviderConfigureOverlay } from "@/components/ProviderConfigureOverlay";
import { useProviders, type Provider } from "@/hooks/useProviders";

export default function ProvidersPage() {
  const { providers, loading, error } = useProviders();
  const [selected, setSelected] = useState<Provider | null>(null);

  return (
    <div>
      <TopBar
        title="Providers"
        subtitle="Paste your LLM provider API keys once — every agent inherits them."
      />

      {error && (
        <div className="mx-8 mt-6 rounded-md border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          {error}
        </div>
      )}

      {loading ? (
        <div className="px-8 py-12 text-sm text-[var(--eaos-text-muted)]">Loading...</div>
      ) : (
        <div className="mx-8 my-6 overflow-hidden rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)]">
          <table className="w-full text-sm">
            <thead className="text-left text-[var(--eaos-text-muted)]">
              <tr className="border-b border-[var(--eaos-border)]">
                <th className="px-4 py-3 font-normal">Provider</th>
                <th className="px-4 py-3 font-normal">Status</th>
                <th className="px-4 py-3 font-normal"></th>
              </tr>
            </thead>
            <tbody>
              {providers.map((p) => (
                <tr
                  key={p.id}
                  className="border-b border-[var(--eaos-border)] last:border-b-0"
                >
                  <td className="px-4 py-3">{p.displayName}</td>
                  <td className="px-4 py-3 text-[var(--eaos-text-muted)]">
                    {p.configured ? "Configured" : "Not configured"}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => setSelected(p)}
                      className="rounded-md border border-[var(--eaos-border)] px-3 py-1 text-xs hover:bg-white hover:text-black"
                    >
                      {p.configured ? "Update" : "Configure"}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <ProviderConfigureOverlay provider={selected} onClose={() => setSelected(null)} />
    </div>
  );
}
