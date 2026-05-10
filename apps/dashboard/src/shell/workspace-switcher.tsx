"use client";

import * as React from "react";
import {
  BoxIcon,
  Building2Icon,
  CheckIcon,
  ChevronDownIcon,
  MailIcon,
  PlusIcon,
  UserIcon,
} from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/ui/dropdown-menu";
import { Input } from "@/ui/input";
import { useSidebar } from "@/ui/sidebar";
import {
  useAcceptOrganizationInvite,
  canAdministerWorkspace,
  useCreateOrganizationWorkspace,
  useCreateWorkspace,
  usePendingOrganizationInvites,
  useSwitchWorkspace,
  useWorkspaces,
  type WorkspacePayload,
} from "@/features/manage";

export function WorkspaceSwitcher() {
  const { state } = useSidebar();
  const collapsed = state === "collapsed";
  const [createOpen, setCreateOpen] = React.useState(false);
  const [name, setName] = React.useState("");
  const [search, setSearch] = React.useState("");

  const { workspaces, currentWorkspace: current, loading } = useWorkspaces();
  const { invites } = usePendingOrganizationInvites();
  const { switchWorkspace } = useSwitchWorkspace();
  const { acceptOrganizationInvite, loading: acceptingInvite } =
    useAcceptOrganizationInvite();
  const { createWorkspace, loading: creatingPersonal } = useCreateWorkspace();
  const { createOrganizationWorkspace, loading: creatingOrganization } =
    useCreateOrganizationWorkspace();
  const creating = creatingPersonal || creatingOrganization;
  const createInOrganization =
    current?.ownerKind === "organization" &&
    current.organizationId &&
    canAdministerWorkspace(current.role);
  const visibleWorkspaces = workspaces.filter((workspace) =>
    workspace.name.toLowerCase().includes(search.trim().toLowerCase()),
  );
  const personalWorkspaces = visibleWorkspaces.filter(
    (workspace) => workspace.ownerKind === "personal",
  );
  const organizationWorkspaces = visibleWorkspaces.filter(
    (workspace) => workspace.ownerKind === "organization",
  );

  async function handleSwitch(id: string) {
    if (id === current?.id) return;
    await switchWorkspace(id);
  }

  async function handleCreate(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) return;
    if (createInOrganization && current?.organizationId) {
      await createOrganizationWorkspace({
        organizationId: current.organizationId,
        name: trimmed,
      });
    } else {
      await createWorkspace(trimmed);
    }
    setName("");
    setCreateOpen(false);
  }

  async function handleAcceptInvite(memberId: string, organizationName: string) {
    await acceptOrganizationInvite(memberId);
    toast.success(`Joined ${organizationName}`);
  }

  if (collapsed) return null;

  return (
    <>
      <DropdownMenu
        onOpenChange={(open) => {
          if (!open) setSearch("");
        }}
      >
        <DropdownMenuTrigger
          render={
            <button
              type="button"
              className="mx-2 mb-2 flex h-9 w-[calc(100%-1rem)] items-center gap-2 rounded-md border border-sidebar-border bg-sidebar px-2 text-left text-sm text-sidebar-foreground shadow-xs transition-colors hover:border-sidebar-ring focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sidebar-ring"
            />
          }
        >
          {current?.ownerKind === "organization" ? (
            <Building2Icon className="size-4 shrink-0 text-primary" />
          ) : (
            <BoxIcon className="size-4 shrink-0 text-primary" />
          )}
          <span className="min-w-0 flex-1 truncate">
            {loading ? "Workspace" : (current?.name ?? "Default")}
          </span>
          <ChevronDownIcon className="size-4 shrink-0 opacity-60" />
        </DropdownMenuTrigger>
        <DropdownMenuContent
          side="bottom"
          align="start"
          sideOffset={4}
          className="w-64 rounded-xl p-0"
        >
          <div className="p-2">
            <Input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              onKeyDown={(event) => event.stopPropagation()}
              placeholder="Search workspaces..."
              className="h-9 border-0 bg-muted/60 shadow-none focus-visible:ring-0"
            />
          </div>
          <div className="max-h-56 overflow-y-auto px-1 pb-1">
            {invites.length > 0 && (
              <div className="py-1">
                <div className="px-2 py-1 text-xs font-medium text-muted-foreground">
                  Invitations
                </div>
                {invites.map((invite) => (
                  <DropdownMenuItem
                    key={invite.id}
                    onClick={() =>
                      void handleAcceptInvite(invite.id, invite.organizationName)
                    }
                    disabled={acceptingInvite}
                    className="h-auto items-start gap-2 py-2"
                  >
                    <MailIcon className="mt-0.5 size-4 shrink-0 text-primary" />
                    <span className="min-w-0 flex-1">
                      <span className="block truncate">
                        {invite.organizationName}
                      </span>
                      <span className="block text-xs text-muted-foreground">
                        Accept invite as {invite.role}
                      </span>
                    </span>
                  </DropdownMenuItem>
                ))}
              </div>
            )}
            <WorkspaceGroup
              label="Personal"
              workspaces={personalWorkspaces}
              currentId={current?.id}
              onSwitch={handleSwitch}
            />
            <WorkspaceGroup
              label="Organizations"
              workspaces={organizationWorkspaces}
              currentId={current?.id}
              onSwitch={handleSwitch}
            />
          </div>
          <DropdownMenuSeparator className="my-0" />
          <DropdownMenuItem
            onClick={() => {
              setCreateOpen(true);
            }}
            className="m-1 h-9 gap-2"
          >
            <PlusIcon className="size-4" />
            <span>Create workspace</span>
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>New workspace</DialogTitle>
          </DialogHeader>
          <form className="space-y-4" onSubmit={handleCreate}>
            <Input
              autoFocus
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder="Workspace name"
            />
            <DialogFooter>
              <Button type="submit" disabled={creating || !name.trim()}>
                Create
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </>
  );
}

function WorkspaceGroup({
  label,
  workspaces,
  currentId,
  onSwitch,
}: {
  label: string;
  workspaces: WorkspacePayload[];
  currentId?: string;
  onSwitch: (id: string) => Promise<void>;
}) {
  if (workspaces.length === 0) return null;

  return (
    <div className="py-1">
      <div className="px-2 py-1 text-xs font-medium text-muted-foreground">
        {label}
      </div>
      {workspaces.map((workspace) => (
        <DropdownMenuItem
          key={workspace.id}
          onClick={() => void onSwitch(workspace.id)}
          className="h-9 gap-2"
        >
          {workspace.ownerKind === "organization" ? (
            <Building2Icon className="size-4 text-primary" />
          ) : (
            <UserIcon className="size-4 text-primary" />
          )}
          <span className="min-w-0 flex-1 truncate">{workspace.name}</span>
          {workspace.role && workspace.ownerKind === "organization" && (
            <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] leading-none text-muted-foreground">
              {workspace.role}
            </span>
          )}
          {workspace.id === currentId && (
            <CheckIcon className="size-4 text-primary" />
          )}
        </DropdownMenuItem>
      ))}
    </div>
  );
}
