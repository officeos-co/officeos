"use client";

import { useState } from "react";
import { TopBar } from "@/components/TopBar";
import { StatusBadge } from "@/components/StatusBadge";
import { ProviderConfigureOverlay } from "@/components/ProviderConfigureOverlay";
import { useProviders, type Provider } from "@/hooks/useProviders";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

/** Providers whose keys are managed by the platform — no BYOK allowed. */
const PLATFORM_KEY_PROVIDERS = new Set(["anthropic", "google", "xai"]);

export default function ProvidersPage() {
  const { providers, loading, error } = useProviders();
  const [selected, setSelected] = useState<Provider | null>(null);

  return (
    <div>
      <TopBar
        title="Providers"
        subtitle="LLM providers — Anthropic, Gemini, and xAI use platform keys. OpenAI supports custom BYOK keys."
      />

      {error && (
        <div className="mx-8 mt-6 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {loading ? (
        <div className="px-8 py-12 text-sm text-muted-foreground">Loading...</div>
      ) : (
        <div className="mx-8 my-6 rounded-lg border border-border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Provider</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Key source</TableHead>
                <TableHead className="text-right" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {providers.map((p) => {
                const isPlatformManaged = PLATFORM_KEY_PROVIDERS.has(p.name);
                return (
                  <TableRow key={p.id}>
                    <TableCell className="font-medium">{p.displayName}</TableCell>
                    <TableCell>
                      <StatusBadge status={p.configured ? "ready" : "not installed"} />
                    </TableCell>
                    <TableCell className="text-xs text-muted-foreground">
                      {isPlatformManaged ? "Platform key" : "Your key (BYOK)"}
                    </TableCell>
                    <TableCell className="text-right">
                      {!isPlatformManaged && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setSelected(p)}
                        >
                          {p.configured ? "Update" : "Configure"}
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </div>
      )}

      <ProviderConfigureOverlay provider={selected} onClose={() => setSelected(null)} />
    </div>
  );
}
