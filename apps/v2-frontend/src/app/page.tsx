"use client";

import { useRouter } from "next/navigation";
import { Bot, Puzzle, KeyRound, Activity } from "lucide-react";
import { useAgents, type Agent } from "@/hooks/useAgents";
import { useSkills } from "@/hooks/useSkills";
import { useProviders } from "@/hooks/useProviders";
import { StatusBadge } from "@/components/StatusBadge";
import { formatDate, shortId } from "@/utils/format";

function StatCard({
  value,
  label,
  sub,
  icon: Icon,
}: {
  value: string | number;
  label: string;
  sub?: string;
  icon: React.ComponentType<{ className?: string }>;
}) {
  return (
    <div className="rounded-xl border border-border bg-card p-5">
      <div className="flex items-start justify-between">
        <div>
          <div className="text-3xl font-semibold tracking-tight">{value}</div>
          <div className="mt-1 text-sm text-muted-foreground">{label}</div>
          {sub && (
            <div className="mt-0.5 text-xs text-muted-foreground/70">{sub}</div>
          )}
        </div>
        <div className="rounded-lg border border-border p-2 text-muted-foreground">
          <Icon className="h-4 w-4" />
        </div>
      </div>
    </div>
  );
}

function statusGroup(agents: Agent[]) {
  let running = 0;
  let pending = 0;
  let errors = 0;
  for (const a of agents) {
    if (a.status === "running" || a.status === "ready" || a.status === "online") running++;
    else if (a.status === "pending" || a.status === "building") pending++;
    else if (a.status === "failed" || a.status === "not_found") errors++;
  }
  return { running, pending, errors };
}

export default function DashboardPage() {
  const router = useRouter();
  const { agents } = useAgents();
  const { skills } = useSkills();
  const { providers } = useProviders();

  const { running, pending, errors } = statusGroup(agents);
  const installedSkills = skills.filter((s) => s.installed).length;
  const configuredProviders = providers.filter((p) => p.configured).length;

  return (
    <div>
      {/* Header */}
      <div className="border-b border-border bg-card/50 px-8 py-6">
        <h1 className="text-xl font-semibold tracking-tight">
          EnterpriseAgentOS
        </h1>
      </div>

      <div className="px-8 py-6">
        {/* Stat cards */}
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard
            value={agents.length}
            label="Agents"
            sub={`${running} running, ${pending} pending, ${errors} errors`}
            icon={Bot}
          />
          <StatCard
            value={installedSkills}
            label="Skills Installed"
            sub={`${skills.length} available`}
            icon={Puzzle}
          />
          <StatCard
            value={configuredProviders}
            label="Providers"
            sub={`${providers.length} total`}
            icon={KeyRound}
          />
          <StatCard
            value={running}
            label="Live Agents"
            sub={running > 0 ? "All systems nominal" : "No agents running"}
            icon={Activity}
          />
        </div>

        {/* Two-column layout */}
        <div className="mt-8 grid grid-cols-1 gap-6 lg:grid-cols-2">
          {/* Recent agents */}
          <div>
            <div className="mb-4 text-[11px] font-semibold uppercase tracking-widest text-muted-foreground">
              Recent Agents
            </div>
            <div className="rounded-xl border border-border bg-card">
              {agents.length === 0 ? (
                <div className="px-5 py-8 text-center text-sm text-muted-foreground">
                  No agents yet. Create one to get started.
                </div>
              ) : (
                <div className="divide-y divide-border">
                  {agents.slice(0, 6).map((agent) => (
                    <button
                      key={agent.id}
                      type="button"
                      onClick={() => router.push(`/agents/${agent.id}`)}
                      className="flex w-full items-center gap-4 px-5 py-3.5 text-left transition-colors hover:bg-muted/50"
                    >
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-medium">{agent.name}</span>
                          <StatusBadge status={agent.status} />
                        </div>
                        <div className="mt-0.5 text-xs text-muted-foreground">
                          {agent.provider} · {agent.model ?? "default model"}
                        </div>
                      </div>
                      <div className="shrink-0 text-xs text-muted-foreground">
                        {formatDate(agent.createdAt)}
                      </div>
                    </button>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Recent skills */}
          <div>
            <div className="mb-4 text-[11px] font-semibold uppercase tracking-widest text-muted-foreground">
              Installed Skills
            </div>
            <div className="rounded-xl border border-border bg-card">
              {installedSkills === 0 ? (
                <div className="px-5 py-8 text-center text-sm text-muted-foreground">
                  No skills installed yet.
                </div>
              ) : (
                <div className="divide-y divide-border">
                  {skills
                    .filter((s) => s.installed)
                    .slice(0, 6)
                    .map((skill) => (
                      <button
                        key={skill.name}
                        type="button"
                        onClick={() => router.push(`/skills/${skill.name}`)}
                        className="flex w-full items-center gap-4 px-5 py-3.5 text-left transition-colors hover:bg-muted/50"
                      >
                        <span className="text-xl">{skill.emoji}</span>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2">
                            <span className="text-sm font-medium">{skill.title}</span>
                            <StatusBadge status={skill.configured ? "ready" : "needs credentials"} />
                          </div>
                          <div className="mt-0.5 text-xs text-muted-foreground">
                            {skill.llmTools.length} tool{skill.llmTools.length === 1 ? "" : "s"}
                          </div>
                        </div>
                      </button>
                    ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
