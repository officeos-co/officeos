"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { Download, Trash2 } from "lucide-react";
import { TopBar } from "@/components/TopBar";
import { StatusBadge } from "@/components/StatusBadge";
import { SkillCredentialsForm } from "@/components/SkillCredentialsForm";
import { useSkills, type Skill } from "@/hooks/useSkills";

export default function SkillDetailPage() {
  const params = useParams<{ name: string }>();
  const name = params.name;
  const { skills, loading, install, uninstall, putCredentials } = useSkills();
  const [doc, setDoc] = useState<string | null>(null);
  const [docLoading, setDocLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const skill = skills.find((s) => s.name === name);

  useEffect(() => {
    setDocLoading(true);
    fetch(`/api/skills/${encodeURIComponent(name)}/doc`)
      .then(async (res) => {
        if (!res.ok) {
          setDoc(null);
          return;
        }
        setDoc(await res.text());
      })
      .catch(() => setDoc(null))
      .finally(() => setDocLoading(false));
  }, [name]);

  const handleAction = async (action: () => Promise<unknown>) => {
    setBusy(true);
    try {
      await action();
    } finally {
      setBusy(false);
    }
  };

  if (loading) {
    return (
      <div className="px-8 py-12 text-sm text-[var(--eaos-text-muted)]">Loading…</div>
    );
  }

  if (!skill) {
    return (
      <div>
        <TopBar title="Skill not found" />
        <div className="mx-8 mt-6 rounded-md border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          No skill named &ldquo;{name}&rdquo; found.
        </div>
        <div className="mx-8 mt-4">
          <Link
            href="/skills"
            className="rounded-md border border-[var(--eaos-border)] px-3 py-1 text-xs hover:bg-white hover:text-black"
          >
            ← Back to skills
          </Link>
        </div>
      </div>
    );
  }

  const status = skill.installed
    ? skill.configured
      ? "ready"
      : "needs credentials"
    : "not installed";

  return (
    <div>
      <TopBar
        title={
          <span className="flex items-center gap-3">
            <span className="text-3xl">{skill.emoji}</span>
            <span>{skill.title}</span>
          </span>
        }
        subtitle={
          <span className="flex items-center gap-2">
            <StatusBadge status={status} />
            <span>
              {skill.llmTools.length} tool{skill.llmTools.length === 1 ? "" : "s"}
            </span>
          </span>
        }
        action={
          <div className="flex items-center gap-2">
            {skill.installed ? (
              <button
                type="button"
                onClick={() => handleAction(() => uninstall(skill.name))}
                disabled={busy}
                className="flex items-center gap-1 rounded-md border border-red-500/40 px-4 py-2 text-sm text-red-300 hover:bg-red-500/20 disabled:opacity-50"
              >
                <Trash2 className="h-4 w-4" />
                Uninstall
              </button>
            ) : (
              <button
                type="button"
                onClick={() => handleAction(() => install(skill.name))}
                disabled={busy}
                className="flex items-center gap-1 rounded-md bg-white px-4 py-2 text-sm font-medium text-black disabled:opacity-50"
              >
                <Download className="h-4 w-4" />
                Install
              </button>
            )}
            <Link
              href="/skills"
              className="rounded-md border border-[var(--eaos-border)] px-3 py-2 text-sm hover:bg-white hover:text-black"
            >
              ← All skills
            </Link>
          </div>
        }
      />

      <div className="mx-8 my-6 space-y-6">
        {/* Description */}
        <div className="text-sm text-[var(--eaos-text-muted)]">{skill.description}</div>

        {/* Credentials */}
        {skill.installed && (
          <section>
            <h2 className="mb-3 text-[11px] uppercase tracking-wider text-[var(--eaos-text-muted)]">
              Credentials
            </h2>
            <div className="rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)] p-4">
              <SkillCredentialsForm
                fields={skill.credentialFields}
                onSubmit={async (creds) => { await putCredentials(skill.name, creds); }}
                configured={skill.configured}
              />
            </div>
          </section>
        )}

        {/* Tools */}
        <section>
          <h2 className="mb-3 text-[11px] uppercase tracking-wider text-[var(--eaos-text-muted)]">
            Tools ({skill.llmTools.length})
          </h2>
          <div className="space-y-2">
            {skill.llmTools.map((tool) => (
              <div
                key={tool.name}
                className="rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)] px-4 py-3"
              >
                <div className="font-mono text-xs">{tool.name}</div>
                <div className="mt-1 text-[11px] text-[var(--eaos-text-muted)]">
                  {tool.description}
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* Documentation (.md) */}
        <section>
          <h2 className="mb-3 text-[11px] uppercase tracking-wider text-[var(--eaos-text-muted)]">
            SKILL.md
          </h2>
          <div className="rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)] p-4">
            {docLoading ? (
              <div className="text-sm text-[var(--eaos-text-muted)]">Loading documentation…</div>
            ) : doc ? (
              <pre className="whitespace-pre-wrap break-words font-mono text-xs leading-relaxed">
                {doc}
              </pre>
            ) : (
              <div className="text-sm text-[var(--eaos-text-muted)]">
                No documentation available for this skill.
              </div>
            )}
          </div>
        </section>
      </div>
    </div>
  );
}
