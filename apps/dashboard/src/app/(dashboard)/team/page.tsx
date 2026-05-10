"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { PageHeader } from "@/shell/page-header";
import { PageContainer } from "@/shell/page-container";
import { Button } from "@/ui/button";
import { HelpTooltip, WithTooltip } from "@/ui/help-tooltip";
import { Input } from "@/ui/input";
import { Label } from "@/ui/label";
import { Separator } from "@/ui/separator";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/ui/dialog";
import {
  Building2Icon,
  PlusIcon,
  ShieldIcon,
  Trash2Icon,
  UserPlusIcon,
} from "lucide-react";
import { Skeleton } from "@/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/ui/select";
import { useAuthContext } from "@/contexts/AuthContext";
import {
  type WorkspaceRole,
  useOrganization,
  useInviteMember,
  useRemoveMember,
  useRenameOrg,
  useWorkspaces,
  useCreateOrganizationWorkspace,
  useDeleteWorkspace,
  useAddWorkspaceMember,
  useUpdateWorkspaceMemberRole,
  useGrantWorkspaceToOrganization,
  useRevokeWorkspaceOrganizationGrant,
} from "@/features/manage";

export default function TeamPage() {
  const { user } = useAuthContext();
  const { organization, loading, error } = useOrganization();
  const { workspaces } = useWorkspaces();
  const { inviteMember } = useInviteMember();
  const { removeMember } = useRemoveMember();
  const { renameOrg } = useRenameOrg();
  const { createOrganizationWorkspace } = useCreateOrganizationWorkspace();
  const { deleteWorkspace } = useDeleteWorkspace();
  const { addWorkspaceMember } = useAddWorkspaceMember();
  const { updateWorkspaceMemberRole } = useUpdateWorkspaceMemberRole();
  const { grantWorkspaceToOrganization } = useGrantWorkspaceToOrganization();
  const { revokeWorkspaceOrganizationGrant } =
    useRevokeWorkspaceOrganizationGrant();

  const [inviteOpen, setInviteOpen] = useState(false);
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteRole, setInviteRole] = useState<"Admin" | "Member">("Member");
  const [workspaceOpen, setWorkspaceOpen] = useState(false);
  const [workspaceName, setWorkspaceName] = useState("");
  const [accessWorkspaceId, setAccessWorkspaceId] = useState("");
  const [accessUserId, setAccessUserId] = useState("");
  const [accessRole, setAccessRole] = useState<WorkspaceRole>("Editor");
  const [grantWorkspaceId, setGrantWorkspaceId] = useState("");
  const [grantOrganizationId, setGrantOrganizationId] = useState("");
  const [grantRole, setGrantRole] = useState<WorkspaceRole>("Viewer");
  const [orgName, setOrgName] = useState("");
  const serverName = organization?.name ?? "";
  if (orgName === "" && serverName !== "") setOrgName(serverName);

  useEffect(() => {
    if (error)
      toast.error("Failed to load team", { description: error.message });
  }, [error]);

  async function handleInvite() {
    if (!inviteEmail.includes("@")) return;
    await inviteMember({ email: inviteEmail, role: inviteRole });
    setInviteEmail("");
    setInviteRole("Member");
    setInviteOpen(false);
  }

  async function handleRemove(memberId: string) {
    await removeMember(memberId);
  }

  async function handleRenameBlur() {
    if (organization && orgName && orgName !== organization.name) {
      await renameOrg(orgName);
    }
  }

  async function handleCreateWorkspace() {
    const trimmed = workspaceName.trim();
    if (!organization || !trimmed) return;
    await createOrganizationWorkspace({
      organizationId: organization.id,
      name: trimmed,
    });
    setWorkspaceName("");
    setWorkspaceOpen(false);
    toast.success("Workspace created");
  }

  async function handleDeleteWorkspace(id: string) {
    await deleteWorkspace(id);
    toast.success("Workspace deleted");
  }

  async function handleSetAccess() {
    if (!accessWorkspaceId || !accessUserId) return;
    await addWorkspaceMember({
      workspaceId: accessWorkspaceId,
      userId: accessUserId,
      role: accessRole,
    });
    await updateWorkspaceMemberRole({
      workspaceId: accessWorkspaceId,
      userId: accessUserId,
      role: accessRole,
    });
    toast.success("Workspace access updated");
  }

  async function handleGrantOrganization() {
    if (!grantWorkspaceId || !grantOrganizationId) return;
    await grantWorkspaceToOrganization({
      workspaceId: grantWorkspaceId,
      organizationId: grantOrganizationId,
      maxRole: grantRole,
    });
    setGrantOrganizationId("");
    toast.success("Workspace grant saved");
  }

  async function handleRevokeOrganizationGrant() {
    if (!grantWorkspaceId || !grantOrganizationId) return;
    await revokeWorkspaceOrganizationGrant(grantWorkspaceId, grantOrganizationId);
    setGrantOrganizationId("");
    toast.success("Workspace grant revoked");
  }

  if (loading && !organization) {
    return (
      <>
        <PageHeader
          page="Team"
          subtitle="Manage organization members and access."
          width="narrow"
        />
        <PageContainer width="narrow" className="flex flex-1 flex-col gap-6 pb-4">
          <section>
            <Skeleton className="h-4 w-24 mb-3" />
            <Skeleton className="h-9 w-64 rounded-md" />
          </section>
          <Skeleton className="h-px w-full" />
          <section>
            <Skeleton className="h-4 w-28 mb-3" />
            <div className="space-y-2">
              {Array.from({ length: 3 }).map((_, i) => (
                <Skeleton key={i} className="h-10 w-full rounded-md" />
              ))}
            </div>
          </section>
        </PageContainer>
      </>
    );
  }

  if (!organization) {
    return (
      <>
        <PageHeader
          page="Team"
          subtitle="Manage organization members and access."
          width="narrow"
        />
        <div className="flex items-center justify-center py-20">
          <p className="text-sm text-muted-foreground">
            Unable to load team information.
          </p>
        </div>
      </>
    );
  }

  const members = organization.members;
  const activeMembers = members.filter((member) => member.userId);
  const orgWorkspaces = workspaces.filter(
    (workspace) => workspace.organizationId === organization.id,
  );
  const currentOrgMember = members.find((member) => member.userId === user?.id);
  const isOrgAdmin =
    currentOrgMember?.role === "Owner" || currentOrgMember?.role === "Admin";
  const selectedAccessWorkspace = orgWorkspaces.find(
    (workspace) => workspace.id === accessWorkspaceId,
  );
  const selectedGrantWorkspace = orgWorkspaces.find(
    (workspace) => workspace.id === grantWorkspaceId,
  );

  return (
    <>
      <PageHeader
        page="Team"
        subtitle="Manage organization members and access."
        width="narrow"
        action={
          <WithTooltip tooltip="Invite a teammate into this organization. Member access can be reviewed here.">
            <Button size="sm" onClick={() => setInviteOpen(true)}>
              <UserPlusIcon className="size-3.5" />
              Invite member
            </Button>
          </WithTooltip>
        }
      />
      <PageContainer width="narrow" className="flex flex-1 flex-col gap-6 pb-4">
        <section>
          <h3 className="text-sm font-semibold mb-3">Organization</h3>
          <div className="space-y-2 max-w-sm">
            <Label>
              Organization name
              <HelpTooltip>
                Used for workspace display. Renaming does not change billing or
                provider configuration.
              </HelpTooltip>
            </Label>
            <Input
              value={orgName}
              onChange={(e) => setOrgName(e.target.value)}
              onBlur={handleRenameBlur}
            />
          </div>
        </section>

        <Separator />

        <section>
          <div className="mb-3 flex items-center justify-between gap-3">
            <h3 className="text-sm font-semibold">
              Workspaces ({orgWorkspaces.length})
            </h3>
            {isOrgAdmin && (
              <WithTooltip tooltip="Create a workspace owned by this organization.">
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => setWorkspaceOpen(true)}
                >
                  <PlusIcon className="size-3.5" />
                  New workspace
                </Button>
              </WithTooltip>
            )}
          </div>
          <div className="divide-y rounded-md border">
            {orgWorkspaces.length === 0 ? (
              <div className="px-3 py-6 text-sm text-muted-foreground">
                No organization workspaces available.
              </div>
            ) : (
              orgWorkspaces.map((workspace) => (
                <div
                  key={workspace.id}
                  className="flex min-h-12 items-center gap-3 px-3 py-2"
                >
                  <Building2Icon className="size-4 text-primary" />
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <span className="truncate text-sm font-medium">
                        {workspace.name}
                      </span>
                      {workspace.isDefault && (
                        <span className="rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground">
                          default
                        </span>
                      )}
                    </div>
                    <div className="text-xs text-muted-foreground">
                      {workspace.role ?? "No role"}
                    </div>
                  </div>
                  {isOrgAdmin && !workspace.isDefault && (
                    <WithTooltip tooltip="Delete this workspace and its scoped resources.">
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive"
                        onClick={() => handleDeleteWorkspace(workspace.id)}
                      >
                        <Trash2Icon className="size-3.5" />
                      </Button>
                    </WithTooltip>
                  )}
                </div>
              ))
            )}
          </div>
        </section>

        <Separator />

        <section>
          <h3 className="mb-3 text-sm font-semibold">Workspace access</h3>
          <div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_140px_auto]">
            <Select
              value={accessWorkspaceId || undefined}
              onValueChange={(value) => value && setAccessWorkspaceId(value)}
            >
              <SelectTrigger className="w-full">
                <SelectValue placeholder="Workspace">
                  {selectedAccessWorkspace?.name}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                {orgWorkspaces.map((workspace) => (
                  <SelectItem key={workspace.id} value={workspace.id}>
                    {workspace.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Select
              value={accessUserId || undefined}
              onValueChange={(value) => value && setAccessUserId(value)}
            >
              <SelectTrigger className="w-full">
                <SelectValue placeholder="Member">
                  {activeMembers.find((member) => member.userId === accessUserId)
                    ?.email}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                {activeMembers.map((member) => (
                  <SelectItem key={member.id} value={member.userId!}>
                    {member.email}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <WorkspaceRoleSelect value={accessRole} onChange={setAccessRole} />
            <Button
              size="sm"
              onClick={handleSetAccess}
              disabled={!isOrgAdmin || !accessWorkspaceId || !accessUserId}
            >
              <ShieldIcon className="size-3.5" />
              Set access
            </Button>
          </div>
        </section>

        <Separator />

        <section>
          <h3 className="mb-3 text-sm font-semibold">External organization access</h3>
          <div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_140px_auto_auto]">
            <Select
              value={grantWorkspaceId || undefined}
              onValueChange={(value) => value && setGrantWorkspaceId(value)}
            >
              <SelectTrigger className="w-full">
                <SelectValue placeholder="Workspace">
                  {selectedGrantWorkspace?.name}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                {orgWorkspaces.map((workspace) => (
                  <SelectItem key={workspace.id} value={workspace.id}>
                    {workspace.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Input
              value={grantOrganizationId}
              onChange={(event) => setGrantOrganizationId(event.target.value)}
              placeholder="Organization ID"
            />
            <WorkspaceRoleSelect value={grantRole} onChange={setGrantRole} />
            <Button
              size="sm"
              onClick={handleGrantOrganization}
              disabled={!isOrgAdmin || !grantWorkspaceId || !grantOrganizationId}
            >
              Grant
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={handleRevokeOrganizationGrant}
              disabled={!isOrgAdmin || !grantWorkspaceId || !grantOrganizationId}
            >
              Revoke
            </Button>
          </div>
        </section>

        <Separator />

        <section>
          <h3 className="text-sm font-semibold mb-3">
            Members ({members.length})
          </h3>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-left">
                <th className="px-0 py-2.5 font-medium">Name</th>
                <th className="px-0 py-2.5 font-medium">Email</th>
                <th className="px-0 py-2.5 font-medium">Role</th>
                <th className="px-0 py-2.5 font-medium">Joined</th>
                <th className="px-0 py-2.5 w-[40px]" />
              </tr>
            </thead>
            <tbody>
              {members.map((m) => (
                <tr key={m.id} className="border-b last:border-0">
                  <td className="px-0 py-2.5 font-medium">
                    {m.email.split("@")[0]}
                  </td>
                  <td className="px-0 py-2.5 text-muted-foreground">
                    {m.email}
                  </td>
                  <td className="px-0 py-2.5">
                    <WithTooltip tooltip={m.role === "Owner" ? "Owners manage organization settings and members." : "Members can use organization resources according to their assigned access."}>
                      <span
                        className={`rounded bg-muted px-1.5 py-0.5 text-xs ${m.role === "Owner" ? "font-medium" : ""}`}
                      >
                        {m.role}
                      </span>
                    </WithTooltip>
                    {m.status === "invited" && (
                      <span className="ml-2 rounded bg-amber-100 text-amber-900 px-1.5 py-0.5 text-xs">
                        invited
                      </span>
                    )}
                  </td>
                  <td className="px-0 py-2.5 text-muted-foreground">
                    {m.joinedAgo}
                  </td>
                  <td className="px-0 py-2.5">
                    {m.role !== "Owner" && (
                      <WithTooltip tooltip="Remove this member from the organization.">
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive"
                          onClick={() => handleRemove(m.id)}
                        >
                          <Trash2Icon className="size-3.5" />
                        </Button>
                      </WithTooltip>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      </PageContainer>

      <Dialog open={inviteOpen} onOpenChange={setInviteOpen}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Invite team member</DialogTitle>
            <DialogDescription>
              They&apos;ll receive an email invitation to join your
              organization.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3 pt-2">
            <div className="space-y-1.5">
              <Label>
                Email address
                <HelpTooltip>
                  The invitation is sent to this email address.
                </HelpTooltip>
              </Label>
              <Input
                type="email"
                value={inviteEmail}
                onChange={(e) => setInviteEmail(e.target.value)}
                placeholder="colleague@company.com"
              />
            </div>
            <div className="space-y-1.5">
              <Label>Organization role</Label>
              <Select
                value={inviteRole}
                onValueChange={(value) =>
                  value && setInviteRole(value as "Admin" | "Member")
                }
              >
                <SelectTrigger className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Member">Member</SelectItem>
                  <SelectItem value="Admin">Admin</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setInviteOpen(false)}
            >
              Cancel
            </Button>
            <WithTooltip tooltip="Send the organization invitation.">
              <Button
                size="sm"
                onClick={handleInvite}
                disabled={!inviteEmail.includes("@")}
              >
                Send invite
              </Button>
            </WithTooltip>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={workspaceOpen} onOpenChange={setWorkspaceOpen}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Create organization workspace</DialogTitle>
            <DialogDescription>
              Resources and integrations in this workspace are scoped to this
              organization workspace.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3 pt-2">
            <Label>Workspace name</Label>
            <Input
              autoFocus
              value={workspaceName}
              onChange={(event) => setWorkspaceName(event.target.value)}
              placeholder="Operations"
            />
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
              disabled={!workspaceName.trim()}
            >
              Create
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}

function WorkspaceRoleSelect({
  value,
  onChange,
}: {
  value: WorkspaceRole;
  onChange: (value: WorkspaceRole) => void;
}) {
  return (
    <Select
      value={value}
      onValueChange={(next) => next && onChange(next as WorkspaceRole)}
    >
      <SelectTrigger className="w-full">
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="Viewer">Viewer</SelectItem>
        <SelectItem value="Editor">Editor</SelectItem>
        <SelectItem value="Admin">Admin</SelectItem>
      </SelectContent>
    </Select>
  );
}
