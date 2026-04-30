"use client"

import type { McpServer } from "../data/integrations"
import {
  SettingsIcon,
  PlugIcon,
  CheckIcon,
} from "lucide-react"

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
  const ready = !needsCreds || i.configured

  if (variant === "marketplace") {
    return (
      <div
        role="button"
        tabIndex={0}
        onClick={onClick}
        onKeyDown={(e) => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); onClick?.() } }}
        className="flex flex-col gap-3 rounded-xl border border-border p-4 text-left transition-colors hover:bg-muted/50 cursor-pointer"
      >
        <div className="flex items-start gap-3">
          <div className="size-8 shrink-0 [&>svg]:size-8" dangerouslySetInnerHTML={{ __html: i.logo }} />
          <div className="min-w-0 flex-1">
            <span className="font-medium text-sm">{i.title}</span>
          </div>
          <IntegrationAction
            integration={i}
            ready={ready}
            needsCreds={needsCreds}
            selected={false}
            onConfigure={onConfigure}
          />
        </div>
        <p className="text-sm line-clamp-2 text-muted-foreground">{i.description}</p>
        {i.category && (
          <div className="flex flex-wrap gap-1 mt-1.5">
            <span className="inline-flex items-center rounded-md bg-muted px-2 py-0.5 text-[11px] text-muted-foreground">
              {i.category}
            </span>
          </div>
        )}
      </div>
    )
  }

  // Quickstart variant — compact inline card
  return (
    <div
      className={`flex items-center gap-2.5 rounded-lg border px-3 py-2 text-sm transition-colors ${selected ? "border-primary bg-primary/5" : "border-border"}`}
    >
      <div className="size-[18px] shrink-0 [&>svg]:size-[18px]" dangerouslySetInnerHTML={{ __html: i.logo }} />
      <span className="flex-1 truncate">{i.title}</span>
      <IntegrationAction
        integration={i}
        ready={ready}
        needsCreds={needsCreds}
        selected={selected}
        onConfigure={onConfigure}
        onToggle={onToggle}
      />
    </div>
  )
}

function IntegrationAction({
  integration: i,
  ready,
  needsCreds,
  selected,
  onConfigure,
  onToggle,
}: {
  integration: McpServer
  ready: boolean
  needsCreds: boolean
  selected: boolean
  onConfigure?: () => void
  onToggle?: () => void
}) {
  if (needsCreds && !i.configured) {
    return (
      <button type="button" onClick={(e) => { e.stopPropagation(); onConfigure?.() }}
        className="flex items-center gap-1 rounded-md bg-amber-100 px-2 py-1 text-[11px] font-medium text-amber-700 hover:bg-amber-200 transition-colors shrink-0">
        <SettingsIcon className="size-3" />
        Configure
      </button>
    )
  }

  if (ready && !selected && onToggle) {
    return (
      <button type="button" onClick={(e) => { e.stopPropagation(); onToggle() }}
        className="flex items-center gap-1 rounded-md bg-emerald-100 px-2 py-1 text-[11px] font-medium text-emerald-700 hover:bg-emerald-200 transition-colors shrink-0">
        <PlugIcon className="size-3" />
        Use
      </button>
    )
  }

  if (ready && selected && onToggle) {
    return (
      <button type="button" onClick={(e) => { e.stopPropagation(); onToggle() }}
        className="flex items-center gap-1 rounded-md bg-primary/10 px-2 py-1 text-[11px] font-medium text-primary hover:bg-primary/20 transition-colors shrink-0">
        <CheckIcon className="size-3" />
        Added
      </button>
    )
  }

  // Available badge
  return (
    <span className="flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest text-emerald-700 shrink-0">
      <CheckIcon className="size-3" /> Available
    </span>
  )
}
