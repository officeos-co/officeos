"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { EmptyState } from "@/components/ui/empty-state";
import { HelpTooltip } from "@/components/ui/help-tooltip";
import { Label } from "@/components/ui/label";
import { SearchInput } from "@/components/ui/search-input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  ToolPermissionCard,
  type ToolPermission,
} from "@/components/permission-cards";
import {
  useIntegrations,
  sortIntegrations,
} from "@/features/agents/api/useIntegrations";
import { useAgentBindings } from "@/features/agents/api/useAgentBindings";
import {
  useAgentToolCatalog,
  useAgentToolPermissions,
  useSetAgentToolPermissions,
} from "@/features/agents/api/useAgents";
import type { McpServer } from "@/features/agents/data/integrations";
import {
  type AtlasConnection,
  parseJsonArray,
  useIntegrationConnections,
  useIntegrationIndexedRecords,
} from "@/features/atlas";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  AlertTriangleIcon,
  DatabaseIcon,
  ExternalLinkIcon,
  MonitorIcon,
  TerminalIcon,
} from "lucide-react";

function graphQLErrorMessage(error: unknown, fallback: string) {
  if (
    typeof error === "object" &&
    error !== null &&
    "graphQLErrors" in error &&
    Array.isArray(error.graphQLErrors)
  ) {
    const first = error.graphQLErrors[0] as { message?: unknown } | undefined;
    if (typeof first?.message === "string") return first.message;
  }
  return fallback;
}

export function AgentIntegrationsTab({ agentId }: { agentId: string }) {
  const { integrations } = useIntegrations();
  const { skillSlugs } = useAgentBindings(agentId);
  const { tools: toolCatalog } = useAgentToolCatalog(agentId);
  const { connections } = useIntegrationConnections({ pollInterval: 5000 });
  const { permissions: savedToolPermissions } =
    useAgentToolPermissions(agentId);
  const { setAgentToolPermissions } = useSetAgentToolPermissions();
  const [toolPermissions, setToolPermissions] = useState<
    Record<string, ToolPermission>
  >({});
  const [groupPermissions, setGroupPermissions] = useState<
    Record<string, ToolPermission>
  >({});
  const [view, setView] = useState<"tools" | "data">("tools");
  const lastSyncedPermissionsRef = useRef<string>("");

  useEffect(() => {
    const key = savedToolPermissions
      .map((p) => `${p.skillName}:${p.toolName}:${p.mode}`)
      .sort()
      .join("|");
    if (key === lastSyncedPermissionsRef.current) return;
    lastSyncedPermissionsRef.current = key;

    const nextTools: Record<string, ToolPermission> = {};
    const nextGroups: Record<string, ToolPermission> = {};
    for (const permission of savedToolPermissions) {
      const mode = permission.mode === "DENY" ? "deny" : "allow";
      if (permission.toolName) {
        nextTools[`${permission.skillName}:${permission.toolName}`] = mode;
      } else {
        nextGroups[permission.skillName] = mode;
      }
    }
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setToolPermissions(nextTools);
    setGroupPermissions(nextGroups);
  }, [savedToolPermissions]);

  async function persistToolPermissions(
    nextTools: Record<string, ToolPermission>,
    nextGroups: Record<string, ToolPermission>,
  ) {
    const entries: Array<{
      skill: string;
      tool: string;
      mode: "ALLOW" | "DENY";
    }> = [];

    for (const [key, permission] of Object.entries(nextTools)) {
      const [skill, ...toolParts] = key.split(":");
      entries.push({
        skill,
        tool: toolParts.join(":"),
        mode: permission === "deny" ? "DENY" : "ALLOW",
      });
    }

    for (const [skill, permission] of Object.entries(nextGroups)) {
      entries.push({
        skill,
        tool: "",
        mode: permission === "deny" ? "DENY" : "ALLOW",
      });
    }

    try {
      await setAgentToolPermissions(agentId, entries);
    } catch (error) {
      toast.error(graphQLErrorMessage(error, "Failed to save tool permissions"));
    }
  }

  function updateToolPermission(key: string, permission: ToolPermission) {
    const next = { ...toolPermissions, [key]: permission };
    setToolPermissions(next);
    void persistToolPermissions(next, groupPermissions);
  }

  function updateGroupPermission(skill: string, permission: ToolPermission) {
    const next = { ...groupPermissions, [skill]: permission };
    setGroupPermissions(next);
    void persistToolPermissions(toolPermissions, next);
  }

  function updateIndexedDataMode(integrationName: string, enabled: boolean) {
    updateToolPermission(
      `${integrationName}:__indexed_data`,
      enabled ? "allow" : "deny",
    );
  }

  const activeIntegrations = sortIntegrations(integrations).filter((i) =>
    skillSlugs.includes(i.name),
  );
  const indexableIntegrations = activeIntegrations.filter(
    (integration) => integration.isIndexable,
  );
  const visibleView = indexableIntegrations.length > 0 ? view : "tools";
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
  const unconfigured = activeIntegrations.filter(
    (i) => i.credentialFields.length > 0 && !i.configured,
  );

  return (
    <div className="space-y-6 pt-4">
      <div className="space-y-1">
        <Label>
          Permissions
          <HelpTooltip>
            Agent integrations are fixed after creation. This page controls
            which tools the already-attached MCP servers may expose.
          </HelpTooltip>
        </Label>
        <p className="text-xs text-muted-foreground">
          Tool access is mutable. Attached MCP servers and channel resources are
          managed outside the agent detail view.
        </p>
      </div>

      {unconfigured.length > 0 && (
        <div className="rounded-xl border border-amber-300 bg-amber-50 p-4">
          <div className="flex items-start gap-3">
            <AlertTriangleIcon className="mt-0.5 size-5 shrink-0 text-amber-600" />
            <div className="min-w-0 flex-1">
              <p className="text-sm font-medium text-amber-800">
                {unconfigured.length === 1
                  ? `${unconfigured[0].title} requires credentials before it can be used.`
                  : `${unconfigured.length} MCP servers require credentials before they can be used.`}
              </p>
              <p className="mt-1 text-xs text-amber-700">
                Configure credentials on the MCP server page. The agent binding
                itself cannot be changed here.
              </p>
              <div className="mt-3 flex flex-wrap gap-2">
                {unconfigured.map((integration) => (
                  <Link
                    key={integration.name}
                    href={`/integrations/${integration.name}`}
                    className="inline-flex items-center gap-1.5 rounded-md bg-amber-100 px-2.5 py-1 text-xs font-medium text-amber-800 transition-colors hover:bg-amber-200"
                  >
                    <span
                      className="size-3.5 shrink-0 [&>svg]:size-3.5"
                      dangerouslySetInnerHTML={{ __html: integration.logo }}
                    />
                    {integration.title}
                    <ExternalLinkIcon className="size-3" />
                  </Link>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}

      {indexableIntegrations.length > 0 && (
        <div className="inline-flex rounded-lg border border-border bg-muted/30 p-1">
          {[
            ["tools", "Tools"],
            ["data", "Data explorer"],
          ].map(([key, label]) => (
            <button
              key={key}
              type="button"
              onClick={() => setView(key as "tools" | "data")}
              className={`rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
                visibleView === key
                  ? "bg-background text-foreground shadow-sm"
                  : "text-muted-foreground hover:text-foreground"
              }`}
            >
              {label}
            </button>
          ))}
        </div>
      )}

      {visibleView === "data" ? (
        <AgentDataExplorer
          integrations={indexableIntegrations}
          connections={connections}
        />
      ) : (
        <div className="space-y-3">
          <ToolPermissionCard
            title="Built-in tools"
            subtitle="agent_toolset"
            icon={<TerminalIcon className="size-4" />}
            tools={backendBuiltInTools}
            permissions={toolPermissions}
            onToggle={updateToolPermission}
            groupPerm={groupPermissions["builtin"] ?? "allow"}
            onGroupPerm={(permission) =>
              updateGroupPermission("builtin", permission)
            }
            prefix="builtin"
          />
          {backendBrowserTools.length > 0 && (
            <ToolPermissionCard
              title="Browser tools"
              subtitle="internal_browser"
              icon={<MonitorIcon className="size-4" />}
              tools={backendBrowserTools}
              permissions={toolPermissions}
              onToggle={updateToolPermission}
              groupPerm={groupPermissions["browser"] ?? "allow"}
              onGroupPerm={(permission) =>
                updateGroupPermission("browser", permission)
              }
              prefix="browser"
            />
          )}
          {activeIntegrations.map((integration) => {
            const indexedDataEnabled =
              toolPermissions[`${integration.name}:__indexed_data`] === "allow";
            const indexState = getIndexState(integration, connections);

            return (
              <div key={integration.name} className="space-y-3">
                {integration.isIndexable ? (
                  <div className="rounded-lg border border-border p-4">
                    <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_220px] md:items-center">
                      <div className="min-w-0">
                        <div className="flex flex-wrap items-center gap-2">
                          <DatabaseIcon className="size-4 text-muted-foreground" />
                          <span className="text-sm font-medium">
                            {integration.title} indexed data
                          </span>
                          <span className="rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">
                            {indexState.label}
                          </span>
                        </div>
                        <p className="mt-1 text-xs text-muted-foreground">
                          {indexState.description}
                        </p>
                      </div>
                      <Select
                        value={indexedDataEnabled ? "tools_index" : "tools"}
                        onValueChange={(value) =>
                          updateIndexedDataMode(
                            integration.name,
                            value === "tools_index",
                          )
                        }
                      >
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="tools">MCP tools only</SelectItem>
                          <SelectItem value="tools_index">
                            MCP tools and indexed data
                          </SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>
                ) : null}
                <ToolPermissionCard
                  title={integration.title}
                  subtitle={integration.name}
                  icon={
                    <span
                      className="size-4 [&>svg]:size-4"
                      dangerouslySetInnerHTML={{ __html: integration.logo }}
                    />
                  }
                  tools={integration.tools}
                  permissions={toolPermissions}
                  onToggle={updateToolPermission}
                  groupPerm={groupPermissions[integration.name] ?? "allow"}
                  onGroupPerm={(permission) =>
                    updateGroupPermission(integration.name, permission)
                  }
                  prefix={integration.name}
                />
              </div>
            );
          })}
        </div>
      )}

      <div className="pb-8" />
    </div>
  );
}

function AgentDataExplorer({
  integrations,
  connections,
}: {
  integrations: McpServer[];
  connections: AtlasConnection[];
}) {
  const [connectionId, setConnectionId] = useState("");
  const [entity, setEntity] = useState("");
  const [search, setSearch] = useState("");

  const filteredConnections = useMemo(
    () =>
      connections.filter((connection) =>
        integrations.some((integration) =>
          providerMatches(
            connection.provider,
            integration.provider || integration.name,
          ),
        ),
      ),
    [connections, integrations],
  );
  const selectedConnection =
    filteredConnections.find((connection) => connection.id === connectionId) ??
    filteredConnections[0] ??
    null;
  const entityOptions = selectedConnection
    ? parseJsonArray(selectedConnection.entitiesJson)
    : [];
  const effectiveEntity = entityOptions.includes(entity)
    ? entity
    : entityOptions[0] ?? "";
  const { page, loading } = useIntegrationIndexedRecords({
    connectionId: selectedConnection?.id,
    entity: effectiveEntity,
    query: search,
    limit: 25,
  });

  if (filteredConnections.length === 0) {
    return (
      <EmptyState message="No indexed data sources are configured for the attached indexable integrations." />
    );
  }

  return (
    <section className="space-y-3">
      <div className="flex flex-col gap-2 lg:flex-row lg:items-center">
        <SearchInput
          placeholder="Search indexed data..."
          value={search}
          onChange={setSearch}
          className="lg:w-72"
        />
        <Select
          value={selectedConnection?.id ?? "none"}
          onValueChange={(value) => {
            setConnectionId(value);
            setEntity("");
          }}
        >
          <SelectTrigger className="lg:w-[240px]">
            <SelectValue>{selectedConnection?.displayName}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            {filteredConnections.map((connection) => (
              <SelectItem key={connection.id} value={connection.id}>
                {connection.displayName}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={effectiveEntity || "none"} onValueChange={setEntity}>
          <SelectTrigger className="lg:w-[180px]">
            <SelectValue>
              {effectiveEntity ? effectiveEntity.replaceAll("_", " ") : "Entity"}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {entityOptions.map((item) => (
              <SelectItem key={item} value={item}>
                {item.replaceAll("_", " ")}
              </SelectItem>
            ))}
            {entityOptions.length === 0 ? (
              <SelectItem value="none" disabled>
                No entities
              </SelectItem>
            ) : null}
          </SelectContent>
        </Select>
        {selectedConnection && effectiveEntity ? (
          <Link
            href={`/atlas/explorer?connectionId=${selectedConnection.id}&entity=${encodeURIComponent(effectiveEntity)}`}
            className="inline-flex items-center gap-1.5 text-sm text-muted-foreground underline underline-offset-4 hover:text-foreground"
          >
            Open full explorer
            <ExternalLinkIcon className="size-3.5" />
          </Link>
        ) : null}
      </div>

      <div className="overflow-hidden rounded-lg border border-border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Title</TableHead>
              <TableHead>Entity</TableHead>
              <TableHead>External ID</TableHead>
              <TableHead>Indexed</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {page.records.map((record) => (
              <TableRow key={record.id}>
                <TableCell className="max-w-[24rem]">
                  <div className="truncate font-medium">{record.title}</div>
                  <div className="truncate text-xs text-muted-foreground">
                    {record.searchText}
                  </div>
                </TableCell>
                <TableCell className="font-mono text-xs">
                  {record.entity}
                </TableCell>
                <TableCell className="max-w-[16rem] truncate font-mono text-xs">
                  {record.externalId}
                </TableCell>
                <TableCell>{formatDate(record.updatedAt)}</TableCell>
              </TableRow>
            ))}
            {!loading && page.records.length === 0 && (
              <TableRow>
                <TableCell colSpan={4} className="p-0">
                  <EmptyState message="No indexed records found." />
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
    </section>
  );
}

function getIndexState(
  integration: { provider?: string; name: string; entities: string[] },
  connections: Array<{
    provider: string;
    status: string;
    entityStatuses: Array<{ recordCount: number }>;
  }>,
) {
  const matching = connections.filter((connection) =>
    providerMatches(connection.provider, integration.provider ?? integration.name),
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
  const active = matching.find((connection) => connection.status === "Indexing");
  const ready = matching.find((connection) => connection.status === "Ready");
  const failed = matching.find((connection) => connection.status === "Failed");

  if (active) {
    return {
      label: "Indexing",
      description: `${records} indexed records are available while the index refreshes.`,
    };
  }
  if (ready || records > 0) {
    return {
      label: "Indexed",
      description: `${records} records indexed across ${matching.length} source${matching.length === 1 ? "" : "s"}.`,
    };
  }
  if (failed) {
    return {
      label: "Index failed",
      description: "The latest indexing run failed. Check the integration data explorer for details.",
    };
  }
  return {
    label: "Not indexed",
    description: `${integration.entities.length} indexable data type${integration.entities.length === 1 ? "" : "s"} are available.`,
  };
}

function providerMatches(left: string, right: string) {
  const normalize = (value: string) =>
    value.toLowerCase().replace(/[^a-z0-9]/g, "");
  return normalize(left) === normalize(right);
}

function formatDate(value: string) {
  try {
    return new Intl.DateTimeFormat(undefined, {
      month: "short",
      day: "numeric",
      hour: "numeric",
      minute: "2-digit",
    }).format(new Date(value));
  } catch {
    return "Unknown";
  }
}
