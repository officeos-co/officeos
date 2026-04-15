"use client";

import { useState } from "react";
import { TopBar } from "@/components/shared/TopBar";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { ProviderConfigureOverlay } from "@/components/providers/ProviderConfigureOverlay";
import { useProviders, type Provider } from "@/hooks/useProviders";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

const PLATFORM_KEY_PROVIDERS = new Set(["anthropic", "google", "xai"]);

export default function ProvidersPage() {
  const { providers, loading, error } = useProviders();
  const [selected, setSelected] = useState<Provider | null>(null);

  return (
    <div>
      <TopBar title="Providers" />

      {error && (
        <div className="mx-6 mt-4 rounded-md border border-destructive/20 bg-destructive/5 px-4 py-2.5 text-[13px] text-destructive">
          {error}
        </div>
      )}

      <div className="px-6 py-4">
        {loading ? (
          <div className="space-y-2">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="flex items-center gap-4 py-3">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-3 w-16" />
                <Skeleton className="h-3 w-28" />
              </div>
            ))}
          </div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                <TableHead className="text-[12px] font-normal text-muted-foreground">Provider</TableHead>
                <TableHead className="text-[12px] font-normal text-muted-foreground">Status</TableHead>
                <TableHead className="text-[12px] font-normal text-muted-foreground">Key source</TableHead>
                <TableHead className="text-right w-[100px]" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {providers.map((p) => {
                const isPlatformManaged = PLATFORM_KEY_PROVIDERS.has(p.name);
                return (
                  <TableRow key={p.id}>
                    <TableCell className="text-[13px] font-medium">{p.displayName}</TableCell>
                    <TableCell>
                      <StatusBadge status={p.configured ? "ready" : "not installed"} />
                    </TableCell>
                    <TableCell className="text-[13px] text-muted-foreground">
                      {isPlatformManaged ? "Platform" : "BYOK"}
                    </TableCell>
                    <TableCell className="text-right">
                      {!isPlatformManaged && (
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setSelected(p)}
                          className="h-7 text-[12px]"
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
        )}
      </div>

      <ProviderConfigureOverlay provider={selected} onClose={() => setSelected(null)} />
    </div>
  );
}
