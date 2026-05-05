"use client";

import Image from "next/image";
import { PageHeader } from "@/components/page-header";
import { PageContainer } from "@/components/page-container";
import { HelpTooltip, WithTooltip } from "@/components/ui/help-tooltip";
import { Skeleton } from "@/components/ui/skeleton";
import { Separator } from "@/components/ui/separator";
import { useProviders } from "@/features/manage";
import { getProviderTooltip } from "@/features/agents/model-tooltips";

/** Logo map — provider name (lowercase) → public asset path */
const LOGOS: Record<string, string> = {
  openai: "/openai.svg",
  anthropic: "/anthropic.svg",
  google: "/google.svg",
  xai: "/xai.svg",
};

function ProviderLogo({ name, size = 24 }: { name: string; size?: number }) {
  const src = LOGOS[name.toLowerCase()];
  if (!src) {
    return (
      <div
        className="flex shrink-0 items-center justify-center rounded border bg-muted text-xs font-semibold text-muted-foreground"
        style={{ width: size, height: size }}
      >
        AI
      </div>
    );
  }
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
        <PageHeader
          page="Providers"
          subtitle="Review model providers available to this deployment."
          width="narrow"
        />
        <PageContainer width="narrow" className="flex flex-1 flex-col gap-6 pb-4">
          <Skeleton className="h-4 w-40 mb-3" />
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-16 w-full rounded-lg" />
          ))}
        </PageContainer>
      </>
    );
  }

  return (
    <>
      <PageHeader
        page="Providers"
        subtitle="Review model providers available to this deployment."
        width="narrow"
      />
      <PageContainer width="narrow" className="flex flex-1 flex-col gap-6 pb-4">
        {/* Configured providers */}
        {configured.length > 0 && (
          <section>
            <h3 className="mb-3 flex items-center gap-2 text-sm font-semibold">
              Configured Providers
              <HelpTooltip>
                Connected providers expose their concrete model list to agents.
                Auto only appears when Anthropic is configured.
              </HelpTooltip>
            </h3>
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
                      <WithTooltip tooltip={getProviderTooltip(p.name, true)}>
                        <span className="rounded bg-primary/10 px-1.5 py-0.5 text-xs font-medium text-primary">
                          Connected
                        </span>
                      </WithTooltip>
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
            <h3 className="mb-1 flex items-center gap-2 text-sm font-semibold">
              Not Configured
              <HelpTooltip>
                These providers stay hidden from model pickers until their API
                key is configured on the backend.
              </HelpTooltip>
            </h3>
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
                    <WithTooltip tooltip={getProviderTooltip(p.name, false)}>
                      <span className="text-xs text-muted-foreground">
                        {p.models.length} model
                        {p.models.length !== 1 ? "s" : ""}
                      </span>
                    </WithTooltip>
                  </div>
                </div>
              ))}
            </div>
          </section>
        )}
      </PageContainer>
    </>
  );
}
