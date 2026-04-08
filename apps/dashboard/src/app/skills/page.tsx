"use client";

// Global skills page — v1 demo, talks to /api/v1/skills_v2.
//
// Intentionally bypasses the generated orval client: this is a fresh
// backend module and we want the page to work without a codegen roundtrip.
// It uses plain fetch + the existing Bearer session token.

import { useCallback, useEffect, useState } from "react";

import { getSessionToken } from "@/auth/clerk";
import { getApiBaseUrl } from "@/lib/api-base";

export const dynamic = "force-dynamic";

type CredentialField = {
  key: string;
  label: string;
  kind: "password" | "text" | "textarea";
  required: boolean;
  placeholder?: string | null;
  help?: string | null;
};

type LlmTool = {
  name: string;
  description: string;
  parameters: Record<string, unknown>;
};

type SkillState = {
  name: string;
  title: string;
  description: string;
  emoji: string;
  installed: boolean;
  configured: boolean;
  credential_fields: CredentialField[];
  llm_tools: LlmTool[];
};

async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getSessionToken();
  const res = await fetch(`${getApiBaseUrl()}${path}`, {
    ...init,
    headers: {
      ...(init?.headers || {}),
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`${res.status} ${res.statusText}: ${text}`);
  }
  return (await res.json()) as T;
}

export default function SkillsPage() {
  const [skills, setSkills] = useState<SkillState[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editing, setEditing] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await api<{ skills: SkillState[] }>("/api/v1/skills_v2");
      setSkills(data.skills);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  const toggleInstall = async (skill: SkillState) => {
    try {
      const action = skill.installed ? "uninstall" : "install";
      await api(`/api/v1/skills_v2/${skill.name}/${action}`, { method: "POST" });
      await reload();
    } catch (e) {
      setError((e as Error).message);
    }
  };

  return (
    <main className="mx-auto max-w-5xl px-6 py-10">
      <header className="mb-8">
        <h1 className="text-2xl font-semibold">Skills</h1>
        <p className="mt-1 text-sm text-muted">
          Global skills available to every agent in the organization. Install
          a skill and paste its credentials — new turns pick it up without a
          restart.
        </p>
      </header>

      {error && (
        <div className="mb-4 rounded-md border border-red-500/40 bg-red-500/10 px-4 py-3 text-sm text-red-400">
          {error}
        </div>
      )}

      {loading && <p className="text-sm text-muted">Loading…</p>}

      <div className="grid gap-4">
        {skills.map((s) => (
          <article
            key={s.name}
            className="rounded-2xl border border-[color:var(--border)] bg-[color:var(--surface)] p-5"
          >
            <div className="flex items-start justify-between gap-4">
              <div className="flex items-start gap-3">
                <span className="text-3xl leading-none">{s.emoji}</span>
                <div>
                  <h2 className="text-lg font-medium">{s.title}</h2>
                  <p className="mt-1 text-sm text-muted">{s.description}</p>
                  <p className="mt-2 text-xs text-muted">
                    {s.llm_tools.length} tools:{" "}
                    {s.llm_tools.map((t) => t.name).join(", ")}
                  </p>
                  <div className="mt-2 flex gap-2 text-xs">
                    <span
                      className={`rounded px-2 py-0.5 ${
                        s.installed
                          ? "bg-emerald-500/15 text-emerald-400"
                          : "bg-neutral-500/15 text-neutral-400"
                      }`}
                    >
                      {s.installed ? "Installed" : "Not installed"}
                    </span>
                    <span
                      className={`rounded px-2 py-0.5 ${
                        s.configured
                          ? "bg-emerald-500/15 text-emerald-400"
                          : "bg-amber-500/15 text-amber-400"
                      }`}
                    >
                      {s.configured ? "Configured" : "Needs credentials"}
                    </span>
                  </div>
                </div>
              </div>
              <div className="flex flex-col gap-2">
                <button
                  type="button"
                  className="rounded-md border border-[color:var(--border)] px-3 py-1.5 text-sm hover:bg-white/5"
                  onClick={() => void toggleInstall(s)}
                >
                  {s.installed ? "Uninstall" : "Install"}
                </button>
                <button
                  type="button"
                  className="rounded-md border border-[color:var(--border)] px-3 py-1.5 text-sm hover:bg-white/5"
                  onClick={() => setEditing(editing === s.name ? null : s.name)}
                >
                  {editing === s.name ? "Close" : "Configure"}
                </button>
              </div>
            </div>

            {editing === s.name && (
              <CredentialsForm
                skill={s}
                onSaved={async () => {
                  setEditing(null);
                  await reload();
                }}
                onError={(m) => setError(m)}
              />
            )}
          </article>
        ))}
      </div>
    </main>
  );
}

function CredentialsForm({
  skill,
  onSaved,
  onError,
}: {
  skill: SkillState;
  onSaved: () => void | Promise<void>;
  onError: (m: string) => void;
}) {
  const [values, setValues] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      await api(`/api/v1/skills_v2/${skill.name}/credentials`, {
        method: "PUT",
        body: JSON.stringify({ credentials: values }),
      });
      await onSaved();
    } catch (err) {
      onError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <form onSubmit={submit} className="mt-5 grid gap-3 border-t border-[color:var(--border)] pt-5">
      {skill.credential_fields.map((f) => (
        <label key={f.key} className="grid gap-1 text-sm">
          <span className="font-medium">
            {f.label}
            {f.required ? " *" : ""}
          </span>
          {f.kind === "textarea" ? (
            <textarea
              className="min-h-[8rem] rounded-md border border-[color:var(--border)] bg-transparent px-3 py-2 font-mono text-xs"
              placeholder={f.placeholder ?? ""}
              value={values[f.key] ?? ""}
              onChange={(e) =>
                setValues((v) => ({ ...v, [f.key]: e.target.value }))
              }
            />
          ) : (
            <input
              type={f.kind === "password" ? "password" : "text"}
              className="rounded-md border border-[color:var(--border)] bg-transparent px-3 py-2"
              placeholder={f.placeholder ?? ""}
              value={values[f.key] ?? ""}
              onChange={(e) =>
                setValues((v) => ({ ...v, [f.key]: e.target.value }))
              }
            />
          )}
          {f.help && <span className="text-xs text-muted">{f.help}</span>}
        </label>
      ))}
      <div>
        <button
          type="submit"
          disabled={saving}
          className="rounded-md border border-[color:var(--border)] bg-white/5 px-4 py-1.5 text-sm disabled:opacity-50"
        >
          {saving ? "Saving…" : "Save credentials"}
        </button>
      </div>
    </form>
  );
}
