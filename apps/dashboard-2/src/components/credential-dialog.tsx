"use client"

import { useState } from "react"
import Image from "next/image"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import type { CredentialField } from "@/data/integrations"

export function CredentialDialog({
  open,
  onOpenChange,
  name,
  logo,
  credentials,
  onSave,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  name: string
  logo: string
  credentials: CredentialField[]
  onSave: (values: Record<string, string>) => void
}) {
  const [values, setValues] = useState<Record<string, string>>({})

  function handleSave() {
    onSave(values)
    setValues({})
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <Image src={logo} alt={name} width={24} height={24} className="shrink-0" />
            <div>
              <DialogTitle className="text-base">Configure {name}</DialogTitle>
              <DialogDescription>Enter credentials to connect.</DialogDescription>
            </div>
          </div>
        </DialogHeader>
        <div className="space-y-3 pt-2">
          {credentials.map((c) => (
            <div key={c.key} className="space-y-1.5">
              <Label className="text-xs">{c.label}</Label>
              <Input
                type={c.type}
                placeholder={c.placeholder}
                value={values[c.key] ?? ""}
                onChange={(e) => setValues((prev) => ({ ...prev, [c.key]: e.target.value }))}
              />
            </div>
          ))}
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <Button variant="ghost" size="sm" onClick={() => onOpenChange(false)}>Cancel</Button>
          <Button size="sm" onClick={handleSave}>Save credentials</Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
