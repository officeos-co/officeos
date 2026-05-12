"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";
import {
  ArrowLeftIcon,
  CheckIcon,
  ChevronDownIcon,
  ExternalLinkIcon,
  KeyIcon,
  PlusIcon,
} from "lucide-react";
import { buildOAuthUrl } from "@/lib/auth-url";
import { Button } from "@/ui/button";
import { HelpTooltip, WithTooltip } from "@/ui/help-tooltip";
import { Input } from "@/ui/input";
import { Label } from "@/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/ui/dialog";
import { getDialogWidthClassName } from "@/shell/page-container";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/ui/select";
import type { CredentialField, McpServer } from "../data/integrations";

type CredentialAuthType = "oauth" | "bearer";

type CredentialSetupProps = {
  integrations: McpServer[];
  selectedName: string | null;
  onSelectedNameChange: (name: string) => void;
  returnTo?: string | ((server: McpServer) => string);
  submitLabel?: string;
  showHeading?: boolean;
  onAddCustomMcp?: () => void;
  onSave: (
    server: McpServer,
    values: Record<string, string>,
  ) => Promise<void> | void;
  onSaved?: () => void;
};

export function CredentialSetup({
  integrations,
  selectedName,
  onSelectedNameChange,
  returnTo = "/integrations",
  submitLabel,
  showHeading = true,
  onAddCustomMcp,
  onSave,
  onSaved,
}: CredentialSetupProps) {
  const selected = useMemo(
    () =>
      selectedName
        ? integrations.find((server) => server.name === selectedName) ?? null
        : null,
    [integrations, selectedName],
  );
  const [values, setValues] = useState<Record<string, string>>({});
  const [authType, setAuthType] = useState<CredentialAuthType>("oauth");
  const [acknowledged, setAcknowledged] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [view, setView] = useState<"setup" | "details">("setup");
  const selectedOAuthProvider = selected?.oauthProvider ?? null;
  const selectedPreferredAuthType: CredentialAuthType = selected?.oauthProvider
    ? "oauth"
    : "bearer";

  useEffect(() => {
    if (!selectedName) return;
    setAuthType(selectedPreferredAuthType);
  }, [selectedName, selectedPreferredAuthType]);

  const manualCredentials = selected
    ? selected.oauthProvider
      ? bearerTokenFields
      : selected.credentialFields
    : [];
  const showingManualCredentials =
    Boolean(selected) && (!selected?.oauthProvider || authType === "bearer");
  const showingOAuthConnection =
    Boolean(selectedOAuthProvider) && authType === "oauth";
  const canSave =
    Boolean(selected) &&
    showingManualCredentials &&
    acknowledged &&
    !saving &&
    manualCredentials.every((field) =>
      field.required ? Boolean(values[field.name]?.trim()) : true,
    );

  function selectServer(name: string | null) {
    if (!name) return;
    onSelectedNameChange(name);
    setValues({});
    setAcknowledged(false);
    setError(null);
    setView("setup");
    const server = integrations.find((candidate) => candidate.name === name);
    setAuthType(server?.oauthProvider ? "oauth" : "bearer");
  }

  async function handleSave() {
    if (!selected || !canSave) return;
    setSaving(true);
    setError(null);
    try {
      await onSave(
        selected,
        toCredentialPayload(values, authType, selected.oauthProvider, selected.credentialFields),
      );
      setValues({});
      setAcknowledged(false);
      onSaved?.();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  if (view === "details" && selected) {
    return (
      <ConnectorDetailPanel
        server={selected}
        onBack={() => setView("setup")}
        onConnect={() => setView("setup")}
      />
    );
  }

  return (
    <div className="space-y-5">
      {showHeading ? (
        <div>
          <h2 className="text-base font-semibold">Add credential</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Select an MCP server and configure the workspace credential agents
            will use for it.
          </p>
        </div>
      ) : null}

      <div className="space-y-2">
        <Label>
          Type
          <HelpTooltip>
            OAuth opens the provider authorization flow. Bearer token stores a
            static authorization token for this MCP server.
          </HelpTooltip>
        </Label>
        <div className="inline-grid h-8 grid-cols-2 rounded-lg bg-muted p-0.5">
          <AuthTypeButton
            active={authType === "oauth"}
            disabled={Boolean(selected && !selected.oauthProvider)}
            onClick={() => setAuthType("oauth")}
          >
            OAuth
          </AuthTypeButton>
          <AuthTypeButton
            active={authType === "bearer"}
            onClick={() => setAuthType("bearer")}
          >
            Bearer token
          </AuthTypeButton>
        </div>
      </div>

      <div className="space-y-2">
        <div className="flex items-center justify-between gap-3">
          <Label>MCP Server</Label>
          {selected ? (
            <Button
              size="sm"
              variant="link"
              className="h-auto px-0 py-0"
              onClick={() => setView("details")}
            >
              Details
            </Button>
          ) : null}
        </div>
        <Select value={selectedName ?? ""} onValueChange={selectServer}>
          <SelectTrigger className="h-10 w-full">
            {selected ? (
              <SelectedServerValue server={selected} />
            ) : (
              <SelectValue placeholder="Select MCP server..." />
            )}
          </SelectTrigger>
          <SelectContent
            alignItemWithTrigger
            className="max-h-80"
            showScrollButtons={false}
          >
            <SelectGroup>
              {integrations.map((server) => (
                <SelectItem key={server.name} value={server.name}>
                  <span
                    className="flex size-5 shrink-0 items-center justify-center [&>img]:size-5 [&>img]:object-contain [&>svg]:size-5"
                    dangerouslySetInnerHTML={{ __html: server.logo }}
                  />
                  <span className="min-w-0 truncate">{server.title}</span>
                  {server.configured ? (
                    <span className="ml-auto shrink-0 text-xs text-muted-foreground">
                      Connected
                    </span>
                  ) : null}
                </SelectItem>
              ))}
              {onAddCustomMcp ? (
                <button
                  type="button"
                  className="mt-1 flex w-full items-center justify-between rounded-md border border-dashed border-border px-2 py-2 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
                  onClick={(event) => {
                    event.preventDefault();
                    event.stopPropagation();
                    onAddCustomMcp();
                  }}
                >
                  <span>Add MCP</span>
                  <PlusIcon className="size-4" />
                </button>
              ) : null}
            </SelectGroup>
          </SelectContent>
        </Select>
      </div>

      {selected ? (
        <>
          {showingOAuthConnection && selectedOAuthProvider ? (
            <>
              <div className="rounded-lg border border-border p-3">
                <div className="text-sm font-medium">
                  {selected.configured
                    ? "OAuth is connected"
                    : `Connect ${providerLabel(selectedOAuthProvider)}`}
                </div>
                <p className="mt-1 text-sm leading-6 text-muted-foreground">
                  Use the configured workspace OAuth flow. This authorizes all
                  MCP servers using {providerLabel(selectedOAuthProvider)}.
                </p>
              </div>
              <SharedCredentialNotice
                acknowledged={acknowledged}
                onAcknowledgedChange={setAcknowledged}
              />
              <div className="flex justify-end">
                {acknowledged ? (
                  <Button
                    size="sm"
                    nativeButton={false}
                    render={
                      <a
                        href={buildOAuthUrl(
                          selectedOAuthProvider,
                          resolveReturnTo(returnTo, selected),
                        )}
                      />
                    }
                  >
                    <KeyIcon className="size-4" />
                    {selected.configured ? "Reconnect" : "Connect"}
                  </Button>
                ) : (
                  <Button size="sm" disabled>
                    <KeyIcon className="size-4" />
                    {selected.configured ? "Reconnect" : "Connect"}
                  </Button>
                )}
              </div>
            </>
          ) : null}

          {showingManualCredentials ? (
            <>
              <CredentialSection
                title={
                  selected.oauthProvider
                    ? "Bearer token"
                    : "MCP server credentials"
                }
                help="Stored by the backend for this MCP server. Agents receive tool access, not the raw credential values."
                showOptional={Boolean(selected.oauthProvider)}
              >
                {manualCredentials.map((field) => (
                  <Input
                    key={field.name}
                    type={field.type === "password" ? "password" : "text"}
                    placeholder={field.label}
                    value={values[field.name] ?? ""}
                    onChange={(event) =>
                      setValues((prev) => ({
                        ...prev,
                        [field.name]: event.target.value,
                      }))
                    }
                  />
                ))}
              </CredentialSection>

              <SharedCredentialNotice
                acknowledged={acknowledged}
                onAcknowledgedChange={setAcknowledged}
              />

              {error ? (
                <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                  {error}
                </div>
              ) : null}

              <div className="flex justify-end">
                <WithTooltip tooltip="Add this credential for the selected MCP server.">
                  <Button size="sm" disabled={!canSave} onClick={handleSave}>
                    {saving
                      ? "Adding..."
                      : submitLabel ??
                        (selected.configured
                          ? "Update credential"
                          : "Add credential")}
                  </Button>
                </WithTooltip>
              </div>
            </>
          ) : null}
        </>
      ) : null}
    </div>
  );
}

export function CredentialForm({
  name,
  slug,
  logo,
  oauthProvider,
  serverUrl,
  credentials,
  submitLabel,
  showHeading,
  onSave,
  onSaved,
}: {
  name: string;
  slug?: string;
  logo: string;
  oauthProvider?: string | null;
  serverUrl?: string | null;
  credentials: CredentialField[];
  submitLabel?: string;
  showHeading?: boolean;
  onSave: (values: Record<string, string>) => Promise<void> | void;
  onSaved?: () => void;
}) {
  const server = useMemo<McpServer>(
    () => ({
      id: slug ?? name,
      name: slug ?? name,
      provider: slug ?? name,
      title: name,
      subtitle: "",
      description: "",
      transportType: "",
      command: serverUrl ?? "",
      args: [],
      url: serverUrl ?? "",
      logo,
      category: "",
      credentialFields: credentials,
      oauthProvider: oauthProvider ?? null,
      oauthScopes: [],
      oauthConfigured: false,
      configured: false,
      isBuiltin: true,
      authorName: "",
      authorUrl: "",
      documentationUrl: "",
      repositoryUrl: "",
      tools: [],
      capabilities: [],
    }),
    [credentials, logo, name, oauthProvider, serverUrl, slug],
  );

  return (
    <CredentialSetup
      integrations={[server]}
      selectedName={server.name}
      onSelectedNameChange={() => undefined}
      submitLabel={submitLabel}
      showHeading={showHeading}
      onSave={(_, values) => onSave(values)}
      onSaved={onSaved}
    />
  );
}

export function CredentialDialog({
  open,
  onOpenChange,
  ...props
}: CredentialSetupProps & {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className={getDialogWidthClassName(
          "narrow",
          "max-h-[min(820px,calc(100vh-48px))] overflow-y-auto p-6",
        )}
      >
        <DialogHeader>
          <DialogTitle>Add credential</DialogTitle>
          <DialogDescription>
            Select an MCP server and configure its workspace credential.
          </DialogDescription>
        </DialogHeader>
        <CredentialSetup
          {...props}
          showHeading={false}
          onSaved={() => {
            props.onSaved?.();
            onOpenChange(false);
          }}
        />
      </DialogContent>
    </Dialog>
  );
}

function ConnectionState({ configured }: { configured: boolean }) {
  return (
    <span
      className={
        configured
          ? "shrink-0 rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-700"
          : "shrink-0 rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-700"
      }
    >
      {configured ? "Connected" : "Not connected"}
    </span>
  );
}

function ConnectorDetailPanel({
  server,
  onBack,
  onConnect,
}: {
  server: McpServer;
  onBack: () => void;
  onConnect: () => void;
}) {
  const needsSetup =
    Boolean(server.oauthProvider) || server.credentialFields.length > 0;
  const connectorUrl = server.url || server.command;

  return (
    <section className="min-h-0 overflow-y-auto pr-1">
      <button
        type="button"
        className="mb-4 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        onClick={onBack}
      >
        <ArrowLeftIcon className="size-4" />
        Back
      </button>
      <div className="flex items-start gap-3">
        <span
          className="flex size-12 shrink-0 items-center justify-center rounded-lg border border-border bg-muted/40 [&>img]:size-7 [&>img]:object-contain [&>svg]:size-7"
          dangerouslySetInnerHTML={{ __html: server.logo }}
        />
        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-4">
            <div>
              <h2 className="text-xl font-semibold">{server.title}</h2>
              <p className="text-sm text-muted-foreground">
                {server.subtitle || server.category || "MCP server"}
              </p>
            </div>
            <Button
              size="sm"
              variant={server.configured ? "outline" : "default"}
              onClick={onConnect}
            >
              {server.configured ? "Reconnect" : needsSetup ? "Connect" : "Add"}
            </Button>
          </div>
        </div>
      </div>

      <p className="mt-6 max-w-3xl text-sm leading-6 text-foreground/70">
        {server.description}
      </p>

      {server.authorName ? (
        <p className="mt-5 text-sm text-muted-foreground">
          Developed by{" "}
          {server.authorUrl ? (
            <a
              href={server.authorUrl}
              target="_blank"
              rel="noreferrer"
              className="text-foreground underline underline-offset-4"
            >
              {server.authorName}
            </a>
          ) : (
            <span className="text-foreground">{server.authorName}</span>
          )}
        </p>
      ) : null}

      {server.tools.length > 0 ? (
        <div className="mt-7">
          <div className="mb-3 flex items-center gap-2 text-sm font-medium">
            Tools
            <span className="rounded bg-muted px-1.5 py-0.5 text-xs">
              {server.tools.length}
            </span>
          </div>
          <div className="flex flex-wrap gap-2">
            {server.tools.map((tool) => (
              <span
                key={tool.name}
                className="rounded-full border border-border bg-muted/40 px-3 py-1 font-mono text-xs"
              >
                {tool.name}
              </span>
            ))}
          </div>
        </div>
      ) : null}

      <div className="mt-7 border-t border-border pt-5">
        <h3 className="mb-4 text-sm font-medium">Details</h3>
        <dl className="grid gap-x-10 gap-y-4 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-muted-foreground">Author</dt>
            <dd className="mt-1">{server.authorName || "Unknown"}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">Connector URL</dt>
            <dd className="mt-1 truncate font-mono text-xs">
              {connectorUrl || "Managed connector"}
            </dd>
          </div>
          <div>
            <dt className="text-muted-foreground">Category</dt>
            <dd className="mt-1">{server.category || "MCP server"}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">Identifier</dt>
            <dd className="mt-1 font-mono text-xs">{server.name}</dd>
          </div>
        </dl>
        <div className="mt-5 flex flex-wrap gap-3 text-sm">
          {server.documentationUrl ? (
            <a
              href={server.documentationUrl}
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-1 underline underline-offset-4"
            >
              Documentation
              <ExternalLinkIcon className="size-3.5" />
            </a>
          ) : null}
          {server.repositoryUrl ? (
            <a
              href={server.repositoryUrl}
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-1 underline underline-offset-4"
            >
              Source
              <ExternalLinkIcon className="size-3.5" />
            </a>
          ) : null}
        </div>
      </div>
    </section>
  );
}

function SelectedServerValue({ server }: { server: McpServer }) {
  return (
    <span className="flex min-w-0 flex-1 items-center gap-2 text-left">
      <span
        className="flex size-5 shrink-0 items-center justify-center [&>img]:size-5 [&>img]:object-contain [&>svg]:size-5"
        dangerouslySetInnerHTML={{ __html: server.logo }}
      />
      <span className="min-w-0 truncate">{server.title}</span>
      {server.configured ? <ConnectionState configured /> : null}
    </span>
  );
}

function SharedCredentialNotice({
  acknowledged,
  onAcknowledgedChange,
}: {
  acknowledged: boolean;
  onAcknowledgedChange: (acknowledged: boolean) => void;
}) {
  return (
    <div className="rounded-lg border border-amber-300 bg-amber-50 p-3 text-amber-900">
      <p className="text-sm leading-6">
        This credential will be shared across this workspace. Anyone with API
        key access can use this credential in an agent session to access the
        service associated with the credential, including reading data and
        taking actions on behalf of the credential owner. Learn more{" "}
        <a
          className="underline underline-offset-4"
          href="/docs/credentials"
          target="_blank"
          rel="noreferrer"
        >
          here
        </a>
        .
      </p>
      <label className="mt-4 grid cursor-pointer grid-cols-[16px_minmax(0,1fr)] gap-3 text-sm leading-6">
        <button
          type="button"
          role="checkbox"
          aria-checked={acknowledged}
          className="mt-1 flex size-4 items-center justify-center rounded border border-amber-400 bg-background text-amber-900"
          onClick={() => onAcknowledgedChange(!acknowledged)}
        >
          {acknowledged ? <CheckIcon className="size-3" /> : null}
        </button>
        <span>
          I acknowledge this credential is shared and that I am responsible for
          its storage and use.
        </span>
      </label>
    </div>
  );
}

function AuthTypeButton({
  active,
  disabled = false,
  onClick,
  children,
}: {
  active: boolean;
  disabled?: boolean;
  onClick: () => void;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      className={
        active
          ? "rounded-md bg-background px-3 text-sm font-medium shadow-sm"
          : "rounded-md px-3 text-sm font-medium text-muted-foreground disabled:cursor-not-allowed disabled:opacity-50"
      }
      onClick={onClick}
    >
      {children}
    </button>
  );
}

function CredentialSection({
  title,
  help,
  children,
  showOptional = false,
}: {
  title: string;
  help: string;
  children: ReactNode;
  showOptional?: boolean;
}) {
  return (
    <div className="rounded-lg border border-border p-3">
      <div className="mb-3 flex items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-2">
          <ChevronDownIcon className="size-4 shrink-0 text-muted-foreground" />
          <span className="text-sm font-medium">{title}</span>
          {showOptional ? (
            <span className="rounded bg-muted px-1.5 py-0.5 text-xs font-normal text-muted-foreground">
              Optional
            </span>
          ) : null}
        </div>
        <HelpTooltip>{help}</HelpTooltip>
      </div>
      <div className="space-y-2">{children}</div>
    </div>
  );
}

function toCredentialPayload(
  values: Record<string, string>,
  authType: CredentialAuthType,
  oauthProvider: string | null | undefined,
  declaredFields: CredentialField[],
) {
  const payload: Record<string, string> = {};
  const add = (key: string, value: string | undefined) => {
    if (value?.trim()) payload[key] = value.trim();
  };

  if (oauthProvider && authType === "bearer") {
    add(providerKey(oauthProvider, "BEARER_TOKEN"), values.BEARER_TOKEN);
    add("BEARER_TOKEN", values.BEARER_TOKEN);
    return payload;
  }

  for (const field of declaredFields) {
    add(field.name, values[field.name]);
  }
  return payload;
}

function providerKey(provider: string, suffix: string) {
  return `${provider.replace(/[^a-zA-Z0-9]/g, "_").toUpperCase()}_${suffix}`;
}

function providerLabel(provider: string) {
  if (provider.toLowerCase() === "google") return "Google";
  if (provider.toLowerCase() === "github") return "GitHub";
  return provider;
}

function resolveReturnTo(
  returnTo: string | ((server: McpServer) => string),
  server: McpServer,
) {
  return typeof returnTo === "function" ? returnTo(server) : returnTo;
}

function errorMessage(err: unknown) {
  if (err instanceof Error) return err.message;
  return "Credential could not be saved.";
}

const bearerTokenFields: CredentialField[] = [
  {
    name: "BEARER_TOKEN",
    label: "Bearer token",
    type: "password",
    required: true,
  },
];
