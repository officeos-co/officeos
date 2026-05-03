"use client"

import type { Channel } from "../data/channels"
import { WithTooltip } from "@/components/ui/help-tooltip"
import { PlugIcon, CheckIcon } from "lucide-react"

type ChannelCardProps = {
  channel: Channel
  selected?: boolean
  onConnect?: () => void
  onToggle?: () => void
}

export function ChannelCard({
  channel: c,
  selected = false,
  onConnect,
  onToggle,
}: ChannelCardProps) {
  return (
    <div
      className={`flex items-center gap-2.5 rounded-lg border px-3 py-2 text-sm transition-colors ${selected ? "border-primary bg-primary/5" : "border-border"}`}
    >
      <div className="size-[18px] shrink-0 [&>svg]:size-[18px]" dangerouslySetInnerHTML={{ __html: c.logo }} />
      <span className="flex-1 truncate">{c.name}</span>
      {!c.added && (
        <WithTooltip tooltip="Connect this channel account before an agent can receive or send messages there.">
          <button type="button" onClick={() => onConnect?.()}
            className="flex items-center gap-1 rounded-md bg-muted px-2 py-1 text-[11px] font-medium text-foreground hover:bg-muted/80 transition-colors shrink-0">
            <PlugIcon className="size-3" />
            Connect
          </button>
        </WithTooltip>
      )}
      {c.added && !selected && onToggle && (
        <WithTooltip tooltip="Bind this connected channel to the agent.">
          <button type="button" onClick={() => onToggle()}
            className="flex items-center gap-1 rounded-md bg-emerald-100 px-2 py-1 text-[11px] font-medium text-emerald-700 hover:bg-emerald-200 transition-colors shrink-0">
            <PlugIcon className="size-3" />
            Use
          </button>
        </WithTooltip>
      )}
      {c.added && selected && onToggle && (
        <WithTooltip tooltip="This channel is bound to the agent. Click to remove the binding.">
          <button type="button" onClick={() => onToggle()}
            className="flex items-center gap-1 rounded-md bg-primary/10 px-2 py-1 text-[11px] font-medium text-primary hover:bg-primary/20 transition-colors shrink-0">
            <CheckIcon className="size-3" />
            Added
          </button>
        </WithTooltip>
      )}
    </div>
  )
}
