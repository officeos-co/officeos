"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import posthog from "posthog-js";
import { Check } from "lucide-react";
import { useAgents } from "@/hooks/useAgents";
import { useSkills } from "@/hooks/useSkills";
import { useProviders } from "@/hooks/useProviders";
import { apiFetch } from "@/hooks/useApi";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { Skill } from "@/types/skill";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

type PermissionMode = "allow" | "deny";

type SkillPermission = {
  skillName: string;
  mode: PermissionMode;
};

type Template = {
  id: string;
  emoji: string;
  name: string;
  description: string;
  systemPrompt: string;
};

// ---------------------------------------------------------------------------
// Static data — lives inline per dashboard CLAUDE.md convention
// ---------------------------------------------------------------------------

const TEMPLATES: Template[] = [
  {
    id: "research",
    emoji: "🔬",
    name: "Research Agent",
    description: "Deep-dives topics and summarises findings.",
    systemPrompt:
      "You are a research assistant. When given a topic, search thoroughly, synthesise sources, and return a clear, well-cited summary.",
  },
  {
    id: "sales",
    emoji: "💼",
    name: "Sales Agent",
    description: "Qualifies leads and drafts outreach.",
    systemPrompt:
      "You are a sales assistant. Qualify inbound leads, identify pain points, and draft personalised outreach emails.",
  },
  {
    id: "support",
    emoji: "🎧",
    name: "Support Agent",
    description: "Answers customer questions from your knowledge base.",
    systemPrompt:
      "You are a customer support agent. Answer questions accurately and escalate when you are unsure.",
  },
  {
    id: "content",
    emoji: "✍️",
    name: "Content Agent",
    description: "Writes and edits long-form content.",
    systemPrompt:
      "You are a content writer. Produce well-structured, engaging long-form content that matches the given brief.",
  },
  {
    id: "dev",
    emoji: "🛠️",
    name: "Dev Agent",
    description: "Writes, reviews, and debugs code.",
    systemPrompt:
      "You are a software engineer assistant. Write clean, well-tested code and provide clear explanations for every change.",
  },
];

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

type NewAgentOverlayProps = {
  open: boolean;
  onClose: () => void;
};

// ---------------------------------------------------------------------------
// Sub-components
// ---------------------------------------------------------------------------

function ProgressDots({ step, total }: { step: number; total: number }) {
  return (
    <div className="flex items-center justify-center gap-2">
      {Array.from({ length: total }).map((_, i) => (
        <span
          key={i}
          className={[
            "h-2 w-2 rounded-full transition-colors",
            i < step ? "bg-primary" : "bg-muted",
          ].join(" ")}
        />
      ))}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Step 1 — Quickstart
// ---------------------------------------------------------------------------

type Step1Props = {
  name: string;
  onNameChange: (v: string) => void;
  selectedTemplate: string | null;
  onSelectTemplate: (id: string | null) => void;
  error: string | null;
  onNext: () => void;
  onClose: () => void;
};

function StepQuickstart({
  name,
  onNameChange,
  selectedTemplate,
  onSelectTemplate,
  error,
  onNext,
  onClose,
}: Step1Props) {
  return (
    <div className="flex flex-col gap-6">
      <div className="space-y-2">
        <Label htmlFor="agent-name">Agent name</Label>
        <Input
          id="agent-name"
          autoFocus
          value={name}
          onChange={(e) => onNameChange(e.target.value)}
          placeholder="my-agent"
        />
        {error && <p className="text-sm text-destructive">{error}</p>}
      </div>

      <div className="space-y-3">
        <p className="text-sm font-medium">Start from</p>
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3">
          {TEMPLATES.map((t) => (
            <button
              key={t.id}
              type="button"
              onClick={() =>
                onSelectTemplate(selectedTemplate === t.id ? null : t.id)
              }
              className={[
                "relative flex flex-col gap-1 rounded-lg border p-3 text-left text-sm transition-colors",
                selectedTemplate === t.id
                  ? "border-primary bg-primary/5"
                  : "border-border hover:bg-muted",
              ].join(" ")}
            >
              {selectedTemplate === t.id && (
                <span className="absolute right-2 top-2 flex h-4 w-4 items-center justify-center rounded-full bg-primary text-primary-foreground">
                  <Check className="h-2.5 w-2.5" />
                </span>
              )}
              <span className="text-lg">{t.emoji}</span>
              <span className="font-medium">{t.name}</span>
              <span className="text-xs text-muted-foreground">
                {t.description}
              </span>
            </button>
          ))}

          <button
            type="button"
            onClick={() => onSelectTemplate(null)}
            className={[
              "flex flex-col gap-1 rounded-lg border p-3 text-left text-sm transition-colors",
              selectedTemplate === null
                ? "border-primary bg-primary/5"
                : "border-border hover:bg-muted",
            ].join(" ")}
          >
            <span className="text-lg">⬜</span>
            <span className="font-medium">From scratch</span>
            <span className="text-xs text-muted-foreground">
              Blank system prompt — configure later.
            </span>
          </button>
        </div>
      </div>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" size="sm" onClick={onClose}>
          Cancel
        </Button>
        <Button type="button" size="sm" onClick={onNext}>
          Next
        </Button>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Step 2 — Tools
// ---------------------------------------------------------------------------

type Step2Props = {
  skills: Skill[];
  loading: boolean;
  selected: Set<string>;
  onToggle: (name: string) => void;
  onSelectAll: () => void;
  onClear: () => void;
  onBack: () => void;
  onNext: () => void;
};

function StepTools({
  skills,
  loading,
  selected,
  onToggle,
  onSelectAll,
  onClear,
  onBack,
  onNext,
}: Step2Props) {
  const availableSkills = skills.filter((s) => s.installed || s.configured);

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">
          {availableSkills.length === 0
            ? "No installed tools found. Install tools from the Skills page."
            : `${selected.size} of ${availableSkills.length} selected`}
        </p>
        {availableSkills.length > 0 && (
          <div className="flex gap-3 text-xs">
            <button
              type="button"
              className="text-primary underline-offset-2 hover:underline"
              onClick={onSelectAll}
            >
              Select all
            </button>
            <button
              type="button"
              className="text-muted-foreground underline-offset-2 hover:underline"
              onClick={onClear}
            >
              Clear
            </button>
          </div>
        )}
      </div>

      {loading ? (
        <p className="text-sm text-muted-foreground">Loading tools…</p>
      ) : (
        <div className="grid grid-cols-1 gap-2 max-h-72 overflow-y-auto pr-1">
          {availableSkills.map((skill) => {
            const isSelected = selected.has(skill.name);
            return (
              <button
                key={skill.name}
                type="button"
                onClick={() => onToggle(skill.name)}
                className={[
                  "flex items-start gap-3 rounded-lg border p-3 text-left text-sm transition-colors",
                  isSelected
                    ? "border-primary bg-primary/5"
                    : "border-border hover:bg-muted",
                ].join(" ")}
              >
                <span className="text-xl shrink-0">{skill.emoji}</span>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-medium truncate">{skill.title}</span>
                    {isSelected && (
                      <span className="flex h-4 w-4 shrink-0 items-center justify-center rounded-full bg-primary text-primary-foreground">
                        <Check className="h-2.5 w-2.5" />
                      </span>
                    )}
                  </div>
                  <span className="text-xs text-muted-foreground line-clamp-2">
                    {skill.description}
                  </span>
                </div>
              </button>
            );
          })}
        </div>
      )}

      <div className="flex justify-between gap-2">
        <Button type="button" variant="outline" size="sm" onClick={onBack}>
          Back
        </Button>
        <Button type="button" size="sm" onClick={onNext}>
          Next
        </Button>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Step 3 — Permissions
// ---------------------------------------------------------------------------

type Step3Props = {
  skills: Skill[];
  selectedSkillNames: Set<string>;
  permissions: SkillPermission[];
  onTogglePermission: (skillName: string) => void;
  onBack: () => void;
  onNext: () => void;
};

function StepPermissions({
  skills,
  selectedSkillNames,
  permissions,
  onTogglePermission,
  onBack,
  onNext,
}: Step3Props) {
  const selectedSkills = skills.filter((s) => selectedSkillNames.has(s.name));

  return (
    <div className="flex flex-col gap-6">
      <p className="text-sm text-muted-foreground">
        Choose which tools the agent can use. <strong>Deny</strong> blocks the
        tool entirely.
      </p>

      {selectedSkills.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          No tools selected.
        </p>
      ) : (
        <div className="flex flex-col gap-2 max-h-72 overflow-y-auto pr-1">
          {selectedSkills.map((skill) => {
            const perm = permissions.find((p) => p.skillName === skill.name);
            const mode: PermissionMode = perm?.mode ?? "allow";
            return (
              <div
                key={skill.name}
                className="flex items-center justify-between rounded-lg border border-border px-3 py-2.5"
              >
                <div className="flex items-center gap-2 min-w-0">
                  <span className="text-base">{skill.emoji}</span>
                  <span className="text-sm font-medium truncate">
                    {skill.title}
                  </span>
                </div>
                <div className="flex rounded-md border border-border overflow-hidden shrink-0 ml-3">
                  <button
                    type="button"
                    onClick={() => {
                      if (mode !== "allow") onTogglePermission(skill.name);
                    }}
                    className={[
                      "px-3 py-1 text-xs transition-colors",
                      mode === "allow"
                        ? "bg-primary text-primary-foreground"
                        : "bg-background text-muted-foreground hover:bg-muted",
                    ].join(" ")}
                  >
                    Allow
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      if (mode !== "deny") onTogglePermission(skill.name);
                    }}
                    className={[
                      "px-3 py-1 text-xs transition-colors",
                      mode === "deny"
                        ? "bg-primary text-primary-foreground"
                        : "bg-background text-muted-foreground hover:bg-muted",
                    ].join(" ")}
                  >
                    Deny
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      <div className="flex justify-between gap-2">
        <Button type="button" variant="outline" size="sm" onClick={onBack}>
          Back
        </Button>
        <Button type="button" size="sm" onClick={onNext}>
          Next
        </Button>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Step 4 — Launch
// ---------------------------------------------------------------------------

type Step4Props = {
  name: string;
  selectedSkillNames: Set<string>;
  permissions: SkillPermission[];
  submitting: boolean;
  error: string | null;
  onBack: () => void;
  onLaunch: () => void;
};

function StepLaunch({
  name,
  selectedSkillNames,
  permissions,
  submitting,
  error,
  onBack,
  onLaunch,
}: Step4Props) {
  const allowCount = permissions.filter((p) => p.mode === "allow").length;
  const denyCount = permissions.filter((p) => p.mode === "deny").length;

  return (
    <div className="flex flex-col gap-6">
      <div className="rounded-lg border border-border bg-muted/30 p-4 space-y-3">
        <div className="flex items-center justify-between text-sm">
          <span className="text-muted-foreground">Name</span>
          <span className="font-medium">{name}</span>
        </div>
        <div className="flex items-center justify-between text-sm">
          <span className="text-muted-foreground">Tools selected</span>
          <span className="font-medium">{selectedSkillNames.size}</span>
        </div>
        {selectedSkillNames.size > 0 && (
          <>
            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Allowed</span>
              <span className="font-medium">{allowCount}</span>
            </div>
            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Denied</span>
              <span className="font-medium">{denyCount}</span>
            </div>
          </>
        )}
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      <div className="flex justify-between gap-2">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onBack}
          disabled={submitting}
        >
          Back
        </Button>
        <Button
          type="button"
          size="sm"
          onClick={onLaunch}
          disabled={submitting}
        >
          {submitting ? "Launching…" : "Launch agent"}
        </Button>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Main component
// ---------------------------------------------------------------------------

const STEP_LABELS = ["Quickstart", "Tools", "Permissions", "Launch"] as const;
const TOTAL_STEPS = STEP_LABELS.length;

export function NewAgentOverlay({ open, onClose }: NewAgentOverlayProps) {
  const router = useRouter();
  const { create } = useAgents();
  const { skills, loading: skillsLoading } = useSkills();
  const { providers } = useProviders();

  const [step, setStep] = useState(1);
  const [name, setName] = useState("");
  const [selectedTemplate, setSelectedTemplate] = useState<string | null>(null);
  const [selectedSkills, setSelectedSkills] = useState<Set<string>>(new Set());
  const [permissions, setPermissions] = useState<SkillPermission[]>([]);
  const [nameError, setNameError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  // Reset all state when the overlay opens
  useEffect(() => {
    if (open) {
      setStep(1);
      setName("");
      setSelectedTemplate(null);
      setSelectedSkills(new Set());
      setPermissions([]);
      setNameError(null);
      setSubmitError(null);
      setSubmitting(false);
    }
  }, [open]);

  // Sync permissions list whenever selected skills change — new skills default to "allow"
  useEffect(() => {
    setPermissions((prev) => {
      const existing = new Map(prev.map((p) => [p.skillName, p]));
      return Array.from(selectedSkills).map(
        (name) => existing.get(name) ?? { skillName: name, mode: "allow" },
      );
    });
  }, [selectedSkills]);

  const handleSelectTemplate = (id: string | null) => {
    setSelectedTemplate(id);
  };

  const handleNextFromStep1 = () => {
    if (!name.trim()) {
      setNameError("Name is required");
      return;
    }
    setNameError(null);
    setStep(2);
  };

  const handleToggleSkill = (skillName: string) => {
    setSelectedSkills((prev) => {
      const next = new Set(prev);
      if (next.has(skillName)) {
        next.delete(skillName);
      } else {
        next.add(skillName);
      }
      return next;
    });
  };

  const handleSelectAllSkills = () => {
    const available = skills
      .filter((s) => s.installed || s.configured)
      .map((s) => s.name);
    setSelectedSkills(new Set(available));
  };

  const handleClearSkills = () => {
    setSelectedSkills(new Set());
  };

  const handleTogglePermission = (skillName: string) => {
    setPermissions((prev) =>
      prev.map((p) =>
        p.skillName === skillName
          ? { ...p, mode: p.mode === "allow" ? "deny" : "allow" }
          : p,
      ),
    );
  };

  const handleLaunch = async () => {
    setSubmitError(null);
    setSubmitting(true);

    try {
      const firstProvider = providers[0]?.name ?? "anthropic";

      const agent = await create({
        name: name.trim(),
        provider: firstProvider,
      });

      const allowedSkills = permissions
        .filter((p) => p.mode === "allow")
        .map((p) => p.skillName);

      if (allowedSkills.length > 0) {
        try {
          await apiFetch(`/api/agents/${agent.id}/skills`, {
            method: "POST",
            body: JSON.stringify({ skillNames: allowedSkills }),
          });
        } catch {
          // Non-fatal: agent created, skills can be assigned later
          console.warn("Failed to assign initial skills to agent");
        }
      }

      posthog.capture("agent_created", {
        agent_name: name.trim(),
        provider: firstProvider,
        template: selectedTemplate ?? "scratch",
        skill_count: selectedSkills.size,
        allow_skills: allowedSkills.length,
        deny_skills: permissions.filter((p) => p.mode === "deny").length,
      });

      onClose();
      router.push(`/agents/${agent.id}`);
    } catch (err) {
      setSubmitError(
        err instanceof Error ? err.message : "Failed to launch agent",
      );
    } finally {
      setSubmitting(false);
    }
  };

  if (!open) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 backdrop-blur-sm">
      <div className="relative w-full max-w-[600px] mx-4 rounded-xl border border-border bg-background shadow-xl">
        <div className="flex flex-col gap-6 p-6">
          {/* Header */}
          <div className="flex flex-col gap-3">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold">Create an agent</h2>
              <button
                type="button"
                onClick={onClose}
                className="text-muted-foreground hover:text-foreground transition-colors text-sm"
                aria-label="Close"
              >
                ✕
              </button>
            </div>
            <div className="flex items-center gap-3">
              <ProgressDots step={step} total={TOTAL_STEPS} />
              <span className="text-xs text-muted-foreground">
                Step {step} of {TOTAL_STEPS} — {STEP_LABELS[step - 1]}
              </span>
            </div>
          </div>

          {/* Step content */}
          {step === 1 && (
            <StepQuickstart
              name={name}
              onNameChange={setName}
              selectedTemplate={selectedTemplate}
              onSelectTemplate={handleSelectTemplate}
              error={nameError}
              onNext={handleNextFromStep1}
              onClose={onClose}
            />
          )}

          {step === 2 && (
            <StepTools
              skills={skills}
              loading={skillsLoading}
              selected={selectedSkills}
              onToggle={handleToggleSkill}
              onSelectAll={handleSelectAllSkills}
              onClear={handleClearSkills}
              onBack={() => setStep(1)}
              onNext={() => setStep(3)}
            />
          )}

          {step === 3 && (
            <StepPermissions
              skills={skills}
              selectedSkillNames={selectedSkills}
              permissions={permissions}
              onTogglePermission={handleTogglePermission}
              onBack={() => setStep(2)}
              onNext={() => setStep(4)}
            />
          )}

          {step === 4 && (
            <StepLaunch
              name={name}
              selectedSkillNames={selectedSkills}
              permissions={permissions}
              submitting={submitting}
              error={submitError}
              onBack={() => setStep(3)}
              onLaunch={handleLaunch}
            />
          )}
        </div>
      </div>
    </div>
  );
}
