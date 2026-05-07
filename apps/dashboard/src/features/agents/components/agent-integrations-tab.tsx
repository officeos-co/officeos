"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { HelpTooltip } from "@/components/ui/help-tooltip";
import { Label } from "@/components/ui/label";
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
import {
  AlertTriangleIcon,
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
  const { permissions: savedToolPermissions } =
    useAgentToolPermissions(agentId);
  const { setAgentToolPermissions } = useSetAgentToolPermissions();
  const [toolPermissions, setToolPermissions] = useState<
    Record<string, ToolPermission>
  >({});
  const [groupPermissions, setGroupPermissions] = useState<
    Record<string, ToolPermission>
  >({});
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

  const activeIntegrations = sortIntegrations(integrations).filter((i) =>
    skillSlugs.includes(i.name),
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
        {activeIntegrations.map((integration) => (
          <ToolPermissionCard
            key={integration.name}
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
        ))}
      </div>

      <div className="pb-8" />
    </div>
  );
}
