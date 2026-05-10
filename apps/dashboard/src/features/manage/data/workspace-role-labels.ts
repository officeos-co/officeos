import { WorkspaceRole } from "../api/useWorkspaces";

export const NO_WORKSPACE_ACCESS = "none";

export const WORKSPACE_ROLE_TOOLTIPS: Record<WorkspaceRole, string> = {
  [WorkspaceRole.Viewer]: "Can view workspace resources.",
  [WorkspaceRole.Editor]:
    "Can create agents, attach resources, and manage integrations and credentials.",
  [WorkspaceRole.Admin]:
    "Can do everything editors can, plus assign workspace roles.",
};

export function workspaceRoleTooltip(role?: WorkspaceRole | null) {
  return role ? WORKSPACE_ROLE_TOOLTIPS[role] : "No workspace access.";
}
