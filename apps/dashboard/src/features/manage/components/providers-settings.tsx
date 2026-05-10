"use client";

import * as React from "react";
import Image from "next/image";
import {
  CheckCircle2Icon,
  ExternalLinkIcon,
  SettingsIcon,
  UnplugIcon,
} from "lucide-react";
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
  useDisconnectCodexOAuthProvider,
  useCodexOAuthProvider,
  useOrganization,
  useOrganizationProviderProfiles,
  usePollCodexOAuthLogin,
  useProviderSetupStatus,
  useProviders,
  useRefreshProviderQueries,
  useSaveBedrockProviderSetup,
  useSaveFoundryProviderSetup,
  useSaveOrganizationProviderProfile,
  useSaveVertexProviderSetup,
  useStartCodexOAuthLogin,
  type CodexOAuthLogin,
  type OrganizationProviderProfile,
  type Provider,
  type ProviderSetupStatus,
} from "@/features/manage";

const LOGOS: Record<string, string> = {
  openai: "/openai.svg",
  "openai-codex": "/openai.svg",
  anthropic: "/anthropic.svg",
  google: "/google.svg",
  xai: "/xai.svg",
  "google-vertex": "/google.svg",
};

const CLOUD_PROVIDERS = [
  {
    slug: "aws-bedrock",
    name: "Amazon Bedrock",
    authKinds: [
      "aws_environment",
      "aws_profile",
      "aws_access_key",
      "aws_bedrock_api_key",
      "gateway",
    ],
    models:
      "us.anthropic.claude-sonnet-4-6, us.anthropic.claude-haiku-4-5-20251001-v1:0",
  },
  {
    slug: "google-vertex",
    name: "Google Vertex AI",
    authKinds: [
      "google_application_default",
      "google_service_account_file",
      "gateway",
    ],
    models: "claude-sonnet-4-6, claude-haiku-4-5@20251001",
  },
  {
    slug: "azure-foundry",
    name: "Microsoft Foundry",
    authKinds: ["azure_default_credential", "azure_api_key", "gateway"],
    models: "claude-sonnet-4-6, claude-haiku-4-5",
  },
];
const CLOUD_PROVIDER_SLUGS = new Set(
  CLOUD_PROVIDERS.map((provider) => provider.slug),
);

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
  const [authKind, setAuthKind] = React.useState(
    status?.authKind ?? provider.authKinds[0],
  );
  const [enabled, setEnabled] = React.useState(status?.enabled ?? true);
  const [models, setModels] = React.useState(
    status?.pinnedModels.join(", ") || provider.models,
  );
  const [region, setRegion] = React.useState(
    envValue(status, "AWS_REGION") ||
      envValue(status, "CLOUD_ML_REGION") ||
      "global",
  );
  const [projectId, setProjectId] = React.useState(
    envValue(status, "ANTHROPIC_VERTEX_PROJECT_ID"),
  );
  const [resource, setResource] = React.useState(
    envValue(status, "ANTHROPIC_FOUNDRY_RESOURCE"),
  );
  const [profile, setProfile] = React.useState(envValue(status, "AWS_PROFILE"));
  const [credentialsPath, setCredentialsPath] = React.useState(
    envValue(status, "GOOGLE_APPLICATION_CREDENTIALS"),
  );
  const [baseUrl, setBaseUrl] = React.useState(
    envValue(status, "ANTHROPIC_BEDROCK_BASE_URL") ||
      envValue(status, "ANTHROPIC_VERTEX_BASE_URL") ||
      envValue(status, "ANTHROPIC_FOUNDRY_BASE_URL"),
  );
  const [apiKey, setApiKey] = React.useState("");
  const [awsAccessKeyId, setAwsAccessKeyId] = React.useState("");
  const [awsSecretAccessKey, setAwsSecretAccessKey] = React.useState("");
  const [awsSessionToken, setAwsSessionToken] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);

  const { saveBedrockProviderSetup, loading: savingBedrock } =
    useSaveBedrockProviderSetup();
  const { saveVertexProviderSetup, loading: savingVertex } =
    useSaveVertexProviderSetup();
  const { saveFoundryProviderSetup, loading: savingFoundry } =
    useSaveFoundryProviderSetup();
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
            <Field
              label={
                provider.slug === "aws-bedrock"
                  ? "AWS region"
                  : "Vertex location"
              }
              value={region}
              onChange={setRegion}
            />
          )}
          {provider.slug === "aws-bedrock" && authKind === "aws_profile" && (
            <Field label="AWS profile" value={profile} onChange={setProfile} />
          )}
          {provider.slug === "aws-bedrock" && authKind === "aws_access_key" && (
            <div className="grid gap-3 sm:grid-cols-2">
              <Field
                label="Access key ID"
                value={awsAccessKeyId}
                onChange={setAwsAccessKeyId}
              />
              <Field
                label="Secret access key"
                type="password"
                value={awsSecretAccessKey}
                onChange={setAwsSecretAccessKey}
              />
              <Field
                label="Session token"
                value={awsSessionToken}
                onChange={setAwsSessionToken}
              />
            </div>
          )}
          {provider.slug === "google-vertex" && !skipProviderAuth && (
            <>
              <Field
                label="Project ID"
                value={projectId}
                onChange={setProjectId}
              />
              {authKind === "google_service_account_file" && (
                <Field
                  label="Credentials file path"
                  value={credentialsPath}
                  onChange={setCredentialsPath}
                />
              )}
            </>
          )}
          {provider.slug === "azure-foundry" && !skipProviderAuth && (
            <Field label="Resource" value={resource} onChange={setResource} />
          )}
          {(authKind === "aws_bedrock_api_key" ||
            authKind === "azure_api_key") && (
            <Field
              label="API key"
              type="password"
              value={apiKey}
              onChange={setApiKey}
            />
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

function PlatformProviderDialog({
  provider,
  organizationId,
  profile,
  onSaved,
}: {
  provider: Provider;
  organizationId: string;
  profile?: OrganizationProviderProfile;
  onSaved: () => Promise<unknown>;
}) {
  const [open, setOpen] = React.useState(false);
  const [displayName, setDisplayName] = React.useState(
    profile?.displayName ?? provider.displayName,
  );
  const [enabled, setEnabled] = React.useState(profile?.enabled ?? true);
  const [models, setModels] = React.useState(
    (profile?.allowedModels.length
      ? profile.allowedModels
      : provider.models
    ).join(", "),
  );
  const [apiKey, setApiKey] = React.useState("");
  const [baseUrl, setBaseUrl] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);
  const { saveOrganizationProviderProfile, loading: saving } =
    useSaveOrganizationProviderProfile();
  const custom = provider.name === "custom";

  async function save() {
    setError(null);
    try {
      const allowedModels = splitModels(models);
      await saveOrganizationProviderProfile({
        organizationId,
        provider: provider.name,
        displayName: displayName.trim() || provider.displayName,
        allowedModels,
        apiKey,
        enabled,
        authKind: "api_key",
        credentials: custom
          ? [
              { key: "baseUrl", value: baseUrl },
              { key: "apiKey", value: apiKey },
            ]
          : [{ key: "apiKey", value: apiKey }],
      });
      await onSaved();
      setApiKey("");
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
          <DialogTitle>{provider.displayName}</DialogTitle>
        </DialogHeader>
        <div className="grid gap-3">
          <Field
            label="Display name"
            value={displayName}
            onChange={setDisplayName}
          />
          {custom && (
            <Field label="Base URL" value={baseUrl} onChange={setBaseUrl} />
          )}
          <Field
            label={custom ? "API key (optional)" : "API key"}
            type="password"
            value={apiKey}
            onChange={setApiKey}
          />
          <Field label="Models" value={models} onChange={setModels} />
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

function CodexProviderCard({
  provider,
  onChanged,
}: {
  provider?: Provider;
  onChanged: () => Promise<unknown>;
}) {
  const [login, setLogin] = React.useState<CodexOAuthLogin | null>(null);
  const [message, setMessage] = React.useState<string | null>(null);
  const [error, setError] = React.useState<string | null>(null);
  const { startCodexOAuthLogin, loading: starting } = useStartCodexOAuthLogin();
  const { pollCodexOAuthLogin } = usePollCodexOAuthLogin();
  const { disconnectCodexOAuthProvider, loading: disconnecting } =
    useDisconnectCodexOAuthProvider();
  const refreshProviderQueries = useRefreshProviderQueries();
  const [localConnected, setLocalConnected] = React.useState(
    Boolean(provider?.configured),
  );
  const pollRef = React.useRef(pollCodexOAuthLogin);
  const onChangedRef = React.useRef(onChanged);
  const connected = localConnected || Boolean(provider?.configured);

  React.useEffect(() => {
    pollRef.current = pollCodexOAuthLogin;
  }, [pollCodexOAuthLogin]);

  React.useEffect(() => {
    onChangedRef.current = onChanged;
  }, [onChanged]);

  React.useEffect(() => {
    setLocalConnected(Boolean(provider?.configured));
  }, [provider?.configured]);

  React.useEffect(() => {
    if (!login) return;
    const loginId = login.loginId;
    let cancelled = false;
    let timeout: number | undefined;

    async function poll() {
      try {
        const result = await pollRef.current(loginId);
        if (cancelled) return;
        if (result?.completed) {
          if (result.success) {
            setMessage(
              `Connected${result.accountEmail ? ` as ${result.accountEmail}` : ""}.`,
            );
            setLocalConnected(true);
            setLogin(null);
            await onChangedRef.current();
            await refreshProviderQueries();
          } else {
            setError(result.error ?? "Codex OAuth failed.");
          }
          return;
        }
      } catch (err) {
        setError(
          err instanceof Error ? err.message : "Codex OAuth polling failed.",
        );
      }
      if (!cancelled && login) {
        timeout = window.setTimeout(poll, 2500);
      }
    }

    void poll();

    return () => {
      cancelled = true;
      if (timeout) window.clearTimeout(timeout);
    };
  }, [login, refreshProviderQueries]);

  async function start() {
    setError(null);
    setMessage(null);
    const result = await startCodexOAuthLogin();
    if (result) {
      setLogin(result);
      window.open(result.authUrl, "_blank", "noopener,noreferrer");
    }
  }

  async function disconnect() {
    setError(null);
    setMessage(null);
    await disconnectCodexOAuthProvider();
    setLogin(null);
    setLocalConnected(false);
    await onChanged();
    await refreshProviderQueries();
  }

  return (
    <div className="flex items-center gap-3 rounded-lg border bg-card p-4">
      <ProviderLogo name="openai-codex" />
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium">OpenAI Codex</span>
          <span
            className={`rounded px-1.5 py-0.5 text-xs font-medium ${connected ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground"}`}
          >
            {connected ? "Connected" : login ? "Waiting" : "Not configured"}
          </span>
        </div>
        <div className="mt-1 text-xs text-muted-foreground">
          {login ? (
            <span>
              Open the OpenAI authentication page to finish connecting Codex.{" "}
              <a
                href={login.authUrl}
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-1 text-foreground underline-offset-2 hover:underline"
              >
                Open authentication URL <ExternalLinkIcon className="size-3" />
              </a>
            </span>
          ) : connected ? (
            "Connected to your personal Codex subscription."
          ) : (
            "Connect a ChatGPT subscription through Codex OAuth."
          )}
        </div>
        {message && <div className="mt-1 text-xs text-primary">{message}</div>}
        {error && <div className="mt-1 text-xs text-destructive">{error}</div>}
      </div>
      {connected ? (
        <Button
          variant="outline"
          size="sm"
          onClick={disconnect}
          disabled={disconnecting}
        >
          <UnplugIcon />
          Disconnect
        </Button>
      ) : (
        <Button
          variant="outline"
          size="sm"
          onClick={start}
          disabled={starting || Boolean(login)}
        >
          <CheckCircle2Icon />
          {login ? "Pending" : "Connect"}
        </Button>
      )}
    </div>
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
      <Input
        id={id}
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
    </div>
  );
}

export function ProvidersSettings() {
  const refreshProviderQueries = useRefreshProviderQueries();
  const { organization, loading: orgLoading } = useOrganization();
  const {
    providers,
    loading: providersLoading,
    refetch: refetchProviders,
  } = useProviders();
  const {
    provider: codexProvider,
    loading: codexLoading,
    refetch: refetchCodexProvider,
  } = useCodexOAuthProvider();
  const {
    statuses,
    loading: setupLoading,
    refetch: refetchSetup,
  } = useProviderSetupStatus(organization?.id);
  const {
    profiles,
    loading: profilesLoading,
    refetch: refetchProfiles,
  } = useOrganizationProviderProfiles(organization?.id);
  const loading =
    orgLoading ||
    providersLoading ||
    codexLoading ||
    (organization ? profilesLoading : false) ||
    (organization ? setupLoading : false);

  const effectiveCodexProvider =
    codexProvider ?? providers.find((item) => item.name === "openai-codex");

  if (loading) {
    return (
      <>
        <PageHeader
          page="Providers"
          subtitle="Manage model provider availability."
          width="narrow"
        />
        <PageContainer
          width="narrow"
          className="flex flex-1 flex-col gap-3 pb-4"
        >
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-20 w-full rounded-lg" />
          ))}
        </PageContainer>
      </>
    );
  }

  if (!organization) {
    return (
      <>
        <PageHeader
          page="Providers"
          subtitle="Manage personal model provider availability."
          width="narrow"
        />
        <PageContainer
          width="narrow"
          className="flex flex-1 flex-col gap-6 pb-4"
        >
          <section className="grid gap-2">
            <CodexProviderCard
              provider={effectiveCodexProvider}
              onChanged={async () => {
                await Promise.all([refetchCodexProvider(), refetchProviders()]);
              }}
            />
          </section>
          <section className="grid gap-2">
            <h3 className="text-sm font-semibold">Platform providers</h3>
            {providers
              .filter((provider) => provider.name !== "openai-codex")
              .map((provider) => (
                <div
                  key={provider.id}
                  className="flex items-center gap-3 rounded-lg border bg-card p-4"
                >
                  <ProviderLogo name={provider.name} />
                  <div className="min-w-0 flex-1">
                    <span className="text-sm font-medium">
                      {provider.displayName}
                    </span>
                    <div className="text-xs text-muted-foreground">
                      {provider.models.length} models
                    </div>
                  </div>
                  <span
                    className={`rounded px-1.5 py-0.5 text-xs font-medium ${provider.configured ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground"}`}
                  >
                    {provider.configured ? "Connected" : "Not configured"}
                  </span>
                </div>
              ))}
          </section>
        </PageContainer>
      </>
    );
  }

  return (
    <>
      <PageHeader
        page="Providers"
        subtitle="Configure model providers and model pins."
        width="narrow"
      />
      <PageContainer width="narrow" className="flex flex-1 flex-col gap-6 pb-4">
        <section className="grid gap-2">
          {CLOUD_PROVIDERS.map((provider) => {
            const status = statuses.find(
              (item) => item.provider === provider.slug,
            );
            return (
              <div
                key={provider.slug}
                className="flex items-center gap-3 rounded-lg border bg-card p-4"
              >
                <ProviderLogo name={provider.slug} />
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium">{provider.name}</span>
                    <span
                      className={`rounded px-1.5 py-0.5 text-xs font-medium ${status?.configured ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground"}`}
                    >
                      {status?.configured ? "Connected" : "Not configured"}
                    </span>
                  </div>
                  <div className="mt-1 truncate text-xs text-muted-foreground">
                    {(status?.pinnedModels.length
                      ? status.pinnedModels
                      : splitModels(provider.models)
                    ).join(", ")}
                  </div>
                </div>
                <CloudProviderDialog
                  provider={provider}
                  organizationId={organization.id}
                  status={status}
                  onSaved={async () => {
                    await Promise.all([
                      refetchSetup(),
                      refetchProviders(),
                      refreshProviderQueries(),
                    ]);
                  }}
                />
              </div>
            );
          })}
          <CodexProviderCard
            provider={effectiveCodexProvider}
            onChanged={async () => {
              await Promise.all([
                refetchCodexProvider(),
                refetchSetup(),
                refetchProviders(),
              ]);
            }}
          />
        </section>
        <section className="grid gap-2">
          <h3 className="text-sm font-semibold">API providers</h3>
          {providers
            .filter(
              (provider) =>
                provider.name !== "openai-codex" &&
                !CLOUD_PROVIDER_SLUGS.has(provider.name),
            )
            .map((provider) => {
              const profile = profiles.find(
                (item) => item.provider === provider.name,
              );
              return (
                <div
                  key={provider.id}
                  className="flex items-center gap-3 rounded-lg border bg-card p-4"
                >
                  <ProviderLogo name={provider.name} />
                  <div className="min-w-0 flex-1">
                    <span className="text-sm font-medium">
                      {provider.displayName}
                    </span>
                    <div className="text-xs text-muted-foreground">
                      {provider.models.length} models
                      {profile ? " · dashboard" : " · env"}
                    </div>
                  </div>
                  <span
                    className={`rounded px-1.5 py-0.5 text-xs font-medium ${provider.configured ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground"}`}
                  >
                    {provider.configured ? "Connected" : "Not configured"}
                  </span>
                  <PlatformProviderDialog
                    provider={provider}
                    organizationId={organization.id}
                    profile={profile}
                    onSaved={async () => {
                      await Promise.all([
                        refetchProfiles(),
                        refetchProviders(),
                        refreshProviderQueries(),
                      ]);
                    }}
                  />
                </div>
              );
            })}
        </section>
      </PageContainer>
    </>
  );
}
