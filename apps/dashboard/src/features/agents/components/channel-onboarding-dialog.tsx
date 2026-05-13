"use client"

import { useState } from "react"
import { Button } from "@/ui/button"
import { WithTooltip } from "@/ui/help-tooltip"
import { Input } from "@/ui/input"
import { Label } from "@/ui/label"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/ui/dialog"
import type { Channel } from "../data/channels"
import { useCreateChannelConnection } from "../api/useChannels"
import { LoaderIcon } from "lucide-react"

export function ChannelOnboardingDialog({
  open,
  onOpenChange,
  channel,
  onComplete,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  channel: Channel
  onComplete: () => void
}) {
  const { createChannelConnection } = useCreateChannelConnection()
  const [inputs, setInputs] = useState<Record<string, string>>({})
  const [displayName, setDisplayName] = useState(channel.name)
  const [connecting, setConnecting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const credentialFields = channel.onboarding.filter((field) => field.type === "input" && field.inputKey)

  function reset() {
    setInputs({})
    setDisplayName(channel.name)
    setError(null)
    setConnecting(false)
  }

  async function handleComplete() {
    setConnecting(true)
    setError(null)
    try {
      await createChannelConnection({
        channelType: channel.slug,
        displayName: displayName.trim() || channel.name,
        config: inputs,
      })
      onComplete()
      reset()
      onOpenChange(false)
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to connect channel")
      setConnecting(false)
    }
  }

  function canContinue(): boolean {
    return credentialFields.every((field) => {
      if (!field.inputKey || field.inputRequired === false) return true
      return (inputs[field.inputKey] ?? "").trim().length > 0
    })
  }

  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v) reset(); onOpenChange(v) }}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="size-7 shrink-0 [&>svg]:size-7" dangerouslySetInnerHTML={{ __html: channel.logo }} />
            <div>
              <DialogTitle className="text-base">Connect {channel.name}</DialogTitle>
              <DialogDescription>Enter the credentials for this channel.</DialogDescription>
            </div>
          </div>
        </DialogHeader>

        {error && (
          <p className="text-sm text-destructive pt-1">{error}</p>
        )}

        <div className="space-y-4 pt-2">
          <div className="space-y-1.5">
            <Label className="text-xs">
              Channel name
              <span className="text-muted-foreground ml-1">(optional)</span>
            </Label>
            <Input
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              placeholder={channel.name}
            />
          </div>

          {credentialFields.map((field) => (
            <div key={field.inputKey} className="space-y-1.5">
              <Label className="text-xs">
                {field.inputLabel ?? field.title}
                {field.inputRequired === false && <span className="text-muted-foreground ml-1">(optional)</span>}
              </Label>
              <Input
                type={field.inputKind === "password" ? "password" : "text"}
                placeholder={field.inputPlaceholder}
                value={inputs[field.inputKey!] ?? ""}
                onChange={(event) => setInputs((prev) => ({ ...prev, [field.inputKey!]: event.target.value }))}
              />
              {field.inputHelp && (
                <p className="text-xs text-muted-foreground">{field.inputHelp}</p>
              )}
            </div>
          ))}

          {credentialFields.length === 0 && (
            <p className="text-sm text-muted-foreground">
              Create a channel connection that agents can bind to.
            </p>
          )}

          <div className="flex items-center gap-2 pt-2">
            <WithTooltip tooltip="Create the channel connection with these credentials.">
              <Button size="sm" onClick={handleComplete} disabled={connecting || !canContinue()}>
                {connecting && <LoaderIcon className="size-3 animate-spin" />}
                Connect channel
              </Button>
            </WithTooltip>
            <Button size="sm" variant="ghost" onClick={() => onOpenChange(false)} className="ml-auto" disabled={connecting}>Cancel</Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
