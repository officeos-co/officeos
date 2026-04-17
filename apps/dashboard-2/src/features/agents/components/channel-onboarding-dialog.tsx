"use client"

import { useState } from "react"
import Image from "next/image"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"
import type { Channel, OnboardingStep } from "../data/channels"
import {
  ExternalLinkIcon,
  CopyIcon,
  QrCodeIcon,
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
  const [step, setStep] = useState(0)
  const [inputs, setInputs] = useState<Record<string, string>>({})
  const current = channel.onboarding[step]
  const isLast = step === channel.onboarding.length - 1

  function handleComplete() {
    onComplete()
    setStep(0)
    setInputs({})
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v) { setStep(0); setInputs({}) }; onOpenChange(v) }}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <Image src={channel.logo} alt={channel.name} width={28} height={28} className="shrink-0" />
            <div>
              <DialogTitle className="text-base">Connect {channel.name}</DialogTitle>
              <DialogDescription>{channel.protocol}</DialogDescription>
            </div>
          </div>
        </DialogHeader>

        {/* Progress */}
        <div className="flex items-center gap-1.5 pt-1">
          {channel.onboarding.map((_, i) => (
            <div key={i} className={`flex-1 h-1 rounded-full ${i <= step ? "bg-primary" : "bg-border"}`} />
          ))}
        </div>

        {/* Step content */}
        <div className="space-y-4 pt-2">
          <div>
            <div className="flex items-center gap-2 text-sm font-medium mb-1">
              <span className="flex size-5 items-center justify-center rounded-full bg-primary text-primary-foreground text-[10px]">{step + 1}</span>
              {current.title}
            </div>
            <p className="text-sm text-muted-foreground">{current.description}</p>
          </div>

          {current.action === "url" && current.value && (
            <a href={current.value} target="_blank" rel="noopener noreferrer"
              className="flex items-center gap-2 text-sm text-primary hover:underline">
              <ExternalLinkIcon className="size-3.5" />
              {current.value.replace(/https?:\/\//, "").split("/")[0]}
            </a>
          )}

          {current.action === "copy" && current.value && (
            <div className="flex items-center gap-2">
              <code className="flex-1 rounded-lg bg-muted px-3 py-2 text-xs font-mono break-all">{current.value}</code>
              <Button size="icon" variant="outline" className="shrink-0 size-8" onClick={() => navigator.clipboard.writeText(current.value!)}>
                <CopyIcon className="size-3.5" />
              </Button>
            </div>
          )}

          {current.action === "qr" && current.value && (
            <div className="rounded-lg border border-border p-6 flex flex-col items-center gap-2">
              <div className="size-32 bg-muted rounded-lg flex items-center justify-center">
                <QrCodeIcon className="size-12 text-muted-foreground/30" />
              </div>
              <p className="text-xs text-muted-foreground">{current.value}</p>
            </div>
          )}

          {current.action === "input" && current.inputKey && (
            <div className="space-y-1.5">
              <Label className="text-xs">{current.inputLabel}</Label>
              <Input
                type="password"
                placeholder={current.inputPlaceholder}
                value={inputs[current.inputKey] ?? ""}
                onChange={(e) => setInputs((prev) => ({ ...prev, [current.inputKey!]: e.target.value }))}
              />
            </div>
          )}
        </div>

        <div className="flex items-center gap-2 pt-2">
          {isLast ? (
            <Button size="sm" onClick={handleComplete}>Connect channel</Button>
          ) : (
            <Button size="sm" onClick={() => setStep(step + 1)}>Continue</Button>
          )}
          {step > 0 && <Button size="sm" variant="ghost" onClick={() => setStep(step - 1)}>Back</Button>}
          <Button size="sm" variant="ghost" onClick={() => onOpenChange(false)} className="ml-auto">Cancel</Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
