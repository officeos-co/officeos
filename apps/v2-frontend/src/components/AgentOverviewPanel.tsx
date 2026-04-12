"use client";

import { useEffect, useState } from "react";
import { ChevronRight, CheckCircle } from "lucide-react";
import type { Agent } from "@/hooks/useAgents";
import { apiFetch } from "@/hooks/useApi";
import { cn } from "@/lib/utils";

type Skill = {
  name: string;
  title: string;
  description: string;
  emoji: string;
  installed: boolean;
  configured: boolean;
  llmTools: { name: string; description: string }[];
};

const NATIVE_TOOLS = [
  "memory_store",
  "memory_recall",
  "memory_export",
  "memory_forget",
  "memory_purge",
  "file_read",
  "file_write",
  "file_edit",
  "glob_search",
  "content_search",
  "web_fetch",
  "web_search",
  "http_request",
  "obsidian_find_by_category",
  "obsidian_query_by_property",
  "ask_user",
  "escalate",
  "delegate",
  "canvas",
  "shell",
  "tool_search",
];

type Props = {
  agent: Agent;
};

export function AgentOverviewPanel({ agent }: Props) {
  const [skills, setSkills] = useState<Skill[] | null>(null);
  const [skillsError, setSkillsError] = useState<string | null>(null);
  const [expandedSkills, setExpandedSkills] = useState<Set<string>>(new Set());
  const [nativeExpanded, setNativeExpanded] = useState(false);

  useEffect(() => {
    apiFetch<Skill[]>("/api/skills")
      .then(setSkills)
      .catch((err: unknown) => {
        setSkillsError(err instanceof Error ? err.message : "Failed to load skills");
        setSkills([]);
      });
  }, [agent.id]);

  const toggleSkill = (name: string) =>
    setExpandedSkills((prev) => {
      const next = new Set(prev);
      if (next.has(name)) next.delete(name);
      else next.add(name);
      return next;
    });

  const installedSkills = skills?.filter((s) => s.installed || s.configured) ?? [];

  return (
    <div className="mx-auto max-w-2xl px-8 py-6">
      {/* Model section */}
      <div className="py-4 border-b border-border">
        <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground/70 mb-1">
          Model
        </p>
        <p className="text-sm font-medium">
          {agent.model ?? "auto (smart routing)"}
        </p>
        {agent.provider && (
          <p className="text-xs text-muted-foreground mt-0.5">{agent.provider}</p>
        )}
      </div>

      {/* Tools section */}
      <div className="py-4">
        <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground/70 mb-3">
          Tools
        </p>

        {skillsError && (
          <div className="mb-3 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-2 text-xs text-destructive">
            {skillsError}
          </div>
        )}

        {skills === null ? (
          <p className="text-sm text-muted-foreground">Loading tools…</p>
        ) : (
          <div className="rounded-lg border border-border overflow-hidden">
            {/* Installed skills */}
            {installedSkills.map((skill) => {
              const isExpanded = expandedSkills.has(skill.name);
              return (
                <div key={skill.name} className="border-b border-border last:border-b-0">
                  <div className="flex items-center gap-3 px-4 py-3">
                    <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-lg border border-border bg-card text-lg">
                      {skill.emoji}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-semibold">{skill.title}</p>
                      <p className="text-xs text-muted-foreground truncate">{skill.description}</p>
                    </div>
                  </div>

                  {skill.llmTools.length > 0 && (
                    <>
                      <button
                        type="button"
                        onClick={() => toggleSkill(skill.name)}
                        className="flex w-full items-center justify-between px-4 py-2 text-xs text-muted-foreground bg-muted/40 hover:bg-accent/50 transition-colors"
                      >
                        <div className="flex items-center gap-2">
                          <ChevronRight
                            className={cn(
                              "h-3 w-3 transition-transform",
                              isExpanded && "rotate-90",
                            )}
                          />
                          <span>Actions ({skill.llmTools.length})</span>
                        </div>
                        <span className="rounded-full bg-muted px-2 py-0.5 text-[10px]">
                          Always allow
                        </span>
                      </button>

                      {isExpanded && (
                        <div className="pb-2 bg-muted/20">
                          {skill.llmTools.map((tool) => (
                            <div
                              key={tool.name}
                              className="flex items-center justify-between pl-10 pr-4 py-1.5"
                            >
                              <span className="font-mono text-xs">{tool.name}</span>
                              <span className="text-[10px] text-muted-foreground flex items-center gap-1">
                                <CheckCircle className="h-3 w-3 text-emerald-500" />
                                Always allow
                              </span>
                            </div>
                          ))}
                        </div>
                      )}
                    </>
                  )}
                </div>
              );
            })}

            {/* Native tools — single card */}
            <div className={cn(installedSkills.length > 0 && "border-t border-border")}>
              <div className="flex items-center gap-3 px-4 py-3">
                <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-lg border border-border bg-card text-lg">
                  🔧
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold">Native Tools</p>
                  <p className="text-xs text-muted-foreground font-mono">agent_toolset_20260401</p>
                </div>
              </div>

              <button
                type="button"
                onClick={() => setNativeExpanded((v) => !v)}
                className="flex w-full items-center justify-between px-4 py-2 text-xs text-muted-foreground bg-muted/40 hover:bg-accent/50 transition-colors"
              >
                <div className="flex items-center gap-2">
                  <ChevronRight
                    className={cn(
                      "h-3 w-3 transition-transform",
                      nativeExpanded && "rotate-90",
                    )}
                  />
                  <span>Tools ({NATIVE_TOOLS.length})</span>
                </div>
                <span className="rounded-full bg-muted px-2 py-0.5 text-[10px]">
                  Always allow
                </span>
              </button>

              {nativeExpanded && (
                <div className="pb-2 bg-muted/20">
                  {NATIVE_TOOLS.map((toolName) => (
                    <div
                      key={toolName}
                      className="flex items-center justify-between pl-10 pr-4 py-1.5"
                    >
                      <span className="font-mono text-xs">{toolName}</span>
                      <span className="text-[10px] text-muted-foreground flex items-center gap-1">
                        <CheckCircle className="h-3 w-3 text-emerald-500" />
                        Always allow
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
