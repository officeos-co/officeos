"use client";

import { use, useState } from "react";
import Link from "next/link";
import { notFound, useRouter, useSearchParams } from "next/navigation";
import { cn } from "@/lib/utils";
import { PageContainer } from "@/shell/page-container";
import { Button } from "@/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/ui/select";
import { Skeleton } from "@/ui/skeleton";
import {
  useDisconnectIntegration,
  useDeleteIntegration,
  useIntegration,
} from "@/features/agents";
import type { McpServer } from "@/features/agents";
import { IntegrationDataExplorerTab } from "@/features/agents";
import { IntegrationToolsTab } from "@/features/agents";
import { useIntegrationConnections } from "@/features/atlas";
import {
  AlertCircleIcon,
  CheckCircle2Icon,
  DatabaseIcon,
  ExternalLinkIcon,
  KeyIcon,
  Trash2Icon,
} from "lucide-react";

const BASE_TABS = [{ key: "tools", label: "Tools" }] as const;
const INDEXABLE_TABS = [
  { key: "tools", label: "Tools" },
  { key: "data", label: "Data explorer" },
] as const;

function IntegrationSkeleton() {
  return (
    <>
      <div className="sticky top-0 z-10 bg-background">
        <PageContainer width="wide" className="border-b border-border">
          <div className="flex items-start justify-between py-4">
            <div className="space-y-2">
              <div className="flex items-center gap-2.5">
                <Skeleton className="size-10 rounded-lg" />
                <Skeleton className="h-6 w-48" />
                <Skeleton className="h-6 w-24 rounded-full" />
              </div>
              <Skeleton className="h-3 w-96" />
            </div>
            <Skeleton className="h-8 w-28 rounded-md" />
          </div>
          <div className="-mb-px flex gap-1">
            <Skeleton className="h-8 w-16" />
            <Skeleton className="h-8 w-28" />
          </div>
        </PageContainer>
      </div>
      <PageContainer width="wide" className="flex flex-1 flex-col pt-4">
        <Skeleton className="h-48 w-full rounded-lg" />
      </PageContainer>
    </>
  );
}

export default function IntegrationDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { slug } = use(params);
  const { integration, loading } = useIntegration(slug);
  const { connections } = useIntegrationConnections({ pollInterval: 5000 });
  const deleteIntegration = useDeleteIntegration();
  const disconnectIntegration = useDisconnectIntegration();
  const [runtimeMode, setRuntimeMode] = useState<"regular" | "indexed">("regular");
  const requestedTab = searchParams.get("tab");
  const oauthError = searchParams.get("oauthError");

  if (!integration) {
    if (loading) return <IntegrationSkeleton />;
    return notFound();
  }

  const currentIntegration = integration;
  const tabs = integration.isIndexable ? INDEXABLE_TABS : BASE_TABS;
  const tab = tabs.some((candidate) => candidate.key === requestedTab)
    ? requestedTab
    : "tools";
  const indexState = getIndexState(integration, connections);
  const hasCredentialFields = integration.credentialFields.length > 0;
  const hasOAuth = Boolean(integration.oauthProvider);
  const needsConnection = hasCredentialFields || hasOAuth;
  const authLabel = integration.oauthProvider ?? (hasCredentialFields ? "credentials" : "none");

  async function handleUninstall() {
    if (currentIntegration.isBuiltin) {
      await disconnectIntegration(currentIntegration.name);
    } else {
      await deleteIntegration(currentIntegration.name);
    }
    router.push("/integrations");
  }

  function dismissOAuthError() {
    const params = new URLSearchParams(searchParams);
    params.delete("oauthError");
    const query = params.toString();
    router.replace(
      query
        ? `/integrations/${currentIntegration.name}?${query}`
        : `/integrations/${currentIntegration.name}`,
    );
  }

  return (
    <div className="flex min-h-screen flex-col">
      <div className="sticky top-0 z-10 bg-background">
        <PageContainer width="wide" className="border-b border-border">
          <div className="flex items-start justify-between gap-4 py-4">
            <div className="min-w-0">
              <div className="flex items-center gap-2.5">
                <span
                  className="flex size-10 shrink-0 items-center justify-center [&>img]:size-10 [&>img]:object-contain [&>svg]:size-10"
                  dangerouslySetInnerHTML={{ __html: integration.logo }}
                />
                <h1 className="truncate text-lg font-semibold">
                  {integration.title}
                </h1>
                {needsConnection ? (
                  <StatusPill
                    configured={integration.configured}
                    label={integration.configured ? "Configured" : "Credentials required"}
                  />
                ) : null}
                {integration.isIndexable ? (
                  <span className="inline-flex items-center gap-1 rounded-full bg-sky-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest text-sky-800">
                    <DatabaseIcon className="size-3" />
                    {indexState.label}
                  </span>
                ) : null}
              </div>
              <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-muted-foreground">
                <span className="font-mono">{integration.name}</span>
                {integration.category ? (
                  <>
                    <span>·</span>
                    <span className="capitalize">{integration.category}</span>
                  </>
                ) : null}
                <span>·</span>
                <span>
                  {runtimeMode === "indexed" && integration.isIndexable
                    ? `1 indexed tool + ${integration.tools.length} direct ${
                        integration.tools.length === 1 ? "tool" : "tools"
                      }`
                    : `${integration.tools.length} ${
                        integration.tools.length === 1 ? "tool" : "tools"
                      }`}
                </span>
                <span>·</span>
                <span className="capitalize">Auth: {authLabel}</span>
                {integration.authorName ? (
                  <>
                    <span>·</span>
                    <span>{integration.authorName}</span>
                  </>
                ) : null}
              </div>
              {(integration.subtitle || integration.description) && (
                <p className="mt-2 max-w-4xl text-sm leading-6 text-foreground/70">
                  {integration.subtitle || integration.description}
                </p>
              )}
            </div>
            <div className="flex shrink-0 flex-wrap items-center justify-end gap-2">
              {integration.isIndexable ? (
                <Select
                  value={runtimeMode}
                  onValueChange={(value) =>
                    setRuntimeMode(value === "indexed" ? "indexed" : "regular")
                  }
                >
                  <SelectTrigger className="h-8 w-[190px] text-xs">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="regular">Regular mode</SelectItem>
                    <SelectItem value="indexed">Indexed data mode</SelectItem>
                  </SelectContent>
                </Select>
              ) : null}
              {integration.documentationUrl ? (
                <Button
                  variant="outline"
                  size="sm"
                  nativeButton={false}
                  render={
                    <a
                      href={integration.documentationUrl}
                      target="_blank"
                      rel="noreferrer"
                    />
                  }
                >
                  Documentation
                  <ExternalLinkIcon className="size-3.5" />
                </Button>
              ) : null}
              {integration.repositoryUrl ? (
                <Button
                  variant="outline"
                  size="sm"
                  nativeButton={false}
                  render={
                    <a
                      href={integration.repositoryUrl}
                      target="_blank"
                      rel="noreferrer"
                    />
                  }
                >
                  Source
                  <ExternalLinkIcon className="size-3.5" />
                </Button>
              ) : null}
              {hasOAuth ? (
                <Button
                  size="sm"
                  variant={integration.oauthConfigured ? "outline" : "default"}
                  nativeButton={false}
                  render={<Link href={`/integrations/${integration.name}/setup`} />}
                >
                  <KeyIcon className="size-4" />
                  {integration.oauthConfigured
                    ? "Reconfigure"
                    : "Add credential"}
                </Button>
              ) : hasCredentialFields ? (
                <Button
                  size="sm"
                  variant={integration.configured ? "outline" : "default"}
                  nativeButton={false}
                  render={<Link href={`/integrations/${integration.name}/setup`} />}
                >
                  <KeyIcon className="size-4" />
                  {integration.configured ? "Reconfigure" : "Configure"}
                </Button>
              ) : null}
              <Button
                size="sm"
                variant={integration.isBuiltin ? "destructive" : "outline"}
                onClick={handleUninstall}
              >
                <Trash2Icon className="size-4" />
                {integration.isBuiltin ? "Disconnect" : "Uninstall"}
              </Button>
            </div>
          </div>

          {tabs.length > 1 && (
            <div className="-mb-px flex">
              {tabs.map((item) => (
                <Link
                  key={item.key}
                  href={`/integrations/${integration.name}?tab=${item.key}`}
                  className={`border-b-2 px-4 py-2.5 text-sm font-medium transition-colors ${
                    tab === item.key
                      ? "border-foreground text-foreground"
                      : "border-transparent text-muted-foreground hover:text-foreground"
                  }`}
                >
                  {item.label}
                </Link>
              ))}
            </div>
          )}
        </PageContainer>
      </div>

      <PageContainer width="wide" className="flex flex-1 flex-col gap-4">
        {oauthError ? (
          <div
            role="alert"
            className="mt-4 flex items-start justify-between gap-3 rounded-lg border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive"
          >
            <span className="flex min-w-0 items-start gap-2">
              <AlertCircleIcon className="mt-0.5 size-4 shrink-0" />
              <span>{oauthError}</span>
            </span>
            <Button
              size="sm"
              variant="ghost"
              className="h-auto px-1 py-0 text-destructive hover:bg-destructive/10"
              onClick={dismissOAuthError}
            >
              Dismiss
            </Button>
          </div>
        ) : null}

        {tab === "tools" && (
          <IntegrationToolsTab
            integration={integration}
            mode={runtimeMode}
          />
        )}
        {tab === "data" && integration.isIndexable && (
          <IntegrationDataExplorerTab
            integration={integration}
            connections={connections}
            indexedModeEnabled={runtimeMode === "indexed"}
            onEnableIndexedMode={() => setRuntimeMode("indexed")}
          />
        )}
      </PageContainer>
    </div>
  );
}

function StatusPill({ configured, label }: { configured: boolean; label: string }) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest",
        configured ? "bg-emerald-100 text-emerald-800" : "bg-amber-100 text-amber-800",
      )}
    >
      {configured ? (
        <CheckCircle2Icon className="size-3" />
      ) : (
        <AlertCircleIcon className="size-3" />
      )}
      {label}
    </span>
  );
}

function getIndexState(
  integration: Pick<McpServer, "provider" | "name" | "entities">,
  connections: Array<{
    provider: string;
    status: string;
    entityStatuses: Array<{ recordCount: number }>;
  }>,
) {
  const matching = connections.filter((connection) =>
    providerMatches(connection.provider, integration.provider || integration.name),
  );
  const records = matching.reduce(
    (sum, connection) =>
      sum +
      connection.entityStatuses.reduce(
        (entitySum, entity) => entitySum + entity.recordCount,
        0,
      ),
    0,
  );
  if (matching.some((connection) => connection.status === "Indexing")) {
    return { label: "Indexing" };
  }
  if (matching.some((connection) => connection.status === "Ready") || records > 0) {
    return { label: "Indexed" };
  }
  if (matching.some((connection) => connection.status === "Failed")) {
    return { label: "Index failed" };
  }
  return {
    label: integration.entities.length > 0 ? "Indexable" : "Not indexable",
  };
}

function providerMatches(left: string, right: string) {
  const normalize = (value: string) =>
    value.toLowerCase().replace(/[^a-z0-9]/g, "");
  return normalize(left) === normalize(right);
}
