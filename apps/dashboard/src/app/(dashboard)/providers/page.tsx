"use client";

import Image from "next/image";
import { PageHeader } from "@/components/page-header";
import { Skeleton } from "@/components/ui/skeleton";
import { Separator } from "@/components/ui/separator";
import { useProviders, type Provider } from "@/features/manage";

/** Logo map — provider name (lowercase) → public asset path */
const LOGOS: Record<string, string> = {
  openai: "/openai.svg",
  anthropic: "/anthropic.svg",
  google: "/google.svg",
  xai: "/xai.svg",
};

function ProviderLogo({ name, size = 24 }: { name: string; size?: number }) {
  const src = LOGOS[name.toLowerCase()];
  if (!src) return null;
  return (
    <Image
      src={src}
      alt={name}
      width={size}
      height={size}
      className="shrink-0 invert dark:invert-0"
    />
  );
}

export default function ProvidersPage() {
  const { providers, loading } = useProviders();

  const configured = providers.filter((p) => p.configured);
  const unconfigured = providers.filter((p) => !p.configured);

  if (loading) {
    return (
      <>
        <PageHeader group="Manage" page="Providers" />
        <div className="flex flex-1 flex-col gap-6 p-4 pt-0 max-w-3xl mx-auto w-full">
          <Skeleton className="h-4 w-40 mb-3" />
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-16 w-full rounded-lg" />
          ))}
        </div>
      </>
    );
  }

  return (
    <>
      <PageHeader group="Manage" page="Providers" />
      <div className="flex flex-1 flex-col gap-6 p-4 pt-0 max-w-3xl mx-auto w-full">
        {/* Configured providers */}
        {configured.length > 0 && (
          <section>
            <h3 className="text-sm font-semibold mb-3">Configured Providers</h3>
            <div className="space-y-2">
              {configured.map((p) => (
                <div
                  key={p.id}
                  className="flex items-center gap-3 rounded-lg border bg-card p-4"
                >
                  <ProviderLogo name={p.name} />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="font-medium text-sm">
                        {p.displayName}
                      </span>
                      <span className="rounded bg-primary/10 px-1.5 py-0.5 text-xs font-medium text-primary">
                        Connected
                      </span>
                    </div>
                    <span className="text-xs text-muted-foreground">
                      {p.models.length} model
                      {p.models.length !== 1 ? "s" : ""} available
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </section>
        )}

        {configured.length > 0 && unconfigured.length > 0 && <Separator />}

        {/* Unconfigured providers */}
        {unconfigured.length > 0 && (
          <section>
            <h3 className="text-sm font-semibold mb-1">Not Configured</h3>
            <p className="text-xs text-muted-foreground mb-3">
              Set the corresponding environment variable to enable these
              providers.
            </p>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
              {unconfigured.map((p) => (
                <div
                  key={p.id}
                  className="flex items-center gap-3 rounded-lg border bg-card p-4 opacity-60"
                >
                  <ProviderLogo name={p.name} />
                  <div className="flex-1 min-w-0">
                    <span className="font-medium text-sm">{p.displayName}</span>
                    <br />
                    <span className="text-xs text-muted-foreground">
                      {p.models.length} model
                      {p.models.length !== 1 ? "s" : ""}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </section>
        )}
      </div>
    </>
  );
}
