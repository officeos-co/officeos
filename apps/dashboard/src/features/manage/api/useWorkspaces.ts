"use client";

import { gql, useMutation, useQuery } from "@apollo/client";
import { apolloClient } from "@/lib/graphql/client";

export type WorkspaceOwnerKind = "personal" | "organization";

export enum WorkspaceRole {
  Owner = "Owner",
  Admin = "Admin",
  Editor = "Editor",
  Viewer = "Viewer",
}

export type WorkspacePayload = {
  id: string;
  ownerKind: WorkspaceOwnerKind;
  ownerUserId: string | null;
  organizationId: string | null;
  name: string;
  isDefault: boolean;
  role: WorkspaceRole | null;
  createdAt: string;
  updatedAt: string;
};

export type WorkspaceMemberPayload = {
  id: string;
  workspaceId: string;
  userId: string;
  role: WorkspaceRole;
  createdAt: string;
};

export type WorkspaceOrganizationGrantPayload = {
  id: string;
  workspaceId: string;
  organizationId: string;
  maxRole: WorkspaceRole;
  createdAt: string;
};

export const WORKSPACES_QUERY = gql`
  query Workspaces {
    workspaces {
      id
      ownerKind
      ownerUserId
      organizationId
      name
      isDefault
      role
      createdAt
      updatedAt
    }
    currentWorkspace {
      id
      ownerKind
      ownerUserId
      organizationId
      name
      isDefault
      role
      createdAt
      updatedAt
    }
  }
`;

export const WORKSPACE_MEMBERS_QUERY = gql`
  query WorkspaceMembers($workspaceId: UUID!) {
    workspaceMembers(workspaceId: $workspaceId) {
      id
      workspaceId
      userId
      role
      createdAt
    }
  }
`;

export const ORGANIZATION_WORKSPACE_MEMBERS_QUERY = gql`
  query OrganizationWorkspaceMembers($organizationId: UUID!) {
    organizationWorkspaceMembers(organizationId: $organizationId) {
      id
      workspaceId
      userId
      role
      createdAt
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

const CREATE_ORGANIZATION_WORKSPACE = gql`
  mutation CreateOrganizationWorkspace(
    $input: CreateOrganizationWorkspaceInput!
  ) {
    createOrganizationWorkspace(input: $input) {
      id
      name
      ownerKind
      organizationId
      role
    }
  }
`;

const DELETE_WORKSPACE = gql`
  mutation DeleteWorkspace($id: UUID!) {
    deleteWorkspace(id: $id)
  }
`;

const ADD_WORKSPACE_MEMBER = gql`
  mutation AddWorkspaceMember($input: AddWorkspaceMemberInput!) {
    addWorkspaceMember(input: $input) {
      id
      workspaceId
      userId
      role
      createdAt
    }
  }
`;

const UPDATE_WORKSPACE_MEMBER_ROLE = gql`
  mutation UpdateWorkspaceMemberRole($input: UpdateWorkspaceMemberRoleInput!) {
    updateWorkspaceMemberRole(input: $input) {
      id
      workspaceId
      userId
      role
      createdAt
    }
  }
`;

const REMOVE_WORKSPACE_MEMBER = gql`
  mutation RemoveWorkspaceMember($workspaceId: UUID!, $userId: UUID!) {
    removeWorkspaceMember(workspaceId: $workspaceId, userId: $userId)
  }
`;

const GRANT_WORKSPACE_TO_ORGANIZATION = gql`
  mutation GrantWorkspaceToOrganization(
    $input: GrantWorkspaceOrganizationInput!
  ) {
    grantWorkspaceToOrganization(input: $input) {
      id
      workspaceId
      organizationId
      maxRole
      createdAt
    }
  }
`;

const REVOKE_WORKSPACE_ORGANIZATION_GRANT = gql`
  mutation RevokeWorkspaceOrganizationGrant(
    $workspaceId: UUID!
    $organizationId: UUID!
  ) {
    revokeWorkspaceOrganizationGrant(
      workspaceId: $workspaceId
      organizationId: $organizationId
    )
  }
`;

type WorkspacesRaw = {
  workspaces: WorkspacePayload[];
  currentWorkspace: WorkspacePayload;
};

export function useWorkspaces(): {
  workspaces: WorkspacePayload[];
  currentWorkspace: WorkspacePayload | null;
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery<WorkspacesRaw>(WORKSPACES_QUERY);
  return {
    workspaces: data?.workspaces ?? [],
    currentWorkspace: data?.currentWorkspace ?? null,
    loading,
    error: error ?? undefined,
  };
}

export function useCanManageWorkspaceFeatures(): {
  canManage: boolean;
  loading: boolean;
  currentWorkspace: WorkspacePayload | null;
} {
  const { currentWorkspace, loading } = useWorkspaces();
  return {
    canManage:
      currentWorkspace?.ownerKind === "personal" ||
      canEditWorkspace(currentWorkspace?.role),
    loading,
    currentWorkspace,
  };
}

export function canEditWorkspace(role?: WorkspaceRole | null) {
  return (
    role === WorkspaceRole.Owner ||
    role === WorkspaceRole.Admin ||
    role === WorkspaceRole.Editor
  );
}

export function canAdministerWorkspace(role?: WorkspaceRole | null) {
  return role === WorkspaceRole.Owner || role === WorkspaceRole.Admin;
}

export function useWorkspaceMembers(workspaceId?: string | null): {
  members: WorkspaceMemberPayload[];
  loading: boolean;
  error?: Error;
  refetch: () => Promise<unknown>;
} {
  const { data, loading, error, refetch } = useQuery<{
    workspaceMembers: WorkspaceMemberPayload[];
  }>(WORKSPACE_MEMBERS_QUERY, {
    variables: { workspaceId },
    skip: !workspaceId,
  });
  return {
    members: data?.workspaceMembers ?? [],
    loading,
    error: error ?? undefined,
    refetch: async () => {
      if (!workspaceId) return;
      await refetch();
    },
  };
}

export function useOrganizationWorkspaceMembers(organizationId?: string | null): {
  members: WorkspaceMemberPayload[];
  loading: boolean;
  error?: Error;
} {
  const { data, loading, error } = useQuery<{
    organizationWorkspaceMembers: WorkspaceMemberPayload[];
  }>(ORGANIZATION_WORKSPACE_MEMBERS_QUERY, {
    variables: { organizationId },
    skip: !organizationId,
  });
  return {
    members: data?.organizationWorkspaceMembers ?? [],
    loading,
    error: error ?? undefined,
  };
}

export function useSwitchWorkspace() {
  const [fn, state] = useMutation(SWITCH_WORKSPACE);
  return {
    switchWorkspace: async (id: string) => {
      await fn({ variables: { id } });
      await apolloClient.resetStore();
    },
    ...state,
  };
}

export function useCreateWorkspace() {
  const [fn, state] = useMutation(CREATE_WORKSPACE);
  return {
    createWorkspace: async (name: string) => {
      await fn({ variables: { input: { name } } });
      await apolloClient.resetStore();
    },
    ...state,
  };
}

export function useCreateOrganizationWorkspace() {
  const [fn, state] = useMutation(CREATE_ORGANIZATION_WORKSPACE);
  return {
    createOrganizationWorkspace: async (input: {
      organizationId: string;
      name: string;
    }) => {
      await fn({ variables: { input } });
      await apolloClient.resetStore();
    },
    ...state,
  };
}

export function useDeleteWorkspace() {
  const [fn, state] = useMutation(DELETE_WORKSPACE);
  return {
    deleteWorkspace: async (id: string) => {
      const { data } = await fn({ variables: { id } });
      await apolloClient.resetStore();
      return Boolean(data?.deleteWorkspace);
    },
    ...state,
  };
}

export function useAddWorkspaceMember() {
  const [fn, state] = useMutation(ADD_WORKSPACE_MEMBER);
  return {
    addWorkspaceMember: async (input: {
      workspaceId: string;
      userId: string;
      role?: WorkspaceRole;
    }) => {
      const { data } = await fn({ variables: { input } });
      await apolloClient.refetchQueries({
        include: [
          WORKSPACES_QUERY,
          "WorkspaceMembers",
          "OrganizationWorkspaceMembers",
        ],
      });
      return data?.addWorkspaceMember as WorkspaceMemberPayload | undefined;
    },
    ...state,
  };
}

export function useUpdateWorkspaceMemberRole() {
  const [fn, state] = useMutation(UPDATE_WORKSPACE_MEMBER_ROLE);
  return {
    updateWorkspaceMemberRole: async (input: {
      workspaceId: string;
      userId: string;
      role: WorkspaceRole;
    }) => {
      const { data } = await fn({ variables: { input } });
      await apolloClient.refetchQueries({
        include: [
          WORKSPACES_QUERY,
          "WorkspaceMembers",
          "OrganizationWorkspaceMembers",
        ],
      });
      return data?.updateWorkspaceMemberRole as
        | WorkspaceMemberPayload
        | undefined;
    },
    ...state,
  };
}

export function useRemoveWorkspaceMember() {
  const [fn, state] = useMutation(REMOVE_WORKSPACE_MEMBER);
  return {
    removeWorkspaceMember: async (workspaceId: string, userId: string) => {
      const { data } = await fn({ variables: { workspaceId, userId } });
      await apolloClient.refetchQueries({
        include: [
          WORKSPACES_QUERY,
          "WorkspaceMembers",
          "OrganizationWorkspaceMembers",
        ],
      });
      return Boolean(data?.removeWorkspaceMember);
    },
    ...state,
  };
}

export function useGrantWorkspaceToOrganization() {
  const [fn, state] = useMutation(GRANT_WORKSPACE_TO_ORGANIZATION);
  return {
    grantWorkspaceToOrganization: async (input: {
      workspaceId: string;
      organizationId: string;
      maxRole?: WorkspaceRole;
    }) => {
      const { data } = await fn({ variables: { input } });
      await apolloClient.refetchQueries({ include: [WORKSPACES_QUERY] });
      return data?.grantWorkspaceToOrganization as
        | WorkspaceOrganizationGrantPayload
        | undefined;
    },
    ...state,
  };
}

export function useRevokeWorkspaceOrganizationGrant() {
  const [fn, state] = useMutation(REVOKE_WORKSPACE_ORGANIZATION_GRANT);
  return {
    revokeWorkspaceOrganizationGrant: async (
      workspaceId: string,
      organizationId: string,
    ) => {
      const { data } = await fn({ variables: { workspaceId, organizationId } });
      await apolloClient.refetchQueries({ include: [WORKSPACES_QUERY] });
      return Boolean(data?.revokeWorkspaceOrganizationGrant);
    },
    ...state,
  };
}
