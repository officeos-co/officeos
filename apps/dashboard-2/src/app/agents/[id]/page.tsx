"use client"

import { use, useState, useEffect, useCallback } from "react"
import Image from "next/image"
import Link from "next/link"
import { useSearchParams, useRouter } from "next/navigation"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"
import { PageHeader } from "@/components/page-header"
import { LogTable } from "@/components/log-table"
import { ToolPermissionCard, ChannelPermissionCard, type ToolPermission } from "@/components/permission-cards"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Label } from "@/components/ui/label"
import { Separator } from "@/components/ui/separator"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { mockAgent, mockAgentLogs, mockMemoryFiles } from "@/data/agent-mock"
import { integrations, builtInTools, sourceUrl } from "@/data/integrations"
import { channels, type ChannelPermissions } from "@/data/channels"
import {
  SendIcon,
  ClockIcon,
  FileTextIcon,
  Loader2Icon,
  TerminalIcon,
  CheckIcon,
} from "lucide-react"

/* ── Status badge ────────────────────────────────────────── */

const statusStyles: Record<string, { bg: string; text: string; label: string }> = {
  running: { bg: "bg-emerald-100", text: "text-emerald-700", label: "RUNNING" },
  pending: { bg: "bg-amber-100", text: "text-amber-700", label: "PENDING" },
  booting: { bg: "bg-blue-100", text: "text-blue-700", label: "BOOTING" },
  stopped: { bg: "bg-zinc-100", text: "text-zinc-500", label: "STOPPED" },
  failed: { bg: "bg-red-100", text: "text-red-700", label: "FAILED" },
}

function StatusBadge({ status }: { status: string }) {
  const style = statusStyles[status] ?? statusStyles.stopped
  return (
    <span className={`inline-flex rounded-full px-2.5 py-1 text-[10px] font-semibold uppercase tracking-widest ${style.bg} ${style.text}`}>
      {style.label}
    </span>
  )
}

/* ── Tabs (URL-driven) ───────────────────────────────────── */

const TABS = [
  { key: "agent", label: "Agent" },
  { key: "logs", label: "Logs" },
  { key: "memory", label: "Memory" },
  { key: "cron", label: "Cron" },
] as const
type TabKey = (typeof TABS)[number]["key"]


/* ── Agent tab (mirrors quickstart) ──────────────────────── */

function AgentTab() {
  const [agentName, setAgentName] = useState(mockAgent.name)
  const [prompt, setPrompt] = useState(mockAgent.prompt)
  const [model, setModel] = useState(mockAgent.model)
  const [selectedIntegrations, setSelectedIntegrations] = useState<Set<string>>(new Set(mockAgent.integrations))
  const [selectedChannels, setSelectedChannels] = useState<Set<string>>(new Set(mockAgent.channels))
  const [toolPermissions, setToolPermissions] = useState<Record<string, ToolPermission>>({})
  const [groupPermissions, setGroupPermissions] = useState<Record<string, ToolPermission>>({})
  const [channelPerms, setChannelPerms] = useState<Record<string, ChannelPermissions>>(() => {
    const cp: Record<string, ChannelPermissions> = {}
    for (const slug of mockAgent.channels) {
      const ch = channels.find((c) => c.slug === slug)
      if (ch) cp[slug] = { ...ch.defaultPermissions }
    }
    return cp
  })

  function toggleIntegration(slug: string) {
    setSelectedIntegrations((prev) => { const next = new Set(prev); if (next.has(slug)) next.delete(slug); else next.add(slug); return next })
  }

  function toggleChannel(slug: string) {
    setSelectedChannels((prev) => {
      const next = new Set(prev)
      if (next.has(slug)) {
        next.delete(slug)
        setChannelPerms((cp) => { const n = { ...cp }; delete n[slug]; return n })
      } else {
        next.add(slug)
        const ch = channels.find((c) => c.slug === slug)
        if (ch) setChannelPerms((cp) => ({ ...cp, [slug]: { ...ch.defaultPermissions } }))
      }
      return next
    })
  }

  const activeIntegrations = integrations.filter((i) => selectedIntegrations.has(i.slug))
  const activeChannels = channels.filter((c) => selectedChannels.has(c.slug))

  return (
    <div className="pt-4 space-y-6">
      {/* Name + Model */}
      <div className="grid grid-cols-[1fr_200px] gap-4">
        <div className="space-y-2">
          <Label htmlFor="agent-name">Agent name</Label>
          <Input id="agent-name" value={agentName} onChange={(e) => setAgentName(e.target.value)} />
        </div>
        <div className="space-y-2">
          <Label>Model</Label>
          <Select value={model} onValueChange={(v) => { if (v) setModel(v) }}>
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="claude-sonnet-4-6">Claude Sonnet 4.6</SelectItem>
              <SelectItem value="claude-opus-4-6">Claude Opus 4.6</SelectItem>
              <SelectItem value="claude-haiku-4-5">Claude Haiku 4.5</SelectItem>
              <SelectItem value="gemini-2.5-pro">Gemini 2.5 Pro</SelectItem>
              <SelectItem value="gpt-4o">GPT-4o</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* System prompt */}
      <div className="space-y-2">
        <Label htmlFor="prompt">System prompt</Label>
        <Textarea id="prompt" value={prompt} onChange={(e) => setPrompt(e.target.value)} rows={5} />
      </div>

      <Separator />

      {/* Integrations */}
      <div className="space-y-3">
        <Label>Integrations</Label>
        <div className="grid grid-cols-3 gap-2">
          {integrations.map((i) => {
            const active = selectedIntegrations.has(i.slug)
            return (
              <button key={i.slug} type="button" onClick={() => toggleIntegration(i.slug)}
                className={`flex items-center gap-2.5 rounded-lg border px-3 py-2 text-left text-sm transition-colors ${active ? "border-primary bg-primary/5" : "border-border hover:bg-muted/50"}`}>
                <Image src={i.logo} alt={i.name} width={18} height={18} className="shrink-0" />
                <span className="flex-1 truncate">{i.name}</span>
                {active && <CheckIcon className="size-3.5 text-primary shrink-0" />}
              </button>
            )
          })}
        </div>
      </div>

      {/* Channels */}
      <div className="space-y-3">
        <Label>Channels</Label>
        <div className="grid grid-cols-3 gap-2">
          {channels.map((c) => {
            const active = selectedChannels.has(c.slug)
            return (
              <button key={c.slug} type="button" onClick={() => toggleChannel(c.slug)}
                className={`flex items-center gap-2.5 rounded-lg border px-3 py-2 text-left text-sm transition-colors ${active ? "border-primary bg-primary/5" : "border-border hover:bg-muted/50"}`}>
                <Image src={c.logo} alt={c.name} width={18} height={18} className="shrink-0" />
                <span className="flex-1 truncate">{c.name}</span>
                {active && <CheckIcon className="size-3.5 text-primary shrink-0" />}
              </button>
            )
          })}
        </div>
      </div>

      <Separator />

      {/* Tool permissions */}
      <div className="space-y-3">
        <Label>Tool permissions</Label>
        <ToolPermissionCard
          title="Built-in tools" subtitle="agent_toolset"
          icon={<TerminalIcon className="size-4" />}
          tools={builtInTools} permissions={toolPermissions}
          onToggle={(k, p) => setToolPermissions((prev) => ({ ...prev, [k]: p }))}
          groupPerm={groupPermissions["builtin"] ?? "allow"}
          onGroupPerm={(p) => setGroupPermissions((prev) => ({ ...prev, builtin: p }))}
          prefix="builtin"
        />
        {activeIntegrations.map((i) => (
          <ToolPermissionCard
            key={i.slug} title={i.name} subtitle={sourceUrl(i.slug).replace("https://github.com/", "")}
            icon={<Image src={i.logo} alt={i.name} width={16} height={16} />}
            tools={i.tools} permissions={toolPermissions}
            onToggle={(k, p) => setToolPermissions((prev) => ({ ...prev, [k]: p }))}
            groupPerm={groupPermissions[i.slug] ?? "allow"}
            onGroupPerm={(p) => setGroupPermissions((prev) => ({ ...prev, [i.slug]: p }))}
            prefix={i.slug}
          />
        ))}
      </div>

      {/* Channel permissions */}
      {activeChannels.length > 0 && (
        <div className="space-y-3">
          <Label>Channel permissions</Label>
          {activeChannels.map((c) => (
            <ChannelPermissionCard
              key={c.slug} channel={c}
              perms={channelPerms[c.slug] ?? c.defaultPermissions}
              onChange={(p) => setChannelPerms((prev) => ({ ...prev, [c.slug]: p }))}
            />
          ))}
        </div>
      )}

      <div className="flex items-center gap-3 pb-8">
        <Button size="sm">Save changes</Button>
      </div>
    </div>
  )
}

/* ── Logs tab ────────────────────────────────────────────── */

function LogsTab() {
  const [message, setMessage] = useState("")
  return (
    <div className="flex flex-col flex-1 pt-4">
      <div className="flex-1 overflow-x-auto">
        <LogTable logs={mockAgentLogs} />
      </div>
      <div className="border-t border-border p-3 mt-4">
        <div className="flex items-center gap-2 max-w-3xl mx-auto">
          <Input value={message} onChange={(e) => setMessage(e.target.value)} placeholder="Send a message to the agent..." className="flex-1" onKeyDown={(e) => { if (e.key === "Enter" && message.trim()) setMessage("") }} />
          <Button size="icon" disabled={!message.trim()} onClick={() => setMessage("")}><SendIcon className="size-4" /></Button>
        </div>
      </div>
    </div>
  )
}

/* ── Memory tab ──────────────────────────────────────────── */

function MemoryTab() {
  const [selected, setSelected] = useState("USER.md")
  const files = Object.keys(mockMemoryFiles)
  return (
    <div className="flex gap-4 pt-4">
      <div className="w-48 shrink-0 space-y-1">
        {files.map((f) => (
          <button key={f} type="button" onClick={() => setSelected(f)}
            className={`flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm text-left transition-colors ${selected === f ? "bg-primary/5 border border-primary" : "hover:bg-muted/50"}`}>
            <FileTextIcon className="size-4 text-muted-foreground shrink-0" />
            <span className="font-mono text-xs">{f}</span>
          </button>
        ))}
      </div>
      <div className="flex-1 rounded-xl border border-border bg-card overflow-y-auto">
        <div className="px-4 py-3 border-b border-border"><span className="font-mono text-sm">{selected}</span></div>
        <div className="p-6 prose prose-sm max-w-none prose-headings:font-semibold prose-headings:text-foreground prose-h1:text-lg prose-h1:mt-0 prose-h1:mb-3 prose-h2:text-sm prose-h2:mt-5 prose-h2:mb-2 prose-p:text-sm prose-p:text-muted-foreground prose-li:text-sm prose-li:text-muted-foreground prose-strong:text-foreground prose-code:rounded prose-code:bg-muted prose-code:px-1.5 prose-code:py-0.5 prose-code:text-xs prose-code:before:content-none prose-code:after:content-none">
          <ReactMarkdown remarkPlugins={[remarkGfm]}>{mockMemoryFiles[selected] ?? ""}</ReactMarkdown>
        </div>
      </div>
    </div>
  )
}

/* ── Cron tab ────────────────────────────────────────────── */

function CronTab() {
  return (
    <div className="flex flex-col items-center justify-center py-16 text-center">
      <ClockIcon className="size-8 text-muted-foreground/30 mb-3" />
      <p className="text-sm font-medium">Scheduled tasks</p>
      <p className="text-sm text-muted-foreground mt-1">Configure cron jobs for this agent. Coming soon.</p>
    </div>
  )
}

/* ── Boot screen ─────────────────────────────────────────── */

function BootScreen({ agentName, onReady }: { agentName: string; onReady: () => void }) {
  const [stage, setStage] = useState(0)
  const stages = ["Provisioning pod...", "Loading model...", "Connecting integrations...", "Mounting memory vault...", "Agent ready."]

  useEffect(() => {
    if (stage < stages.length - 1) {
      const t = setTimeout(() => setStage(stage + 1), 1000)
      return () => clearTimeout(t)
    } else {
      const t = setTimeout(onReady, 500)
      return () => clearTimeout(t)
    }
  }, [stage, stages.length, onReady])

  return (
    <div className="flex flex-1 flex-col items-center justify-center">
      <Loader2Icon className="size-8 animate-spin text-muted-foreground mb-4" />
      <p className="text-sm font-medium mb-1">Launching {agentName}</p>
      <div className="space-y-1 text-center">
        {stages.slice(0, stage + 1).map((s, i) => (
          <p key={i} className={`text-xs ${i === stage && stage < stages.length - 1 ? "text-muted-foreground" : i < stage ? "text-muted-foreground/50" : "text-emerald-600 font-medium"}`}>
            {i < stage ? "✓ " : i === stage && stage < stages.length - 1 ? "→ " : "✓ "}{s}
          </p>
        ))}
      </div>
    </div>
  )
}

/* ── Page ─────────────────────────────────────────────────── */

export default function AgentDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const searchParams = useSearchParams()
  const router = useRouter()
  const booting = searchParams.get("boot") === "true"
  const [isBooting, setIsBooting] = useState(booting)
  const tab = (searchParams.get("tab") as TabKey) ?? "agent"

  const handleBootReady = useCallback(() => {
    setIsBooting(false)
    router.replace(`/agents/${id}?tab=agent`)
  }, [id, router])

  if (isBooting) {
    return (
      <>
        <PageHeader group="Agents" page={mockAgent.name} />
        <BootScreen agentName={mockAgent.name} onReady={handleBootReady} />
      </>
    )
  }

  return (
    <>
      <PageHeader group="Agents" page={mockAgent.name} />

      {/* Sticky agent header + tabs */}
      <div className="sticky top-0 z-10 bg-background border-b border-border">
        <div className="max-w-6xl mx-auto w-full px-4">
          {/* Agent info row */}
          <div className="flex items-start justify-between py-4">
            <div>
              <div className="flex items-center gap-2.5">
                <h1 className="text-lg font-semibold">{mockAgent.name}</h1>
                <StatusBadge status={mockAgent.status} />
              </div>
              <div className="mt-1 text-xs text-muted-foreground">
                <span className="font-mono">{mockAgent.id}</span>
                <span className="mx-1.5">·</span>
                <span>Last updated 41 minutes ago</span>
              </div>
              <p className="mt-1 text-sm text-muted-foreground">{mockAgent.prompt.slice(0, 120)}{mockAgent.prompt.length > 120 ? "…" : ""}</p>
            </div>
            <div className="flex items-center gap-2 shrink-0">
              <Button variant="outline" size="sm" render={<Link href="/agents" />}>All agents</Button>
            </div>
          </div>

          {/* Tab bar */}
          <div className="flex -mb-px">
            {TABS.map((t) => (
              <Link
                key={t.key}
                href={`/agents/${id}?tab=${t.key}`}
                className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
                  tab === t.key
                    ? "border-foreground text-foreground"
                    : "border-transparent text-muted-foreground hover:text-foreground"
                }`}
              >
                {t.label}
              </Link>
            ))}
          </div>
        </div>
      </div>

      <div className="flex flex-1 flex-col px-4 max-w-6xl mx-auto w-full">
        <div className="flex-1 flex flex-col">
          {tab === "agent" && <AgentTab />}
          {tab === "logs" && <LogsTab />}
          {tab === "memory" && <MemoryTab />}
          {tab === "cron" && <CronTab />}
        </div>
      </div>
    </>
  )
}
