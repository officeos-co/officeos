"use client"

import type { Channel } from "../data/channels"
import { WithTooltip } from "@/components/ui/help-tooltip"
import { CatalogCard } from "@/components/catalog-card"
import { PlugIcon, CheckIcon, RadioIcon } from "lucide-react"

type ChannelCardProps = {
  channel: Channel
  selected?: boolean
  variant?: "compact" | "marketplace"
  onConnect?: () => void
  onToggle?: () => void
  onClick?: () => void
}

export function ChannelCard({
  channel: c,
  selected = false,
  variant = "compact",
  onConnect,
  onToggle,
  onClick,
}: ChannelCardProps) {
  const action = (
    <ChannelAction
      channel={c}
      selected={selected}
      onConnect={onConnect}
      onToggle={onToggle}
    />
  )

  if (variant === "marketplace") {
    return (
      <CatalogCard
        logo={c.logo}
        title={c.name}
        description={c.description}
        onClick={onClick}
        action={action}
      />
    )
  }

  return (
    <CatalogCard
      variant="compact"
      logo={c.logo}
      title={c.name}
      selected={selected}
      action={action}
    />
  )
}

function ChannelAction({
  channel: c,
  selected,
  onConnect,
  onToggle,
}: {
  channel: Channel
  selected: boolean
  onConnect?: () => void
  onToggle?: () => void
}) {
  if (!c.added) {
    return (
      <WithTooltip tooltip="Connect this channel account before an agent can receive or send messages there.">
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation()
            onConnect?.()
          }}
          className="flex shrink-0 items-center gap-1 rounded-md bg-muted px-2 py-1 text-[11px] font-medium text-foreground transition-colors hover:bg-muted/80"
        >
          <PlugIcon className="size-3" />
          Connect
        </button>
      </WithTooltip>
    )
  }

  if (!selected && onToggle) {
    return (
      <WithTooltip tooltip="Bind this connected channel to the agent.">
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation()
            onToggle()
          }}
          className="flex shrink-0 items-center gap-1 rounded-md bg-emerald-100 px-2 py-1 text-[11px] font-medium text-emerald-800 transition-colors hover:bg-emerald-200"
        >
          <PlugIcon className="size-3" />
          Use
        </button>
      </WithTooltip>
    )
  }

  if (selected && onToggle) {
    return (
      <WithTooltip tooltip="This channel is bound to the agent. Click to remove the binding.">
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation()
            onToggle()
          }}
          className="flex shrink-0 items-center gap-1 rounded-md bg-primary/10 px-2 py-1 text-[11px] font-medium text-primary transition-colors hover:bg-primary/20"
        >
          <CheckIcon className="size-3" />
          Added
        </button>
      </WithTooltip>
    )
  }

  return (
    <WithTooltip tooltip="This channel account is connected and can be bound to agents.">
      <span className="flex shrink-0 items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest text-emerald-800">
        <RadioIcon className="size-3" /> Live
      </span>
    </WithTooltip>
  )
}
