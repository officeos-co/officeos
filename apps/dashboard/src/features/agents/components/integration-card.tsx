"use client"

import type { Integration } from "../data/integrations"
import {
  DownloadIcon,
  SettingsIcon,
  PlugIcon,
  CheckIcon,
  LoaderIcon,
} from "lucide-react"

type IntegrationCardProps = {
  integration: Integration
  selected?: boolean
  /** "quickstart" shows Install → Configure → Use/Added flow.
   *  "marketplace" shows Install/Configure/Installed badge for the catalog. */
  variant?: "quickstart" | "marketplace"
  installing?: boolean
  onInstall?: () => void
  onConfigure?: () => void
  onToggle?: () => void
  onClick?: () => void
}

export function IntegrationCard({
  integration: i,
  selected = false,
  variant = "quickstart",
  installing = false,
  onInstall,
  onConfigure,
  onToggle,
  onClick,
}: IntegrationCardProps) {
  const needsCreds = i.credentialFields.length > 0
  const ready = i.installed && (!needsCreds || i.configured)

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
            <span className="font-medium text-sm">{i.name}</span>
          </div>
          <IntegrationAction
            integration={i}
            ready={ready}
            needsCreds={needsCreds}
            selected={false}
            installing={installing}
            onInstall={onInstall}
            onConfigure={onConfigure}
          />
        </div>
        <p className="text-sm line-clamp-2 text-muted-foreground">{i.description}</p>
        {i.categories.length > 0 && (
          <div className="flex flex-wrap gap-1 mt-1.5">
            {i.categories.map((cat) => (
              <span key={cat} className="inline-flex items-center rounded-md bg-muted px-2 py-0.5 text-[11px] text-muted-foreground">
                {cat}
              </span>
            ))}
          </div>
        )}
        <div className="flex items-center gap-3 text-xs text-muted-foreground">
          <span>{i.tools.length} tools</span>
          <span>·</span>
          <span className="flex items-center gap-1">
            <svg className="size-3" fill="currentColor" viewBox="0 0 24 24"><path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z" /></svg>
            {i.likes}
          </span>
        </div>
      </div>
    )
  }

  // Quickstart variant — compact inline card
  return (
    <div
      className={`flex items-center gap-2.5 rounded-lg border px-3 py-2 text-sm transition-colors ${selected ? "border-primary bg-primary/5" : "border-border"}`}
    >
      <div className="size-[18px] shrink-0 [&>svg]:size-[18px]" dangerouslySetInnerHTML={{ __html: i.logo }} />
      <span className="flex-1 truncate">{i.name}</span>
      <IntegrationAction
        integration={i}
        ready={ready}
        needsCreds={needsCreds}
        selected={selected}
        installing={installing}
        onInstall={onInstall}
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
  installing,
  onInstall,
  onConfigure,
  onToggle,
}: {
  integration: Integration
  ready: boolean
  needsCreds: boolean
  selected: boolean
  installing?: boolean
  onInstall?: () => void
  onConfigure?: () => void
  onToggle?: () => void
}) {
  if (!i.installed) {
    return (
      <button type="button" disabled={installing} onClick={(e) => { e.stopPropagation(); onInstall?.() }}
        className="flex items-center gap-1 rounded-md bg-muted px-2 py-1 text-[11px] font-medium text-foreground hover:bg-muted/70 transition-colors shrink-0 disabled:opacity-50">
        {installing ? <LoaderIcon className="size-3 animate-spin" /> : <DownloadIcon className="size-3" />}
        {installing ? "Installing…" : "Install"}
      </button>
    )
  }

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

  // Marketplace: installed + configured = badge only
  return (
    <span className="flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest text-emerald-700 shrink-0">
      <CheckIcon className="size-3" /> Installed
    </span>
  )
}
