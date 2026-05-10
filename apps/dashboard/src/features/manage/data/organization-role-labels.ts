import type { OrgMember } from "../api/useOrganization";

export const ORGANIZATION_ROLE_TOOLTIPS: Record<OrgMember["role"], string> = {
  Owner: "Full organization control, including billing, members, providers, and workspaces.",
  Admin: "Can manage organization members, providers, workspaces, workspace access, and all workspace features.",
  Editor: "Gets editor access by default in organization workspaces.",
  Viewer: "Gets viewer access by default in organization workspaces.",
};

export const ORGANIZATION_ROLE_LABELS: Record<OrgMember["role"], string> = {
  Owner: "Owner",
  Admin: "Admin",
  Editor: "Editor",
  Viewer: "Viewer",
};

export function organizationRoleTooltip(role: OrgMember["role"]) {
  return ORGANIZATION_ROLE_TOOLTIPS[role];
}

export function organizationRoleLabel(role: OrgMember["role"]) {
  return ORGANIZATION_ROLE_LABELS[role];
}
