"use client";

import { useState } from "react";
import { Modal } from "./Modal";
import { useAgents } from "@/hooks/useAgents";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

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
      await create({
        name: name.trim(),
        provider: providerFromModel(model),
        model,
      });
      setName("");
      setModel("auto");
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
