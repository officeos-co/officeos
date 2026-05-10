import type { OrgMember } from "../api/useOrganization";

export const ORGANIZATION_ROLE_TOOLTIPS: Record<OrgMember["role"], string> = {
  Owner: "Full organization control, including billing, members, providers, and workspaces.",
  Admin: "Can manage organization members, providers, workspaces, and workspace access.",
  Member: "Can use organization workspaces according to assigned workspace roles.",
};

export function organizationRoleTooltip(role: OrgMember["role"]) {
  return ORGANIZATION_ROLE_TOOLTIPS[role];
}
