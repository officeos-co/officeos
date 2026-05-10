"use client";

import * as React from "react";
import Image from "next/image";
import { CheckCircle2Icon, SettingsIcon } from "lucide-react";
import { PageHeader } from "@/shell/page-header";
import { PageContainer } from "@/shell/page-container";
import { Button } from "@/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/ui/dialog";
import { Input } from "@/ui/input";
import { Label } from "@/ui/label";
import { Skeleton } from "@/ui/skeleton";
import { Switch } from "@/ui/switch";
import {
  useBilling,
  useOrganization,
  useProviderSetupStatus,
  useProviders,
  useSaveBedrockProviderSetup,
  useSaveFoundryProviderSetup,
  useSaveVertexProviderSetup,
  type ProviderSetupStatus,
} from "@/features/manage";

const LOGOS: Record<string, string> = {
  openai: "/openai.svg",
  anthropic: "/anthropic.svg",
  google: "/google.svg",
  xai: "/xai.svg",
  "google-vertex": "/google.svg",
};

const CLOUD_PROVIDERS = [
  {
    slug: "aws-bedrock",
    name: "Amazon Bedrock",
    authKinds: ["aws_environment", "aws_profile", "aws_access_key", "aws_bedrock_api_key", "gateway"],
    models: "us.anthropic.claude-sonnet-4-6, us.anthropic.claude-haiku-4-5-20251001-v1:0",
  },
  {
    slug: "google-vertex",
    name: "Google Vertex AI",
    authKinds: ["google_application_default", "google_service_account_file", "gateway"],
    models: "claude-sonnet-4-6, claude-haiku-4-5@20251001",
  },
  {
    slug: "azure-foundry",
    name: "Microsoft Foundry",
    authKinds: ["azure_default_credential", "azure_api_key", "gateway"],
    models: "claude-sonnet-4-6, claude-haiku-4-5",
  },
];

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

function splitModels(value: string) {
  return value
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);
}

function envValue(status: ProviderSetupStatus | undefined, key: string) {
  return status?.environment.find((item) => item.key === key)?.value ?? "";
}

function CloudProviderDialog({
  provider,
  organizationId,
  status,
  onSaved,
}: {
  provider: (typeof CLOUD_PROVIDERS)[number];
  organizationId: string;
  status?: ProviderSetupStatus;
  onSaved: () => Promise<unknown>;
}) {
  const [open, setOpen] = React.useState(false);
  const [authKind, setAuthKind] = React.useState(status?.authKind ?? provider.authKinds[0]);
  const [enabled, setEnabled] = React.useState(status?.enabled ?? true);
  const [models, setModels] = React.useState(status?.pinnedModels.join(", ") || provider.models);
  const [region, setRegion] = React.useState(envValue(status, "AWS_REGION") || envValue(status, "CLOUD_ML_REGION") || "global");
  const [projectId, setProjectId] = React.useState(envValue(status, "ANTHROPIC_VERTEX_PROJECT_ID"));
  const [resource, setResource] = React.useState(envValue(status, "ANTHROPIC_FOUNDRY_RESOURCE"));
  const [profile, setProfile] = React.useState(envValue(status, "AWS_PROFILE"));
  const [credentialsPath, setCredentialsPath] = React.useState(envValue(status, "GOOGLE_APPLICATION_CREDENTIALS"));
  const [baseUrl, setBaseUrl] = React.useState(
    envValue(status, "ANTHROPIC_BEDROCK_BASE_URL") ||
      envValue(status, "ANTHROPIC_VERTEX_BASE_URL") ||
      envValue(status, "ANTHROPIC_FOUNDRY_BASE_URL")
  );
  const [apiKey, setApiKey] = React.useState("");
  const [awsAccessKeyId, setAwsAccessKeyId] = React.useState("");
  const [awsSecretAccessKey, setAwsSecretAccessKey] = React.useState("");
  const [awsSessionToken, setAwsSessionToken] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);

  const { saveBedrockProviderSetup, loading: savingBedrock } = useSaveBedrockProviderSetup();
  const { saveVertexProviderSetup, loading: savingVertex } = useSaveVertexProviderSetup();
  const { saveFoundryProviderSetup, loading: savingFoundry } = useSaveFoundryProviderSetup();
  const saving = savingBedrock || savingVertex || savingFoundry;
  const skipProviderAuth = authKind === "gateway";

  async function save() {
    setError(null);
    try {
      const pinnedModels = splitModels(models);
      if (provider.slug === "aws-bedrock") {
        await saveBedrockProviderSetup({
          organizationId,
          displayName: provider.name,
          awsRegion: region || null,
          authKind,
          awsProfile: profile || null,
          awsAccessKeyId: awsAccessKeyId || null,
          awsSecretAccessKey: awsSecretAccessKey || null,
          awsSessionToken: awsSessionToken || null,
          bedrockApiKey: apiKey || null,
          baseUrl: baseUrl || null,
          skipProviderAuth,
          pinnedModels,
          enabled,
        });
      } else if (provider.slug === "google-vertex") {
        await saveVertexProviderSetup({
          organizationId,
          displayName: provider.name,
          projectId: projectId || null,
          location: region || null,
          authKind,
          credentialsPath: credentialsPath || null,
          baseUrl: baseUrl || null,
          skipProviderAuth,
          pinnedModels,
          enabled,
        });
      } else {
        await saveFoundryProviderSetup({
          organizationId,
          displayName: provider.name,
          resource: resource || null,
          baseUrl: baseUrl || null,
          authKind,
          apiKey: apiKey || null,
          skipProviderAuth,
          pinnedModels,
          enabled,
        });
      }
      await onSaved();
      setOpen(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Provider setup failed.");
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger render={<Button variant="outline" size="sm" />}>
        <SettingsIcon />
        Configure
      </DialogTrigger>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{provider.name}</DialogTitle>
        </DialogHeader>
        <div className="grid gap-3">
          <div className="grid gap-1.5">
            <Label htmlFor={`${provider.slug}-auth`}>Authentication</Label>
            <select
              id={`${provider.slug}-auth`}
              className="h-8 rounded-lg border border-input bg-background px-2 text-sm"
              value={authKind}
              onChange={(event) => setAuthKind(event.target.value)}
            >
              {provider.authKinds.map((kind) => (
                <option key={kind} value={kind}>
                  {kind.replaceAll("_", " ")}
                </option>
              ))}
            </select>
          </div>
          {provider.slug !== "azure-foundry" && !skipProviderAuth && (
            <Field label={provider.slug === "aws-bedrock" ? "AWS region" : "Vertex location"} value={region} onChange={setRegion} />
          )}
          {provider.slug === "aws-bedrock" && authKind === "aws_profile" && (
            <Field label="AWS profile" value={profile} onChange={setProfile} />
          )}
          {provider.slug === "aws-bedrock" && authKind === "aws_access_key" && (
            <div className="grid gap-3 sm:grid-cols-2">
              <Field label="Access key ID" value={awsAccessKeyId} onChange={setAwsAccessKeyId} />
              <Field label="Secret access key" type="password" value={awsSecretAccessKey} onChange={setAwsSecretAccessKey} />
              <Field label="Session token" value={awsSessionToken} onChange={setAwsSessionToken} />
            </div>
          )}
          {provider.slug === "google-vertex" && !skipProviderAuth && (
            <>
              <Field label="Project ID" value={projectId} onChange={setProjectId} />
              {authKind === "google_service_account_file" && (
                <Field label="Credentials file path" value={credentialsPath} onChange={setCredentialsPath} />
              )}
            </>
          )}
          {provider.slug === "azure-foundry" && !skipProviderAuth && (
            <Field label="Resource" value={resource} onChange={setResource} />
          )}
          {(authKind === "aws_bedrock_api_key" || authKind === "azure_api_key") && (
            <Field label="API key" type="password" value={apiKey} onChange={setApiKey} />
          )}
          {(skipProviderAuth || provider.slug === "azure-foundry") && (
            <Field label="Base URL" value={baseUrl} onChange={setBaseUrl} />
          )}
          <Field label="Pinned models" value={models} onChange={setModels} />
          <label className="flex items-center gap-2 text-sm">
            <Switch checked={enabled} onCheckedChange={setEnabled} />
            Enabled
          </label>
          {error && <p className="text-sm text-destructive">{error}</p>}
        </div>
        <DialogFooter>
          <Button onClick={save} disabled={saving}>
            <CheckCircle2Icon />
            Save
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function Field({
  label,
  value,
  onChange,
  type = "text",
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
}) {
  const id = React.useId();
  return (
    <div className="grid gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Input id={id} type={type} value={value} onChange={(event) => onChange(event.target.value)} />
    </div>
  );
}

export function ProvidersSettings() {
  const { billing, loading: billingLoading } = useBilling();
  const { organization, loading: orgLoading } = useOrganization();
  const { providers, loading: providersLoading } = useProviders();
  const {
    statuses,
    loading: setupLoading,
    refetch: refetchSetup,
  } = useProviderSetupStatus(organization?.id);
  const enterprise = billing?.plan?.toLowerCase() === "enterprise";
  const loading = billingLoading || orgLoading || providersLoading || (enterprise && setupLoading);

  if (loading) {
    return (
      <>
        <PageHeader page="Providers" subtitle="Manage model provider availability." width="narrow" />
        <PageContainer width="narrow" className="flex flex-1 flex-col gap-3 pb-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-20 w-full rounded-lg" />
          ))}
        </PageContainer>
      </>
    );
  }

  if (!enterprise || !organization) {
    return (
      <>
        <PageHeader page="Providers" subtitle="Enterprise provider management." width="narrow" />
        <PageContainer width="narrow" className="pb-4">
          <div className="rounded-lg border bg-card p-4 text-sm text-muted-foreground">
            Provider management is available on Enterprise plans.
          </div>
        </PageContainer>
      </>
    );
  }

  return (
    <>
      <PageHeader page="Providers" subtitle="Configure cloud-hosted Claude providers and model pins." width="narrow" />
      <PageContainer width="narrow" className="flex flex-1 flex-col gap-6 pb-4">
        <section className="grid gap-2">
          {CLOUD_PROVIDERS.map((provider) => {
            const status = statuses.find((item) => item.provider === provider.slug);
            return (
              <div key={provider.slug} className="flex items-center gap-3 rounded-lg border bg-card p-4">
                <ProviderLogo name={provider.slug} />
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium">{provider.name}</span>
                    <span className={`rounded px-1.5 py-0.5 text-xs font-medium ${status?.configured ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground"}`}>
                      {status?.configured ? "Connected" : "Not configured"}
                    </span>
                  </div>
                  <div className="mt-1 truncate text-xs text-muted-foreground">
                    {(status?.pinnedModels.length ? status.pinnedModels : splitModels(provider.models)).join(", ")}
                  </div>
                </div>
                <CloudProviderDialog provider={provider} organizationId={organization.id} status={status} onSaved={refetchSetup} />
              </div>
            );
          })}
        </section>
        <section className="grid gap-2">
          <h3 className="text-sm font-semibold">Platform providers</h3>
          {providers.map((provider) => (
            <div key={provider.id} className="flex items-center gap-3 rounded-lg border bg-card p-4">
              <ProviderLogo name={provider.name} />
              <div className="min-w-0 flex-1">
                <span className="text-sm font-medium">{provider.displayName}</span>
                <div className="text-xs text-muted-foreground">{provider.models.length} models</div>
              </div>
              <span className={`rounded px-1.5 py-0.5 text-xs font-medium ${provider.configured ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground"}`}>
                {provider.configured ? "Connected" : "Not configured"}
              </span>
            </div>
          ))}
        </section>
      </PageContainer>
    </>
  );
}
