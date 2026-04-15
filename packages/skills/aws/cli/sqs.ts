import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { sqsQuery, xmlVal, xmlAll, xmlBlocks, resolveRegion } from "../core/client.ts";

export const sqs: Record<string, ActionDefinition> = {
  list_queues: {
    description: "List SQS queues.",
    params: z.object({
      prefix: z.string().optional().describe("Queue name prefix"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.array(z.object({
      queue_url: z.string().describe("Queue URL"),
      queue_name: z.string().describe("Queue name"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const p: Record<string, string> = { Action: "ListQueues" };
      if (params.prefix) p["QueueNamePrefix"] = params.prefix;
      const xml = await sqsQuery(ctx, region, p);
      return xmlAll(xml, "QueueUrl").map((url) => ({
        queue_url: url,
        queue_name: url.split("/").pop() ?? url,
      }));
    },
  },

  send_message: {
    description: "Send a message to an SQS queue.",
    params: z.object({
      queue_url: z.string().describe("SQS queue URL"),
      body: z.string().describe("Message body"),
      delay_seconds: z.number().min(0).max(900).default(0).describe("Delay before visible"),
      attributes: z.string().optional().describe("JSON object of message attributes"),
      group_id: z.string().optional().describe("Message group ID (FIFO queues)"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      message_id: z.string().describe("Message ID"),
      md5_of_body: z.string().describe("MD5 of message body"),
      sequence_number: z.string().nullable().describe("Sequence number (FIFO queues)"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const p: Record<string, string> = {
        Action: "SendMessage",
        QueueUrl: params.queue_url,
        MessageBody: params.body,
        DelaySeconds: String(params.delay_seconds),
      };
      if (params.group_id) p["MessageGroupId"] = params.group_id;
      const xml = await sqsQuery(ctx, region, p);
      return {
        message_id: xmlVal(xml, "MessageId"),
        md5_of_body: xmlVal(xml, "MD5OfMessageBody"),
        sequence_number: xmlVal(xml, "SequenceNumber") || null,
      };
    },
  },

  receive_messages: {
    description: "Receive messages from an SQS queue.",
    params: z.object({
      queue_url: z.string().describe("SQS queue URL"),
      max_messages: z.number().min(1).max(10).default(1).describe("Max messages to receive"),
      wait_time: z.number().min(0).max(20).default(0).describe("Long poll wait seconds"),
      visibility_timeout: z.number().optional().describe("Override visibility timeout"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.array(z.object({
      message_id: z.string().describe("Message ID"),
      receipt_handle: z.string().describe("Receipt handle"),
      body: z.string().describe("Message body"),
      md5_of_body: z.string().describe("MD5 of body"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const p: Record<string, string> = {
        Action: "ReceiveMessage",
        QueueUrl: params.queue_url,
        MaxNumberOfMessages: String(params.max_messages),
        WaitTimeSeconds: String(params.wait_time),
      };
      if (params.visibility_timeout !== undefined) p["VisibilityTimeout"] = String(params.visibility_timeout);
      const xml = await sqsQuery(ctx, region, p);
      return xmlBlocks(xml, "Message").map((m) => ({
        message_id: xmlVal(m, "MessageId"),
        receipt_handle: xmlVal(m, "ReceiptHandle"),
        body: xmlVal(m, "Body"),
        md5_of_body: xmlVal(m, "MD5OfBody"),
      }));
    },
  },

  delete_message: {
    description: "Delete a message from an SQS queue.",
    params: z.object({
      queue_url: z.string().describe("SQS queue URL"),
      receipt_handle: z.string().describe("Receipt handle from receive"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      deleted: z.boolean().describe("Whether deletion succeeded"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      await sqsQuery(ctx, region, { Action: "DeleteMessage", QueueUrl: params.queue_url, ReceiptHandle: params.receipt_handle });
      return { deleted: true };
    },
  },

  create_queue: {
    description: "Create a new SQS queue.",
    params: z.object({
      queue_name: z.string().describe("Queue name"),
      fifo: z.boolean().default(false).describe("Create as FIFO queue"),
      visibility_timeout: z.number().default(30).describe("Default visibility timeout (s)"),
      message_retention: z.number().default(345600).describe("Message retention period (s)"),
      delay_seconds: z.number().default(0).describe("Default delivery delay (s)"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      queue_url: z.string().describe("Queue URL"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const name = params.fifo && !params.queue_name.endsWith(".fifo") ? params.queue_name + ".fifo" : params.queue_name;
      const p: Record<string, string> = {
        Action: "CreateQueue",
        QueueName: name,
        "Attribute.1.Name": "VisibilityTimeout",
        "Attribute.1.Value": String(params.visibility_timeout),
        "Attribute.2.Name": "MessageRetentionPeriod",
        "Attribute.2.Value": String(params.message_retention),
        "Attribute.3.Name": "DelaySeconds",
        "Attribute.3.Value": String(params.delay_seconds),
      };
      if (params.fifo) {
        p["Attribute.4.Name"] = "FifoQueue";
        p["Attribute.4.Value"] = "true";
      }
      const xml = await sqsQuery(ctx, region, p);
      return { queue_url: xmlVal(xml, "QueueUrl") };
    },
  },
};
