"use client";

import { TopBar } from "@/components/TopBar";
import { EmptyState } from "@/components/EmptyState";
import { useSkills } from "@/hooks/useSkills";

export default function SkillsPage() {
  const { skills, loading } = useSkills();

  return (
    <div>
      <TopBar
        title="Skills"
        subtitle="Install skills once — every agent in the workspace inherits them."
      />
      {loading ? (
        <div className="px-8 py-12 text-sm text-[var(--eaos-text-muted)]">Loading...</div>
      ) : skills.length === 0 ? (
        <EmptyState
          title="No skills yet"
          description="Skill marketplace is coming in Phase 2."
        />
      ) : (
        <div className="px-8 py-6 text-sm">{skills.length} skills installed</div>
      )}
    </div>
  );
}
