"use client"

import { useState } from "react"
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
import type { CredentialField } from "../data/integrations"

export function CredentialDialog({
  open,
  onOpenChange,
  name,
  slug,
  logo,
  credentials,
  onSave,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  name: string
  slug?: string
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
            <div className="size-6 shrink-0 [&>svg]:size-6" dangerouslySetInnerHTML={{ __html: logo }} />
            <div>
              <DialogTitle className="text-base">Configure {name}</DialogTitle>
              <DialogDescription>
                Enter credentials to connect.
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>
        <div className="space-y-3 pt-2">
          {credentials.map((c) => (
            <div key={c.name} className="space-y-1.5">
              <Label className="text-xs">{c.label}{c.required && <span className="text-red-500 ml-0.5">*</span>}</Label>
              <Input
                type={c.type === "password" ? "password" : "text"}
                value={values[c.name] ?? ""}
                onChange={(e) => setValues((prev) => ({ ...prev, [c.name]: e.target.value }))}
              />
            </div>
          ))}
        </div>
        {credentials.length > 0 && (
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" size="sm" onClick={() => onOpenChange(false)}>Cancel</Button>
            <Button size="sm" onClick={handleSave}>Save credentials</Button>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
