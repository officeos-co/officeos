"use client";

import { useMemo } from "react";
import Link from "next/link";
import { HelpTooltip } from "@/ui/help-tooltip";
import { Label } from "@/ui/label";
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
  const toolPermissions = useMemo<Record<string, ToolPermission>>(() => ({}), []);
  const noopPermissionChange = () => {};

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

      <div className="pb-8" />
    </div>
  );
}
