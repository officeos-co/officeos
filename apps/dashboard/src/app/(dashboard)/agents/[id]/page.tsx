"use client"

import { use, useState, useEffect } from "react"
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
import { Skeleton } from "@/components/ui/skeleton"
import { Switch } from "@/components/ui/switch"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Separator } from "@/components/ui/separator"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { mockFileTree, type FileNode } from "@/features/agents/data/agent-mock"
import { builtInTools } from "@/features/agents/data/integrations"
import { type ChannelPermissions } from "@/features/agents/data/channels"
import { useAgent } from "@/features/agents"
import { useAgentLogs } from "@/features/analytics"
import { useSendAgentMessage } from "@/features/agents"
import { useIntegrations } from "@/features/agents"
import { useChannels, useModels } from "@/features/agents"
import {
  SendIcon,
  ClockIcon,
  FileTextIcon,
  FolderIcon,
  ChevronRightIcon,
  TerminalIcon,
  CheckIcon,
  PlusIcon,
  Trash2Icon,
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
  { key: "integrations", label: "Integrations" },
  { key: "logs", label: "Logs" },
  { key: "memory", label: "Memory" },
  { key: "cron", label: "Cron" },
] as const
type TabKey = (typeof TABS)[number]["key"]


/* ── Agent tab (mirrors quickstart) ──────────────────────── */

function AgentTab({ agentId }: { agentId: string }) {
  const { agent } = useAgent(agentId)
  const { integrations } = useIntegrations()
  const { channels } = useChannels()
  const mockAgent = agent ?? { id: agentId, name: "", model: "claude-sonnet-4-6", status: "stopped", prompt: "", integrations: [] as string[], channels: [] as string[], createdAt: Date.now() }
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
      {/* Integrations */}
      <div className="space-y-3">
        <Label>Integrations</Label>
        <div className="grid grid-cols-3 gap-2">
          {integrations.map((i) => {
            const active = selectedIntegrations.has(i.slug)
            return (
              <button key={i.slug} type="button" onClick={() => toggleIntegration(i.slug)}
                className={`flex items-center gap-2.5 rounded-lg border px-3 py-2 text-left text-sm transition-colors ${active ? "border-primary bg-primary/5" : "border-border hover:bg-muted/50"}`}>
                <div className="size-[18px] shrink-0 [&>svg]:size-[18px]" dangerouslySetInnerHTML={{ __html: i.logo }} />
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
                <div className="size-[18px] shrink-0 [&>svg]:size-[18px]" dangerouslySetInnerHTML={{ __html: c.logo }} />
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

function LogsTab({ agentId }: { agentId: string }) {
  const { logs } = useAgentLogs(agentId)
  return (
    <div className="flex flex-col flex-1 pt-4">
      <div className="flex-1 overflow-x-auto">
        <LogTable logs={logs} />
      </div>
    </div>
  )
}

/* ── Memory tab ──────────────────────────────────────────── */

function FileTreeItem({
  node,
  depth,
  selectedPath,
  onSelect,
  parentPath,
}: {
  node: FileNode
  depth: number
  selectedPath: string
  onSelect: (path: string, content: string) => void
  parentPath: string
}) {
  const [open, setOpen] = useState(depth < 2)
  const path = parentPath ? `${parentPath}/${node.name}` : node.name

  if (node.type === "folder") {
    return (
      <div>
        <button
          type="button"
          onClick={() => setOpen(!open)}
          className="flex w-full items-center gap-1.5 rounded-md px-2 py-1.5 text-left text-xs hover:bg-muted/50 transition-colors"
          style={{ paddingLeft: `${depth * 12 + 8}px` }}
        >
          <ChevronRightIcon className={`size-3 text-muted-foreground shrink-0 transition-transform ${open ? "rotate-90" : ""}`} />
          <FolderIcon className="size-3.5 text-muted-foreground shrink-0" />
          <span className="font-mono truncate">{node.name}</span>
        </button>
        {open && node.children?.map((child) => (
          <FileTreeItem
            key={child.name}
            node={child}
            depth={depth + 1}
            selectedPath={selectedPath}
            onSelect={onSelect}
            parentPath={path}
          />
        ))}
      </div>
    )
  }

  const isSelected = selectedPath === path
  return (
    <button
      type="button"
      onClick={() => onSelect(path, node.content ?? "")}
      className={`flex w-full items-center gap-1.5 rounded-md px-2 py-1.5 text-left text-xs transition-colors ${
        isSelected ? "bg-primary/5 text-foreground" : "hover:bg-muted/50 text-muted-foreground"
      }`}
      style={{ paddingLeft: `${depth * 12 + 8 + 16}px` }}
    >
      <FileTextIcon className="size-3.5 shrink-0" />
      <span className="font-mono truncate">{node.name}</span>
    </button>
  )
}

function MemoryTab() {
  const [selectedPath, setSelectedPath] = useState("USER.md")
  const [content, setContent] = useState(mockFileTree[0]?.content ?? "")

  function handleSelect(path: string, fileContent: string) {
    setSelectedPath(path)
    setContent(fileContent)
  }

  return (
    <div className="flex gap-4 pt-4 min-h-0">
      {/* File browser */}
      <div className="w-56 shrink-0 overflow-y-auto rounded-xl border border-border bg-card">
        <div className="px-3 py-2.5 border-b border-border text-xs font-medium text-muted-foreground">Files</div>
        <div className="p-1.5">
          {mockFileTree.map((node) => (
            <FileTreeItem
              key={node.name}
              node={node}
              depth={0}
              selectedPath={selectedPath}
              onSelect={handleSelect}
              parentPath=""
            />
          ))}
        </div>
      </div>

      {/* File content */}
      <div className="flex-1 rounded-xl border border-border bg-card overflow-y-auto">
        <div className="px-4 py-2.5 border-b border-border flex items-center gap-2">
          <FileTextIcon className="size-3.5 text-muted-foreground" />
          <span className="font-mono text-xs">{selectedPath}</span>
        </div>
        <div className="p-6 prose prose-sm max-w-none prose-headings:font-semibold prose-headings:text-foreground prose-h1:text-lg prose-h1:mt-0 prose-h1:mb-3 prose-h2:text-sm prose-h2:mt-5 prose-h2:mb-2 prose-p:text-sm prose-p:text-muted-foreground prose-li:text-sm prose-li:text-muted-foreground prose-strong:text-foreground prose-code:rounded prose-code:bg-muted prose-code:px-1.5 prose-code:py-0.5 prose-code:text-xs prose-code:before:content-none prose-code:after:content-none">
          <ReactMarkdown remarkPlugins={[remarkGfm]}>{content}</ReactMarkdown>
        </div>
      </div>
    </div>
  )
}

/* ── Cron tab ────────────────────────────────────────────── */

type CronJob = {
  id: string
  name: string
  expression: string
  human: string
  prompt: string
  enabled: boolean
  lastRun: string | null
  nextRun: string
}

const mockCronJobs: CronJob[] = [
  {
    id: "cron_001",
    name: "Daily research digest",
    expression: "0 9 * * 1-5",
    human: "Every weekday at 9:00 AM",
    prompt: "Search for the latest AI industry news and compile a digest. Post the summary to #research on Slack.",
    enabled: true,
    lastRun: "Today at 09:00",
    nextRun: "Tomorrow at 09:00",
  },
  {
    id: "cron_002",
    name: "Weekly ticket cleanup",
    expression: "0 17 * * 5",
    human: "Every Friday at 5:00 PM",
    prompt: "Review all open Linear issues older than 14 days. Comment on stale ones asking for updates. Close issues with no activity in 30 days.",
    enabled: true,
    lastRun: "Last Friday at 17:00",
    nextRun: "Friday at 17:00",
  },
  {
    id: "cron_003",
    name: "Compliance check",
    expression: "0 8 1 * *",
    human: "1st of every month at 8:00 AM",
    prompt: "Search for new regulatory updates relevant to our industry. Cross-reference against internal policies in Notion. Flag any gaps to #compliance.",
    enabled: false,
    lastRun: "Apr 1 at 08:00",
    nextRun: "May 1 at 08:00",
  },
]

const CRON_PRESETS = [
  { label: "Every hour", expression: "0 * * * *" },
  { label: "Every day at 9 AM", expression: "0 9 * * *" },
  { label: "Every weekday at 9 AM", expression: "0 9 * * 1-5" },
  { label: "Every Monday at 9 AM", expression: "0 9 * * 1" },
  { label: "Every Friday at 5 PM", expression: "0 17 * * 5" },
  { label: "1st of every month", expression: "0 8 1 * *" },
]

function CronTab() {
  const [jobs, setJobs] = useState<CronJob[]>(mockCronJobs)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [name, setName] = useState("")
  const [expression, setExpression] = useState("")
  const [prompt, setPrompt] = useState("")

  function handleCreate() {
    const newJob: CronJob = {
      id: `cron_${Date.now()}`,
      name: name || "Untitled job",
      expression,
      human: CRON_PRESETS.find((p) => p.expression === expression)?.label ?? expression,
      prompt,
      enabled: true,
      lastRun: null,
      nextRun: "Pending first run",
    }
    setJobs([newJob, ...jobs])
    setDialogOpen(false)
    setName("")
    setExpression("")
    setPrompt("")
  }

  function toggleEnabled(id: string) {
    setJobs((prev) => prev.map((j) => j.id === id ? { ...j, enabled: !j.enabled } : j))
  }

  function deleteJob(id: string) {
    setJobs((prev) => prev.filter((j) => j.id !== id))
  }

  return (
    <div className="pt-4 space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-sm font-medium">Scheduled tasks</h3>
          <p className="text-xs text-muted-foreground">Cron jobs run this agent on a schedule with a specific prompt.</p>
        </div>
        <Button size="sm" onClick={() => setDialogOpen(true)}>
          <PlusIcon className="size-3.5" />
          New schedule
        </Button>
      </div>

      {/* Job list — always expanded */}
      <div className="space-y-2">
        {jobs.map((job) => (
          <div key={job.id} className="rounded-xl border border-border">
            <div className="flex items-center gap-4 px-4 py-3">
              <Switch checked={job.enabled} onCheckedChange={() => toggleEnabled(job.id)} />
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  <span className={`text-sm font-medium ${!job.enabled ? "text-muted-foreground" : ""}`}>{job.name}</span>
                  <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-[10px] text-muted-foreground">{job.expression}</code>
                </div>
                <div className="text-xs text-muted-foreground mt-0.5">{job.human}</div>
              </div>
              <div className="flex items-center gap-4 shrink-0 text-xs text-muted-foreground">
                <div className="text-right hidden sm:block">
                  <div>Last: {job.lastRun ?? "Never"}</div>
                  <div>Next: {job.nextRun}</div>
                </div>
                <Button variant="ghost" size="sm" className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive" onClick={() => deleteJob(job.id)}>
                  <Trash2Icon className="size-3.5" />
                </Button>
              </div>
            </div>
            <div className="border-t border-border px-4 py-3">
              <div className="text-xs text-muted-foreground mb-1">Prompt</div>
              <p className="text-sm">{job.prompt}</p>
            </div>
          </div>
        ))}
      </div>

      {jobs.length === 0 && (
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <ClockIcon className="size-8 text-muted-foreground/30 mb-3" />
          <p className="text-sm font-medium">No scheduled tasks</p>
          <p className="text-sm text-muted-foreground mt-1">Create a cron job to run this agent on a schedule.</p>
          <Button size="sm" className="mt-4" onClick={() => setDialogOpen(true)}>
            <PlusIcon className="size-3.5" /> New schedule
          </Button>
        </div>
      )}

      {/* Create dialog */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>New scheduled task</DialogTitle>
            <DialogDescription>Run this agent automatically on a cron schedule.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 pt-2">
            <div className="space-y-2">
              <Label htmlFor="cron-name">Name</Label>
              <Input id="cron-name" value={name} onChange={(e) => setName(e.target.value)} placeholder="e.g. Daily research digest" />
            </div>

            <div className="space-y-2">
              <Label>Schedule</Label>
              <div className="flex flex-wrap gap-1.5">
                {CRON_PRESETS.map((p) => (
                  <button
                    key={p.expression}
                    type="button"
                    onClick={() => setExpression(p.expression)}
                    className={`rounded-lg border px-3 py-1.5 text-xs transition-colors ${
                      expression === p.expression ? "border-primary bg-primary/5" : "border-border hover:bg-muted/50"
                    }`}
                  >
                    {p.label}
                  </button>
                ))}
              </div>
              <div className="flex items-center gap-2">
                <Input
                  value={expression}
                  onChange={(e) => setExpression(e.target.value)}
                  placeholder="Custom: 0 9 * * 1-5"
                  className="max-w-[200px] font-mono text-xs"
                />
                {expression && (
                  <span className="text-xs text-muted-foreground">
                    {CRON_PRESETS.find((p) => p.expression === expression)?.label ?? "Custom schedule"}
                  </span>
                )}
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="cron-prompt">Prompt</Label>
              <Textarea id="cron-prompt" value={prompt} onChange={(e) => setPrompt(e.target.value)} placeholder="What should the agent do on this schedule?" rows={3} />
            </div>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" size="sm" onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button size="sm" onClick={handleCreate} disabled={!expression.trim()}>Create schedule</Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}

/* ── Skeleton loading state ───────────────────────────────── */

function AgentSkeleton() {
  return (
    <>
      <div className="sticky top-0 z-10 bg-background border-b border-border">
        <div className="max-w-6xl mx-auto w-full px-4">
          <div className="flex items-start justify-between py-4">
            <div className="space-y-2">
              <div className="flex items-center gap-2.5">
                <Skeleton className="h-6 w-48" />
                <Skeleton className="h-6 w-20 rounded-full" />
              </div>
              <Skeleton className="h-3 w-72" />
              <Skeleton className="h-4 w-96" />
            </div>
            <Skeleton className="h-8 w-24 rounded-md" />
          </div>
          <div className="flex gap-1 -mb-px">
            <Skeleton className="h-8 w-16" />
            <Skeleton className="h-8 w-12" />
            <Skeleton className="h-8 w-18" />
            <Skeleton className="h-8 w-12" />
          </div>
        </div>
      </div>
      <div className="flex flex-1 flex-col px-4 max-w-6xl mx-auto w-full pt-6 space-y-4">
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-32 w-full rounded-xl" />
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-24 w-full rounded-xl" />
        <Skeleton className="h-24 w-full rounded-xl" />
      </div>
    </>
  )
}

/* ── Page ─────────────────────────────────────────────────── */

export default function AgentDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params)
  const searchParams = useSearchParams()
  const router = useRouter()
  const initialStatus = searchParams.get("status")
  const { agent, loading: agentLoading } = useAgent(id)
  const { models } = useModels()
  const mockAgent = agent ?? { id, name: "", model: "", status: "stopped", prompt: "", integrations: [] as string[], channels: [] as string[], createdAt: Date.now() }
  const [agentStatus, setAgentStatus] = useState(initialStatus === "booting" ? "booting" : mockAgent.status)
  const [model, setModel] = useState(mockAgent.model)
  const tab = (searchParams.get("tab") as TabKey) ?? "integrations"

  // Simulate boot → running transition
  useEffect(() => {
    if (agentStatus === "booting") {
      const t = setTimeout(() => {
        setAgentStatus("running")
        router.replace(`/agents/${id}?tab=integrations`)
      }, 5000)
      return () => clearTimeout(t)
    }
  }, [agentStatus, id, router])

  if (agentStatus === "booting" || (agentLoading && !agent)) {
    return (
      <>
        <PageHeader group="Agents" page={mockAgent.name || "Loading..."} />
        <AgentSkeleton />
      </>
    )
  }

  const [message, setMessage] = useState("")
  const { sendAgentMessage } = useSendAgentMessage()
  const submit = () => {
    if (!message.trim()) return
    sendAgentMessage(id, message)
    setMessage("")
  }

  return (
    <>
      <PageHeader group="Agents" page={mockAgent.name} />

      {/* Sticky agent header + tabs */}
      <div className="sticky top-0 z-10 bg-background border-b border-border">
        <div className="max-w-6xl mx-auto w-full px-4">
          <div className="flex items-start justify-between py-4">
            <div>
              <div className="flex items-center gap-2.5">
                <h1 className="text-lg font-semibold">{mockAgent.name}</h1>
                <StatusBadge status={agentStatus} />
              </div>
              <div className="mt-1 text-xs text-muted-foreground">
                <span className="font-mono">{mockAgent.id}</span>
                <span className="mx-1.5">·</span>
                <span>Last updated 41 minutes ago</span>
              </div>
            </div>
            <div className="flex items-center gap-2 shrink-0">
              <Select value={model} onValueChange={(v) => { if (v) setModel(v) }}>
                <SelectTrigger className="w-[180px] h-8 text-xs">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {models.map((m) => (
                    <SelectItem key={m.id} value={m.id}>{m.displayName}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <Button variant="outline" size="sm" render={<Link href="/agents" />}>All agents</Button>
            </div>
          </div>

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

      <div className="flex flex-1 flex-col px-4 max-w-6xl mx-auto w-full pb-20">
        <div className="flex-1 flex flex-col">
          {tab === "integrations" && <AgentTab agentId={id} />}
          {tab === "logs" && <LogsTab agentId={id} />}
          {tab === "memory" && <MemoryTab />}
          {tab === "cron" && <CronTab />}
        </div>
      </div>

      {/* Sticky chat bar — visible on all tabs */}
      <div className="sticky bottom-0 z-10 border-t border-border bg-background/80 backdrop-blur-sm p-3">
        <div className="flex items-center gap-2 max-w-3xl mx-auto">
          <Input value={message} onChange={(e) => setMessage(e.target.value)} placeholder="Send a message to the agent..." className="flex-1" onKeyDown={(e) => { if (e.key === "Enter" && message.trim()) submit() }} />
          <Button size="icon" disabled={!message.trim()} onClick={submit}><SendIcon className="size-4" /></Button>
        </div>
      </div>
    </>
  )
}
