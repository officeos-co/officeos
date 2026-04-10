"use client";

import { useRouter } from "next/navigation";
import { TopBar } from "@/components/TopBar";
import { SkillGridCard } from "@/components/SkillGridCard";
import { useSkills } from "@/hooks/useSkills";

export default function SkillsPage() {
  const { skills, loading, error } = useSkills();
  const router = useRouter();

  const installed = skills.filter((s) => s.installed).length;

  return (
    <div>
      <TopBar
        title="Skills"
        subtitle={`${installed} of ${skills.length} installed — every agent inherits them.`}
      />

      {loading ? (
        <div className="px-8 py-12 text-sm text-[var(--eaos-text-muted)]">Loading…</div>
      ) : error ? (
        <div className="mx-8 mt-6 rounded-md border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          {error}
        </div>
      ) : (
        <div className="px-8 py-6">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
            {skills.map((skill) => (
              <SkillGridCard
                key={skill.name}
                skill={skill}
                onClick={() => router.push(`/skills/${skill.name}`)}
              />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
