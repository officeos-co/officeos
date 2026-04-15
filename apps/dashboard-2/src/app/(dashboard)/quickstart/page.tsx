"use client"

import { useState } from "react"
import Image from "next/image"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { PageHeader } from "@/components/page-header"
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
import { integrations, builtInTools, sourceUrl, type Tool } from "@/data/integrations"
import { channels, type Channel, type ChannelPermissions } from "@/data/channels"
import {
  SearchIcon,
  RocketIcon,
  CheckIcon,
  ChevronDownIcon,
  ChevronRightIcon,
  TerminalIcon,
  RadioIcon,
} from "lucide-react"

/* ── Templates ───────────────────────────────────────────── */

type Template = {
  name: string
  description: string
  integrations: string[]
  channels: string[]
  prompt: string
}

const templates: Template[] = [
  { name: "Blank agent", description: "A blank starting point.", integrations: [], channels: [], prompt: "" },
  { name: "Deep researcher", description: "Multi-step web research with citations.", integrations: ["browser"], channels: [], prompt: "You are a research assistant. Conduct thorough web research, synthesize findings, and present them with source citations." },
  { name: "Support agent", description: "Answers questions from docs, escalates via Slack.", integrations: ["notion"], channels: ["slack"], prompt: "You are a customer support agent. Answer questions using the knowledge base in Notion. Escalate to #support-escalation on Slack when needed." },
  { name: "Incident commander", description: "Triages alerts, creates tickets, runs war room.", integrations: ["linear", "browser"], channels: ["slack"], prompt: "You are an incident commander. Triage alerts, create Linear issues, and coordinate the response in #incidents on Slack." },
  { name: "Code reviewer", description: "Reviews PRs for bugs and security.", integrations: ["github"], channels: [], prompt: "Review pull request diffs for bugs, security vulnerabilities, and style issues. Leave constructive comments." },
  { name: "Feedback miner", description: "Clusters feedback into themes.", integrations: ["notion"], channels: ["slack"], prompt: "Collect feedback from Slack and Notion, cluster into themes, and draft actionable tasks." },
  { name: "Sprint retro", description: "Writes the retro doc from Linear.", integrations: ["linear", "notion"], channels: [], prompt: "Pull completed issues from the latest Linear sprint, identify patterns, and write a retro summary in Notion." },
  { name: "Compliance monitor", description: "Flags regulatory risks.", integrations: ["browser", "notion"], channels: ["slack"], prompt: "Search for regulatory updates, cross-reference against internal policies, and flag risks to #compliance." },
  { name: "Sales assistant", description: "Enriches leads and drafts outreach.", integrations: ["hubspot", "browser"], channels: [], prompt: "Research leads, draft personalized outreach emails, and log activity in HubSpot." },
  { name: "Data analyst", description: "Answers questions from web data.", integrations: ["browser"], channels: [], prompt: "Search for data sources, extract structured information, and present clear answers." },
]

/* ── Permission types ────────────────────────────────────── */

type ToolPermission = "ask" | "allow" | "deny"

const permissionLabels: Record<ToolPermission, string> = { ask: "Ask", allow: "Always allow", deny: "Deny" }
const permissionColors: Record<ToolPermission, string> = { ask: "text-muted-foreground", allow: "text-emerald-600", deny: "text-red-500" }

function PermissionCycleButton({ value, onChange }: { value: ToolPermission; onChange: (p: ToolPermission) => void }) {
  const cycle: ToolPermission[] = ["ask", "allow", "deny"]
  return (
    <button type="button" onClick={() => onChange(cycle[(cycle.indexOf(value) + 1) % cycle.length])} className={`text-xs whitespace-nowrap hover:underline ${permissionColors[value]}`}>
      {permissionLabels[value]}
    </button>
  )
}

const channelPermissionLabels: Record<string, string> = { allow: "Allow", ask: "Ask", deny: "Deny" }

function ChannelPermSelect({ value, onChange }: { value: "allow" | "ask" | "deny"; onChange: (v: "allow" | "ask" | "deny") => void }) {
  const cycle: Array<"allow" | "ask" | "deny"> = ["allow", "ask", "deny"]
  const color = value === "allow" ? "text-emerald-600" : value === "deny" ? "text-red-500" : "text-muted-foreground"
  return (
    <button type="button" onClick={() => onChange(cycle[(cycle.indexOf(value) + 1) % cycle.length])} className={`text-xs whitespace-nowrap hover:underline ${color}`}>
      {channelPermissionLabels[value]}
    </button>
  )
}

/* ── Tool permission section ─────────────────────────────── */

function ToolPermissionSection({
  title, subtitle, icon, tools, permissions, onToggle, groupPerm, onGroupPerm, prefix,
}: {
  title: string; subtitle?: string; icon: React.ReactNode; tools: Tool[]
  permissions: Record<string, ToolPermission>; onToggle: (key: string, p: ToolPermission) => void
  groupPerm: ToolPermission; onGroupPerm: (p: ToolPermission) => void; prefix: string
}) {
  const [expanded, setExpanded] = useState(true)
  return (
    <div className="rounded-xl border border-border">
      <div className="flex items-center gap-3 px-4 py-3">
        <div className="flex size-8 items-center justify-center rounded-lg bg-muted shrink-0">{icon}</div>
        <div className="flex-1 min-w-0">
          <div className="text-sm font-medium">{title}</div>
          {subtitle && <div className="text-xs text-muted-foreground">{subtitle}</div>}
        </div>
      </div>
      <div className="border-t border-border">
        <button type="button" onClick={() => setExpanded(!expanded)} className="flex w-full items-center gap-2 px-4 py-2.5 text-left hover:bg-muted/50 transition-colors">
          {expanded ? <ChevronDownIcon className="size-4 text-muted-foreground" /> : <ChevronRightIcon className="size-4 text-muted-foreground" />}
          <span className="text-xs font-medium">Tool permissions</span>
          <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] text-muted-foreground">{tools.length}</span>
          <span className="ml-auto"><PermissionCycleButton value={groupPerm} onChange={onGroupPerm} /></span>
        </button>
        {expanded && tools.map((tool) => {
          const key = `${prefix}:${tool.name}`
          const perm = permissions[key] ?? groupPerm
          return (
            <div key={tool.name} className="flex items-center gap-4 px-4 py-2.5 border-t border-border">
              <code className="rounded bg-muted px-2 py-0.5 font-mono text-xs min-w-[100px]">{tool.name}</code>
              <span className="flex-1 text-sm text-muted-foreground">{tool.description}</span>
              <PermissionCycleButton value={perm} onChange={(p) => onToggle(key, p)} />
            </div>
          )
        })}
      </div>
    </div>
  )
}

/* ── Channel permission section ──────────────────────────── */

function ChannelPermissionSection({
  channel, perms, onChange,
}: {
  channel: Channel; perms: ChannelPermissions; onChange: (p: ChannelPermissions) => void
}) {
  const [expanded, setExpanded] = useState(true)
  return (
    <div className="rounded-xl border border-border">
      <div className="flex items-center gap-3 px-4 py-3">
        <Image src={channel.logo} alt={channel.name} width={32} height={32} className="shrink-0 rounded-lg" />
        <div className="flex-1 min-w-0">
          <div className="text-sm font-medium">{channel.name}</div>
          <div className="text-xs text-muted-foreground">{channel.protocol}</div>
        </div>
      </div>
      <div className="border-t border-border">
        <button type="button" onClick={() => setExpanded(!expanded)} className="flex w-full items-center gap-2 px-4 py-2.5 text-left hover:bg-muted/50 transition-colors">
          {expanded ? <ChevronDownIcon className="size-4 text-muted-foreground" /> : <ChevronRightIcon className="size-4 text-muted-foreground" />}
          <span className="text-xs font-medium">Channel permissions</span>
        </button>
        {expanded && (
          <>
            <div className="flex items-center justify-between px-4 py-2.5 border-t border-border">
              <div>
                <div className="text-sm">Receive messages</div>
                <div className="text-xs text-muted-foreground">Inbound messages from users to the agent</div>
              </div>
              <ChannelPermSelect value={perms.receive} onChange={(v) => onChange({ ...perms, receive: v })} />
            </div>
            <div className="flex items-center justify-between px-4 py-2.5 border-t border-border">
              <div>
                <div className="text-sm">Send replies</div>
                <div className="text-xs text-muted-foreground">Agent responds in the same conversation</div>
              </div>
              <ChannelPermSelect value={perms.send} onChange={(v) => onChange({ ...perms, send: v })} />
            </div>
            <div className="flex items-center justify-between px-4 py-2.5 border-t border-border">
              <div>
                <div className="text-sm">Initiate conversations</div>
                <div className="text-xs text-muted-foreground">Agent starts new conversations proactively</div>
              </div>
              <ChannelPermSelect value={perms.initiate} onChange={(v) => onChange({ ...perms, initiate: v })} />
            </div>
          </>
        )}
      </div>
    </div>
  )
}

/* ── Page ─────────────────────────────────────────────────── */

export default function QuickstartPage() {
  const router = useRouter()
  const [search, setSearch] = useState("")
  const [selectedTemplate, setSelectedTemplate] = useState<Template | null>(null)
  const [agentName, setAgentName] = useState("")
  const [prompt, setPrompt] = useState("")
  const [model, setModel] = useState("claude-sonnet-4-6")
  const [selectedIntegrations, setSelectedIntegrations] = useState<Set<string>>(new Set())
  const [selectedChannels, setSelectedChannels] = useState<Set<string>>(new Set())
  const [toolPermissions, setToolPermissions] = useState<Record<string, ToolPermission>>({})
  const [groupPermissions, setGroupPermissions] = useState<Record<string, ToolPermission>>({})
  const [channelPerms, setChannelPerms] = useState<Record<string, ChannelPermissions>>({})

  function selectTemplate(t: Template) {
    setSelectedTemplate(t)
    setAgentName(t.name === "Blank agent" ? "" : t.name)
    setPrompt(t.prompt)
    setSelectedIntegrations(new Set(t.integrations))
    setSelectedChannels(new Set(t.channels))
    setToolPermissions({})
    setGroupPermissions({})
    // Set default channel permissions
    const cp: Record<string, ChannelPermissions> = {}
    for (const slug of t.channels) {
      const ch = channels.find((c) => c.slug === slug)
      if (ch) cp[slug] = { ...ch.defaultPermissions }
    }
    setChannelPerms(cp)
  }

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

  const filteredTemplates = templates.filter((t) => !search || t.name.toLowerCase().includes(search.toLowerCase()))
  const activeIntegrations = integrations.filter((i) => selectedIntegrations.has(i.slug))
  const activeChannels = channels.filter((c) => selectedChannels.has(c.slug))

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Quickstart"
        action={
          <Button size="sm" disabled={!agentName.trim()} onClick={() => router.push(`/agents/new?status=booting`)}>
            <RocketIcon />
            Launch agent
          </Button>
        }
      />
      <div className="flex flex-1 overflow-hidden">
        {/* Left: Agent configuration */}
        <div className="flex-1 overflow-y-auto p-4 pt-0">
          <div className="space-y-6">
            {/* Name + Model */}
            <div className="grid grid-cols-[1fr_200px] gap-4">
              <div className="space-y-2">
                <Label htmlFor="agent-name">Agent name</Label>
                <Input id="agent-name" value={agentName} onChange={(e) => setAgentName(e.target.value)} placeholder="e.g. Research Assistant" />
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
              <Textarea id="prompt" value={prompt} onChange={(e) => setPrompt(e.target.value)} placeholder="Describe what this agent should do..." rows={5} />
            </div>

            <Separator />

            {/* Integrations (API tools) */}
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <div>
                  <Label>Integrations</Label>
                  <p className="text-xs text-muted-foreground">API-based tools the agent can call.</p>
                </div>
                <Link href="/integrations" className="text-xs text-muted-foreground hover:text-foreground">Manage integrations →</Link>
              </div>
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

            {/* Channels (WebSocket session connectors) */}
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <div>
                  <Label>Channels</Label>
                  <p className="text-xs text-muted-foreground">Messaging platforms that connect to the agent's session via WebSocket.</p>
                </div>
                <Link href="/channels" className="text-xs text-muted-foreground hover:text-foreground">Manage channels →</Link>
              </div>
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
              <ToolPermissionSection
                title="Built-in tools" subtitle="agent_toolset"
                icon={<TerminalIcon className="size-4" />}
                tools={builtInTools} permissions={toolPermissions}
                onToggle={(k, p) => setToolPermissions((prev) => ({ ...prev, [k]: p }))}
                groupPerm={groupPermissions["builtin"] ?? "allow"}
                onGroupPerm={(p) => setGroupPermissions((prev) => ({ ...prev, builtin: p }))}
                prefix="builtin"
              />
              {activeIntegrations.map((i) => (
                <ToolPermissionSection
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
                <p className="text-xs text-muted-foreground">Control how each channel interacts with the agent session.</p>
                {activeChannels.map((c) => (
                  <ChannelPermissionSection
                    key={c.slug}
                    channel={c}
                    perms={channelPerms[c.slug] ?? c.defaultPermissions}
                    onChange={(p) => setChannelPerms((prev) => ({ ...prev, [c.slug]: p }))}
                  />
                ))}
              </div>
            )}

            <div className="h-8" />
          </div>
        </div>

        {/* Right: Templates panel */}
        <div className="hidden w-[420px] shrink-0 border-l border-border overflow-y-auto lg:block">
          <div className="p-4 space-y-3">
            <h3 className="text-sm font-medium">Templates</h3>
            <div className="relative">
              <SearchIcon className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
              <Input placeholder="Search..." value={search} onChange={(e) => setSearch(e.target.value)} className="pl-8" />
            </div>
            <div className="grid grid-cols-2 gap-2">
              {filteredTemplates.map((t) => {
                const isSelected = selectedTemplate?.name === t.name
                return (
                  <button key={t.name} type="button" onClick={() => selectTemplate(t)}
                    className={`flex flex-col gap-1.5 rounded-lg border p-3 text-left text-sm transition-colors ${isSelected ? "border-primary bg-primary/5" : "border-border hover:bg-muted/50"}`}>
                    <span className="font-medium text-xs">{t.name}</span>
                    <span className="text-[11px] text-muted-foreground line-clamp-2">{t.description}</span>
                    {(t.integrations.length > 0 || t.channels.length > 0) && (
                      <div className="flex items-center gap-1 mt-0.5">
                        {t.integrations.map((slug) => {
                          const i = integrations.find((x) => x.slug === slug)
                          return i ? <Image key={slug} src={i.logo} alt={i.name} width={14} height={14} className="shrink-0" /> : null
                        })}
                        {t.channels.length > 0 && t.integrations.length > 0 && <span className="text-[10px] text-muted-foreground mx-0.5">·</span>}
                        {t.channels.map((slug) => {
                          const c = channels.find((x) => x.slug === slug)
                          return c ? <Image key={slug} src={c.logo} alt={c.name} width={14} height={14} className="shrink-0" /> : null
                        })}
                      </div>
                    )}
                  </button>
                )
              })}
            </div>
          </div>
        </div>
      </div>
    </>
  )
}
