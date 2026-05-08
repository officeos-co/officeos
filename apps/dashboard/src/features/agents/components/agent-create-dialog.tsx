"use client";

import { useState, type ReactNode } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  DatabaseIcon,
  ChevronDownIcon,
  ChevronRightIcon,
  MessageSquareIcon,
  MonitorIcon,
  PlugIcon,
  PlusIcon,
  RocketIcon,
  TerminalIcon,
} from "lucide-react";
import { getDialogWidthClassName } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import { HelpTooltip, WithTooltip } from "@/components/ui/help-tooltip";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useAnalytics } from "@/features/analytics";
import { useAgentToolCatalog, useCreateAgent } from "../api/useAgents";
import { useBrowserResources, useMemoryStores } from "../api/useAgentResources";
import { useChannelConnections } from "../api/useChannels";
import { useIntegrations } from "../api/useIntegrations";
import { useModels } from "../api/useModels";
import { ResourceAttachmentCard } from "./resource-attachment-card";
import type { Tool } from "@/features/agents/data/integrations";
import { getModelTooltip } from "@/features/agents/model-tooltips";
import { isDevelopment } from "@/lib/env";

type ToolPermission = "allow" | "deny";
type AgentResourceType = "browser" | "memory_store" | "channel" | "connector";
type AgentResource = {
  id: string;
  type: AgentResourceType;
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

const resourceConfig: Record<
  AgentResourceType,
  {
    title: string;
    selectorLabel: string;
    selectorPlaceholder: string;
    manageHref: string;
    manageLabel: string;
    icon: ReactNode;
  }
> = {
  browser: {
    title: "Browser",
    selectorLabel: "Browser",
    selectorPlaceholder: "Select a browser",
    manageHref: "/browser",
    manageLabel: "Manage browsers",
    icon: <MonitorIcon className="size-4" />,
  },
  memory_store: {
    title: "Memory Store",
    selectorLabel: "Memory store",
    selectorPlaceholder: "Select a memory store",
    manageHref: "/memory-stores",
    manageLabel: "Manage memory stores",
    icon: <DatabaseIcon className="size-4" />,
  },
  channel: {
    title: "Channel",
    selectorLabel: "Channel",
    selectorPlaceholder: "Select a channel",
    manageHref: "/channels",
    manageLabel: "Manage channels",
    icon: <MessageSquareIcon className="size-4" />,
  },
  connector: {
    title: "Connector",
    selectorLabel: "Connector",
    selectorPlaceholder: "Select a connector",
    manageHref: "/integrations",
    manageLabel: "Manage connectors",
    icon: <PlugIcon className="size-4" />,
  },
};

function PermissionCycleButton({
  value,
  onChange,
}: {
  value: ToolPermission;
  onChange: (permission: ToolPermission) => void;
}) {
  const cycle: ToolPermission[] = ["allow", "deny"];
  const tooltip =
    value === "allow"
      ? "Allowed means the agent may call this tool without an extra approval prompt."
      : "Deny means the tool is hidden from the agent and blocked at execution time.";

  function nextPermission() {
    onChange(cycle[(cycle.indexOf(value) + 1) % cycle.length]);
  }

  return (
    <WithTooltip tooltip={tooltip}>
      <span
        role="button"
        tabIndex={0}
        onClick={(event) => {
          event.stopPropagation();
          nextPermission();
        }}
        onKeyDown={(event) => {
          if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            event.stopPropagation();
            nextPermission();
          }
        }}
        className={`cursor-pointer whitespace-nowrap text-xs hover:underline ${permissionColors[value]}`}
      >
        {permissionLabels[value]}
      </span>
    </WithTooltip>
  );
}

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
  icon: ReactNode;
  tools: Tool[];
  permissions: Record<string, ToolPermission>;
  onToggle: (key: string, permission: ToolPermission) => void;
  groupPerm: ToolPermission;
  onGroupPerm: (permission: ToolPermission) => void;
  prefix: string;
}) {
  const [expanded, setExpanded] = useState(false);

  return (
    <div className="rounded-lg border border-border">
      <div className="flex items-center gap-3 px-4 py-3">
        <div className="flex size-8 shrink-0 items-center justify-center rounded-md bg-muted">
          {icon}
        </div>
        <div className="min-w-0 flex-1">
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
          className="flex w-full items-center gap-2 px-4 py-2.5 text-left transition-colors hover:bg-muted/50"
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
                className="flex items-center gap-4 border-t border-border px-4 py-2.5"
              >
                <code className="min-w-[100px] rounded bg-muted px-2 py-0.5 font-mono text-xs">
                  {tool.name}
                </code>
                <span className="flex-1 text-sm text-muted-foreground">
                  {tool.description}
                </span>
                <PermissionCycleButton
                  value={perm}
                  onChange={(permission) => onToggle(key, permission)}
                />
              </div>
            );
          })}
      </div>
    </div>
  );
}

export function AgentCreateDialog({
  open,
  onOpenChange,
  onCreated,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated: () => void;
}) {
  const router = useRouter();
  const { integrations } = useIntegrations();
  const { connections: channelConnections } = useChannelConnections();
  const { createAgent } = useCreateAgent();
  const { models, defaultModelId } = useModels();
  const { tools: toolCatalog } = useAgentToolCatalog();
  const { browserResources } = useBrowserResources();
  const { memoryStores } = useMemoryStores();
  const { trackAgentCreated } = useAnalytics();
  const [creating, setCreating] = useState(false);
  const [agentName, setAgentName] = useState("");
  const [prompt, setPrompt] = useState("");
  const [model, setModel] = useState<string | null>(null);
  const [toolPermissions, setToolPermissions] = useState<
    Record<string, ToolPermission>
  >({});
  const [groupPermissions, setGroupPermissions] = useState<
    Record<string, ToolPermission>
  >({});
  const [resources, setResources] = useState<AgentResource[]>([]);

  function addResource(type: AgentResourceType) {
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
  }

  function updateResource(
    id: string,
    patch: Partial<Omit<AgentResource, "id" | "type">>,
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

  function resetForm() {
    setAgentName("");
    setPrompt("");
    setModel(null);
    setToolPermissions({});
    setGroupPermissions({});
    setResources([]);
  }

  function setOpen(next: boolean) {
    if (creating) return;
    if (!next) resetForm();
    onOpenChange(next);
  }

  const selectedConnectorNames = new Set(
    resources
      .filter(
        (resource) => resource.type === "connector" && resource.resourceId,
      )
      .map((resource) => resource.resourceId),
  );
  const activeIntegrations = integrations.filter((integration) =>
    selectedConnectorNames.has(integration.name),
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
    model && models.some((modelOption) => modelOption.id === model)
      ? model
      : defaultModelId;
  const selectedModelInfo = models.find(
    (modelOption) => modelOption.id === selectedModel,
  );
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
  const connectorOptions = integrations
    .filter((integration) => integration.configured)
    .map((integration) => ({
      id: integration.name,
      label: integration.title,
    }));
  const resourcesValid = resources.every((resource) => resource.resourceId);
  const canCreate =
    Boolean(agentName.trim()) && Boolean(selectedModel) && resourcesValid;

  async function create() {
    if (!canCreate || creating) return;
    setCreating(true);

    try {
      const tpList: Array<{ tool: string; mode: "ALLOW" | "DENY" }> = [];
      for (const [key, mode] of Object.entries(toolPermissions)) {
        tpList.push({
          tool: key,
          mode: mode === "deny" ? "DENY" : "ALLOW",
        });
      }
      for (const [prefix, mode] of Object.entries(groupPermissions)) {
        if (mode === "deny") tpList.push({ tool: prefix, mode: "DENY" });
      }

      const startupPrompt = prompt.trim();
      const created = await createAgent({
        name: agentName.trim(),
        model: selectedModel,
        provider: selectedModelInfo?.provider ?? "anthropic",
        systemPrompt: startupPrompt,
        toolNames: Array.from(selectedConnectorNames),
        toolPermissions: tpList,
        channelSlugs: [],
        resources: resources
          .filter((resource) => resource.type !== "connector")
          .map((resource) => ({
            resourceType: resource.type,
            resourceId: resource.resourceId,
            accessMode: resource.accessMode,
            instructions: resource.instructions.trim() || null,
          })),
        bootstrapMessage: startupPrompt || undefined,
      });

      trackAgentCreated({
        agentName: agentName.trim(),
        provider: selectedModelInfo?.provider ?? "unknown",
        skillCount: selectedConnectorNames.size,
        allowSkills: Object.values(toolPermissions).filter(
          (permission) => permission === "allow",
        ).length,
        denySkills: Object.values(toolPermissions).filter(
          (permission) => permission === "deny",
        ).length,
      });
      onCreated();
      resetForm();
      onOpenChange(false);
      if (created?.id) router.push(`/agents/${created.id}`);
    } catch (error) {
      console.error("Failed to create agent", error);
    } finally {
      setCreating(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent
        className={getDialogWidthClassName(
          "thin",
          "flex max-h-[calc(100vh-3rem)] flex-col overflow-hidden p-6 sm:max-h-[calc(100vh-5rem)]",
        )}
      >
        <DialogHeader>
          <DialogTitle className="text-xl">Create agent</DialogTitle>
          <DialogDescription>
            Set up a managed agent with startup instructions and attached
            resources.
          </DialogDescription>
        </DialogHeader>

        <div className="min-h-0 flex-1 space-y-6 overflow-y-auto pr-1">
          <div className="grid gap-4 md:grid-cols-[minmax(0,1fr)_220px]">
            <div className="space-y-2">
              <Label htmlFor="agent-name">Name</Label>
              <Input
                id="agent-name"
                value={agentName}
                onChange={(event) => setAgentName(event.target.value)}
                placeholder="Research assistant"
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
                  className="flex items-center justify-center gap-2 rounded-md border border-dashed border-border px-3 py-2 text-sm text-muted-foreground transition-colors hover:border-foreground hover:text-foreground"
                >
                  <PlusIcon className="size-4" />
                  Add provider
                </Link>
              ) : (
                <Select
                  value={selectedModel}
                  onValueChange={(value) => {
                    if (value) setModel(value);
                  }}
                >
                  <SelectTrigger>
                    <SelectValue>
                      {selectedModelInfo?.displayName ?? selectedModel}
                    </SelectValue>
                  </SelectTrigger>
                  <SelectContent className="w-max min-w-(--anchor-width) max-w-[calc(100vw-2rem)]">
                    {models.map((modelOption) => (
                      <SelectItem
                        key={modelOption.id}
                        value={modelOption.id}
                        title={getModelTooltip(modelOption.id)}
                      >
                        {modelOption.displayName}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="startup-prompt">
              Startup prompt
              <HelpTooltip>
                This becomes the agent&apos;s standing instruction and is used
                to bootstrap the first run.
              </HelpTooltip>
            </Label>
            <Textarea
              id="startup-prompt"
              value={prompt}
              onChange={(event) => setPrompt(event.target.value)}
              placeholder="Describe what this agent should do..."
              rows={5}
            />
          </div>

          <Separator />

          <div className="space-y-3">
            <div>
              <Label>Resources</Label>
              <p className="text-xs text-muted-foreground">
                Attach connectors, channels, browsers, or memory stores to the
                agent.
              </p>
            </div>
            <div className="space-y-4">
              {resources.map((resource) => {
                const config = resourceConfig[resource.type];

                return (
                  <ResourceAttachmentCard
                    key={resource.id}
                    title={config.title}
                    icon={config.icon}
                    selectorLabel={config.selectorLabel}
                    selectorPlaceholder={config.selectorPlaceholder}
                    manageHref={config.manageHref}
                    manageLabel={config.manageLabel}
                    options={
                      resource.type === "browser"
                        ? browserOptions
                        : resource.type === "memory_store"
                          ? memoryStoreOptions
                          : resource.type === "channel"
                            ? channelOptions
                            : connectorOptions
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
            <DropdownMenu>
              <DropdownMenuTrigger
                render={<Button type="button" variant="outline" />}
              >
                <PlusIcon className="size-4" />
                Resource
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start">
                <DropdownMenuItem onClick={() => addResource("browser")}>
                  <MonitorIcon className="mr-2 size-4" />
                  {resourceConfig.browser.title}
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => addResource("memory_store")}>
                  <DatabaseIcon className="mr-2 size-4" />
                  {resourceConfig.memory_store.title}
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => addResource("channel")}>
                  <MessageSquareIcon className="mr-2 size-4" />
                  {resourceConfig.channel.title}
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => addResource("connector")}>
                  <PlugIcon className="mr-2 size-4" />
                  {resourceConfig.connector.title}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>

          <Separator />

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
              onToggle={(key, permission) =>
                setToolPermissions((prev) => ({
                  ...prev,
                  [key]: permission,
                }))
              }
              groupPerm={groupPermissions["builtin"] ?? "allow"}
              onGroupPerm={(permission) =>
                setGroupPermissions((prev) => ({
                  ...prev,
                  builtin: permission,
                }))
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
                onToggle={(key, permission) =>
                  setToolPermissions((prev) => ({
                    ...prev,
                    [key]: permission,
                  }))
                }
                groupPerm={groupPermissions["browser"] ?? "allow"}
                onGroupPerm={(permission) =>
                  setGroupPermissions((prev) => ({
                    ...prev,
                    browser: permission,
                  }))
                }
                prefix="browser"
              />
            )}
            {activeIntegrations.map((integration) => (
              <ToolPermissionSection
                key={integration.name}
                title={integration.title}
                subtitle={integration.name}
                icon={
                  <div
                    className="size-4 [&>svg]:size-4"
                    dangerouslySetInnerHTML={{ __html: integration.logo }}
                  />
                }
                tools={integration.tools}
                permissions={toolPermissions}
                onToggle={(key, permission) =>
                  setToolPermissions((prev) => ({
                    ...prev,
                    [key]: permission,
                  }))
                }
                groupPerm={groupPermissions[integration.name] ?? "allow"}
                onGroupPerm={(permission) =>
                  setGroupPermissions((prev) => ({
                    ...prev,
                    [integration.name]: permission,
                  }))
                }
                prefix={integration.name}
              />
            ))}
          </div>
        </div>

        <div className="flex items-center justify-end gap-2 border-t border-border pt-4">
          <Button
            type="button"
            variant="ghost"
            disabled={creating}
            onClick={() => setOpen(false)}
          >
            Cancel
          </Button>
          <Button
            type="button"
            disabled={!canCreate || creating}
            onClick={create}
          >
            <RocketIcon className="size-4" />
            {creating ? "Creating..." : "Create agent"}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
