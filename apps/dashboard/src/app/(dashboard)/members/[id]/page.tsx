"use client";

import { use } from "react";
import { notFound, useRouter } from "next/navigation";
import { PageContainer } from "@/shell/page-container";
import { Skeleton } from "@/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/ui/table";
import {
  useOrganization,
  useOrganizationWorkspaceMembers,
  useWorkspaces,
  workspaceRoleTooltip,
} from "@/features/manage";

export default function MemberDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const router = useRouter();
  const { id } = use(params);
  const { organization, loading: organizationLoading } = useOrganization();
  const { workspaces, currentWorkspace, loading: workspacesLoading } =
    useWorkspaces();
  const { members: workspaceMembers, loading: membershipsLoading } =
    useOrganizationWorkspaceMembers(organization?.id);
  const member = organization?.members.find((item) => item.id === id);

  if (!member) {
    if (organizationLoading) return <MemberDetailSkeleton />;
    return notFound();
  }

  const activeOrganizationId =
    currentWorkspace?.ownerKind === "organization"
      ? currentWorkspace.organizationId
      : null;
  const organizationWorkspaces = activeOrganizationId
    ? workspaces.filter(
        (workspace) =>
          workspace.ownerKind === "organization" &&
          workspace.organizationId === activeOrganizationId,
      )
    : [];
  const memberWorkspaceRows = member.userId
    ? organizationWorkspaces
        .map((workspace) => {
          const assignment = workspaceMembers.find(
            (workspaceMember) =>
              workspaceMember.workspaceId === workspace.id &&
              workspaceMember.userId === member.userId,
          );
          return assignment ? { workspace, assignment } : null;
        })
        .filter((row): row is NonNullable<typeof row> => Boolean(row))
    : [];
  const loading =
    workspacesLoading || membershipsLoading || organizationLoading;

  return (
    <div className="flex min-h-screen flex-col">
      <div className="sticky top-0 z-10 bg-background">
        <PageContainer width="wide" className="border-b border-border">
          <div className="flex items-start justify-between gap-4 py-4">
            <div className="min-w-0">
              <h1 className="truncate text-lg font-semibold">
                {member.email.split("@")[0]}
              </h1>
              <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-muted-foreground">
                <span>{member.email}</span>
                <span>·</span>
                <span>{member.role}</span>
                <span>·</span>
                <span>{member.status}</span>
              </div>
            </div>
          </div>
        </PageContainer>
      </div>

      <PageContainer width="wide" className="flex flex-1 flex-col gap-4 py-4">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Workspace</TableHead>
              <TableHead>Role</TableHead>
              <TableHead>Created at</TableHead>
              <TableHead>ID</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading &&
              memberWorkspaceRows.length === 0 &&
              Array.from({ length: 4 }).map((_, index) => (
                <TableRow key={index}>
                  <TableCell>
                    <Skeleton className="h-4 w-36" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-20" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-32" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-52" />
                  </TableCell>
                </TableRow>
              ))}
            {memberWorkspaceRows.map(({ workspace, assignment }) => (
              <TableRow
                key={workspace.id}
                className="cursor-pointer"
                onClick={() => router.push(`/workspaces/${workspace.id}`)}
              >
                <TableCell className="font-medium">{workspace.name}</TableCell>
                <TableCell>
                  <span
                    title={workspaceRoleTooltip(assignment.role)}
                    className="rounded bg-muted px-1.5 py-0.5 text-xs"
                  >
                    {assignment.role}
                  </span>
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {formatDate(workspace.createdAt)}
                </TableCell>
                <TableCell className="font-mono text-xs text-muted-foreground">
                  {workspace.id}
                </TableCell>
              </TableRow>
            ))}
            {!loading && memberWorkspaceRows.length === 0 && (
              <TableRow>
                <TableCell
                  colSpan={4}
                  className="h-24 text-center text-muted-foreground"
                >
                  {member.userId
                    ? "This member does not have access to any organization workspaces."
                    : "This invitation has not been accepted yet."}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </PageContainer>
    </div>
  );
}

function MemberDetailSkeleton() {
  return (
    <>
      <div className="sticky top-0 z-10 bg-background">
        <PageContainer width="wide" className="border-b border-border">
          <div className="space-y-2 py-4">
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-3 w-96" />
          </div>
        </PageContainer>
      </div>
      <PageContainer width="wide" className="flex flex-1 flex-col gap-4 py-4">
        <Skeleton className="h-64 w-full rounded-lg" />
      </PageContainer>
    </>
  );
}

function formatDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}
