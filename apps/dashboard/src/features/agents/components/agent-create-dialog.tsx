"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  DatabaseIcon,
  MessageSquareIcon,
  MonitorIcon,
  PlugIcon,
  PlusIcon,
  RocketIcon,
} from "lucide-react";
import { getDialogWidthClassName } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import { HelpTooltip } from "@/components/ui/help-tooltip";
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
import { useCreateAgent } from "../api/useAgents";
import { useBrowserResources, useMemoryStores } from "../api/useAgentResources";
import { useChannelConnections } from "../api/useChannels";
import { useIntegrations } from "../api/useIntegrations";
import { useModels } from "../api/useModels";
import { ResourceAttachmentCard } from "./resource-attachment-card";
import { getModelTooltip } from "@/features/agents/model-tooltips";
import { isDevelopment } from "@/lib/env";

type AgentResourceType = "browser" | "memory_store" | "channel" | "connector";
type AgentResource = {
  id: string;
  type: AgentResourceType;
  resourceId: string;
  instructions: string;
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
  const { browserResources } = useBrowserResources();
  const { memoryStores } = useMemoryStores();
  const { trackAgentCreated } = useAnalytics();
  const [creating, setCreating] = useState(false);
  const [agentName, setAgentName] = useState("");
  const [prompt, setPrompt] = useState("");
  const [model, setModel] = useState<string | null>(null);
  const [resources, setResources] = useState<AgentResource[]>([]);
  const shouldScrollToAddResourceRef = useRef(false);
  const addResourceControlRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!shouldScrollToAddResourceRef.current) return;
    if (!addResourceControlRef.current) return;

    shouldScrollToAddResourceRef.current = false;
    addResourceControlRef.current.scrollIntoView({
      behavior: "smooth",
      block: "end",
    });
  }, [resources]);

  function addResource(type: AgentResourceType) {
    const id = crypto.randomUUID();
    shouldScrollToAddResourceRef.current = true;

    setResources((prev) => [
      ...prev,
      {
        id,
        type,
        resourceId: "",
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
      const startupPrompt = prompt.trim();
      const created = await createAgent({
        name: agentName.trim(),
        model: selectedModel,
        provider: selectedModelInfo?.provider ?? "anthropic",
        systemPrompt: startupPrompt,
        toolNames: Array.from(selectedConnectorNames),
        toolPermissions: [],
        channelSlugs: [],
        resources: resources
          .filter((resource) => resource.type !== "connector")
          .map((resource) => ({
            resourceType: resource.type,
            resourceId: resource.resourceId,
            instructions: resource.instructions.trim() || null,
          })),
        bootstrapMessage: startupPrompt || undefined,
      });

      trackAgentCreated({
        agentName: agentName.trim(),
        provider: selectedModelInfo?.provider ?? "unknown",
        skillCount: selectedConnectorNames.size,
        allowSkills: 0,
        denySkills: 0,
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
          "flex max-h-[calc(100vh-3rem)] flex-col gap-0 overflow-hidden p-6 sm:max-h-[calc(100vh-5rem)]",
        )}
      >
        <div className="min-h-0 flex-1 space-y-6 overflow-y-auto pr-1">
          <DialogHeader>
            <DialogTitle className="text-xl">Create agent</DialogTitle>
            <DialogDescription>
              Set up a managed agent with startup instructions and attached
              resources.
            </DialogDescription>
          </DialogHeader>

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
                  target="_blank"
                  rel="noopener noreferrer"
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

          <div className="space-y-3 pb-6">
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
                  <div key={resource.id}>
                    <ResourceAttachmentCard
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
                      instructions={resource.instructions}
                      onValueChange={(value) =>
                        updateResource(resource.id, { resourceId: value })
                      }
                      onInstructionsChange={(value) =>
                        updateResource(resource.id, { instructions: value })
                      }
                      onRemove={() => removeResource(resource.id)}
                    />
                  </div>
                );
              })}
            </div>
            <div ref={addResourceControlRef}>
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
          </div>
        </div>

        <div className="border-t border-border">
          <div className="flex items-center justify-end gap-2 pt-4">
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
        </div>
      </DialogContent>
    </Dialog>
  );
}
