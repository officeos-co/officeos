"use client";

import { Trash2 } from "lucide-react";
import { useRouter } from "next/navigation";
import type { Agent } from "@/types/agent";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { formatDate, shortId } from "@/utils/format";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

type AgentsTableProps = {
  agents: Agent[];
  onDelete: (id: string) => void;
};

export function AgentsTable({ agents, onDelete }: AgentsTableProps) {
  const router = useRouter();

  return (
    <div className="mx-8 my-6 rounded-lg border border-border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>ID</TableHead>
            <TableHead>Name</TableHead>
            <TableHead>Provider</TableHead>
            <TableHead>Model</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Created</TableHead>
            <TableHead className="w-[48px]" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {agents.map((agent) => (
            <TableRow
              key={agent.id}
              onClick={() => router.push(`/agents/${agent.id}`)}
              className="cursor-pointer"
            >
              <TableCell className="font-mono text-xs text-muted-foreground">
                {shortId(agent.id)}
              </TableCell>
              <TableCell className="font-medium">{agent.name}</TableCell>
              <TableCell className="text-muted-foreground">{agent.provider}</TableCell>
              <TableCell className="text-muted-foreground">{agent.model ?? "\u2014"}</TableCell>
              <TableCell>
                <StatusBadge status={agent.status} />
              </TableCell>
              <TableCell className="text-muted-foreground">{formatDate(agent.createdAt)}</TableCell>
              <TableCell>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={(e) => {
                    e.stopPropagation();
                    if (confirm(`Delete agent "${agent.name}"? This removes the pod, data, and vault.`)) {
                      onDelete(agent.id);
                    }
                  }}
                  className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive"
                  aria-label={`Delete ${agent.name}`}
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
