"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { TopBar } from "@/components/shared/TopBar";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { UploadSkillOverlay } from "@/components/skills/UploadSkillOverlay";
import { GitHubSkillOverlay } from "@/components/skills/GitHubSkillOverlay";
import { useSkills } from "@/hooks/useSkills";
import { useSkillRegistry } from "@/hooks/useSkillRegistry";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { GitBranch, Upload } from "lucide-react";

function skillStatus(s: { installed: boolean; configured: boolean }): string {
  if (s.installed && s.configured) return "ready";
  if (s.installed) return "needs credentials";
  return "not installed";
}

export default function SkillsPage() {
  const { skills, loading, error } = useSkills();
  const { entries: registryEntries } = useSkillRegistry();
  const router = useRouter();
  const [showUpload, setShowUpload] = useState(false);
  const [showGitHub, setShowGitHub] = useState(false);

  const installed = skills.filter((s) => s.installed).length;

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
        action={
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="sm" onClick={() => setShowGitHub(true)} className="h-7 text-[12px]">
              <GitBranch className="mr-1 h-3 w-3" />
              GitHub
            </Button>
            <Button variant="ghost" size="sm" onClick={() => setShowUpload(true)} className="h-7 text-[12px]">
              <Upload className="mr-1 h-3 w-3" />
              Upload
            </Button>
          </div>
        }
      />

      {error && (
        <div className="mx-6 mt-4 rounded-md border border-destructive/20 bg-destructive/5 px-4 py-2.5 text-[13px] text-destructive">
          {error}
        </div>
      )}

      {loading ? (
        <div className="px-6 py-6 space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="flex items-center gap-4 py-3">
              <Skeleton className="h-5 w-5" />
              <Skeleton className="h-4 w-32" />
              <Skeleton className="h-4 w-48" />
              <Skeleton className="h-3 w-16" />
            </div>
          ))}
        </div>
      ) : (
        <div className="px-6 py-4">
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                <TableHead className="text-[12px] font-normal text-muted-foreground w-[32px]" />
                <TableHead className="text-[12px] font-normal text-muted-foreground">Name</TableHead>
                <TableHead className="text-[12px] font-normal text-muted-foreground">Description</TableHead>
                <TableHead className="text-[12px] font-normal text-muted-foreground">Tools</TableHead>
                <TableHead className="text-[12px] font-normal text-muted-foreground">Status</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {enrichedSkills.map((skill) => (
                <TableRow
                  key={skill.name}
                  onClick={() => router.push(`/skills/${skill.name}`)}
                  className="cursor-pointer"
                >
                  <TableCell className="text-base">{skill.emoji}</TableCell>
                  <TableCell className="text-[13px] font-medium">{skill.title}</TableCell>
                  <TableCell className="text-[13px] text-muted-foreground max-w-[320px] truncate">
                    {skill.description}
                  </TableCell>
                  <TableCell className="text-[13px] text-muted-foreground">
                    {skill.llmTools.length}
                  </TableCell>
                  <TableCell>
                    <StatusBadge status={skillStatus(skill)} />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <UploadSkillOverlay open={showUpload} onClose={() => setShowUpload(false)} />
      <GitHubSkillOverlay open={showGitHub} onClose={() => setShowGitHub(false)} />
    </div>
  );
}
