"use client";

import { useEffect, useState } from "react";
import { Modal } from "./Modal";
import { useAgents } from "@/hooks/useAgents";
import { apiFetch } from "@/hooks/useApi";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

type AvailableSkill = {
  name: string;
  title: string;
  emoji: string;
  description: string;
  installed: boolean;
  configured: boolean;
};

const MODELS: { label: string; value: string }[] = [
  { label: "Auto (recommended)", value: "auto" },
  { label: "Claude Haiku", value: "claude-haiku-4-5" },
  { label: "Claude Sonnet", value: "claude-sonnet-4-6" },
  { label: "Claude Opus", value: "claude-opus-4-6" },
  { label: "GPT-4o", value: "gpt-4o" },
  { label: "GPT-4o mini", value: "gpt-4o-mini" },
  { label: "Gemini 2.5 Pro", value: "gemini-2.5-pro" },
  { label: "Gemini 2.5 Flash", value: "gemini-2.5-flash" },
  { label: "Grok 4", value: "grok-4" },
];

type NewAgentOverlayProps = {
  open: boolean;
  onClose: () => void;
};

export function NewAgentOverlay({ open, onClose }: NewAgentOverlayProps) {
  const { create } = useAgents();

  const [name, setName] = useState("");
  const [model, setModel] = useState("auto");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [availableSkills, setAvailableSkills] = useState<AvailableSkill[]>([]);
  const [selectedSkills, setSelectedSkills] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (open) {
      apiFetch<AvailableSkill[]>("/api/skills")
        .then((skills) => setAvailableSkills(skills.filter((s) => s.installed || s.configured)))
        .catch(() => setAvailableSkills([]));
    }
  }, [open]);

  const toggleSkill = (skillName: string) => {
    setSelectedSkills((prev) => {
      const next = new Set(prev);
      if (next.has(skillName)) next.delete(skillName);
      else next.add(skillName);
      return next;
    });
  };

  // Derive provider from model selection so the backend record stays consistent
  function providerFromModel(m: string): string {
    if (m.startsWith("claude-")) return "anthropic";
    if (m.startsWith("gpt-") || m === "auto") return "openai";
    if (m.startsWith("gemini-")) return "google";
    if (m.startsWith("grok-")) return "xai";
    return "anthropic"; // safe default
  }

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!name.trim()) {
      setError("Name is required");
      return;
    }
    setSubmitting(true);
    try {
      const agent = await create({
        name: name.trim(),
        provider: providerFromModel(model),
        model,
      });
      // Assign selected skills to the new agent
      if (selectedSkills.size > 0) {
        try {
          await apiFetch(`/api/agents/${agent.id}/skills`, {
            method: "POST",
            body: JSON.stringify({ skillNames: Array.from(selectedSkills) }),
          });
        } catch {
          // Non-fatal: agent was created, skills can be assigned later
          console.warn("Failed to assign initial skills to agent");
        }
      }
      setName("");
      setModel("auto");
      setSelectedSkills(new Set());
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create agent");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal open={open} title="New agent" onClose={onClose}>
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        <div className="space-y-2">
          <Label htmlFor="agent-name">Name</Label>
          <Input
            id="agent-name"
            autoFocus
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="my-agent"
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="agent-model">Model</Label>
          <select
            id="agent-model"
            value={model}
            onChange={(e) => setModel(e.target.value)}
            className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
          >
            {MODELS.map((m) => (
              <option key={m.value} value={m.value}>
                {m.label}
              </option>
            ))}
          </select>
          {model === "auto" && (
            <p className="text-xs text-muted-foreground">
              Smart routing picks the best model for each request automatically.
            </p>
          )}
        </div>

        {availableSkills.length > 0 && (
          <div className="space-y-2">
            <Label>Tools (optional)</Label>
            <p className="text-xs text-muted-foreground">
              Select which tools this agent can use. You can change this later.
            </p>
            <div className="grid grid-cols-1 gap-1.5 max-h-40 overflow-y-auto rounded-md border border-input p-2">
              {availableSkills.map((skill) => (
                <button
                  key={skill.name}
                  type="button"
                  onClick={() => toggleSkill(skill.name)}
                  className={[
                    "flex items-center gap-2 rounded-md px-2.5 py-1.5 text-left text-sm transition-colors",
                    selectedSkills.has(skill.name)
                      ? "bg-primary/10 border border-primary/30"
                      : "hover:bg-muted border border-transparent",
                  ].join(" ")}
                >
                  <span className="text-base">{skill.emoji}</span>
                  <span className="flex-1 min-w-0 truncate">{skill.title}</span>
                  {selectedSkills.has(skill.name) && (
                    <span className="text-[10px] text-primary shrink-0">Selected</span>
                  )}
                </button>
              ))}
            </div>
          </div>
        )}

        {error && <p className="text-sm text-destructive">{error}</p>}

        <div className="flex justify-end gap-2">
          <Button type="button" variant="outline" size="sm" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" size="sm" disabled={submitting}>
            {submitting ? "Creating..." : "Create"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
