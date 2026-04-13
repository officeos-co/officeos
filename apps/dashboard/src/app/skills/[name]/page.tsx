"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import posthog from "posthog-js";
import { Download, Trash2, ArrowLeft } from "lucide-react";
import { TopBar } from "@/components/shared/TopBar";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { SkillCredentialsForm } from "@/components/skills/SkillCredentialsForm";
import { useSkills, type Skill } from "@/hooks/useSkills";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

export default function SkillDetailPage() {
  const params = useParams<{ name: string }>();
  const name = params.name;
  const { skills, loading, install, uninstall, putCredentials, setRunTarget } = useSkills();
  const [doc, setDoc] = useState<string | null>(null);
  const [docLoading, setDocLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const skill = skills.find((s) => s.name === name);

  useEffect(() => {
    setDocLoading(true);
    fetch(`/api/skills/${encodeURIComponent(name)}/doc`)
      .then(async (res) => {
        if (!res.ok) {
          setDoc(null);
          return;
        }
        setDoc(await res.text());
      })
      .catch(() => setDoc(null))
      .finally(() => setDocLoading(false));
  }, [name]);

  const handleAction = async (action: () => Promise<unknown>) => {
    setBusy(true);
    try {
      await action();
    } finally {
      setBusy(false);
    }
  };

  if (loading) {
    return (
      <div>
        {/* Header skeleton */}
        <div className="flex items-center justify-between border-b border-border bg-background px-8 py-6">
          <div className="space-y-2">
            <div className="flex items-center gap-3">
              <Skeleton className="h-9 w-9 rounded-lg" />
              <Skeleton className="h-7 w-40" />
            </div>
            <div className="flex items-center gap-2">
              <Skeleton className="h-5 w-24 rounded-full" />
              <Skeleton className="h-4 w-16" />
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Skeleton className="h-9 w-24 rounded-md" />
            <Skeleton className="h-9 w-24 rounded-md" />
          </div>
        </div>
        <div className="mx-8 my-6 space-y-6">
          {/* Description skeleton */}
          <Skeleton className="h-4 w-3/4" />
          {/* Credentials section skeleton */}
          <section>
            <Skeleton className="h-3 w-24 mb-3" />
            <div className="rounded-lg border border-border bg-card p-4 space-y-3">
              {Array.from({ length: 2 }).map((_, i) => (
                <div key={i} className="space-y-1.5">
                  <Skeleton className="h-3 w-20" />
                  <Skeleton className="h-9 w-full rounded-md" />
                </div>
              ))}
              <Skeleton className="h-9 w-28 rounded-md" />
            </div>
          </section>
          {/* Tools section skeleton */}
          <section>
            <Skeleton className="h-3 w-20 mb-3" />
            <div className="space-y-2">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="rounded-lg border border-border bg-card px-4 py-3 space-y-1.5">
                  <Skeleton className="h-3 w-36 font-mono" />
                  <Skeleton className="h-3 w-64" />
                </div>
              ))}
            </div>
          </section>
          {/* Documentation section skeleton */}
          <section>
            <Skeleton className="h-3 w-20 mb-3" />
            <div className="rounded-lg border border-border bg-card p-4 space-y-2">
              {Array.from({ length: 4 }).map((_, i) => (
                <Skeleton key={i} className="h-3 w-full" style={{ width: `${85 - i * 15}%` }} />
              ))}
            </div>
          </section>
        </div>
      </div>
    );
  }

  if (!skill) {
    return (
      <div>
        <TopBar title="Skill not found" />
        <div className="mx-8 mt-6 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          No skill named &ldquo;{name}&rdquo; found.
        </div>
        <div className="mx-8 mt-4">
          <Link href="/skills" className="inline-flex items-center rounded-md border border-input bg-background px-3 py-1.5 text-sm font-medium shadow-sm hover:bg-accent hover:text-accent-foreground">
            <ArrowLeft className="mr-1.5 h-3.5 w-3.5" />
            Back to skills
          </Link>
        </div>
      </div>
    );
  }

  const status = skill.installed
    ? skill.configured
      ? "ready"
      : "needs credentials"
    : "not installed";

  return (
    <div>
      <TopBar
        title={
          <span className="flex items-center gap-3">
            <span className="text-3xl">{skill.emoji}</span>
            <span>{skill.title}</span>
          </span>
        }
        subtitle={
          <span className="flex items-center gap-2">
            <StatusBadge status={status} />
            <span>
              {skill.llmTools.length} tool{skill.llmTools.length === 1 ? "" : "s"}
            </span>
          </span>
        }
        action={
          <div className="flex items-center gap-2">
            {skill.installed ? (
              <Button
                variant="outline"
                size="sm"
                onClick={() => handleAction(async () => {
                  await uninstall(skill.name);
                  posthog.capture("skill_uninstalled", { skill_name: skill.name });
                })}
                disabled={busy}
                className="text-destructive hover:text-destructive"
              >
                <Trash2 className="mr-1.5 h-3.5 w-3.5" />
                Uninstall
              </Button>
            ) : (
              <Button
                size="sm"
                onClick={() => handleAction(async () => {
                  await install(skill.name);
                  posthog.capture("skill_installed", { skill_name: skill.name });
                })}
                disabled={busy}
              >
                <Download className="mr-1.5 h-3.5 w-3.5" />
                Install
              </Button>
            )}
            <Link href="/skills" className="inline-flex items-center rounded-md border border-input bg-background px-3 py-1.5 text-sm font-medium shadow-sm hover:bg-accent hover:text-accent-foreground">
              <ArrowLeft className="mr-1.5 h-3.5 w-3.5" />
              All skills
            </Link>
          </div>
        }
      />

      <div className="mx-8 my-6 space-y-6">
        {/* Description */}
        <p className="text-sm text-muted-foreground">{skill.description}</p>

        {/* Credentials */}
        {skill.installed && (
          <section>
            <h2 className="mb-3 text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
              Credentials
            </h2>
            <div className="rounded-lg border border-border bg-card p-4">
              <SkillCredentialsForm
                fields={skill.credentialFields}
                onSubmit={async (creds) => { await putCredentials(skill.name, creds); }}
                configured={skill.configured}
                skillName={skill.name}
              />
            </div>
          </section>
        )}

        {/* Execution target */}
        {skill.installed && (
          <section>
            <h2 className="mb-3 text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
              Execution target
            </h2>
            <div className="rounded-lg border border-border bg-card p-4">
              <div className="flex items-center gap-3">
                <button
                  onClick={() => handleAction(() => setRunTarget(skill.name, "cloud"))}
                  disabled={busy}
                  className={cn(
                    "flex-1 rounded-lg border px-4 py-3 text-left text-sm transition-all",
                    skill.runTarget === "cloud"
                      ? "border-primary/40 bg-primary/5"
                      : "border-border hover:border-primary/20"
                  )}
                >
                  <div className="font-medium">Cloud</div>
                  <div className="mt-0.5 text-xs text-muted-foreground">
                    Runs on the platform skill-runtime
                  </div>
                </button>
                <button
                  onClick={() => handleAction(() => setRunTarget(skill.name, "runner"))}
                  disabled={busy}
                  className={cn(
                    "flex-1 rounded-lg border px-4 py-3 text-left text-sm transition-all",
                    skill.runTarget === "runner"
                      ? "border-primary/40 bg-primary/5"
                      : "border-border hover:border-primary/20"
                  )}
                >
                  <div className="font-medium">Self-hosted runner</div>
                  <div className="mt-0.5 text-xs text-muted-foreground">
                    Runs on any online runner in your network
                  </div>
                </button>
              </div>
              {skill.runTarget === "runner" && (
                <p className="mt-3 text-xs text-amber-600">
                  Make sure at least one runner is online. If no runners are available,
                  skill calls will fail with a clear error.
                </p>
              )}
            </div>
          </section>
        )}

        {/* Tools */}
        <section>
          <h2 className="mb-3 text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
            Tools ({skill.llmTools.length})
          </h2>
          <div className="space-y-2">
            {skill.llmTools.map((tool) => (
              <div
                key={tool.name}
                className="rounded-lg border border-border bg-card px-4 py-3"
              >
                <div className="font-mono text-xs">{tool.name}</div>
                <div className="mt-1 text-[11px] text-muted-foreground">
                  {tool.description}
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* Documentation */}
        <section>
          <h2 className="mb-3 text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
            SKILL.md
          </h2>
          <div className="rounded-lg border border-border bg-card p-4">
            {docLoading ? (
              <div className="space-y-2">
                {Array.from({ length: 5 }).map((_, i) => (
                  <Skeleton key={i} className="h-3" style={{ width: `${90 - i * 12}%` }} />
                ))}
              </div>
            ) : doc ? (
              <pre className="whitespace-pre-wrap break-words font-mono text-xs leading-relaxed">
                {doc}
              </pre>
            ) : (
              <div className="text-sm text-muted-foreground">
                No documentation available for this skill.
              </div>
            )}
          </div>
        </section>
      </div>
    </div>
  );
}
