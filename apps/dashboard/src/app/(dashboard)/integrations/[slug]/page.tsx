"use client";

import { use, useState } from "react";
import { notFound } from "next/navigation";
import { cn } from "@/lib/utils";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  useIntegration,
  useSetSkillCredentials,
  CredentialDialog,
  IntegrationDetailTab,
} from "@/features/agents";
import {
  KeyIcon,
  CheckCircle2Icon,
  AlertCircleIcon,
} from "lucide-react";

export default function IntegrationDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = use(params);

  const { integration, loading } = useIntegration(slug);
  const setCredentials = useSetSkillCredentials();
  const [credDialogOpen, setCredDialogOpen] = useState(false);

  if (!integration) {
    if (loading) {
      return (
        <>
          <PageHeader group="MCP Servers" page="Loading..." />
          <div className="flex flex-1 flex-col p-4 pt-0">
            <div className="flex items-start gap-4 mb-4">
              <Skeleton className="size-12 rounded-xl shrink-0" />
              <div className="flex-1 space-y-2">
                <Skeleton className="h-5 w-48" />
                <Skeleton className="h-4 w-96" />
                <Skeleton className="h-4 w-32" />
              </div>
            </div>
            <div className="space-y-4 max-w-3xl">
              <Skeleton className="h-32 w-full rounded-xl" />
              <Skeleton className="h-48 w-full rounded-xl" />
            </div>
          </div>
        </>
      );
    }
    return notFound();
  }

  async function handleSaveCredentials(values: Record<string, string>) {
    if (!integration) return;
    await setCredentials(integration.name, values);
  }

  const hasCredentialFields = integration.credentialFields.length > 0;

  return (
    <>
      <PageHeader
        group="MCP Servers"
        page={integration.title}
        action={
          <div className="flex items-center gap-2">
            {hasCredentialFields && (
              <Button
                size="sm"
                variant={integration.configured ? "outline" : "default"}
                onClick={() => setCredDialogOpen(true)}
              >
                <KeyIcon className="size-4" />
                {integration.configured ? "Reconfigure" : "Configure"}
              </Button>
            )}
          </div>
        }
      />

      <div className="flex flex-1 flex-col p-4 pt-0">
        {/* Header */}
        <div className="flex items-start gap-4 mb-6">
          <div
            className="size-12 shrink-0 rounded-xl [&>svg]:size-12"
            dangerouslySetInnerHTML={{ __html: integration.logo }}
          />
          <div className="flex-1 min-w-0">
            <h1 className="text-lg font-semibold">{integration.title}</h1>
            <p className="text-sm text-muted-foreground">
              {integration.description}
            </p>
            {hasCredentialFields && (
              <div
                className={cn(
                  "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[10px] font-semibold uppercase tracking-widest mt-2",
                  integration.configured
                    ? "bg-emerald-100 text-emerald-700"
                    : "bg-amber-100 text-amber-700",
                )}
              >
                {integration.configured ? (
                  <CheckCircle2Icon className="size-3" />
                ) : (
                  <AlertCircleIcon className="size-3" />
                )}
                {integration.configured ? "Configured" : "Credentials required"}
              </div>
            )}
            <div className="flex items-center gap-4 mt-2 text-xs text-muted-foreground">
              <span>Transport: {integration.transportType}</span>
              {integration.category && <span>{integration.category}</span>}
            </div>
          </div>
        </div>

        {/* Server details */}
        <div className="max-w-3xl space-y-6">
          <div className="rounded-xl border border-border bg-card">
            <div className="px-4 py-3 border-b border-border">
              <span className="text-sm font-medium">Server Details</span>
            </div>
            <dl className="divide-y divide-border">
              <div className="flex justify-between px-4 py-3">
                <dt className="text-sm text-muted-foreground">Name</dt>
                <dd className="font-mono text-xs">{integration.name}</dd>
              </div>
              <div className="flex justify-between px-4 py-3">
                <dt className="text-sm text-muted-foreground">Transport</dt>
                <dd className="font-mono text-xs">{integration.transportType}</dd>
              </div>
              {integration.category && (
                <div className="flex justify-between px-4 py-3">
                  <dt className="text-sm text-muted-foreground">Category</dt>
                  <dd className="text-sm">{integration.category}</dd>
                </div>
              )}
              <div className="flex justify-between px-4 py-3">
                <dt className="text-sm text-muted-foreground">Built-in</dt>
                <dd className="text-sm">{integration.isBuiltin ? "Yes" : "No"}</dd>
              </div>
            </dl>
          </div>

          {/* Credential fields info */}
          {hasCredentialFields && (
            <div className="rounded-xl border border-border bg-card">
              <div className="px-4 py-3 border-b border-border">
                <span className="text-sm font-medium">Required Credentials</span>
                <span className="ml-2 text-xs text-muted-foreground">
                  {integration.credentialFields.length}
                </span>
              </div>
              {integration.credentialFields.map((field, idx) => (
                <div
                  key={field.name}
                  className={`flex items-center gap-4 px-4 py-3${
                    idx < integration.credentialFields.length - 1 ? " border-b border-border" : ""
                  }`}
                >
                  <code className="rounded bg-muted px-2 py-1 font-mono text-xs">
                    {field.name}
                  </code>
                  <span className="text-sm text-muted-foreground">
                    {field.label}
                  </span>
                  {field.required && (
                    <span className="text-[10px] font-medium text-red-500 uppercase">Required</span>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {hasCredentialFields && (
        <CredentialDialog
          open={credDialogOpen}
          onOpenChange={setCredDialogOpen}
          name={integration.title}
          slug={integration.name}
          logo={integration.logo}
          credentials={integration.credentialFields}
          onSave={handleSaveCredentials}
        />
      )}
    </>
  );
}
