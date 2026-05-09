"use client";

import * as React from "react";
import { gql, useMutation, useQuery } from "@apollo/client";
import {
  BoxIcon,
  CheckIcon,
  ChevronDownIcon,
  PlusIcon,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { useSidebar } from "@/components/ui/sidebar";
import { apolloClient } from "@/lib/graphql/client";

type Workspace = {
  id: string;
  name: string;
};

const WORKSPACES_QUERY = gql`
  query Workspaces {
    workspaces {
      id
      name
    }
    currentWorkspace {
      id
      name
    }
  }
`;

const SWITCH_WORKSPACE = gql`
  mutation SwitchWorkspace($id: UUID!) {
    switchWorkspace(id: $id) {
      id
      name
    }
  }
`;

const CREATE_WORKSPACE = gql`
  mutation CreateWorkspace($input: CreateWorkspaceInput!) {
    createWorkspace(input: $input) {
      id
      name
    }
  }
`;

export function WorkspaceSwitcher() {
  const { state } = useSidebar();
  const collapsed = state === "collapsed";
  const [createOpen, setCreateOpen] = React.useState(false);
  const [name, setName] = React.useState("");
  const [search, setSearch] = React.useState("");

  const { data, loading } = useQuery<{
    workspaces: Workspace[];
    currentWorkspace: Workspace;
  }>(WORKSPACES_QUERY);

  const [switchWorkspace] = useMutation(SWITCH_WORKSPACE);
  const [createWorkspace, { loading: creating }] = useMutation(CREATE_WORKSPACE);

  const current = data?.currentWorkspace;
  const workspaces = data?.workspaces ?? [];
  const visibleWorkspaces = workspaces.filter((workspace) =>
    workspace.name.toLowerCase().includes(search.trim().toLowerCase()),
  );

  async function handleSwitch(id: string) {
    if (id === current?.id) return;
    await switchWorkspace({ variables: { id } });
    await apolloClient.resetStore();
  }

  async function handleCreate(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) return;
    await createWorkspace({ variables: { input: { name: trimmed } } });
    setName("");
    setCreateOpen(false);
    await apolloClient.resetStore();
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
          <BoxIcon className="size-4 shrink-0 text-primary" />
          <span className="min-w-0 flex-1 truncate">
            {loading ? "Workspace" : current?.name ?? "Default"}
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
            {visibleWorkspaces.map((workspace) => (
              <DropdownMenuItem
                key={workspace.id}
                onClick={() => void handleSwitch(workspace.id)}
                className="h-9 gap-2"
              >
                <BoxIcon className="size-4 text-primary" />
                <span className="min-w-0 flex-1 truncate">
                  {workspace.name}
                </span>
                {workspace.id === current?.id && (
                  <CheckIcon className="size-4 text-primary" />
                )}
              </DropdownMenuItem>
            ))}
          </div>
          <DropdownMenuSeparator className="my-0" />
          <DropdownMenuItem
            onClick={() => setCreateOpen(true)}
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
            <DialogTitle>New Workspace</DialogTitle>
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
