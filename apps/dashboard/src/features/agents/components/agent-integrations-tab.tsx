"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { EmptyState } from "@/ui/empty-state";
import { HelpTooltip } from "@/ui/help-tooltip";
import { Label } from "@/ui/label";
import { SearchInput } from "@/ui/search-input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/ui/select";
import {
  ToolPermissionCard,
  type ToolPermission,
} from "./permission-cards";
import {
  useIntegrations,
  sortIntegrations,
} from "../api/useIntegrations";
import { useAgentBindings } from "../api/useAgentBindings";
import { useAgentToolCatalog } from "../api/useAgents";
import type { McpServer } from "../data/integrations";
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
} from "@/ui/table";
import {
  AlertTriangleIcon,
  ExternalLinkIcon,
  MonitorIcon,
  TerminalIcon,
} from "lucide-react";

export function AgentIntegrationsTab({ agentId }: { agentId: string }) {
  const { integrations } = useIntegrations();
  const { skillSlugs } = useAgentBindings(agentId);
  const { tools: toolCatalog } = useAgentToolCatalog(agentId);
  const { connections } = useIntegrationConnections({ pollInterval: 5000 });
  const [view, setView] = useState<"tools" | "data">("tools");
  const toolPermissions = useMemo<Record<string, ToolPermission>>(() => ({}), []);
  const noopPermissionChange = () => {};

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
            Agent integrations and tool access are fixed when the agent is
            created.
          </HelpTooltip>
        </Label>
        <p className="text-xs text-muted-foreground">
          This view shows the backend-owned tool catalog exposed to this agent.
          Create a new agent to change integrations or tool policy.
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
            onToggle={noopPermissionChange}
            groupPerm="allow"
            onGroupPerm={noopPermissionChange}
            prefix="builtin"
            disabled
          />
          {backendBrowserTools.length > 0 && (
            <ToolPermissionCard
              title="Browser tools"
              subtitle="internal_browser"
              icon={<MonitorIcon className="size-4" />}
              tools={backendBrowserTools}
              permissions={toolPermissions}
              onToggle={noopPermissionChange}
              groupPerm="allow"
              onGroupPerm={noopPermissionChange}
              prefix="browser"
              disabled
            />
          )}
          {activeIntegrations.map((integration) => {
            return (
              <div key={integration.name} className="space-y-3">
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
                  onToggle={noopPermissionChange}
                  groupPerm="allow"
                  onGroupPerm={noopPermissionChange}
                  prefix={integration.name}
                  disabled
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
            setConnectionId(value ?? "");
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
        <Select
          value={effectiveEntity || "none"}
          onValueChange={(value) => setEntity(value ?? "")}
        >
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
