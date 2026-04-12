"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { TopBar } from "@/components/TopBar";
import { SkillGridCard } from "@/components/SkillGridCard";
import { UploadSkillOverlay } from "@/components/UploadSkillOverlay";
import { GitHubSkillOverlay } from "@/components/GitHubSkillOverlay";
import { useSkills } from "@/hooks/useSkills";
import { Button } from "@/components/ui/button";
import { GitBranch, Upload } from "lucide-react";

export default function SkillsPage() {
  const { skills, loading, error } = useSkills();
  const router = useRouter();
  const [showUpload, setShowUpload] = useState(false);
  const [showGitHub, setShowGitHub] = useState(false);

  const installed = skills.filter((s) => s.installed).length;

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
        <div className="px-8 py-12 text-sm text-muted-foreground">Loading...</div>
      ) : error ? (
        <div className="mx-8 mt-6 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      ) : (
        <div className="px-8 py-6">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2 lg:grid-cols-3">
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

      <UploadSkillOverlay open={showUpload} onClose={() => setShowUpload(false)} />
      <GitHubSkillOverlay open={showGitHub} onClose={() => setShowGitHub(false)} />
    </div>
  );
}
