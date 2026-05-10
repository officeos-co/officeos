"use client"

import type { McpServer } from "../data/integrations"
import { WithTooltip } from "@/ui/help-tooltip"
import {
  SettingsIcon,
  PlugIcon,
  CheckIcon,
} from "lucide-react"
import { CatalogCard, CatalogMeta } from "./catalog-card"

type IntegrationCardProps = {
  integration: McpServer
  selected?: boolean
  variant?: "quickstart" | "marketplace"
  onConfigure?: () => void
  onToggle?: () => void
  onClick?: () => void
}

export function IntegrationCard({
  integration: i,
  selected = false,
  variant = "quickstart",
  onConfigure,
  onToggle,
  onClick,
}: IntegrationCardProps) {
  const needsCreds = i.credentialFields.length > 0
  const needsOAuth = Boolean(i.oauthProvider)
  const needsSetup = needsCreds || needsOAuth
  const ready = !needsSetup || i.configured

  if (variant === "marketplace") {
    return (
      <CatalogCard
        logo={i.logo}
        title={i.title}
        subtitle={i.subtitle}
        description={i.description}
        onClick={onClick}
        action={
          <IntegrationAction
            integration={i}
            ready={ready}
            needsSetup={needsSetup}
            selected={false}
            onConfigure={onConfigure}
          />
        }
        meta={
          <>
            {i.category && <CatalogMeta>{i.category}</CatalogMeta>}
            {i.tools.length > 0 && <CatalogMeta>{i.tools.length} tools</CatalogMeta>}
          </>
        }
      />
    )
  }

  // Quickstart variant — compact inline card
  return (
    <CatalogCard
      variant="compact"
      logo={i.logo}
      title={i.title}
      selected={selected}
      action={
        <IntegrationAction
          integration={i}
          ready={ready}
          needsSetup={needsSetup}
          selected={selected}
          onConfigure={onConfigure}
          onToggle={onToggle}
        />
      }
    />
  )
}

function IntegrationAction({
  integration: i,
  ready,
  needsSetup,
  selected,
  onConfigure,
  onToggle,
}: {
  integration: McpServer
  ready: boolean
  needsSetup: boolean
  selected: boolean
  onConfigure?: () => void
  onToggle?: () => void
}) {
  if (needsSetup && !i.configured) {
    return (
      <WithTooltip
        tooltip={
          i.oauthProvider
            ? "Connect the OAuth account before agents can call this MCP server."
            : "Add the required credentials before agents can call this MCP server."
        }
      >
        <button type="button" onClick={(e) => { e.stopPropagation(); onConfigure?.() }}
          className="flex shrink-0 items-center gap-1 rounded-md bg-amber-100 px-2 py-1 text-[11px] font-medium text-amber-800 transition-colors hover:bg-amber-200">
          <SettingsIcon className="size-3" />
          {i.oauthProvider ? "Connect" : "Configure"}
        </button>
      </WithTooltip>
    )
  }

  if (ready && !selected && onToggle) {
    return (
      <WithTooltip tooltip="Enable this MCP server for the agent. Tool-level access can still be narrowed in permissions below.">
        <button type="button" onClick={(e) => { e.stopPropagation(); onToggle() }}
          className="flex shrink-0 items-center gap-1 rounded-md bg-emerald-100 px-2 py-1 text-[11px] font-medium text-emerald-800 transition-colors hover:bg-emerald-200">
          <PlugIcon className="size-3" />
          Use
        </button>
      </WithTooltip>
    )
  }

  if (ready && selected && onToggle) {
    return (
      <WithTooltip tooltip="This MCP server is enabled for the agent. Click to remove it.">
        <button type="button" onClick={(e) => { e.stopPropagation(); onToggle() }}
          className="flex shrink-0 items-center gap-1 rounded-md bg-primary/10 px-2 py-1 text-[11px] font-medium text-primary transition-colors hover:bg-primary/20">
          <CheckIcon className="size-3" />
          Added
        </button>
      </WithTooltip>
    )
  }

  // Available badge
  return (
    <WithTooltip tooltip="This MCP server is configured and available to add to agents.">
      <span className="flex shrink-0 items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest text-emerald-800">
        <CheckIcon className="size-3" /> Available
      </span>
    </WithTooltip>
  )
}
