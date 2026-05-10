"use client";

import { use } from "react";
import { notFound } from "next/navigation";
import { PageContainer } from "@/shell/page-container";
import { Label } from "@/ui/label";
import { Skeleton } from "@/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/ui/table";
import { useAuthContext } from "@/contexts/AuthContext";
import {
  canAdministerWorkspace,
  type OrgMember,
  type WorkspacePayload,
  WorkspaceRole,
  useAddWorkspaceMember,
  useOrganization,
  useRemoveWorkspaceMember,
  useUpdateWorkspaceMemberRole,
  useWorkspaceMembers,
  useWorkspaces,
  NO_WORKSPACE_ACCESS,
  WORKSPACE_ROLE_TOOLTIPS,
  workspaceRoleTooltip,
} from "@/features/manage";

export default function WorkspaceDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const { user } = useAuthContext();
  const { organization } = useOrganization();
  const { workspaces, loading } = useWorkspaces();
  const workspace = workspaces.find((item) => item.id === id);

  if (!workspace) {
    if (loading) return <WorkspaceDetailSkeleton />;
    return notFound();
  }

  const currentOrgMember = organization?.members.find(
    (member) => member.userId === user?.id,
  );
  const isOrgAdmin =
    currentOrgMember?.role === "Owner" || currentOrgMember?.role === "Admin";

  return (
    <div className="flex min-h-screen flex-col">
      <div className="sticky top-0 z-10 bg-background">
        <PageContainer width="wide" className="border-b border-border">
          <div className="flex items-start justify-between gap-4 py-4">
            <div className="min-w-0">
              <h1 className="truncate text-lg font-semibold">
                {workspace.name}
              </h1>
              <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-muted-foreground">
                <span className="font-mono">{workspace.id}</span>
                <span>·</span>
                <span>{formatDate(workspace.createdAt)}</span>
                <span>·</span>
                <span title={workspaceRoleTooltip(workspace.role)}>
                  {workspace.role ?? "No access"}
                </span>
              </div>
            </div>
          </div>
        </PageContainer>
      </div>

      <PageContainer width="wide" className="flex flex-1 flex-col gap-6 py-4">
        <WorkspaceMembersSection
          workspace={workspace}
          members={organization?.members ?? []}
          canManageAccess={isOrgAdmin || canAdministerWorkspace(workspace.role)}
        />
      </PageContainer>
    </div>
  );
}

function WorkspaceMembersSection({
  workspace,
  members,
  canManageAccess,
}: {
  workspace: WorkspacePayload;
  members: OrgMember[];
  canManageAccess: boolean;
}) {
  const {
    members: workspaceMembers,
    loading,
    refetch,
  } = useWorkspaceMembers(workspace.id);
  const { addWorkspaceMember, loading: adding } = useAddWorkspaceMember();
  const { updateWorkspaceMemberRole, loading: updating } =
    useUpdateWorkspaceMemberRole();
  const { removeWorkspaceMember, loading: removing } =
    useRemoveWorkspaceMember();
  const saving = adding || updating || removing;
  const activeMembers = members.filter((member) => member.userId);

  async function setRole(member: OrgMember, nextRole: WorkspaceRole | "none") {
    if (!member.userId || saving || !canManageAccess) return;
    const existing = workspaceMembers.find(
      (workspaceMember) => workspaceMember.userId === member.userId,
    );

    if (nextRole === NO_WORKSPACE_ACCESS) {
      if (existing) {
        await removeWorkspaceMember(workspace.id, member.userId);
        await refetch();
      }
      return;
    }

    if (existing) {
      if (existing.role === nextRole) return;
      await updateWorkspaceMemberRole({
        workspaceId: workspace.id,
        userId: member.userId,
        role: nextRole,
      });
    } else {
      await addWorkspaceMember({
        workspaceId: workspace.id,
        userId: member.userId,
        role: nextRole,
      });
    }
    await refetch();
  }

  return (
    <section className="space-y-3">
      <div>
        <Label>Members</Label>
        <p className="text-xs text-muted-foreground">
          Assign each organization member a workspace role.
        </p>
      </div>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Name</TableHead>
            <TableHead>Email</TableHead>
            <TableHead>Organization role</TableHead>
            <TableHead className="w-44">Workspace role</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {loading &&
            activeMembers.length === 0 &&
            Array.from({ length: 4 }).map((_, index) => (
              <TableRow key={index}>
                <TableCell>
                  <Skeleton className="h-4 w-28" />
                </TableCell>
                <TableCell>
                  <Skeleton className="h-4 w-40" />
                </TableCell>
                <TableCell>
                  <Skeleton className="h-4 w-20" />
                </TableCell>
                <TableCell>
                  <Skeleton className="h-8 w-36" />
                </TableCell>
              </TableRow>
            ))}
          {activeMembers.map((member) => {
            const assignment = workspaceMembers.find(
              (workspaceMember) => workspaceMember.userId === member.userId,
            );
            const value = assignment?.role ?? NO_WORKSPACE_ACCESS;

            return (
              <TableRow key={member.id}>
                <TableCell className="font-medium">
                  {member.email.split("@")[0]}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {member.email}
                </TableCell>
                <TableCell>
                        <span className="rounded bg-muted px-1.5 py-0.5 text-xs">
                          {member.role}
                        </span>
                </TableCell>
                <TableCell>
                  <Select
                    value={value}
                    disabled={!canManageAccess || saving}
                    onValueChange={(next) =>
                      setRole(
                        member,
                        next === NO_WORKSPACE_ACCESS
                          ? NO_WORKSPACE_ACCESS
                          : (next as WorkspaceRole),
                      )
                    }
                  >
                    <SelectTrigger
                      title={workspaceRoleTooltip(assignment?.role)}
                      className="w-full"
                    >
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem
                        value={NO_WORKSPACE_ACCESS}
                        title={workspaceRoleTooltip(null)}
                      >
                        No access
                      </SelectItem>
                      <SelectItem
                        value={WorkspaceRole.Viewer}
                        title={WORKSPACE_ROLE_TOOLTIPS[WorkspaceRole.Viewer]}
                      >
                        {WorkspaceRole.Viewer}
                      </SelectItem>
                      <SelectItem
                        value={WorkspaceRole.Editor}
                        title={WORKSPACE_ROLE_TOOLTIPS[WorkspaceRole.Editor]}
                      >
                        {WorkspaceRole.Editor}
                      </SelectItem>
                      <SelectItem
                        value={WorkspaceRole.Admin}
                        title={WORKSPACE_ROLE_TOOLTIPS[WorkspaceRole.Admin]}
                      >
                        {WorkspaceRole.Admin}
                      </SelectItem>
                      <SelectItem
                        value={WorkspaceRole.Owner}
                        title={WORKSPACE_ROLE_TOOLTIPS[WorkspaceRole.Owner]}
                      >
                        {WorkspaceRole.Owner}
                      </SelectItem>
                    </SelectContent>
                  </Select>
                </TableCell>
              </TableRow>
            );
          })}
          {!loading && activeMembers.length === 0 && (
            <TableRow>
              <TableCell
                colSpan={4}
                className="h-24 text-center text-muted-foreground"
              >
                No active organization members found.
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </section>
  );
}

function WorkspaceDetailSkeleton() {
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
        <Skeleton className="h-24 w-full rounded-lg" />
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
