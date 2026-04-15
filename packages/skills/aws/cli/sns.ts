import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { snsQuery, xmlVal, xmlBlocks, resolveRegion } from "../core/client.ts";

export const sns: Record<string, ActionDefinition> = {
  list_topics: {
    description: "List SNS topics.",
    params: z.object({
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.array(z.object({
      topic_arn: z.string().describe("Topic ARN"),
      topic_name: z.string().describe("Topic name"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const xml = await snsQuery(ctx, region, { Action: "ListTopics" });
      return xmlBlocks(xml, "member").filter((m) => m.includes("<TopicArn>")).map((m) => {
        const arn = xmlVal(m, "TopicArn");
        return { topic_arn: arn, topic_name: arn.split(":").pop() ?? arn };
      });
    },
  },

  publish: {
    description: "Publish a message to an SNS topic.",
    params: z.object({
      topic_arn: z.string().describe("SNS topic ARN"),
      message: z.string().describe("Message body"),
      subject: z.string().optional().describe("Message subject (email endpoints)"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      message_id: z.string().describe("Published message ID"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const p: Record<string, string> = { Action: "Publish", TopicArn: params.topic_arn, Message: params.message };
      if (params.subject) p["Subject"] = params.subject;
      const xml = await snsQuery(ctx, region, p);
      return { message_id: xmlVal(xml, "MessageId") };
    },
  },

  subscribe: {
    description: "Subscribe an endpoint to an SNS topic.",
    params: z.object({
      topic_arn: z.string().describe("SNS topic ARN"),
      protocol: z.enum(["email", "sms", "sqs", "lambda", "https"]).describe("Subscription protocol"),
      endpoint: z.string().describe("Endpoint (email, phone, ARN, or URL)"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      subscription_arn: z.string().describe("Subscription ARN (pending confirmation for email/sms)"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const xml = await snsQuery(ctx, region, { Action: "Subscribe", TopicArn: params.topic_arn, Protocol: params.protocol, Endpoint: params.endpoint });
      return { subscription_arn: xmlVal(xml, "SubscriptionArn") };
    },
  },

  create_topic: {
    description: "Create a new SNS topic.",
    params: z.object({
      name: z.string().describe("Topic name"),
      fifo: z.boolean().default(false).describe("Create as FIFO topic"),
      tags: z.string().optional().describe("JSON object of tags"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      topic_arn: z.string().describe("Topic ARN"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const topicName = params.fifo && !params.name.endsWith(".fifo") ? params.name + ".fifo" : params.name;
      const p: Record<string, string> = { Action: "CreateTopic", Name: topicName };
      if (params.fifo) {
        p["Attributes.entry.1.key"] = "FifoTopic";
        p["Attributes.entry.1.value"] = "true";
      }
      if (params.tags) {
        const t = JSON.parse(params.tags) as Record<string, string>;
        let idx = 1;
        for (const [k, v] of Object.entries(t)) {
          p[`Tags.member.${idx}.Key`] = k;
          p[`Tags.member.${idx}.Value`] = v;
          idx++;
        }
      }
      const xml = await snsQuery(ctx, region, p);
      return { topic_arn: xmlVal(xml, "TopicArn") };
    },
  },
};
