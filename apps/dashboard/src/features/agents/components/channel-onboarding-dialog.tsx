"use client"

import { useState } from "react"
import { Button } from "@/ui/button"
import { HelpTooltip, WithTooltip } from "@/ui/help-tooltip"
import { Input } from "@/ui/input"
import { Label } from "@/ui/label"
import { Textarea } from "@/ui/textarea"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/ui/dialog"
import type { Channel } from "../data/channels"
import { useCreateChannelConnection } from "../api/useChannels"
import {
  ExternalLinkIcon,
  CopyIcon,
  QrCodeIcon,
  LoaderIcon,
} from "lucide-react"

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
  const [step, setStep] = useState(0)
  const [connecting, setConnecting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const hasSteps = channel.onboarding.length > 0
  const current = hasSteps ? channel.onboarding[step] : null
  const isLast = hasSteps && step === channel.onboarding.length - 1

  function reset() {
    setStep(0)
    setInputs({})
    setError(null)
    setConnecting(false)
  }

  async function handleComplete() {
    setConnecting(true)
    setError(null)
    try {
      await createChannelConnection({
        channelType: channel.slug,
        displayName: channel.name,
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
    if (!current) return true
    if (current.type !== "input" || !current.inputKey) return true
    if (!current.inputRequired) return true
    return (inputs[current.inputKey] ?? "").trim().length > 0
  }

  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v) reset(); onOpenChange(v) }}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="size-7 shrink-0 [&>svg]:size-7" dangerouslySetInnerHTML={{ __html: channel.logo }} />
            <div>
              <DialogTitle className="text-base">Connect {channel.name}</DialogTitle>
              <DialogDescription>Set up {channel.name} integration</DialogDescription>
            </div>
          </div>
        </DialogHeader>

        {error && (
          <p className="text-sm text-destructive pt-1">{error}</p>
        )}

        {!hasSteps ? (
          <div className="space-y-3 pt-2">
            <p className="text-sm text-muted-foreground">
              Connect {channel.name} to start receiving and sending messages through your agents.
            </p>
            <div className="flex items-center gap-2 pt-2">
              <WithTooltip tooltip="Create a channel connection that agents can later bind to.">
                <Button size="sm" onClick={handleComplete} disabled={connecting}>
                  {connecting && <LoaderIcon className="size-3 animate-spin" />}
                  Connect channel
                </Button>
              </WithTooltip>
              <Button size="sm" variant="ghost" onClick={() => onOpenChange(false)} className="ml-auto" disabled={connecting}>Cancel</Button>
            </div>
          </div>
        ) : (
          <>
            {/* Progress */}
            <div className="flex items-center gap-1.5 pt-1">
              {channel.onboarding.map((_, i) => (
                <div key={i} className={`flex-1 h-1 rounded-full ${i <= step ? "bg-primary" : "bg-border"}`} />
              ))}
            </div>

            {/* Step content */}
            {current && (
              <div className="space-y-4 pt-2">
                <div>
                  <div className="flex items-center gap-2 text-sm font-medium mb-1">
                    <span className="flex size-5 items-center justify-center rounded-full bg-primary text-primary-foreground text-[10px]">{step + 1}</span>
                    {current.title}
                  </div>
                  <p className="text-sm text-muted-foreground">{current.description}</p>
                </div>

                {current.type === "url" && current.value && (
                  <a href={current.value} target="_blank" rel="noopener noreferrer"
                    className="flex items-center gap-2 text-sm text-primary hover:underline">
                    <ExternalLinkIcon className="size-3.5" />
                    {current.value.replace(/https?:\/\//, "").split("/")[0]}
                  </a>
                )}

                {current.type === "copy" && current.value && (
                  <div className="flex items-center gap-2">
                    <code className="flex-1 rounded-lg bg-muted px-3 py-2 text-xs font-mono break-all">{current.value}</code>
                    <WithTooltip tooltip="Copy this setup value to the external channel configuration.">
                      <Button size="icon" variant="outline" className="shrink-0 size-8" onClick={() => navigator.clipboard.writeText(current.value!)}>
                        <CopyIcon className="size-3.5" />
                      </Button>
                    </WithTooltip>
                  </div>
                )}

                {current.type === "qr" && current.value && (
                  <div className="rounded-lg border border-border p-6 flex flex-col items-center gap-2">
                    <div className="size-32 bg-muted rounded-lg flex items-center justify-center">
                      <QrCodeIcon className="size-12 text-muted-foreground/30" />
                    </div>
                    <a href={current.value} target="_blank" rel="noopener noreferrer"
                      className="text-xs text-primary hover:underline">{current.value}</a>
                  </div>
                )}

                {current.type === "input" && current.inputKey && (
                  <div className="space-y-1.5">
                    <Label className="text-xs">
                      {current.inputLabel ?? current.title}
                      {!current.inputRequired && <span className="text-muted-foreground ml-1">(optional)</span>}
                      <HelpTooltip>
                        This value is saved as channel configuration for the
                        backend connection.
                      </HelpTooltip>
                    </Label>
                    {current.inputKind === "textarea" ? (
                      <Textarea
                        placeholder={current.inputPlaceholder}
                        value={inputs[current.inputKey] ?? ""}
                        onChange={(e) => setInputs((prev) => ({ ...prev, [current.inputKey!]: e.target.value }))}
                        rows={4}
                        className="font-mono text-xs"
                      />
                    ) : (
                      <Input
                        type={current.inputKind === "password" ? "password" : "text"}
                        placeholder={current.inputPlaceholder}
                        value={inputs[current.inputKey] ?? ""}
                        onChange={(e) => setInputs((prev) => ({ ...prev, [current.inputKey!]: e.target.value }))}
                      />
                    )}
                    {current.inputHelp && (
                      <p className="text-xs text-muted-foreground">{current.inputHelp}</p>
                    )}
                  </div>
                )}
              </div>
            )}

            <div className="flex items-center gap-2 pt-2">
              {isLast ? (
                <WithTooltip tooltip="Create the channel connection with the setup values from this flow.">
                  <Button size="sm" onClick={handleComplete} disabled={connecting || !canContinue()}>
                    {connecting && <LoaderIcon className="size-3 animate-spin" />}
                    Connect channel
                  </Button>
                </WithTooltip>
              ) : (
                <WithTooltip tooltip="Continue to the next setup step.">
                  <Button size="sm" onClick={() => setStep(step + 1)} disabled={!canContinue()}>Continue</Button>
                </WithTooltip>
              )}
              {step > 0 && <Button size="sm" variant="ghost" onClick={() => setStep(step - 1)} disabled={connecting}>Back</Button>}
              <Button size="sm" variant="ghost" onClick={() => onOpenChange(false)} className="ml-auto" disabled={connecting}>Cancel</Button>
            </div>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
