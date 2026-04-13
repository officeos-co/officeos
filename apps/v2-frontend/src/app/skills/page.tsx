"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { TopBar } from "@/components/TopBar";
import { SkillGridCard } from "@/components/SkillGridCard";
import { UploadSkillOverlay } from "@/components/UploadSkillOverlay";
import { GitHubSkillOverlay } from "@/components/GitHubSkillOverlay";
import { useSkills } from "@/hooks/useSkills";
import { useSkillRegistry } from "@/hooks/useSkillRegistry";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { GitBranch, Upload } from "lucide-react";

export default function SkillsPage() {
  const { skills, loading, error } = useSkills();
  const { entries: registryEntries } = useSkillRegistry();
  const router = useRouter();
  const [showUpload, setShowUpload] = useState(false);
  const [showGitHub, setShowGitHub] = useState(false);

  const installed = skills.filter((s) => s.installed).length;

  // Merge registry metadata into skills
  const registryMap = new Map(registryEntries.map((e) => [e.name, e]));
  const enrichedSkills = skills.map((s) => {
    const reg = registryMap.get(s.name);
    return reg
      ? { ...s, registryVersion: reg.version, registryStatus: reg.status as "active" | "disabled" }
      : s;
  });

  return (
    <div>
      <TopBar
        title="Tools"
        subtitle={`${installed} of ${skills.length} installed`}
      />

      <div className="flex gap-2 px-8 pt-4">
        <Button variant="outline" size="sm" onClick={() => setShowGitHub(true)}>
          <GitBranch className="mr-1.5 h-3.5 w-3.5" />
          Connect GitHub Repo
        </Button>
        <Button variant="outline" size="sm" onClick={() => setShowUpload(true)}>
          <Upload className="mr-1.5 h-3.5 w-3.5" />
          Upload Tool
        </Button>
      </div>

      {loading ? (
        <div className="px-8 py-6">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className="rounded-xl border border-border bg-card p-5 space-y-3">
                <div className="flex items-center gap-3">
                  <Skeleton className="h-9 w-9 rounded-lg shrink-0" />
                  <div className="flex-1 space-y-1.5">
                    <Skeleton className="h-4 w-28" />
                    <Skeleton className="h-3 w-16" />
                  </div>
                </div>
                <Skeleton className="h-3 w-full" />
                <Skeleton className="h-3 w-3/4" />
                <Skeleton className="h-8 w-24 rounded-lg mt-2" />
              </div>
            ))}
          </div>
        </div>
      ) : error ? (
        <div className="mx-8 mt-6 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      ) : (
        <div className="px-8 py-6">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2 lg:grid-cols-3">
            {enrichedSkills.map((skill) => (
              <SkillGridCard
                key={skill.name}
                skill={skill}
                onClick={() => router.push(`/skills/${skill.name}`)}
              />
            ))}
          </div>
        </div>
      )}

      <UploadSkillOverlay open={showUpload} onClose={() => setShowUpload(false)} />
      <GitHubSkillOverlay open={showGitHub} onClose={() => setShowGitHub(false)} />
    </div>
  );
}
