"use client";

import Link from "next/link";
import { useState } from "react";
import { HeartPulseIcon, PlusIcon } from "lucide-react";
import { PageContainer } from "@/components/page-container";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
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
  const [createOpen, setCreateOpen] = useState(false);
  const { jobs, loading, creating, createCronJob } = useAllCronJobs();
  const { agents } = useAgents();
  const [agentId, setAgentId] = useState("");
  const [name, setName] = useState("");
  const [frequency, setFrequency] = useState<Frequency>("daily");
  const [prompt, setPrompt] = useState("");

  async function submit() {
    if (!agentId || !prompt.trim()) return;
    const expression = expressionFor(frequency);
    await createCronJob(
      agentId,
      name.trim() || (isHeartbeatCron(expression) ? "Heartbeat" : "Scheduled task"),
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
        width="thin"
        action={
          <Button size="sm" onClick={() => setCreateOpen(true)}>
            <PlusIcon className="size-4" />
            Create
          </Button>
        }
      />
      <PageContainer width="thin" className="flex flex-1 flex-col pb-4">
        <div className="min-h-0 overflow-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Agent</TableHead>
                <TableHead>Schedule</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Next run</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {jobs.map((job) => (
                <TableRow key={job.id}>
                  <TableCell>
                    <Link
                      href={`/cron-jobs/${job.id}`}
                      className="inline-flex items-center gap-2 font-medium hover:underline"
                    >
                      {isHeartbeatCron(job.expression.value) && (
                        <HeartPulseIcon className="size-3.5 text-rose-500" />
                      )}
                      {job.name}
                    </Link>
                  </TableCell>
                  <TableCell>{job.agentName}</TableCell>
                  <TableCell>{describeCronExpression(job.expression.value)}</TableCell>
                  <TableCell>{job.enabled ? "Enabled" : "Disabled"}</TableCell>
                  <TableCell>{job.nextRunAt ? formatDate(job.nextRunAt) : "Pending"}</TableCell>
                </TableRow>
              ))}
              {!loading && jobs.length === 0 && (
                <TableRow>
                  <TableCell
                    colSpan={5}
                    className="py-10 text-center text-muted-foreground"
                  >
                    No cron jobs yet.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      </PageContainer>

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Create cron job</DialogTitle>
            <DialogDescription>
              Run an agent automatically on a simple schedule.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Agent</Label>
              <Select value={agentId} onValueChange={(value) => value && setAgentId(value)}>
                <SelectTrigger>
                  <SelectValue placeholder="Select an agent" />
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
              <Select value={frequency} onValueChange={(value) => value && setFrequency(value as Frequency)}>
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
            <Button disabled={!agentId || !prompt.trim() || creating} onClick={submit}>
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
