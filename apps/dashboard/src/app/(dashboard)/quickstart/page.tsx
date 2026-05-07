"use client";

import { useState } from "react";
import { isDevelopment } from "@/lib/env";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { PageHeader } from "@/components/page-header";
import { PageContainer } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import { HelpTooltip, WithTooltip } from "@/components/ui/help-tooltip";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { type Tool } from "@/features/agents/data/integrations";
import {
  useIntegrations,
  useSetSkillCredentials,
  sortIntegrations,
  useCreateAgent,
  useModels,
  useAgentToolCatalog,
  CredentialDialog,
  IntegrationCard,
  ResourceAttachmentCard,
  useBrowserResources,
  useMemoryStores,
  useChannelConnections,
} from "@/features/agents";
import { useAnalytics } from "@/features/analytics";
import { getModelTooltip } from "@/features/agents/model-tooltips";
import {
  RocketIcon,
  ChevronDownIcon,
  ChevronRightIcon,
  TerminalIcon,
  MonitorIcon,
  AlertTriangleIcon,
  ExternalLinkIcon,
  PlusIcon,
} from "lucide-react";

/* ── Permission types ────────────────────────────────────── */

type ToolPermission = "allow" | "deny";
type QuickstartResourceType = "browser" | "memory_store" | "channel";
type QuickstartResource = {
  id: string;
  type: QuickstartResourceType;
  resourceId: string;
  accessMode: string;
  instructions: string;
};

const permissionLabels: Record<ToolPermission, string> = {
  allow: "Always allow",
  deny: "Deny",
};
const permissionColors: Record<ToolPermission, string> = {
  allow: "text-emerald-600",
  deny: "text-red-500",
};

function PermissionCycleButton({
  value,
  onChange,
}: {
  value: ToolPermission;
  onChange: (p: ToolPermission) => void;
}) {
  const cycle: ToolPermission[] = ["allow", "deny"];
  const tooltip =
    value === "allow"
      ? "Allowed means the agent may call this tool without an extra approval prompt."
      : "Deny means the tool is hidden from the agent and blocked at execution time.";
  return (
    <WithTooltip tooltip={tooltip}>
      <span
        role="button"
        tabIndex={0}
        onClick={(e) => {
          e.stopPropagation();
          onChange(cycle[(cycle.indexOf(value) + 1) % cycle.length]);
        }}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            e.stopPropagation();
            onChange(cycle[(cycle.indexOf(value) + 1) % cycle.length]);
          }
        }}
        className={`text-xs whitespace-nowrap hover:underline cursor-pointer ${permissionColors[value]}`}
      >
        {permissionLabels[value]}
      </span>
    </WithTooltip>
  );
}

function QuickstartIntegrationSkeleton() {
  return (
    <div className="flex flex-col gap-3 rounded-xl border border-border p-3">
      <div className="flex items-start gap-3">
        <Skeleton className="size-8 shrink-0 rounded-lg" />
        <div className="min-w-0 flex-1 space-y-2 pt-0.5">
          <Skeleton className="h-4 w-28" />
          <Skeleton className="h-3 w-20" />
        </div>
        <Skeleton className="size-4 rounded" />
      </div>
      <Skeleton className="h-3 w-full" />
      <Skeleton className="h-3 w-2/3" />
    </div>
  );
}

/* ── Tool permission section ─────────────────────────────── */

function ToolPermissionSection({
  title,
  subtitle,
  icon,
  tools,
  permissions,
  onToggle,
  groupPerm,
  onGroupPerm,
  prefix,
}: {
  title: string;
  subtitle?: string;
  icon: React.ReactNode;
  tools: Tool[];
  permissions: Record<string, ToolPermission>;
  onToggle: (key: string, p: ToolPermission) => void;
  groupPerm: ToolPermission;
  onGroupPerm: (p: ToolPermission) => void;
  prefix: string;
}) {
  const [expanded, setExpanded] = useState(true);
  return (
    <div className="rounded-xl border border-border">
      <div className="flex items-center gap-3 px-4 py-3">
        <div className="flex size-8 items-center justify-center rounded-lg bg-muted shrink-0">
          {icon}
        </div>
        <div className="flex-1 min-w-0">
          <div className="text-sm font-medium">{title}</div>
          {subtitle && (
            <div className="text-xs text-muted-foreground">{subtitle}</div>
          )}
        </div>
      </div>
      <div className="border-t border-border">
        <button
          type="button"
          onClick={() => setExpanded(!expanded)}
          className="flex w-full items-center gap-2 px-4 py-2.5 text-left hover:bg-muted/50 transition-colors"
        >
          {expanded ? (
            <ChevronDownIcon className="size-4 text-muted-foreground" />
          ) : (
            <ChevronRightIcon className="size-4 text-muted-foreground" />
          )}
          <span className="text-xs font-medium">Tool permissions</span>
          <HelpTooltip side="right">
            Group permissions apply to every tool in this section unless a
            specific tool has its own override.
          </HelpTooltip>
          <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] text-muted-foreground">
            {tools.length}
          </span>
          <span className="ml-auto">
            <PermissionCycleButton value={groupPerm} onChange={onGroupPerm} />
          </span>
        </button>
        {expanded &&
          tools.map((tool) => {
            const key = `${prefix}:${tool.name}`;
            const perm = permissions[key] ?? groupPerm;
            return (
              <div
                key={tool.name}
                className="flex items-center gap-4 px-4 py-2.5 border-t border-border"
              >
                <code className="rounded bg-muted px-2 py-0.5 font-mono text-xs min-w-[100px]">
                  {tool.name}
                </code>
                <span className="flex-1 text-sm text-muted-foreground">
                  {tool.description}
                </span>
                <PermissionCycleButton
                  value={perm}
                  onChange={(p) => onToggle(key, p)}
                />
              </div>
            );
          })}
      </div>
    </div>
  );
}

/* ── Page ─────────────────────────────────────────────────── */

export default function QuickstartPage() {
  const router = useRouter();
  const { integrations, loading: integrationsLoading } = useIntegrations();
  const { connections: channelConnections } = useChannelConnections();
  const { createAgent } = useCreateAgent();
  const { models, defaultModelId } = useModels();
  const { tools: toolCatalog } = useAgentToolCatalog();
  const { browserResources } = useBrowserResources();
  const { memoryStores } = useMemoryStores();
  const { trackAgentCreated } = useAnalytics();
  const setSkillCredentials = useSetSkillCredentials();
  const [configureSlug, setConfigureSlug] = useState<string | null>(null);
  const [launching, setLaunching] = useState(false);
  const [agentName, setAgentName] = useState("");
  const [prompt, setPrompt] = useState("");
  const [model, setModel] = useState<string | null>(null);
  const [selectedIntegrations, setSelectedIntegrations] = useState<Set<string>>(
    new Set(),
  );
  const [toolPermissions, setToolPermissions] = useState<
    Record<string, ToolPermission>
  >({});
  const [groupPermissions, setGroupPermissions] = useState<
    Record<string, ToolPermission>
  >({});
  const [resourcePickerOpen, setResourcePickerOpen] = useState(false);
  const [resources, setResources] = useState<QuickstartResource[]>([]);

  function addResource(type: QuickstartResourceType) {
    setResources((prev) => [
      ...prev,
      {
        id: crypto.randomUUID(),
        type,
        resourceId: "",
        accessMode: "read_write",
        instructions: "",
      },
    ]);
    setResourcePickerOpen(false);
  }

  function updateResource(
    id: string,
    patch: Partial<Omit<QuickstartResource, "id" | "type">>,
  ) {
    setResources((prev) =>
      prev.map((resource) =>
        resource.id === id ? { ...resource, ...patch } : resource,
      ),
    );
  }

  function removeResource(id: string) {
    setResources((prev) => prev.filter((resource) => resource.id !== id));
  }

  function toggleIntegration(slug: string) {
    setSelectedIntegrations((prev) => {
      const next = new Set(prev);
      if (next.has(slug)) next.delete(slug);
      else next.add(slug);
      return next;
    });
  }

  const sortedIntegrations = sortIntegrations(integrations);
  const activeIntegrations = integrations.filter((i) =>
    selectedIntegrations.has(i.name),
  );
  const backendBuiltInTools = toolCatalog
    .filter((tool) => tool.group === "builtin")
    .map((tool) => ({
      name: tool.permissionTool || tool.runtimeName,
      description: tool.description,
    }));
  const backendBrowserTools = toolCatalog
    .filter((tool) => tool.group === "browser")
    .map((tool) => ({
      name: tool.permissionTool || tool.runtimeName,
      description: tool.description,
    }));
  const selectedModel =
    model && models.some((m) => m.id === model) ? model : defaultModelId;
  const selectedModelInfo = models.find((m) => m.id === selectedModel);
  const browserOptions = browserResources.map((resource) => ({
    id: resource.id,
    label: resource.displayName,
  }));
  const memoryStoreOptions = memoryStores.map((store) => ({
    id: store.id,
    label: store.displayName,
  }));
  const channelOptions = channelConnections.map((connection) => ({
    id: connection.id,
    label: connection.displayName,
  }));
  const resourcesValid = resources.every((resource) => resource.resourceId);

  return (
    <>
      <PageHeader
        page="Quickstart"
        subtitle="Launch an agent with tools, resources, and starter instructions."
        width="wide"
        action={
          <Button
            size="sm"
            disabled={!agentName.trim() || !selectedModel || !resourcesValid || launching}
            onClick={async () => {
              if (launching) return;
              setLaunching(true);
              try {
                // Build the toolPermissions list: explicit per-tool overrides
                // plus group-level defaults that differ from the implicit
                // "allow" baseline. Keys are "prefix:toolname".
                const tpList: Array<{ tool: string; mode: "ALLOW" | "DENY" }> =
                  [];
                for (const [key, mode] of Object.entries(toolPermissions)) {
                  tpList.push({
                    tool: key,
                    mode: mode === "deny" ? "DENY" : "ALLOW",
                  });
                }
                for (const [prefix, mode] of Object.entries(groupPermissions)) {
                  if (mode === "deny")
                    tpList.push({ tool: prefix, mode: "DENY" });
                }

                const userPrompt = prompt.trim();
                const systemPrompt = userPrompt;

                const created = await createAgent({
                  name: agentName.trim(),
                  model: selectedModel,
                  provider: selectedModelInfo?.provider ?? "anthropic",
                  systemPrompt,
                  toolNames: Array.from(selectedIntegrations),
                  toolPermissions: tpList,
                  channelSlugs: [],
                  resources: resources.map((resource) => ({
                    resourceType: resource.type,
                    resourceId: resource.resourceId,
                    accessMode: resource.accessMode,
                    instructions: resource.instructions.trim() || null,
                  })),
                  bootstrapMessage: systemPrompt || undefined,
                });
                trackAgentCreated({
                  agentName: agentName.trim(),
                  provider: selectedModelInfo?.provider ?? "unknown",
                  skillCount: selectedIntegrations.size,
                  allowSkills: Object.values(toolPermissions).filter(
                    (p) => p === "allow",
                  ).length,
                  denySkills: Object.values(toolPermissions).filter(
                    (p) => p === "deny",
                  ).length,
                });
                if (created?.id) router.push(`/agents/${created.id}`);
              } catch (e) {
                console.error("Failed to create agent", e);
                setLaunching(false);
              }
            }}
          >
            <RocketIcon />
            {launching ? "Launching..." : "Launch agent"}
          </Button>
        }
      />
      <div className="flex flex-1 overflow-hidden">
        {/* Left: Agent configuration */}
        <div className="flex-1 overflow-y-auto pb-4">
          <PageContainer width="wide" className="space-y-6">
            {/* Name + Model */}
            <div className="grid gap-4 md:grid-cols-[minmax(0,480px)_200px]">
              <div className="space-y-2">
                <Label htmlFor="agent-name">Agent name</Label>
                <Input
                  id="agent-name"
                  value={agentName}
                  onChange={(e) => setAgentName(e.target.value)}
                  placeholder="e.g. Research Assistant"
                />
              </div>
              <div className="space-y-2">
                <Label>
                  Model
                  <HelpTooltip>
                    Auto is transparent smart routing and only appears when
                    Anthropic is configured. Other providers expose concrete
                    models directly.
                  </HelpTooltip>
                </Label>
                {models.length === 0 && isDevelopment() ? (
                  <Link
                    href="/providers"
                    className="flex items-center justify-center gap-2 rounded-md border border-dashed border-border px-3 py-2 text-sm text-muted-foreground hover:text-foreground hover:border-foreground transition-colors"
                  >
                    <PlusIcon className="size-4" />
                    Add provider
                  </Link>
                ) : (
                  <Select
                    value={selectedModel}
                    onValueChange={(v) => {
                      if (v) setModel(v);
                    }}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent className="w-max min-w-(--anchor-width) max-w-[calc(100vw-2rem)]">
                      {models.map((m) => (
                        <SelectItem
                          key={m.id}
                          value={m.id}
                          title={getModelTooltip(m.id)}
                        >
                          {m.displayName}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              </div>
            </div>

            {/* System prompt */}
            <div className="space-y-2">
              <Label htmlFor="prompt">
                System prompt
                <HelpTooltip>
                  This becomes the agent&apos;s standing instruction and is used
                  to bootstrap the first run.
                </HelpTooltip>
              </Label>
              <Textarea
                id="prompt"
                value={prompt}
                onChange={(e) => setPrompt(e.target.value)}
                placeholder="Describe what this agent should do..."
                rows={5}
              />
            </div>

            <Separator />

            {/* Integrations (API tools) */}
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <div>
                  <Label>
                    Integrations
                    <HelpTooltip>
                      Integrations are MCP servers. Adding one exposes its tools
                      to the agent, subject to the permissions below.
                    </HelpTooltip>
                  </Label>
                  <p className="text-xs text-muted-foreground">
                    API-based tools the agent can call.
                  </p>
                </div>
                <Link
                  href="/integrations"
                  className="text-xs text-muted-foreground hover:text-foreground"
                >
                  Manage integrations →
                </Link>
              </div>
              <div className="grid grid-cols-3 gap-2">
                {integrationsLoading && sortedIntegrations.length === 0
                  ? Array.from({ length: 20 }).map((_, index) => (
                      <QuickstartIntegrationSkeleton
                        key={`integration-skeleton-${index}`}
                      />
                    ))
                  : sortedIntegrations.map((i) => (
                      <IntegrationCard
                        key={i.name}
                        integration={i}
                        selected={selectedIntegrations.has(i.name)}
                        onConfigure={() => setConfigureSlug(i.name)}
                        onToggle={() => toggleIntegration(i.name)}
                      />
                    ))}
              </div>
            </div>

            {/* Resources */}
            <div className="space-y-3">
              <div>
                <Label>Resources</Label>
                <p className="text-xs text-muted-foreground">
                  Mount files, GitHub repositories, or memory stores into the session.
                </p>
              </div>
              <div className="space-y-4">
                {resources.map((resource) => {
                  const isBrowser = resource.type === "browser";
                  const isMemoryStore = resource.type === "memory_store";
                  return (
                    <ResourceAttachmentCard
                      key={resource.id}
                      title={
                        isBrowser
                          ? "Browser"
                          : isMemoryStore
                            ? "Memory Store"
                            : "Channel"
                      }
                      selectorLabel={
                        isBrowser
                          ? "Browser"
                          : isMemoryStore
                            ? "Memory store"
                            : "Channel"
                      }
                      selectorPlaceholder={
                        isBrowser
                          ? "Select a browser"
                          : isMemoryStore
                            ? "Select a memory store"
                            : "Select a channel"
                      }
                      manageHref={
                        isBrowser
                          ? "/browser"
                          : isMemoryStore
                            ? "/memory-stores"
                            : "/channels"
                      }
                      manageLabel={
                        isBrowser
                          ? "Manage browsers"
                          : isMemoryStore
                            ? "Manage memory stores"
                            : "Manage channels"
                      }
                      options={
                        isBrowser
                          ? browserOptions
                          : isMemoryStore
                            ? memoryStoreOptions
                            : channelOptions
                      }
                      value={resource.resourceId}
                      access={resource.accessMode}
                      instructions={resource.instructions}
                      onValueChange={(value) =>
                        updateResource(resource.id, { resourceId: value })
                      }
                      onAccessChange={(value) =>
                        updateResource(resource.id, { accessMode: value })
                      }
                      onInstructionsChange={(value) =>
                        updateResource(resource.id, { instructions: value })
                      }
                      onRemove={() => removeResource(resource.id)}
                    />
                  );
                })}
              </div>
              <Button
                type="button"
                variant="outline"
                onClick={() => setResourcePickerOpen(true)}
              >
                <PlusIcon className="size-4" />
                Resource
              </Button>
            </div>

            <Separator />

            <Dialog open={resourcePickerOpen} onOpenChange={setResourcePickerOpen}>
              <DialogContent className="max-w-sm">
                <DialogHeader>
                  <DialogTitle>Add resource</DialogTitle>
                  <DialogDescription>
                    Choose a resource type to mount into the first session.
                  </DialogDescription>
                </DialogHeader>
                <div className="grid gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    className="justify-start"
                    onClick={() => addResource("browser")}
                  >
                    <MonitorIcon className="size-4" />
                    Browser
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    className="justify-start"
                    onClick={() => addResource("memory_store")}
                  >
                    Memory Store
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    className="justify-start"
                    onClick={() => addResource("channel")}
                  >
                    Channel
                  </Button>
                </div>
              </DialogContent>
            </Dialog>

            {/* Unconfigured integrations warning */}
            {(() => {
              const unconfigured = activeIntegrations.filter(
                (i) => i.credentialFields.length > 0 && !i.configured,
              );
              if (unconfigured.length === 0) return null;
              return (
                <div className="rounded-xl border border-amber-300 bg-amber-50 p-4">
                  <div className="flex items-start gap-3">
                    <AlertTriangleIcon className="size-5 text-amber-600 shrink-0 mt-0.5" />
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-amber-800">
                        {unconfigured.length === 1
                          ? `${unconfigured[0].title} requires credentials before it can be used.`
                          : `${unconfigured.length} MCP servers require credentials before they can be used.`}
                      </p>
                      <p className="text-xs text-amber-700 mt-1">
                        The agent will not be able to use unconfigured
                        integrations. Set up credentials on the integration
                        page.
                      </p>
                      <div className="flex flex-wrap gap-2 mt-3">
                        {unconfigured.map((i) => (
                          <Link
                            key={i.name}
                            href={`/integrations/${i.name}`}
                            className="inline-flex items-center gap-1.5 rounded-md bg-amber-100 px-2.5 py-1 text-xs font-medium text-amber-800 hover:bg-amber-200 transition-colors"
                          >
                            <div
                              className="size-3.5 shrink-0 [&>svg]:size-3.5"
                              dangerouslySetInnerHTML={{ __html: i.logo }}
                            />
                            {i.title}
                            <ExternalLinkIcon className="size-3" />
                          </Link>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              );
            })()}

            {/* Tool permissions */}
            <div className="space-y-3">
              <Label>
                Tool permissions
                <HelpTooltip>
                  Permissions are stored in the backend and enforced when the
                  agent tries to call tools.
                </HelpTooltip>
              </Label>
              <ToolPermissionSection
                title="Built-in tools"
                subtitle="agent_toolset"
                icon={<TerminalIcon className="size-4" />}
                tools={backendBuiltInTools}
                permissions={toolPermissions}
                onToggle={(k, p) =>
                  setToolPermissions((prev) => ({ ...prev, [k]: p }))
                }
                groupPerm={groupPermissions["builtin"] ?? "allow"}
                onGroupPerm={(p) =>
                  setGroupPermissions((prev) => ({ ...prev, builtin: p }))
                }
                prefix="builtin"
              />
              {backendBrowserTools.length > 0 && (
                <ToolPermissionSection
                  title="Browser tools"
                  subtitle="internal_browser"
                  icon={<MonitorIcon className="size-4" />}
                  tools={backendBrowserTools}
                  permissions={toolPermissions}
                  onToggle={(k, p) =>
                    setToolPermissions((prev) => ({ ...prev, [k]: p }))
                  }
                  groupPerm={groupPermissions["browser"] ?? "allow"}
                  onGroupPerm={(p) =>
                    setGroupPermissions((prev) => ({ ...prev, browser: p }))
                  }
                  prefix="browser"
                />
              )}
              {activeIntegrations.map((i) => (
                <ToolPermissionSection
                  key={i.name}
                  title={i.title}
                  subtitle={i.name}
                  icon={
                    <div
                      className="size-4 [&>svg]:size-4"
                      dangerouslySetInnerHTML={{ __html: i.logo }}
                    />
                  }
                  tools={i.tools}
                  permissions={toolPermissions}
                  onToggle={(k, p) =>
                    setToolPermissions((prev) => ({ ...prev, [k]: p }))
                  }
                  groupPerm={groupPermissions[i.name] ?? "allow"}
                  onGroupPerm={(p) =>
                    setGroupPermissions((prev) => ({ ...prev, [i.name]: p }))
                  }
                  prefix={i.name}
                />
              ))}
            </div>

            <div className="h-8" />
          </PageContainer>
        </div>
      </div>

      {/* Credential dialog for inline configure */}
      {configureSlug &&
        (() => {
          const i = integrations.find((x) => x.name === configureSlug);
          if (!i) return null;
          return (
            <CredentialDialog
              open
              onOpenChange={(open) => {
                if (!open) setConfigureSlug(null);
              }}
              name={i.title}
              slug={i.name}
              logo={i.logo}
              credentials={i.credentialFields}
              onSave={(values) => {
                setSkillCredentials(i.name, values);
                setConfigureSlug(null);
              }}
            />
          );
        })()}

    </>
  );
}
