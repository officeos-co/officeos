"use client"

import { useState } from "react"
import Image from "next/image"
import { HelpTooltip, WithTooltip } from "@/components/ui/help-tooltip"
import type { Tool } from "@/features/agents/data/integrations"
import type { Channel, ChannelPermissions } from "@/features/agents/data/channels"
import {
  ChevronDownIcon,
  ChevronRightIcon,
} from "lucide-react"

/* ── Permission types ────────────────────────────────────── */

export type ToolPermission = "allow" | "deny"

const permissionLabels: Record<ToolPermission, string> = { allow: "Always allow", deny: "Deny" }
const permissionColors: Record<ToolPermission, string> = { allow: "text-emerald-600", deny: "text-red-500" }

export function PermissionCycleButton({ value, onChange }: { value: ToolPermission; onChange: (p: ToolPermission) => void }) {
  const cycle: ToolPermission[] = ["allow", "deny"]
  const tooltip = value === "allow"
    ? "Allowed means the agent may call this tool without an extra approval prompt."
    : "Deny means the tool is hidden from the agent and blocked at execution time."
  return (
    <WithTooltip tooltip={tooltip}>
      <span
        role="button"
        tabIndex={0}
        onClick={(e) => { e.stopPropagation(); onChange(cycle[(cycle.indexOf(value) + 1) % cycle.length]) }}
        onKeyDown={(e) => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); e.stopPropagation(); onChange(cycle[(cycle.indexOf(value) + 1) % cycle.length]) } }}
        className={`text-xs whitespace-nowrap hover:underline cursor-pointer ${permissionColors[value]}`}
      >
        {permissionLabels[value]}
      </span>
    </WithTooltip>
  )
}

/* ── Tool permission card ────────────────────────────────── */

export function ToolPermissionCard({
  title, subtitle, icon, tools, permissions, onToggle, groupPerm, onGroupPerm, prefix,
}: {
  title: string; subtitle?: string; icon: React.ReactNode; tools: Tool[]
  permissions: Record<string, ToolPermission>; onToggle: (key: string, p: ToolPermission) => void
  groupPerm: ToolPermission; onGroupPerm: (p: ToolPermission) => void; prefix: string
}) {
  const [expanded, setExpanded] = useState(true)
  return (
    <div className="rounded-xl border border-border">
      <div className="flex items-center gap-3 px-4 py-3">
        <div className="flex size-8 items-center justify-center rounded-lg bg-muted shrink-0">{icon}</div>
        <div className="flex-1 min-w-0">
          <div className="text-sm font-medium">{title}</div>
          {subtitle && <div className="text-xs text-muted-foreground">{subtitle}</div>}
        </div>
      </div>
      <div className="border-t border-border">
        <button type="button" onClick={() => setExpanded(!expanded)} className="flex w-full items-center gap-2 px-4 py-2.5 text-left hover:bg-muted/50 transition-colors">
          {expanded ? <ChevronDownIcon className="size-4 text-muted-foreground" /> : <ChevronRightIcon className="size-4 text-muted-foreground" />}
          <span className="text-xs font-medium">Tool permissions</span>
          <HelpTooltip side="right">Group permissions apply to every tool in this section unless a specific tool has its own override.</HelpTooltip>
          <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] text-muted-foreground">{tools.length}</span>
          <span className="ml-auto"><PermissionCycleButton value={groupPerm} onChange={onGroupPerm} /></span>
        </button>
        {expanded && tools.map((tool) => {
          const key = `${prefix}:${tool.name}`
          const perm = permissions[key] ?? groupPerm
          return (
            <div key={tool.name} className="flex items-center gap-4 px-4 py-2.5 border-t border-border">
              <code className="rounded bg-muted px-2 py-0.5 font-mono text-xs min-w-[100px]">{tool.name}</code>
              <span className="flex-1 text-sm text-muted-foreground">{tool.description}</span>
              <PermissionCycleButton value={perm} onChange={(p) => onToggle(key, p)} />
            </div>
          )
        })}
      </div>
    </div>
  )
}

/* ── Channel permission cycle button (allow/ask/deny) ────── */

type ChannelPerm = "allow" | "ask" | "deny"
const channelPermLabels: Record<ChannelPerm, string> = { allow: "Allow", ask: "Ask", deny: "Deny" }
const channelPermColors: Record<ChannelPerm, string> = { allow: "text-emerald-600", ask: "text-muted-foreground", deny: "text-red-500" }

function ChannelPermCycleButton({ value, onChange }: { value: ChannelPerm; onChange: (p: ChannelPerm) => void }) {
  const cycle: ChannelPerm[] = ["allow", "ask", "deny"]
  const tooltip = value === "allow"
    ? "Allow means the agent can perform this channel action without asking."
    : value === "ask"
      ? "Ask means the action should require human approval before it runs."
      : "Deny means this channel action is blocked."
  return (
    <WithTooltip tooltip={tooltip}>
      <span
        role="button"
        tabIndex={0}
        onClick={(e) => { e.stopPropagation(); onChange(cycle[(cycle.indexOf(value) + 1) % cycle.length]) }}
        onKeyDown={(e) => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); e.stopPropagation(); onChange(cycle[(cycle.indexOf(value) + 1) % cycle.length]) } }}
        className={`text-xs whitespace-nowrap hover:underline cursor-pointer ${channelPermColors[value]}`}
      >
        {channelPermLabels[value]}
      </span>
    </WithTooltip>
  )
}

/* ── Channel permission card ─────────────────────────────── */

export function ChannelPermissionCard({
  channel, perms, onChange,
}: {
  channel: Channel; perms: ChannelPermissions; onChange: (p: ChannelPermissions) => void
}) {
  const [expanded, setExpanded] = useState(true)

  const channelPerms: { key: keyof ChannelPermissions; label: string; desc: string }[] = [
    { key: "receive", label: "Receive messages", desc: "Inbound messages from users to the agent" },
    { key: "send", label: "Send replies", desc: "Agent responds in the same conversation" },
    { key: "initiate", label: "Initiate conversations", desc: "Agent starts new conversations proactively" },
  ]

  return (
    <div className="rounded-xl border border-border">
      <div className="flex items-center gap-3 px-4 py-3">
        <div className="flex size-8 items-center justify-center rounded-lg bg-muted shrink-0">
          {channel.logo.startsWith("<") ? (
            <span className="size-[18px]" dangerouslySetInnerHTML={{ __html: channel.logo }} />
          ) : (
            <Image src={channel.logo} alt={channel.name} width={18} height={18} />
          )}
        </div>
        <div className="flex-1 min-w-0">
          <div className="text-sm font-medium">{channel.name}</div>
          <div className="text-xs text-muted-foreground">{channel.description}</div>
        </div>
      </div>
      <div className="border-t border-border">
        <button type="button" onClick={() => setExpanded(!expanded)} className="flex w-full items-center gap-2 px-4 py-2.5 text-left hover:bg-muted/50 transition-colors">
          {expanded ? <ChevronDownIcon className="size-4 text-muted-foreground" /> : <ChevronRightIcon className="size-4 text-muted-foreground" />}
          <span className="text-xs font-medium">Channel permissions</span>
          <HelpTooltip side="right">These settings control whether the agent may receive messages, reply, or initiate conversations through this channel.</HelpTooltip>
          <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] text-muted-foreground">3</span>
        </button>
        {expanded && channelPerms.map((cp) => (
          <div key={cp.key} className="flex items-center justify-between px-4 py-2.5 border-t border-border">
            <div>
              <div className="text-sm">{cp.label}</div>
              <div className="text-xs text-muted-foreground">{cp.desc}</div>
            </div>
            <ChannelPermCycleButton
              value={perms[cp.key]}
              onChange={(v) => onChange({ ...perms, [cp.key]: v })}
            />
          </div>
        ))}
      </div>
    </div>
  )
}
