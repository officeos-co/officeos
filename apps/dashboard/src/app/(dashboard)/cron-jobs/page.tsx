"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
  CalendarClockIcon,
  HeartPulseIcon,
  Trash2Icon,
} from "lucide-react";
import { PageContainer } from "@/components/page-container";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { SearchInput } from "@/components/ui/search-input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  TableSelectionCell,
  TableSelectionHead,
  TableSelectionToolbar,
} from "@/components/ui/table";
import { Textarea } from "@/components/ui/textarea";
import {
  describeCronExpression,
  isHeartbeatCron,
  useAgents,
  useAllCronJobs,
} from "@/features/agents";

type Frequency = "heartbeat" | "hourly" | "daily";

function expressionFor(frequency: Frequency) {
  if (frequency === "heartbeat") return "*/30 * * * *";
  if (frequency === "hourly") return "0 * * * *";
  return "0 9 * * *";
}

export default function CronJobsPage() {
  const router = useRouter();
  const [createOpen, setCreateOpen] = useState(false);
  const { jobs, loading, creating, createCronJob, deleteCronJob, refetch } =
    useAllCronJobs();
  const { agents } = useAgents();
  const [search, setSearch] = useState("");
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [agentId, setAgentId] = useState("");
  const [name, setName] = useState("");
  const [frequency, setFrequency] = useState<Frequency>("daily");
  const [prompt, setPrompt] = useState("");
  const selectedAgent = agents.find((agent) => agent.id === agentId);

  const filtered = useMemo(() => {
    const query = search.toLowerCase();
    return jobs.filter((job) => {
      if (!query) return true;
      const schedule = describeCronExpression(job.expression.value);
      const status = job.enabled ? "enabled" : "disabled";
      return (
        job.id.toLowerCase().includes(query) ||
        job.name.toLowerCase().includes(query) ||
        job.agentName.toLowerCase().includes(query) ||
        job.expression.value.toLowerCase().includes(query) ||
        schedule.toLowerCase().includes(query) ||
        status.includes(query)
      );
    });
  }, [jobs, search]);
  const filteredIds = useMemo(() => filtered.map((job) => job.id), [filtered]);
  const selectedVisibleCount = filteredIds.filter((id) =>
    selectedIds.has(id),
  ).length;
  const allVisibleSelected =
    filteredIds.length > 0 && selectedVisibleCount === filteredIds.length;
  const someVisibleSelected = selectedVisibleCount > 0 && !allVisibleSelected;

  function toggleJob(jobId: string, checked: boolean) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (checked) next.add(jobId);
      else next.delete(jobId);
      return next;
    });
  }

  function toggleVisibleJobs(checked: boolean) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      for (const id of filteredIds) {
        if (checked) next.add(id);
        else next.delete(id);
      }
      return next;
    });
  }

  async function deleteSelectedJobs() {
    const ids = Array.from(selectedIds);
    await Promise.all(ids.map((id) => deleteCronJob(id)));
    setSelectedIds(new Set());
    refetch();
  }

  async function submit() {
    if (!agentId || !prompt.trim()) return;
    const expression = expressionFor(frequency);
    await createCronJob(
      agentId,
      name.trim() ||
        (isHeartbeatCron(expression) ? "Heartbeat" : "Scheduled task"),
      expression,
      prompt.trim(),
    );
    setCreateOpen(false);
    setName("");
    setPrompt("");
    setFrequency("daily");
  }

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Cron Jobs"
        subtitle="Manage scheduled agent tasks."
        width="wide"
        action={
          <Button size="sm" onClick={() => setCreateOpen(true)}>
            <CalendarClockIcon className="size-4" />
            Create
          </Button>
        }
      />
      <PageContainer width="wide" className="flex flex-1 flex-col gap-4 pb-4">
        <div className="flex min-h-9 items-center justify-between gap-2">
          <SearchInput
            placeholder="Search cron jobs..."
            value={search}
            onChange={setSearch}
          />
          <TableSelectionToolbar selectedCount={selectedIds.size}>
            <Button
              variant="destructive"
              size="sm"
              onClick={deleteSelectedJobs}
            >
              <Trash2Icon className="size-4" />
              Delete
            </Button>
          </TableSelectionToolbar>
        </div>

        <Table>
          <TableHeader>
            <TableRow>
              <TableSelectionHead
                checked={allVisibleSelected}
                indeterminate={someVisibleSelected}
                onCheckedChange={toggleVisibleJobs}
              />
              <TableHead>ID</TableHead>
              <TableHead>Name</TableHead>
              <TableHead>Agent</TableHead>
              <TableHead>Schedule</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Next run</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filtered.map((job) => (
              <TableRow
                key={job.id}
                data-state={selectedIds.has(job.id) ? "selected" : undefined}
                onClick={() => router.push(`/cron-jobs/${job.id}`)}
                className="cursor-pointer"
              >
                <TableSelectionCell
                  checked={selectedIds.has(job.id)}
                  aria-label={`Select ${job.name}`}
                  onCheckedChange={(checked) => toggleJob(job.id, checked)}
                />
                <TableCell>{job.id}</TableCell>
                <TableCell>
                  <span className="inline-flex items-center gap-2 font-medium">
                    {isHeartbeatCron(job.expression.value) ? (
                      <HeartPulseIcon className="size-3.5 text-rose-500" />
                    ) : (
                      <CalendarClockIcon className="size-3.5 text-muted-foreground" />
                    )}
                    {job.name}
                  </span>
                </TableCell>
                <TableCell>{job.agentName}</TableCell>
                <TableCell>
                  {describeCronExpression(job.expression.value)}
                </TableCell>
                <TableCell>{job.enabled ? "Enabled" : "Disabled"}</TableCell>
                <TableCell>
                  {job.nextRunAt ? formatDate(job.nextRunAt) : "Pending"}
                </TableCell>
              </TableRow>
            ))}
            {!loading && filtered.length === 0 && (
              <TableRow>
                <TableCell colSpan={7} className="p-0">
                  <EmptyState message="No cron jobs found." />
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </PageContainer>

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle className="inline-flex items-center gap-2">
              <CalendarClockIcon className="size-4 text-muted-foreground" />
              Create cron job
            </DialogTitle>
            <DialogDescription>
              Run an agent automatically on a simple schedule.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Agent</Label>
              <Select
                value={agentId}
                onValueChange={(value) => value && setAgentId(value)}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select an agent">
                    {selectedAgent?.name}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  {agents.map((agent) => (
                    <SelectItem key={agent.id} value={agent.id}>
                      {agent.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Name</Label>
              <Input
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder="Daily research digest"
              />
            </div>
            <div className="space-y-2">
              <Label>Frequency</Label>
              <Select
                value={frequency}
                onValueChange={(value) =>
                  value && setFrequency(value as Frequency)
                }
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="heartbeat">Every 30 minutes</SelectItem>
                  <SelectItem value="hourly">Every hour</SelectItem>
                  <SelectItem value="daily">Every day at 09:00 UTC</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Prompt</Label>
              <Textarea
                value={prompt}
                onChange={(event) => setPrompt(event.target.value)}
                placeholder="What should the agent do on this schedule?"
                rows={4}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCreateOpen(false)}>
              Cancel
            </Button>
            <Button
              disabled={!agentId || !prompt.trim() || creating}
              onClick={submit}
            >
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown" : date.toLocaleString();
}
