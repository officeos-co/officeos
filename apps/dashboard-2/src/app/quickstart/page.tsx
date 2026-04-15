"use client"

import { useState } from "react"
import Image from "next/image"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { integrations, builtInTools, sourceUrl, type Integration, type Tool } from "@/data/integrations"
import {
  SearchIcon,
  RocketIcon,
  CheckIcon,
  ChevronDownIcon,
  ChevronRightIcon,
  TerminalIcon,
} from "lucide-react"

type Template = {
  name: string
  description: string
  integrations: string[]
  prompt: string
}

const templates: Template[] = [
  { name: "Blank agent", description: "A blank starting point. Configure everything from scratch.", integrations: [], prompt: "" },
  { name: "Deep researcher", description: "Conducts multi-step web research with source citations.", integrations: ["browser"], prompt: "You are a research assistant. When given a topic, conduct thorough web research, synthesize findings, and present them with source citations." },
  { name: "Support agent", description: "Answers customer questions from your docs and escalates when needed.", integrations: ["notion", "slack"], prompt: "You are a customer support agent. Answer questions using the knowledge base in Notion. If you cannot find an answer, escalate to the #support-escalation Slack channel." },
  { name: "Incident commander", description: "Triages alerts, opens a Linear ticket, and runs the Slack war room.", integrations: ["slack", "linear", "browser"], prompt: "You are an incident commander. When an alert comes in, triage the severity, create a Linear issue, and post an incident summary to the #incidents Slack channel." },
  { name: "Code reviewer", description: "Reviews pull requests for bugs, style issues, and security vulnerabilities.", integrations: ["github"], prompt: "You are a code reviewer. Review the diff for bugs, security vulnerabilities, and style inconsistencies. Leave constructive comments." },
  { name: "Feedback miner", description: "Clusters raw feedback into themes and drafts tasks.", integrations: ["slack", "notion"], prompt: "You are a feedback analyst. Collect feedback from Slack and Notion, cluster into themes, and draft actionable tasks." },
  { name: "Sprint retro facilitator", description: "Pulls a closed sprint from Linear and writes the retro doc.", integrations: ["linear", "notion"], prompt: "Pull completed issues from the latest Linear sprint, identify patterns, and write a retro summary in Notion." },
  { name: "Compliance monitor", description: "Watches for regulatory changes and flags risks.", integrations: ["browser", "notion", "slack"], prompt: "Search for regulatory updates, cross-reference against internal policies in Notion, and flag risks to #compliance." },
  { name: "Sales assistant", description: "Enriches leads, drafts outreach, and logs to CRM.", integrations: ["hubspot", "browser"], prompt: "Research leads using web search, draft personalized outreach emails, and log activity in HubSpot." },
  { name: "Data analyst", description: "Answers questions using web search and structured extraction.", integrations: ["browser"], prompt: "Search for relevant data sources, extract structured information, and present clear answers." },
]

type Permission = "ask" | "allow" | "deny"

const permissionLabels: Record<Permission, string> = {
  ask: "Ask",
  allow: "Always allow",
  deny: "Deny",
}

function PermissionButton({ value, onChange }: { value: Permission; onChange: (p: Permission) => void }) {
  const cycle: Permission[] = ["ask", "allow", "deny"]
  const next = () => onChange(cycle[(cycle.indexOf(value) + 1) % cycle.length])
  const color = value === "allow" ? "text-emerald-600" : value === "deny" ? "text-red-500" : "text-muted-foreground"
  return (
    <button type="button" onClick={next} className={`text-xs whitespace-nowrap ${color} hover:underline`}>
      {permissionLabels[value]}
    </button>
  )
}

function ToolPermissionSection({
  title,
  subtitle,
  icon,
  tools,
  permissions,
  onToggle,
  groupPermission,
  onGroupPermission,
  prefix,
}: {
  title: string
  subtitle?: string
  icon: React.ReactNode
  tools: Tool[]
  permissions: Record<string, Permission>
  onToggle: (key: string, p: Permission) => void
  groupPermission: Permission
  onGroupPermission: (p: Permission) => void
  prefix: string
}) {
  const [expanded, setExpanded] = useState(true)

  return (
    <div className="rounded-xl border border-border">
      {/* Header */}
      <div className="flex items-center gap-3 px-4 py-3">
        <div className="flex size-8 items-center justify-center rounded-lg bg-muted shrink-0">
          {icon}
        </div>
        <div className="flex-1 min-w-0">
          <div className="text-sm font-medium">{title}</div>
          {subtitle && <div className="text-xs text-muted-foreground">{subtitle}</div>}
        </div>
      </div>
      {/* Collapsible tool list */}
      <div className="border-t border-border">
        <button
          type="button"
          onClick={() => setExpanded(!expanded)}
          className="flex w-full items-center gap-2 px-4 py-2.5 text-left hover:bg-muted/50 transition-colors"
        >
          {expanded
            ? <ChevronDownIcon className="size-4 text-muted-foreground" />
            : <ChevronRightIcon className="size-4 text-muted-foreground" />
          }
          <span className="text-xs font-medium">Tool permissions</span>
          <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] text-muted-foreground">{tools.length}</span>
          <span className="ml-auto">
            <PermissionButton value={groupPermission} onChange={onGroupPermission} />
          </span>
        </button>
        {expanded && (
          <div>
            {tools.map((tool) => {
              const key = `${prefix}:${tool.name}`
              const perm = permissions[key] ?? groupPermission
              return (
                <div key={tool.name} className="flex items-center gap-4 px-4 py-2.5 border-t border-border">
                  <code className="rounded bg-muted px-2 py-0.5 font-mono text-xs min-w-[100px]">{tool.name}</code>
                  <span className="flex-1 text-sm text-muted-foreground">{tool.description}</span>
                  <PermissionButton value={perm} onChange={(p) => onToggle(key, p)} />
                </div>
              )
            })}
          </div>
        )}
      </div>
    </div>
  )
}

export default function QuickstartPage() {
  const [search, setSearch] = useState("")
  const [selectedTemplate, setSelectedTemplate] = useState<Template | null>(null)
  const [agentName, setAgentName] = useState("")
  const [prompt, setPrompt] = useState("")
  const [model, setModel] = useState("claude-sonnet-4-6")
  const [selectedIntegrations, setSelectedIntegrations] = useState<Set<string>>(new Set())
  const [toolPermissions, setToolPermissions] = useState<Record<string, Permission>>({})
  const [groupPermissions, setGroupPermissions] = useState<Record<string, Permission>>({})

  function selectTemplate(t: Template) {
    setSelectedTemplate(t)
    setAgentName(t.name === "Blank agent" ? "" : t.name)
    setPrompt(t.prompt)
    setSelectedIntegrations(new Set(t.integrations))
    setToolPermissions({})
    setGroupPermissions({})
  }

  function toggleIntegration(slug: string) {
    setSelectedIntegrations((prev) => {
      const next = new Set(prev)
      if (next.has(slug)) next.delete(slug)
      else next.add(slug)
      return next
    })
  }

  const filteredTemplates = templates.filter(
    (t) => !search || t.name.toLowerCase().includes(search.toLowerCase())
  )

  const activeIntegrations = integrations.filter((i) => selectedIntegrations.has(i.slug))

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Quickstart"
        action={
          <Button size="sm" disabled={!agentName.trim()}>
            <RocketIcon />
            Launch agent
          </Button>
        }
      />
      <div className="flex flex-1 overflow-hidden">
        {/* Left: Agent configuration */}
        <div className="flex-1 overflow-y-auto p-4 pt-0">
          <div className="space-y-6">
            {/* Name + Model row */}
            <div className="grid grid-cols-[1fr_200px] gap-4">
              <div className="space-y-2">
                <Label htmlFor="agent-name">Agent name</Label>
                <Input
                  id="agent-name"
                  value={agentName}
                  onChange={(e) => setAgentName(e.target.value)}
                  placeholder="e.g. Research Assistant"
                />
              </div>
              <div className="space-y-2">
                <Label>Model</Label>
                <Select value={model} onValueChange={(v) => { if (v) setModel(v) }}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
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
              <Textarea
                id="prompt"
                value={prompt}
                onChange={(e) => setPrompt(e.target.value)}
                placeholder="Describe what this agent should do..."
                rows={5}
              />
            </div>

            {/* Integrations picker */}
            <div className="space-y-3">
              <Label>Integrations</Label>
              <div className="grid grid-cols-3 gap-2">
                {integrations.map((i) => {
                  const active = selectedIntegrations.has(i.slug)
                  return (
                    <button
                      key={i.slug}
                      type="button"
                      onClick={() => toggleIntegration(i.slug)}
                      className={`flex items-center gap-2.5 rounded-lg border px-3 py-2 text-left text-sm transition-colors ${
                        active
                          ? "border-primary bg-primary/5"
                          : "border-border hover:bg-muted/50"
                      }`}
                    >
                      <Image src={i.logo} alt={i.name} width={18} height={18} className="shrink-0" />
                      <span className="flex-1 truncate">{i.name}</span>
                      {active && <CheckIcon className="size-3.5 text-primary shrink-0" />}
                    </button>
                  )
                })}
              </div>
            </div>

            {/* Tools & Permissions — Claude style */}
            <div className="space-y-3">
              <Label>Tools and permissions</Label>

              {/* Built-in tools */}
              <ToolPermissionSection
                title="Built-in tools"
                subtitle="agent_toolset"
                icon={<TerminalIcon className="size-4" />}
                tools={builtInTools}
                permissions={toolPermissions}
                onToggle={(k, p) => setToolPermissions((prev) => ({ ...prev, [k]: p }))}
                groupPermission={groupPermissions["builtin"] ?? "allow"}
                onGroupPermission={(p) => setGroupPermissions((prev) => ({ ...prev, builtin: p }))}
                prefix="builtin"
              />

              {/* Integration tools */}
              {activeIntegrations.map((i) => (
                <ToolPermissionSection
                  key={i.slug}
                  title={i.name}
                  subtitle={sourceUrl(i.slug).replace("https://github.com/", "")}
                  icon={<Image src={i.logo} alt={i.name} width={16} height={16} />}
                  tools={i.tools}
                  permissions={toolPermissions}
                  onToggle={(k, p) => setToolPermissions((prev) => ({ ...prev, [k]: p }))}
                  groupPermission={groupPermissions[i.slug] ?? "allow"}
                  onGroupPermission={(p) => setGroupPermissions((prev) => ({ ...prev, [i.slug]: p }))}
                  prefix={i.slug}
                />
              ))}
            </div>

            <div className="h-8" />
          </div>
        </div>

        {/* Right: Templates panel */}
        <div className="hidden w-[420px] shrink-0 border-l border-border overflow-y-auto lg:block">
          <div className="p-4 space-y-3">
            <h3 className="text-sm font-medium">Templates</h3>
            <div className="relative">
              <SearchIcon className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
              <Input
                placeholder="Search..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-8"
              />
            </div>
            <div className="grid grid-cols-2 gap-2">
              {filteredTemplates.map((t) => {
                const isSelected = selectedTemplate?.name === t.name
                return (
                  <button
                    key={t.name}
                    type="button"
                    onClick={() => selectTemplate(t)}
                    className={`flex flex-col gap-1.5 rounded-lg border p-3 text-left text-sm transition-colors ${
                      isSelected
                        ? "border-primary bg-primary/5"
                        : "border-border hover:bg-muted/50"
                    }`}
                  >
                    <span className="font-medium text-xs">{t.name}</span>
                    <span className="text-[11px] text-muted-foreground line-clamp-2">{t.description}</span>
                    {t.integrations.length > 0 && (
                      <div className="flex items-center gap-1 mt-0.5">
                        {t.integrations.map((slug) => {
                          const integration = integrations.find((i) => i.slug === slug)
                          if (!integration) return null
                          return (
                            <Image
                              key={slug}
                              src={integration.logo}
                              alt={integration.name}
                              width={14}
                              height={14}
                              className="shrink-0"
                            />
                          )
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
