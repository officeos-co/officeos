"use client";

import type { Skill } from "@/hooks/useSkills";

type Props = {
  skill: Skill;
  onClick: () => void;
};

const statusStyle = {
  ready: "border-emerald-500/40 bg-emerald-500/10 text-emerald-300",
  "needs credentials": "border-yellow-500/40 bg-yellow-500/10 text-yellow-300",
  "not installed": "border-[var(--eaos-border)] bg-black/30 text-[var(--eaos-text-muted)]",
};

function skillStatus(s: Skill): keyof typeof statusStyle {
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
      className="flex flex-col rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)] p-5 text-left transition-colors hover:bg-black/30"
    >
      <div className="flex items-center gap-3">
        <span className="text-2xl">{skill.emoji}</span>
        <div className="min-w-0 flex-1">
          <div className="text-sm font-semibold">{skill.title}</div>
        </div>
        <span
          className={`flex-shrink-0 rounded border px-2 py-0.5 text-[10px] ${statusStyle[status]}`}
        >
          {status}
        </span>
      </div>

      <p className="mt-3 line-clamp-2 text-xs text-[var(--eaos-text-muted)]">
        {skill.description}
      </p>

      <div className="mt-3 flex items-center gap-3 text-[10px] text-[var(--eaos-text-muted)]">
        <span>
          {skill.llmTools.length} tool{skill.llmTools.length === 1 ? "" : "s"}
        </span>
        {skill.configured && skill.llmTools.length > 0 && (
          <div className="flex flex-wrap gap-1">
            {skill.llmTools.slice(0, 3).map((t) => (
              <span
                key={t.name}
                className="rounded border border-[var(--eaos-border)] px-1.5 py-0.5 font-mono"
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
