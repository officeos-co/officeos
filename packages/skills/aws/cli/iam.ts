import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { iamQuery, xmlVal, xmlBlocks } from "../core/client.ts";

export const iam: Record<string, ActionDefinition> = {
  list_users: {
    description: "List IAM users.",
    params: z.object({
      per_page: z.number().min(1).max(1000).default(100).describe("Results per page"),
    }),
    returns: z.array(z.object({
      user_name: z.string().describe("User name"),
      user_id: z.string().describe("User ID"),
      arn: z.string().describe("User ARN"),
      create_date: z.string().describe("Creation date"),
      path: z.string().describe("Path"),
      password_last_used: z.string().nullable().describe("Password last used"),
    })),
    execute: async (params, ctx) => {
      const xml = await iamQuery(ctx, { Action: "ListUsers", MaxItems: String(params.per_page) });
      return xmlBlocks(xml, "member").filter((m) => m.includes("<UserName>")).map((m) => ({
        user_name: xmlVal(m, "UserName"),
        user_id: xmlVal(m, "UserId"),
        arn: xmlVal(m, "Arn"),
        create_date: xmlVal(m, "CreateDate"),
        path: xmlVal(m, "Path"),
        password_last_used: xmlVal(m, "PasswordLastUsed") || null,
      }));
    },
  },

  list_roles: {
    description: "List IAM roles.",
    params: z.object({
      per_page: z.number().min(1).max(1000).default(100).describe("Results per page"),
    }),
    returns: z.array(z.object({
      role_name: z.string().describe("Role name"),
      role_id: z.string().describe("Role ID"),
      arn: z.string().describe("Role ARN"),
      create_date: z.string().describe("Creation date"),
      description: z.string().describe("Description"),
      max_session_duration: z.number().describe("Max session duration in seconds"),
    })),
    execute: async (params, ctx) => {
      const xml = await iamQuery(ctx, { Action: "ListRoles", MaxItems: String(params.per_page) });
      return xmlBlocks(xml, "member").filter((m) => m.includes("<RoleName>")).map((m) => ({
        role_name: xmlVal(m, "RoleName"),
        role_id: xmlVal(m, "RoleId"),
        arn: xmlVal(m, "Arn"),
        create_date: xmlVal(m, "CreateDate"),
        description: xmlVal(m, "Description") || "",
        max_session_duration: parseInt(xmlVal(m, "MaxSessionDuration") || "3600", 10),
      }));
    },
  },

  list_policies: {
    description: "List IAM policies.",
    params: z.object({
      scope: z.enum(["All", "AWS", "Local"]).default("All").describe("Policy scope filter"),
      per_page: z.number().min(1).max(1000).default(100).describe("Results per page"),
    }),
    returns: z.array(z.object({
      policy_name: z.string().describe("Policy name"),
      policy_id: z.string().describe("Policy ID"),
      arn: z.string().describe("Policy ARN"),
      attachment_count: z.number().describe("Attachment count"),
      create_date: z.string().describe("Creation date"),
      update_date: z.string().describe("Update date"),
      description: z.string().describe("Description"),
    })),
    execute: async (params, ctx) => {
      const xml = await iamQuery(ctx, { Action: "ListPolicies", Scope: params.scope, MaxItems: String(params.per_page) });
      return xmlBlocks(xml, "member").filter((m) => m.includes("<PolicyName>")).map((m) => ({
        policy_name: xmlVal(m, "PolicyName"),
        policy_id: xmlVal(m, "PolicyId"),
        arn: xmlVal(m, "Arn"),
        attachment_count: parseInt(xmlVal(m, "AttachmentCount") || "0", 10),
        create_date: xmlVal(m, "CreateDate"),
        update_date: xmlVal(m, "UpdateDate"),
        description: xmlVal(m, "Description") || "",
      }));
    },
  },

  create_user: {
    description: "Create a new IAM user.",
    params: z.object({
      user_name: z.string().describe("IAM user name"),
      path: z.string().default("/").describe("Path prefix"),
      tags: z.string().optional().describe("JSON object of key-value tags"),
    }),
    returns: z.object({
      user_name: z.string().describe("User name"),
      user_id: z.string().describe("User ID"),
      arn: z.string().describe("User ARN"),
      create_date: z.string().describe("Creation date"),
    }),
    execute: async (params, ctx) => {
      const p: Record<string, string> = { Action: "CreateUser", UserName: params.user_name, Path: params.path };
      if (params.tags) {
        const t = JSON.parse(params.tags) as Record<string, string>;
        let idx = 1;
        for (const [k, v] of Object.entries(t)) {
          p[`Tags.member.${idx}.Key`] = k;
          p[`Tags.member.${idx}.Value`] = v;
          idx++;
        }
      }
      const xml = await iamQuery(ctx, p);
      return {
        user_name: xmlVal(xml, "UserName"),
        user_id: xmlVal(xml, "UserId"),
        arn: xmlVal(xml, "Arn"),
        create_date: xmlVal(xml, "CreateDate"),
      };
    },
  },

  attach_policy: {
    description: "Attach an IAM policy to a user, role, or group.",
    params: z.object({
      user_name: z.string().optional().describe("IAM user name"),
      role_name: z.string().optional().describe("IAM role name"),
      group_name: z.string().optional().describe("IAM group name"),
      policy_arn: z.string().describe("Policy ARN to attach"),
    }),
    returns: z.object({
      attached: z.boolean().describe("Whether attachment succeeded"),
      entity: z.string().describe("Entity the policy was attached to"),
      policy_arn: z.string().describe("Policy ARN"),
    }),
    execute: async (params, ctx) => {
      let action: string;
      let entityKey: string;
      let entityValue: string;
      if (params.user_name) {
        action = "AttachUserPolicy";
        entityKey = "UserName";
        entityValue = params.user_name;
      } else if (params.role_name) {
        action = "AttachRolePolicy";
        entityKey = "RoleName";
        entityValue = params.role_name;
      } else if (params.group_name) {
        action = "AttachGroupPolicy";
        entityKey = "GroupName";
        entityValue = params.group_name;
      } else {
        throw new Error("One of user_name, role_name, or group_name is required");
      }
      await iamQuery(ctx, { Action: action, [entityKey]: entityValue, PolicyArn: params.policy_arn });
      return { attached: true, entity: entityValue, policy_arn: params.policy_arn };
    },
  },
};
