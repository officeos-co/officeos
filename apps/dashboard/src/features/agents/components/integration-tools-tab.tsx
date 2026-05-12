"use client";

import { useState } from "react";
import type { Dispatch, SetStateAction } from "react";
import { cn } from "@/lib/utils";
import { PermissionModeSelect } from "@/ui/permission-mode-select";
import type { PermissionMode } from "@/ui/permission-mode-select";
import type { McpServer } from "../data/integrations";
import { ChevronDownIcon, ChevronRightIcon } from "lucide-react";

export function IntegrationToolsTab({
  integration,
}: {
  integration: McpServer;
}) {
  const [toolPolicies, setToolPolicies] = useState<Record<string, PermissionMode>>({});
  const [directToolsExpanded, setDirectToolsExpanded] = useState(true);
  const directTools = integration.tools.map((tool) => ({
    key: tool.name,
    description: tool.description,
  }));

  if (directTools.length === 0) return null;

  return (
    <div className="space-y-3 pt-4">
      <ToolGroup
        title="MCP tools"
        description="MCP server tool catalog for integration calls."
        status="Available"
        tools={directTools}
        expanded={directToolsExpanded}
        onExpandedChange={setDirectToolsExpanded}
        disabled={false}
        policies={toolPolicies}
        onPolicyChange={setToolPolicies}
      />
    </div>
  );
}

function ToolGroup({
  title,
  description,
  status,
  tools,
  expanded,
  onExpandedChange,
  disabled,
  policies,
  onPolicyChange,
}: {
  title: string;
  description: string;
  status: string;
  tools: Array<{ key: string; description: string }>;
  expanded: boolean;
  onExpandedChange: (expanded: boolean) => void;
  disabled: boolean;
  policies: Record<string, PermissionMode>;
  onPolicyChange: Dispatch<SetStateAction<Record<string, PermissionMode>>>;
}) {
  if (tools.length === 0) return null;

  return (
    <div className={cn("rounded-lg border border-border bg-card", disabled && "opacity-50")}>
      <button
        type="button"
        onClick={() => onExpandedChange(!expanded)}
        className={cn(
          "flex w-full items-center gap-3 px-4 py-3 text-left transition-colors hover:bg-muted/50",
          expanded && "border-b border-border",
        )}
      >
        {expanded ? (
          <ChevronDownIcon className="size-4 text-muted-foreground" />
        ) : (
          <ChevronRightIcon className="size-4 text-muted-foreground" />
        )}
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm font-medium">{title}</span>
            <span className="rounded-full bg-muted px-1.5 py-0.5 text-xs text-muted-foreground">
              {tools.length}
            </span>
            <span className="rounded-full border border-border px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-widest text-muted-foreground">
              {status}
            </span>
          </div>
          <p className="mt-1 text-xs text-muted-foreground">
            {description}
          </p>
        </div>
      </button>
      {expanded &&
        tools.map((tool, idx) => (
          <div
            key={tool.key}
            className={cn(
              "grid gap-3 py-2.5 pl-11 pr-4 md:grid-cols-[minmax(0,1fr)_auto] md:items-center",
              idx < tools.length - 1 && "border-b border-border",
            )}
          >
            <p className="min-w-0 truncate text-sm text-foreground/70">
              {tool.description}
            </p>
            <div onClick={(event) => event.stopPropagation()}>
              <PermissionModeSelect
                value={policies[tool.key] ?? "allow"}
                disabled={disabled}
                onChange={(value) =>
                  onPolicyChange((current) => ({
                    ...current,
                    [tool.key]: value,
                  }))
                }
              />
            </div>
          </div>
        ))}
    </div>
  );
}
