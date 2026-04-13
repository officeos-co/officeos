"use client";

import type { Skill } from "@/hooks/useSkills";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { cn } from "@/lib/utils";

type Props = {
  skill: Skill;
  onClick: () => void;
};

function skillStatus(s: Skill): string {
  if (s.installed && s.configured) return "ready";
  if (s.installed) return "needs credentials";
  return "not installed";
}

export function SkillGridCard({ skill, onClick }: Props) {
  const status = skillStatus(skill);

  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "group flex flex-col rounded-lg border border-border bg-card p-5 text-left transition-all",
        "hover:border-primary/20 hover:bg-card/80 hover:shadow-[0_0_24px_-6px] hover:shadow-primary/5"
      )}
    >
      <div className="flex items-center gap-3">
        <span className="text-2xl">{skill.emoji}</span>
        <div className="min-w-0 flex-1">
          <div className="text-sm font-semibold tracking-tight">{skill.title}</div>
        </div>
        <StatusBadge status={status} />
      </div>

      <p className="mt-3 line-clamp-2 text-[13px] leading-relaxed text-muted-foreground">
        {skill.description}
      </p>

      <div className="mt-3 flex items-center gap-3 text-[11px] text-muted-foreground">
        <span>
          {skill.llmTools.length} tool{skill.llmTools.length === 1 ? "" : "s"}
        </span>
        {skill.registryVersion && (
          <span className="rounded border border-border px-1.5 py-0.5 font-mono">
            v{skill.registryVersion}
          </span>
        )}
        {skill.registryStatus === "disabled" && (
          <span className="rounded border border-amber-500/30 bg-amber-500/10 px-1.5 py-0.5 text-amber-600">
            disabled
          </span>
        )}
        {skill.runTarget === "runner" && (
          <span className="rounded border border-primary/20 bg-primary/5 px-1.5 py-0.5 text-primary">
            self-hosted
          </span>
        )}
        {skill.configured && skill.llmTools.length > 0 && (
          <div className="flex flex-wrap gap-1">
            {skill.llmTools.slice(0, 3).map((t) => (
              <span
                key={t.name}
                className="rounded border border-border px-1.5 py-0.5 font-mono"
              >
                {t.name}
              </span>
            ))}
            {skill.llmTools.length > 3 && (
              <span>+{skill.llmTools.length - 3}</span>
            )}
          </div>
        )}
      </div>
    </button>
  );
}
