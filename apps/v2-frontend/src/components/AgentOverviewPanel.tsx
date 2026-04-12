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

const SUPPORTED_MODELS = [
  "auto",
  "claude-haiku-4-5",
  "claude-sonnet-4-6",
  "claude-opus-4-6",
  "gpt-4o",
  "gpt-4o-mini",
  "gemini-2.5-pro",
  "gemini-2.5-flash",
  "grok-4",
];

type NativeTool = { name: string; description: string };

const NATIVE_TOOLS: NativeTool[] = [
  { name: "memory_store", description: "Save a key-value pair to long-term memory" },
  { name: "memory_recall", description: "Retrieve stored memories by key or query" },
  { name: "memory_export", description: "Export all memories as structured data" },
  { name: "memory_forget", description: "Remove a specific memory entry" },
  { name: "memory_purge", description: "Clear all stored memories" },
  { name: "file_read", description: "Read file contents from the filesystem" },
  { name: "file_write", description: "Write content to a file" },
  { name: "file_edit", description: "Apply targeted edits to a file" },
  { name: "glob_search", description: "Find files matching a glob pattern" },
  { name: "content_search", description: "Search file contents with regex" },
  { name: "web_fetch", description: "Fetch content from a URL" },
  { name: "web_search", description: "Search the web" },
  { name: "http_request", description: "Send an arbitrary HTTP request" },
  { name: "obsidian_find_by_category", description: "Query Obsidian vault by category" },
  { name: "obsidian_query_by_property", description: "Query Obsidian vault by property" },
  { name: "ask_user", description: "Ask the user a question and wait for reply" },
  { name: "escalate", description: "Escalate task to a human operator" },
  { name: "delegate", description: "Delegate a sub-task to another agent" },
  { name: "canvas", description: "Render structured output to the canvas" },
  { name: "shell", description: "Execute a shell command" },
  { name: "tool_search", description: "Search available tools by name or description" },
];

type Props = {
  agent: Agent;
  onAgentUpdated?: () => void;
};

export function AgentOverviewPanel({ agent, onAgentUpdated }: Props) {
  const [skills, setSkills] = useState<Skill[] | null>(null);
  const [skillsError, setSkillsError] = useState<string | null>(null);
  const [expandedSkills, setExpandedSkills] = useState<Set<string>>(new Set());
  const [nativeExpanded, setNativeExpanded] = useState(false);
  const [selectedModel, setSelectedModel] = useState(agent.model ?? "auto");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    apiFetch<Skill[]>("/api/skills")
      .then(setSkills)
      .catch((err: unknown) => {
        setSkillsError(err instanceof Error ? err.message : "Failed to load skills");
        setSkills([]);
      });
  }, [agent.id]);

  useEffect(() => {
    setSelectedModel(agent.model ?? "auto");
  }, [agent.model]);

  const handleModelChange = async (model: string) => {
    setSelectedModel(model);
    setSaving(true);
    try {
      await apiFetch(`/api/agents/${agent.id}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ model: model === "auto" ? null : model }),
      });
      onAgentUpdated?.();
    } catch (err) {
      console.error("Failed to update model:", err);
      setSelectedModel(agent.model ?? "auto");
    } finally {
      setSaving(false);
    }
  };

  const toggleSkill = (name: string) =>
    setExpandedSkills((prev) => {
      const next = new Set(prev);
      if (next.has(name)) next.delete(name);
      else next.add(name);
      return next;
    });

  const installedSkills = skills?.filter((s) => s.installed || s.configured) ?? [];

  return (
    <div className="px-8 py-6">
      {/* Model selector */}
      <div className="py-4 border-b border-border">
        <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground/70 mb-2">
          Model
        </p>
        <select
          value={selectedModel}
          onChange={(e) => handleModelChange(e.target.value)}
          disabled={saving}
          className="w-full rounded-lg border border-border bg-muted/50 px-3 py-2 text-sm outline-none focus:border-primary disabled:opacity-50"
        >
          {SUPPORTED_MODELS.map((m) => (
            <option key={m} value={m}>
              {m === "auto" ? "auto (smart routing)" : m}
            </option>
          ))}
        </select>
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
            {/* Native tools card */}
            <div>
              <div className="flex items-center gap-3 px-4 py-3">
                <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-lg border border-border bg-card text-lg">
                  🔧
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold">Built-in tools</p>
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
                  <span>Tool permissions</span>
                  <span className="text-[10px] text-muted-foreground/70">{NATIVE_TOOLS.length}</span>
                </div>
                <span className="flex items-center gap-1 text-[10px] text-muted-foreground">
                  <CheckCircle className="h-3 w-3" />
                  Always allow
                </span>
              </button>

              {nativeExpanded && (
                <div className="bg-muted/20">
                  {NATIVE_TOOLS.map((tool) => (
                    <div
                      key={tool.name}
                      className="flex items-center px-4 py-2 border-t border-border/50"
                    >
                      <span className="font-mono text-xs w-48 shrink-0 pl-6">{tool.name}</span>
                      <span className="text-xs text-muted-foreground flex-1">{tool.description}</span>
                      <span className="text-[10px] text-muted-foreground flex items-center gap-1 shrink-0 ml-4">
                        <CheckCircle className="h-3 w-3" />
                        Always allow
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Installed skills */}
            {installedSkills.map((skill) => {
              const isExpanded = expandedSkills.has(skill.name);
              return (
                <div key={skill.name} className="border-t border-border">
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
                          <span>Tool permissions</span>
                          <span className="text-[10px] text-muted-foreground/70">{skill.llmTools.length}</span>
                        </div>
                        <span className="flex items-center gap-1 text-[10px] text-muted-foreground">
                          <CheckCircle className="h-3 w-3" />
                          Always allow
                        </span>
                      </button>

                      {isExpanded && (
                        <div className="bg-muted/20">
                          {skill.llmTools.map((tool) => (
                            <div
                              key={tool.name}
                              className="flex items-center px-4 py-2 border-t border-border/50"
                            >
                              <span className="font-mono text-xs w-48 shrink-0 pl-6">{tool.name}</span>
                              <span className="text-xs text-muted-foreground flex-1">{tool.description}</span>
                              <span className="text-[10px] text-muted-foreground flex items-center gap-1 shrink-0 ml-4">
                                <CheckCircle className="h-3 w-3" />
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
          </div>
        )}
      </div>
    </div>
  );
}
