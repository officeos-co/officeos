"use client";

import { useState } from "react";
import { ChevronDown, ChevronUp, Download, Trash2 } from "lucide-react";
import type { Skill } from "@/hooks/useSkills";
import { SkillCredentialsForm } from "./SkillCredentialsForm";

type Props = {
  skill: Skill;
  onInstall: () => Promise<void>;
  onUninstall: () => Promise<void>;
  onPutCredentials: (creds: Record<string, string>) => Promise<void>;
};

export function SkillCard({ skill, onInstall, onUninstall, onPutCredentials }: Props) {
  const [expanded, setExpanded] = useState(false);
  const [busy, setBusy] = useState(false);

  const toggle = async (action: () => Promise<void>) => {
    setBusy(true);
    try {
      await action();
    } finally {
      setBusy(false);
    }
  };

  const statusChip = skill.installed
    ? skill.configured
      ? "border-emerald-500/40 bg-emerald-500/10 text-emerald-300"
      : "border-yellow-500/40 bg-yellow-500/10 text-yellow-300"
    : "border-[var(--eaos-border)] bg-black/30 text-[var(--eaos-text-muted)]";

  const statusLabel = skill.installed
    ? skill.configured
      ? "ready"
      : "needs credentials"
    : "not installed";

  return (
    <div className="rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)]">
      <div className="flex items-start justify-between gap-3 px-4 py-4">
        <div className="flex items-start gap-3">
          <span className="text-2xl">{skill.emoji}</span>
          <div className="min-w-0">
            <div className="text-sm font-medium">{skill.title}</div>
            <div className="mt-0.5 text-xs text-[var(--eaos-text-muted)]">
              {skill.description}
            </div>
            <div className="mt-2 flex items-center gap-2">
              <span className={`rounded border px-2 py-0.5 text-[10px] ${statusChip}`}>
                {statusLabel}
              </span>
              <span className="text-[10px] text-[var(--eaos-text-muted)]">
                {skill.llmTools.length} tool{skill.llmTools.length === 1 ? "" : "s"}
              </span>
            </div>
          </div>
        </div>

        <div className="flex flex-shrink-0 items-center gap-2">
          {skill.installed ? (
            <button
              type="button"
              onClick={() => toggle(onUninstall)}
              disabled={busy}
              className="flex items-center gap-1 rounded-md border border-red-500/40 px-3 py-1.5 text-xs text-red-300 hover:bg-red-500/20 disabled:opacity-50"
            >
              <Trash2 className="h-3 w-3" />
              Uninstall
            </button>
          ) : (
            <button
              type="button"
              onClick={() => toggle(onInstall)}
              disabled={busy}
              className="flex items-center gap-1 rounded-md bg-white px-3 py-1.5 text-xs font-medium text-black disabled:opacity-50"
            >
              <Download className="h-3 w-3" />
              Install
            </button>
          )}
          {skill.installed && (
            <button
              type="button"
              onClick={() => setExpanded(!expanded)}
              className="rounded-md border border-[var(--eaos-border)] p-1.5 hover:bg-black/40"
            >
              {expanded ? (
                <ChevronUp className="h-3.5 w-3.5" />
              ) : (
                <ChevronDown className="h-3.5 w-3.5" />
              )}
            </button>
          )}
        </div>
      </div>

      {expanded && skill.installed && (
        <div className="border-t border-[var(--eaos-border)] px-4 py-4">
          <div className="mb-4">
            <div className="mb-2 text-[11px] uppercase tracking-wider text-[var(--eaos-text-muted)]">
              Credentials
            </div>
            <SkillCredentialsForm
              fields={skill.credentialFields}
              onSubmit={onPutCredentials}
              configured={skill.configured}
            />
          </div>

          <div>
            <div className="mb-2 text-[11px] uppercase tracking-wider text-[var(--eaos-text-muted)]">
              Tools ({skill.llmTools.length})
            </div>
            <div className="space-y-2">
              {skill.llmTools.map((tool) => (
                <div
                  key={tool.name}
                  className="rounded-md border border-[var(--eaos-border)] bg-black/20 px-3 py-2"
                >
                  <div className="font-mono text-xs">{tool.name}</div>
                  <div className="mt-0.5 text-[11px] text-[var(--eaos-text-muted)]">
                    {tool.description}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
