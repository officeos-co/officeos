import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { lambdaFetch, lambdaInvoke, cwlFetch, resolveRegion } from "../core/client.ts";

export const lambda: Record<string, ActionDefinition> = {
  list_functions: {
    description: "List Lambda functions.",
    params: z.object({
      region: z.string().optional().describe("AWS region"),
      per_page: z.number().min(1).max(50).default(50).describe("Results per page"),
    }),
    returns: z.array(z.object({
      function_name: z.string().describe("Function name"),
      runtime: z.string().nullable().describe("Runtime"),
      handler: z.string().nullable().describe("Handler"),
      code_size: z.number().describe("Code size in bytes"),
      last_modified: z.string().describe("Last modified"),
      memory_size: z.number().describe("Memory in MB"),
      timeout: z.number().describe("Timeout in seconds"),
      description: z.string().describe("Description"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const data = await lambdaFetch(ctx, region, "GET", `/2015-03-31/functions?MaxItems=${params.per_page}`);
      return (data.Functions ?? []).map((f: any) => ({
        function_name: f.FunctionName,
        runtime: f.Runtime ?? null,
        handler: f.Handler ?? null,
        code_size: f.CodeSize,
        last_modified: f.LastModified,
        memory_size: f.MemorySize,
        timeout: f.Timeout,
        description: f.Description ?? "",
      }));
    },
  },

  get_function: {
    description: "Get detailed info about a Lambda function.",
    params: z.object({
      function_name: z.string().describe("Function name or ARN"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      function_name: z.string().describe("Function name"),
      function_arn: z.string().describe("Function ARN"),
      runtime: z.string().nullable().describe("Runtime"),
      handler: z.string().nullable().describe("Handler"),
      code_size: z.number().describe("Code size in bytes"),
      description: z.string().describe("Description"),
      timeout: z.number().describe("Timeout in seconds"),
      memory_size: z.number().describe("Memory in MB"),
      last_modified: z.string().describe("Last modified"),
      environment: z.record(z.string()).nullable().describe("Environment variables"),
      layers: z.array(z.string()).describe("Layer ARNs"),
      vpc_config: z.any().nullable().describe("VPC configuration"),
      role: z.string().describe("Execution role ARN"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const data = await lambdaFetch(ctx, region, "GET", `/2015-03-31/functions/${encodeURIComponent(params.function_name)}`);
      const c = data.Configuration ?? data;
      return {
        function_name: c.FunctionName,
        function_arn: c.FunctionArn,
        runtime: c.Runtime ?? null,
        handler: c.Handler ?? null,
        code_size: c.CodeSize,
        description: c.Description ?? "",
        timeout: c.Timeout,
        memory_size: c.MemorySize,
        last_modified: c.LastModified,
        environment: c.Environment?.Variables ?? null,
        layers: (c.Layers ?? []).map((l: any) => l.Arn),
        vpc_config: c.VpcConfig ?? null,
        role: c.Role,
      };
    },
  },

  invoke_function: {
    description: "Invoke a Lambda function.",
    params: z.object({
      function_name: z.string().describe("Function name or ARN"),
      payload: z.string().optional().describe("JSON input payload"),
      invocation_type: z.enum(["RequestResponse", "Event", "DryRun"]).default("RequestResponse").describe("Invocation type"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      status_code: z.number().describe("HTTP status code"),
      payload: z.string().describe("Response payload"),
      function_error: z.string().nullable().describe("Function error"),
      executed_version: z.string().nullable().describe("Executed version"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      return lambdaInvoke(ctx, region, params.function_name, params.payload ?? "{}", params.invocation_type);
    },
  },

  create_function: {
    description: "Create a new Lambda function.",
    params: z.object({
      function_name: z.string().describe("Function name"),
      runtime: z.string().describe("Runtime (e.g. nodejs20.x, python3.12)"),
      handler: z.string().describe("Handler function (e.g. index.handler)"),
      role: z.string().describe("IAM role ARN for execution"),
      zip_file: z.string().optional().describe("Base64-encoded deployment package"),
      s3_bucket: z.string().optional().describe("S3 bucket containing code package"),
      s3_key: z.string().optional().describe("S3 key of code package"),
      memory_size: z.number().min(128).max(10240).default(128).describe("Memory in MB"),
      timeout: z.number().min(1).max(900).default(3).describe("Timeout in seconds"),
      environment: z.string().optional().describe("JSON object of environment variables"),
      description: z.string().optional().describe("Function description"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      function_name: z.string().describe("Function name"),
      function_arn: z.string().describe("Function ARN"),
      runtime: z.string().describe("Runtime"),
      state: z.string().describe("Function state"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const body: any = {
        FunctionName: params.function_name,
        Runtime: params.runtime,
        Handler: params.handler,
        Role: params.role,
        MemorySize: params.memory_size,
        Timeout: params.timeout,
        Code: {},
      };
      if (params.zip_file) body.Code.ZipFile = params.zip_file;
      if (params.s3_bucket) body.Code.S3Bucket = params.s3_bucket;
      if (params.s3_key) body.Code.S3Key = params.s3_key;
      if (params.description) body.Description = params.description;
      if (params.environment) body.Environment = { Variables: JSON.parse(params.environment) };
      const data = await lambdaFetch(ctx, region, "POST", "/2015-03-31/functions", body);
      return {
        function_name: data.FunctionName,
        function_arn: data.FunctionArn,
        runtime: data.Runtime,
        state: data.State ?? "Active",
      };
    },
  },

  update_function_code: {
    description: "Update a Lambda function's code.",
    params: z.object({
      function_name: z.string().describe("Function name or ARN"),
      zip_file: z.string().optional().describe("Base64-encoded deployment package"),
      s3_bucket: z.string().optional().describe("S3 bucket containing code package"),
      s3_key: z.string().optional().describe("S3 key of code package"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      function_name: z.string().describe("Function name"),
      function_arn: z.string().describe("Function ARN"),
      code_sha256: z.string().describe("Code SHA256 hash"),
      last_modified: z.string().describe("Last modified timestamp"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const body: any = {};
      if (params.zip_file) body.ZipFile = params.zip_file;
      if (params.s3_bucket) body.S3Bucket = params.s3_bucket;
      if (params.s3_key) body.S3Key = params.s3_key;
      const data = await lambdaFetch(ctx, region, "PUT", `/2015-03-31/functions/${encodeURIComponent(params.function_name)}/code`, body);
      return {
        function_name: data.FunctionName,
        function_arn: data.FunctionArn,
        code_sha256: data.CodeSha256,
        last_modified: data.LastModified,
      };
    },
  },

  delete_function: {
    description: "Delete a Lambda function.",
    params: z.object({
      function_name: z.string().describe("Function name or ARN"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      deleted: z.boolean().describe("Whether deletion succeeded"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      await lambdaFetch(ctx, region, "DELETE", `/2015-03-31/functions/${encodeURIComponent(params.function_name)}`);
      return { deleted: true };
    },
  },

  list_function_logs: {
    description: "List recent log events for a Lambda function.",
    params: z.object({
      function_name: z.string().describe("Function name"),
      start_time: z.number().optional().describe("Start time (epoch ms)"),
      end_time: z.number().optional().describe("End time (epoch ms)"),
      limit: z.number().min(1).max(10000).default(50).describe("Maximum log events to return"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.array(z.object({
      timestamp: z.number().describe("Event timestamp (epoch ms)"),
      message: z.string().describe("Log message"),
      ingestion_time: z.number().describe("Ingestion time (epoch ms)"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const logGroup = `/aws/lambda/${params.function_name}`;
      const payload: Record<string, any> = {
        logGroupName: logGroup,
        limit: params.limit,
        interleaved: true,
      };
      if (params.start_time) payload.startTime = params.start_time;
      if (params.end_time) payload.endTime = params.end_time;
      const data = await cwlFetch(ctx, region, "FilterLogEvents", payload);
      return (data.events ?? []).map((e: any) => ({
        timestamp: e.timestamp,
        message: e.message,
        ingestion_time: e.ingestionTime,
      }));
    },
  },
};
