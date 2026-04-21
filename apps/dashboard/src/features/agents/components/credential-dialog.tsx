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
import { useOAuthStatus } from "../api/useIntegrations"
import { CheckCircle2 } from "lucide-react"

function OAuthField({ field, skillSlug }: { field: CredentialField; skillSlug?: string }) {
  const provider = field.oauth2?.provider ?? null
  const scopes = field.oauth2?.scopes ?? []
  const { connected, email, refetch } = useOAuthStatus(provider)

  const returnUrl = skillSlug ? `/integrations/${skillSlug}` : "/integrations"

  function handleConnect() {
    const scopeStr = scopes.join(" ")
    // Route through dashboard's /api/* proxy so cookies are on the same domain
    const url = `/api/skills/oauth/${provider}/start?scopes=${encodeURIComponent(scopeStr)}&returnUrl=${encodeURIComponent(returnUrl)}`
    window.location.href = url
  }

  function handleDisconnect() {
    fetch(`/api/skills/oauth/${provider}`, {
      method: "DELETE",
      credentials: "include",
    }).then(() => refetch())
  }

  if (connected) {
    return (
      <div className="flex items-center justify-between rounded-md border px-3 py-2.5">
        <div className="flex items-center gap-2">
          <CheckCircle2 className="size-4 text-emerald-500" />
          <span className="text-sm">Connected as {email ?? provider}</span>
        </div>
        <Button variant="ghost" size="sm" className="text-xs text-muted-foreground" onClick={handleDisconnect}>
          Disconnect
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-1.5">
      <Label className="text-xs">{field.label}</Label>
      <Button variant="outline" className="w-full justify-center gap-2" onClick={handleConnect}>
        <svg className="size-4" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
          <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
          <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/>
          <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
        </svg>
        Connect with Google
      </Button>
      {field.help && <p className="text-[11px] text-muted-foreground">{field.help}</p>}
    </div>
  )
}

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

  const hasOAuth = credentials.some((c) => c.kind === "oauth2")
  const manualFields = credentials.filter((c) => c.kind !== "oauth2")
  const oauthFields = credentials.filter((c) => c.kind === "oauth2")

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
                {hasOAuth ? "Connect your account to get started." : "Enter credentials to connect."}
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>
        <div className="space-y-3 pt-2">
          {oauthFields.map((c) => (
            <OAuthField key={c.key} field={c} skillSlug={slug} />
          ))}
          {manualFields.map((c) => (
            <div key={c.key} className="space-y-1.5">
              <Label className="text-xs">{c.label}{c.required && <span className="text-red-500 ml-0.5">*</span>}</Label>
              <Input
                type={c.kind === "password" ? "password" : "text"}
                placeholder={c.placeholder ?? ""}
                value={values[c.key] ?? ""}
                onChange={(e) => setValues((prev) => ({ ...prev, [c.key]: e.target.value }))}
              />
              {c.help && <p className="text-[11px] text-muted-foreground">{c.help}</p>}
            </div>
          ))}
        </div>
        {manualFields.length > 0 && (
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" size="sm" onClick={() => onOpenChange(false)}>Cancel</Button>
            <Button size="sm" onClick={handleSave}>Save credentials</Button>
          </div>
        )}
        {hasOAuth && manualFields.length === 0 && (
          <div className="flex justify-end pt-2">
            <Button variant="ghost" size="sm" onClick={() => onOpenChange(false)}>Close</Button>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
