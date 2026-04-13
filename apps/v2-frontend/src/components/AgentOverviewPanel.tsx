"use client";

import { useEffect, useState } from "react";
import { ChevronRight, CheckCircle, Plus, X } from "lucide-react";
import type { Agent } from "@/hooks/useAgents";
import { apiFetch } from "@/hooks/useApi";
import { useAgentSkills } from "@/hooks/useAgentSkills";
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
  const [showAddSkill, setShowAddSkill] = useState(false);

  const {
    assignments,
    loading: assignmentsLoading,
    assign,
    remove: removeAssignment,
  } = useAgentSkills(agent.id);

  const assignedNames = new Set(assignments.map((a) => a.skillName));

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
  const assignedSkills = installedSkills.filter((s) => assignedNames.has(s.name));
  const availableToAdd = installedSkills.filter((s) => !assignedNames.has(s.name));

  const handleAddSkill = async (name: string) => {
    try {
      await assign([name]);
    } catch (err) {
      console.error("Failed to assign skill:", err);
    }
  };

  const handleRemoveSkill = async (name: string) => {
    try {
      await removeAssignment(name);
    } catch (err) {
      console.error("Failed to remove skill:", err);
    }
  };

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

      {/* Assigned Tools section */}
      <div className="py-4 border-b border-border">
        <div className="flex items-center justify-between mb-3">
          <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground/70">
            Assigned Tools
          </p>
          {availableToAdd.length > 0 && (
            <button
              type="button"
              onClick={() => setShowAddSkill((v) => !v)}
              className="flex items-center gap-1 rounded-md border border-border px-2.5 py-1 text-xs hover:bg-accent transition-colors"
            >
              <Plus className="h-3 w-3" />
              Add tool
            </button>
          )}
        </div>

        {skillsError && (
          <div className="mb-3 rounded-md border border-destructive/30 bg-destructive/5 px-4 py-2 text-xs text-destructive">
            {skillsError}
          </div>
        )}

        {/* Add skill picker */}
        {showAddSkill && availableToAdd.length > 0 && (
          <div className="mb-3 rounded-lg border border-dashed border-border bg-muted/30 p-3">
            <p className="text-xs text-muted-foreground mb-2">Select a tool to assign to this agent:</p>
            <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
              {availableToAdd.map((skill) => (
                <button
                  key={skill.name}
                  type="button"
                  onClick={() => {
                    handleAddSkill(skill.name);
                    setShowAddSkill(false);
                  }}
                  className="flex items-center gap-2 rounded-lg border border-border bg-card px-3 py-2 text-left text-sm hover:bg-accent transition-colors"
                >
                  <span className="text-base">{skill.emoji}</span>
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium">{skill.title}</p>
                    <p className="text-xs text-muted-foreground truncate">{skill.description}</p>
                  </div>
                  <Plus className="h-4 w-4 text-muted-foreground shrink-0" />
                </button>
              ))}
            </div>
          </div>
        )}

        {assignmentsLoading || skills === null ? (
          <p className="text-sm text-muted-foreground">Loading tools...</p>
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

            {/* Assigned skills */}
            {assignedSkills.map((skill) => {
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
                    <button
                      type="button"
                      onClick={() => handleRemoveSkill(skill.name)}
                      title="Remove from agent"
                      className="flex h-6 w-6 items-center justify-center rounded-md text-muted-foreground hover:bg-destructive/10 hover:text-destructive transition-colors shrink-0"
                    >
                      <X className="h-3.5 w-3.5" />
                    </button>
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

            {assignedSkills.length === 0 && (
              <div className="border-t border-border px-4 py-6 text-center text-sm text-muted-foreground">
                No tools assigned yet. Click &quot;Add tool&quot; to get started.
              </div>
            )}
          </div>
        )}
      </div>

      {/* All available tools (read-only reference) */}
      <div className="py-4">
        <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground/70 mb-3">
          All Available Tools
        </p>

        {skills === null ? (
          <p className="text-sm text-muted-foreground">Loading tools...</p>
        ) : installedSkills.length === 0 ? (
          <p className="text-sm text-muted-foreground">No tools configured. Visit the Skills page to install and configure tools.</p>
        ) : (
          <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
            {installedSkills.map((skill) => {
              const isAssigned = assignedNames.has(skill.name);
              return (
                <div
                  key={skill.name}
                  className={cn(
                    "flex items-center gap-2 rounded-lg border px-3 py-2",
                    isAssigned
                      ? "border-primary/30 bg-primary/5"
                      : "border-border bg-card",
                  )}
                >
                  <span className="text-base">{skill.emoji}</span>
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium">{skill.title}</p>
                    <p className="text-xs text-muted-foreground truncate">{skill.description}</p>
                  </div>
                  {isAssigned ? (
                    <span className="text-[10px] text-primary shrink-0">Assigned</span>
                  ) : (
                    <button
                      type="button"
                      onClick={() => handleAddSkill(skill.name)}
                      className="text-xs text-muted-foreground hover:text-foreground shrink-0"
                    >
                      + Assign
                    </button>
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
