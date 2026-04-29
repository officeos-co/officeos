"use client"

import { useState } from "react"
import { isDevelopment } from "@/lib/env"
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
import { builtInTools, type Tool } from "@/features/agents/data/integrations"
import { type Channel, type ChannelPermissions } from "@/features/agents/data/channels"
import {
  useIntegrations, useInstallSkill, useSetSkillCredentials, sortIntegrations,
  useChannels, useAgentTemplates, useCreateAgent, useModels,
  CredentialDialog, ChannelOnboardingDialog,
  IntegrationCard, ChannelCard,
} from "@/features/agents"
import { useAnalytics } from "@/features/analytics"
import { type Template } from "@/features/agents"
import {
  SearchIcon,
  RocketIcon,
  CheckIcon,
  ChevronDownIcon,
  ChevronRightIcon,
  TerminalIcon,
  AlertTriangleIcon,
  ExternalLinkIcon,
  PlusIcon,
} from "lucide-react"

/* ── Permission types ────────────────────────────────────── */

type ToolPermission = "allow" | "deny"

const permissionLabels: Record<ToolPermission, string> = { allow: "Always allow", deny: "Deny" }
const permissionColors: Record<ToolPermission, string> = { allow: "text-emerald-600", deny: "text-red-500" }

function PermissionCycleButton({ value, onChange }: { value: ToolPermission; onChange: (p: ToolPermission) => void }) {
  const cycle: ToolPermission[] = ["allow", "deny"]
  return (
    <span
      role="button"
      tabIndex={0}
      onClick={(e) => { e.stopPropagation(); onChange(cycle[(cycle.indexOf(value) + 1) % cycle.length]) }}
      onKeyDown={(e) => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); e.stopPropagation(); onChange(cycle[(cycle.indexOf(value) + 1) % cycle.length]) } }}
      className={`text-xs whitespace-nowrap hover:underline cursor-pointer ${permissionColors[value]}`}
    >
      {permissionLabels[value]}
    </span>
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
        <div className="size-8 shrink-0 rounded-lg [&>svg]:size-8" dangerouslySetInnerHTML={{ __html: channel.logo }} />
        <div className="flex-1 min-w-0">
          <div className="text-sm font-medium">{channel.name}</div>
          <div className="text-xs text-muted-foreground">{channel.description}</div>
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
  const { integrations } = useIntegrations()
  const { channels } = useChannels()
  const { templates } = useAgentTemplates()
  const { createAgent } = useCreateAgent()
  const { models, defaultModelId } = useModels()
  const { trackAgentCreated } = useAnalytics()
  const installSkill = useInstallSkill()
  const setSkillCredentials = useSetSkillCredentials()
  const [configureSlug, setConfigureSlug] = useState<string | null>(null)
  const [onboardChannel, setOnboardChannel] = useState<Channel | null>(null)
  const [launching, setLaunching] = useState(false)
  const [search, setSearch] = useState("")
  const [selectedTemplate, setSelectedTemplate] = useState<Template | null>(null)
  const [agentName, setAgentName] = useState("")
  const [prompt, setPrompt] = useState("")
  const [model, setModel] = useState(defaultModelId)
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
  const [installingSlug, setInstallingSlug] = useState<string | null>(null)
  const sortedIntegrations = sortIntegrations(integrations)
  const sortedChannels = [...channels].sort((a, b) => (a.added === b.added ? 0 : a.added ? -1 : 1))
  const activeIntegrations = integrations.filter((i) => selectedIntegrations.has(i.slug))
  const activeChannels = channels.filter((c) => selectedChannels.has(c.slug))

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Quickstart"
        action={
          <Button size="sm" disabled={!agentName.trim() || launching} onClick={async () => {
            if (launching) return
            setLaunching(true)
            try {
              // Build the toolPermissions list: explicit per-tool overrides
              // plus group-level defaults that differ from the implicit
              // "allow" baseline. Keys are "prefix:toolname".
              const tpList: Array<{ tool: string; mode: "ALLOW" | "DENY" }> = []
              for (const [key, mode] of Object.entries(toolPermissions)) {
                tpList.push({ tool: key, mode: mode === "deny" ? "DENY" : "ALLOW" })
              }
              for (const [prefix, mode] of Object.entries(groupPermissions)) {
                if (mode === "deny") tpList.push({ tool: prefix, mode: "DENY" })
              }

              // Merge user prompt into template prompt for the system prompt.
              // The user's prompt field becomes additional context in the
              // bootstrap message so the agent actually acts on it.
              const templatePrompt = selectedTemplate?.prompt ?? ""
              const userPrompt = prompt.trim()
              const systemPrompt = templatePrompt || userPrompt
              const bootstrapMessage = userPrompt && templatePrompt
                ? `${templatePrompt}\n\n---\n\nAdditional instructions:\n${userPrompt}`
                : systemPrompt

              const created = await createAgent({
                name: agentName.trim(),
                model,
                systemPrompt,
                toolNames: Array.from(selectedIntegrations),
                toolPermissions: tpList,
                channelSlugs: Array.from(selectedChannels),
                bootstrapMessage: bootstrapMessage || undefined,
              })
              trackAgentCreated({
                agentName: agentName.trim(),
                provider: model.split("-")[0] ?? "unknown",
                template: selectedTemplate?.name ?? "scratch",
                skillCount: selectedIntegrations.size,
                allowSkills: Object.values(toolPermissions).filter((p) => p === "allow").length,
                denySkills: Object.values(toolPermissions).filter((p) => p === "deny").length,
              })
              if (created?.id) router.push(`/agents/${created.id}`)
            } catch (e) {
              console.error("Failed to create agent", e)
              setLaunching(false)
            }
          }}>
            <RocketIcon />
            {launching ? "Launching..." : "Launch agent"}
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
                {models.length === 0 && isDevelopment() ? (
                  <Link href="/providers" className="flex items-center justify-center gap-2 rounded-md border border-dashed border-border px-3 py-2 text-sm text-muted-foreground hover:text-foreground hover:border-foreground transition-colors">
                    <PlusIcon className="size-4" />
                    Add provider
                  </Link>
                ) : (
                  <Select value={model} onValueChange={(v) => { if (v) setModel(v) }}>
                    <SelectTrigger><SelectValue /></SelectTrigger>
                    <SelectContent>
                      {models.map((m) => (
                        <SelectItem key={m.id} value={m.id}>{m.displayName}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
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
                {sortedIntegrations.map((i) => (
                  <IntegrationCard
                    key={i.slug}
                    integration={i}
                    selected={selectedIntegrations.has(i.slug)}
                    installing={installingSlug === i.slug}
                    onInstall={async () => {
                      setInstallingSlug(i.slug)
                      try { await installSkill(i.slug) }
                      finally { setInstallingSlug(null) }
                    }}
                    onConfigure={() => setConfigureSlug(i.slug)}
                    onToggle={() => toggleIntegration(i.slug)}
                  />
                ))}
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
                {sortedChannels.map((c) => (
                  <ChannelCard
                    key={c.slug}
                    channel={c}
                    selected={selectedChannels.has(c.slug)}
                    onConnect={() => setOnboardChannel(c)}
                    onToggle={() => toggleChannel(c.slug)}
                  />
                ))}
              </div>
            </div>

            <Separator />

            {/* Unconfigured integrations warning */}
            {(() => {
              const unconfigured = activeIntegrations.filter((i) => i.credentialFields.length > 0 && !i.configured)
              if (unconfigured.length === 0) return null
              return (
                <div className="rounded-xl border border-amber-300 bg-amber-50 p-4">
                  <div className="flex items-start gap-3">
                    <AlertTriangleIcon className="size-5 text-amber-600 shrink-0 mt-0.5" />
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-amber-800">
                        {unconfigured.length === 1
                          ? `${unconfigured[0].name} requires credentials before it can be used.`
                          : `${unconfigured.length} integrations require credentials before they can be used.`}
                      </p>
                      <p className="text-xs text-amber-700 mt-1">
                        The agent will not be able to use unconfigured integrations. Set up credentials on the integration page.
                      </p>
                      <div className="flex flex-wrap gap-2 mt-3">
                        {unconfigured.map((i) => (
                          <Link key={i.slug} href={`/integrations/${i.slug}`}
                            className="inline-flex items-center gap-1.5 rounded-md bg-amber-100 px-2.5 py-1 text-xs font-medium text-amber-800 hover:bg-amber-200 transition-colors">
                            <div className="size-3.5 shrink-0 [&>svg]:size-3.5" dangerouslySetInnerHTML={{ __html: i.logo }} />
                            {i.name}
                            <ExternalLinkIcon className="size-3" />
                          </Link>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              )
            })()}

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
                  key={i.slug} title={i.name} subtitle={i.sourceCodeUrl.replace("https://github.com/", "")}
                  icon={<div className="size-4 [&>svg]:size-4" dangerouslySetInnerHTML={{ __html: i.logo }} />}
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

        {/* Right: Templates panel — sticky */}
        <div className="hidden w-[420px] shrink-0 border-l border-border lg:block sticky top-0 self-start h-screen overflow-y-auto">
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
                          return i ? <div key={slug} className="size-[14px] shrink-0 [&>svg]:size-[14px]" dangerouslySetInnerHTML={{ __html: i.logo }} /> : null
                        })}
                        {t.channels.length > 0 && t.integrations.length > 0 && <span className="text-[10px] text-muted-foreground mx-0.5">·</span>}
                        {t.channels.map((slug) => {
                          const c = channels.find((x) => x.slug === slug)
                          return c ? <div key={slug} className="size-[14px] shrink-0 [&>svg]:size-[14px]" dangerouslySetInnerHTML={{ __html: c.logo }} /> : null
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

      {/* Credential dialog for inline configure */}
      {configureSlug && (() => {
        const i = integrations.find((x) => x.slug === configureSlug)
        if (!i) return null
        return (
          <CredentialDialog
            open
            onOpenChange={(open) => { if (!open) setConfigureSlug(null) }}
            name={i.name}
            slug={i.slug}
            logo={i.logo}
            credentials={i.credentialFields}
            onSave={(values) => {
              setSkillCredentials(i.slug, values)
              setConfigureSlug(null)
            }}
          />
        )
      })()}

      {/* Channel onboarding dialog for inline connect */}
      {onboardChannel && (
        <ChannelOnboardingDialog
          open
          onOpenChange={(open) => { if (!open) setOnboardChannel(null) }}
          channel={onboardChannel}
          onComplete={() => setOnboardChannel(null)}
        />
      )}
    </>
  )
}
