"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { PlusIcon } from "lucide-react";
import { PageHeader } from "@/shell/page-header";
import { PageContainer } from "@/shell/page-container";
import { Button } from "@/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/ui/dialog";
import { Input } from "@/ui/input";
import { Label } from "@/ui/label";
import { Skeleton } from "@/ui/skeleton";
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
  useCreateOrganizationWorkspace,
  useOrganization,
  useWorkspaces,
  workspaceRoleTooltip,
} from "@/features/manage";

export default function WorkspacesPage() {
  const router = useRouter();
  const { user } = useAuthContext();
  const { organization } = useOrganization();
  const {
    workspaces,
    currentWorkspace,
    loading: workspacesLoading,
  } = useWorkspaces();
  const { createOrganizationWorkspace, loading: creating } =
    useCreateOrganizationWorkspace();
  const [workspaceOpen, setWorkspaceOpen] = useState(false);
  const [workspaceName, setWorkspaceName] = useState("");
  const activeOrganizationId =
    currentWorkspace?.ownerKind === "organization"
      ? currentWorkspace.organizationId
      : organization?.id ?? null;
  const currentOrgMember = organization?.members.find(
    (member) => member.userId === user?.id,
  );
  const isOrgAdmin =
    currentOrgMember?.role === "Owner" || currentOrgMember?.role === "Admin";
  const canCreateOrganizationWorkspace =
    Boolean(activeOrganizationId) &&
    (isOrgAdmin ||
      (currentWorkspace?.ownerKind === "organization" &&
        canAdministerWorkspace(currentWorkspace?.role)));
  const personalWorkspaceContext =
    currentWorkspace?.ownerKind === "personal" && organization
      ? `You're in a personal workspace. Showing organization settings for ${organization.name}.`
      : null;
  const organizationWorkspaces = activeOrganizationId
    ? workspaces.filter(
        (workspace) =>
          workspace.ownerKind === "organization" &&
          workspace.organizationId === activeOrganizationId,
      )
    : [];

  async function handleCreateWorkspace() {
    const name = workspaceName.trim();
    if (!activeOrganizationId || !name) return;
    await createOrganizationWorkspace({
      organizationId: activeOrganizationId,
      name,
    });
    setWorkspaceName("");
    setWorkspaceOpen(false);
  }

  if (workspacesLoading && !currentWorkspace) {
    return (
      <>
        <PageHeader
          page="Workspaces"
          subtitle="Manage organization workspaces."
          width="thin"
        />
        <PageContainer width="thin" className="flex flex-1 flex-col gap-3 pb-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-12 w-full rounded-md" />
          ))}
        </PageContainer>
      </>
    );
  }

  return (
    <>
      <PageHeader
        page="Workspaces"
        subtitle={
          personalWorkspaceContext ?? "Manage organization workspaces."
        }
        width="thin"
        action={
          canCreateOrganizationWorkspace ? (
            <Button size="sm" onClick={() => setWorkspaceOpen(true)}>
              <PlusIcon className="size-3.5" />
              New workspace
            </Button>
          ) : null
        }
      />
      <PageContainer width="thin" className="flex flex-1 flex-col gap-4 pb-4">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Your role</TableHead>
              <TableHead>Created at</TableHead>
              <TableHead>ID</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {organizationWorkspaces.map((workspace) => (
              <TableRow
                key={workspace.id}
                className="cursor-pointer"
                onClick={() => router.push(`/workspaces/${workspace.id}`)}
              >
                <TableCell className="font-medium">{workspace.name}</TableCell>
                <TableCell>
                  <span
                    title={workspaceRoleTooltip(workspace.role)}
                    className="rounded bg-muted px-1.5 py-0.5 text-xs"
                  >
                    {workspace.role ?? "No access"}
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
            {organizationWorkspaces.length === 0 && (
              <TableRow>
                <TableCell
                  colSpan={4}
                  className="h-24 text-center text-muted-foreground"
                >
                  {activeOrganizationId ? (
                    "No organization workspaces found."
                  ) : (
                    <span className="inline-flex flex-col items-center gap-3">
                      <span>No organization context selected.</span>
                      <Button
                        size="sm"
                        variant="outline"
                        render={<Link href="/organization" />}
                      >
                        Open Organization
                      </Button>
                    </span>
                  )}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </PageContainer>

      <Dialog open={workspaceOpen} onOpenChange={setWorkspaceOpen}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Create workspace</DialogTitle>
            <DialogDescription>
              Create a workspace owned by this organization.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3 pt-2">
            <div className="space-y-1.5">
              <Label htmlFor="workspace-name">Workspace name</Label>
              <Input
                id="workspace-name"
                autoFocus
                value={workspaceName}
                onChange={(event) => setWorkspaceName(event.target.value)}
                placeholder="Operations"
              />
            </div>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setWorkspaceOpen(false)}
            >
              Cancel
            </Button>
            <Button
              size="sm"
              onClick={handleCreateWorkspace}
              disabled={
                creating || !activeOrganizationId || !workspaceName.trim()
              }
            >
              Create
            </Button>
          </div>
        </DialogContent>
      </Dialog>
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
