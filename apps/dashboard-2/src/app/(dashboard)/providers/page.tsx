"use client"

import { useState } from "react"
import Image from "next/image"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Separator } from "@/components/ui/separator"
import {
  ChevronDownIcon,
  ChevronRightIcon,
  KeyRoundIcon,
  CheckIcon,
  ExternalLinkIcon,
} from "lucide-react"

type Model = {
  id: string
  name: string
  context: string
  pricing: string
}

type Provider = {
  name: string
  slug: string
  logo: string
  description: string
  platformManaged: boolean
  byokDiscount: string
  models: Model[]
  docsUrl: string
}

const providers: Provider[] = [
  {
    name: "Anthropic",
    slug: "anthropic",
    logo: "/logos/anthropic.svg",
    description: "Claude models — Opus, Sonnet, Haiku.",
    platformManaged: true,
    byokDiscount: "40%",
    models: [
      { id: "claude-opus-4-6", name: "Claude Opus 4.6", context: "200K", pricing: "$15 / $75 per 1M tokens" },
      { id: "claude-sonnet-4-6", name: "Claude Sonnet 4.6", context: "200K", pricing: "$3 / $15 per 1M tokens" },
      { id: "claude-haiku-4-5", name: "Claude Haiku 4.5", context: "200K", pricing: "$0.80 / $4 per 1M tokens" },
    ],
    docsUrl: "https://docs.anthropic.com",
  },
  {
    name: "OpenAI",
    slug: "openai",
    logo: "/logos/openai.svg",
    description: "GPT and o-series models.",
    platformManaged: true,
    byokDiscount: "40%",
    models: [
      { id: "gpt-4o", name: "GPT-4o", context: "128K", pricing: "$2.50 / $10 per 1M tokens" },
      { id: "gpt-4o-mini", name: "GPT-4o Mini", context: "128K", pricing: "$0.15 / $0.60 per 1M tokens" },
      { id: "o3", name: "o3", context: "200K", pricing: "$10 / $40 per 1M tokens" },
      { id: "o4-mini", name: "o4-mini", context: "200K", pricing: "$1.10 / $4.40 per 1M tokens" },
    ],
    docsUrl: "https://platform.openai.com/docs",
  },
  {
    name: "Google",
    slug: "google",
    logo: "/logos/google.svg",
    description: "Gemini models for multimodal reasoning.",
    platformManaged: true,
    byokDiscount: "40%",
    models: [
      { id: "gemini-2.5-pro", name: "Gemini 2.5 Pro", context: "1M", pricing: "$1.25 / $10 per 1M tokens" },
      { id: "gemini-2.5-flash", name: "Gemini 2.5 Flash", context: "1M", pricing: "$0.15 / $0.60 per 1M tokens" },
    ],
    docsUrl: "https://ai.google.dev/docs",
  },
  {
    name: "Meta",
    slug: "meta",
    logo: "/logos/meta.svg",
    description: "Llama open-weight models via hosted inference.",
    platformManaged: true,
    byokDiscount: "50%",
    models: [
      { id: "llama-4-maverick", name: "Llama 4 Maverick", context: "1M", pricing: "$0.50 / $1.50 per 1M tokens" },
      { id: "llama-4-scout", name: "Llama 4 Scout", context: "512K", pricing: "$0.20 / $0.60 per 1M tokens" },
    ],
    docsUrl: "https://llama.meta.com",
  },
  {
    name: "Mistral",
    slug: "mistral",
    logo: "/logos/mistral.svg",
    description: "Mistral and Codestral models.",
    platformManaged: true,
    byokDiscount: "40%",
    models: [
      { id: "mistral-large", name: "Mistral Large", context: "128K", pricing: "$2 / $6 per 1M tokens" },
      { id: "codestral", name: "Codestral", context: "256K", pricing: "$0.30 / $0.90 per 1M tokens" },
      { id: "mistral-small", name: "Mistral Small", context: "128K", pricing: "$0.10 / $0.30 per 1M tokens" },
    ],
    docsUrl: "https://docs.mistral.ai",
  },
]

function ProviderCard({ provider }: { provider: Provider }) {
  const [expanded, setExpanded] = useState(false)
  const [showByok, setShowByok] = useState(false)
  const [apiKey, setApiKey] = useState("")

  return (
    <div className="rounded-xl border border-border">
      {/* Header */}
      <div className="flex items-center gap-4 px-5 py-4">
        <div className="flex size-10 items-center justify-center rounded-lg bg-muted shrink-0">
          <Image src={provider.logo} alt={provider.name} width={22} height={22} />
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium">{provider.name}</span>
            <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest text-emerald-700">
              ACTIVE
            </span>
          </div>
          <p className="text-xs text-muted-foreground mt-0.5">{provider.description}</p>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <a href={provider.docsUrl} target="_blank" rel="noopener noreferrer" className="text-muted-foreground hover:text-foreground">
            <ExternalLinkIcon className="size-4" />
          </a>
          <Button variant="outline" size="sm" onClick={() => setShowByok(!showByok)}>
            <KeyRoundIcon className="size-3.5" />
            BYOK
          </Button>
          <button type="button" onClick={() => setExpanded(!expanded)} className="text-muted-foreground hover:text-foreground">
            {expanded ? <ChevronDownIcon className="size-5" /> : <ChevronRightIcon className="size-5" />}
          </button>
        </div>
      </div>

      {/* BYOK form */}
      {showByok && (
        <div className="border-t border-border px-5 py-4 bg-muted/30">
          <div className="max-w-md space-y-3">
            <div>
              <p className="text-sm font-medium">Bring your own key</p>
              <p className="text-xs text-muted-foreground mt-0.5">
                Use your own {provider.name} API key for a <span className="font-medium text-emerald-600">{provider.byokDiscount} price reduction</span> on token costs.
              </p>
            </div>
            <div className="space-y-1.5">
              <Label htmlFor={`key-${provider.slug}`} className="text-xs">API Key</Label>
              <div className="flex gap-2">
                <Input
                  id={`key-${provider.slug}`}
                  type="password"
                  value={apiKey}
                  onChange={(e) => setApiKey(e.target.value)}
                  placeholder={`sk-...`}
                  className="flex-1"
                />
                <Button size="sm" disabled={!apiKey.trim()}>Save key</Button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Models table */}
      {expanded && (
        <div className="border-t border-border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-left">
                <th className="px-5 py-2.5 font-medium">Model</th>
                <th className="px-5 py-2.5 font-medium">Context</th>
                <th className="px-5 py-2.5 font-medium">Pricing (in / out)</th>
              </tr>
            </thead>
            <tbody>
              {provider.models.map((m) => (
                <tr key={m.id} className="border-b last:border-0">
                  <td className="px-5 py-2.5">
                    <code className="font-mono text-xs">{m.id}</code>
                    <span className="ml-2 text-xs text-muted-foreground">{m.name}</span>
                  </td>
                  <td className="px-5 py-2.5 text-xs text-muted-foreground">{m.context}</td>
                  <td className="px-5 py-2.5 text-xs text-muted-foreground">{m.pricing}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

export default function ProvidersPage() {
  return (
    <>
      <PageHeader group="Manage" page="Providers" />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0 max-w-5xl">
        <p className="text-sm text-muted-foreground">
          All providers are active by default with platform-managed keys. Bring your own API key for reduced pricing.
        </p>
        <div className="space-y-3">
          {providers.map((p) => (
            <ProviderCard key={p.slug} provider={p} />
          ))}
        </div>
      </div>
    </>
  )
}
