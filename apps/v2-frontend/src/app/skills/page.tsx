"use client";

import { TopBar } from "@/components/TopBar";
import { SkillCard } from "@/components/SkillCard";
import { useSkills } from "@/hooks/useSkills";

export default function SkillsPage() {
  const { skills, loading, error, install, uninstall, putCredentials } = useSkills();

  return (
    <div>
      <TopBar
        title="Skills"
        subtitle="Install skills once — every agent in the workspace inherits them."
      />

      {loading ? (
        <div className="px-8 py-12 text-sm text-[var(--eaos-text-muted)]">Loading…</div>
      ) : error ? (
        <div className="mx-8 mt-6 rounded-md border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          {error}
        </div>
      ) : (
        <div className="px-8 py-6">
          <div className="mb-4 flex items-center gap-3">
            <span className="text-sm text-[var(--eaos-text-muted)]">
              {skills.filter((s) => s.installed).length} of {skills.length} installed
            </span>
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
            {skills.map((skill) => (
              <SkillCard
                key={skill.name}
                skill={skill}
                onInstall={async () => { await install(skill.name); }}
                onUninstall={async () => { await uninstall(skill.name); }}
                onPutCredentials={async (creds) => { await putCredentials(skill.name, creds); }}
              />
            ))}
          </div>

          <div className="mt-6 rounded-md border border-[var(--eaos-border)] bg-[var(--eaos-panel)] px-4 py-3 text-xs text-[var(--eaos-text-muted)]">
            Installed skills are shared by every agent in this workspace. Agents fetch the
            live tool list every ~30s. Once a skill is installed and configured, all running
            agents will pick it up automatically.
          </div>
        </div>
      )}
    </div>
  );
}
