import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { cwQuery, cwlFetch, xmlVal, xmlBlocks, resolveRegion } from "../core/client.ts";

export const cloudwatch: Record<string, ActionDefinition> = {
  get_metrics: {
    description: "Get CloudWatch metric data points.",
    params: z.object({
      namespace: z.string().describe("CloudWatch namespace (e.g. AWS/EC2)"),
      metric_name: z.string().describe("Metric name (e.g. CPUUtilization)"),
      dimensions: z.string().optional().describe("JSON object of dimension name-value pairs"),
      period: z.number().min(60).default(300).describe("Period in seconds"),
      start_time: z.string().describe("ISO 8601 start time"),
      end_time: z.string().describe("ISO 8601 end time"),
      statistics: z.enum(["Average", "Sum", "Minimum", "Maximum", "SampleCount"]).default("Average").describe("Statistic type"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.array(z.object({
      timestamp: z.string().describe("Data point timestamp"),
      value: z.number().describe("Metric value"),
      unit: z.string().describe("Unit"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const p: Record<string, string> = {
        Action: "GetMetricStatistics",
        Namespace: params.namespace,
        MetricName: params.metric_name,
        Period: String(params.period),
        StartTime: params.start_time,
        EndTime: params.end_time,
        "Statistics.member.1": params.statistics,
      };
      if (params.dimensions) {
        const dims = JSON.parse(params.dimensions) as Record<string, string>;
        let idx = 1;
        for (const [k, v] of Object.entries(dims)) {
          p[`Dimensions.member.${idx}.Name`] = k;
          p[`Dimensions.member.${idx}.Value`] = v;
          idx++;
        }
      }
      const xml = await cwQuery(ctx, region, p);
      return xmlBlocks(xml, "member").filter((m) => m.includes("<Timestamp>")).map((m) => ({
        timestamp: xmlVal(m, "Timestamp"),
        value: parseFloat(xmlVal(m, params.statistics) || "0"),
        unit: xmlVal(m, "Unit") || "None",
      }));
    },
  },

  put_metric_alarm: {
    description: "Create or update a CloudWatch metric alarm.",
    params: z.object({
      alarm_name: z.string().describe("Alarm name"),
      namespace: z.string().describe("CloudWatch namespace"),
      metric_name: z.string().describe("Metric name"),
      threshold: z.number().describe("Threshold value"),
      comparison: z.string().describe("Comparison operator"),
      evaluation_periods: z.number().describe("Number of periods to evaluate"),
      period: z.number().default(300).describe("Period in seconds"),
      statistic: z.enum(["Average", "Sum", "Minimum", "Maximum", "SampleCount"]).default("Average").describe("Statistic"),
      actions: z.array(z.string()).optional().describe("SNS topic ARNs to notify"),
      dimensions: z.string().optional().describe("JSON object of dimension name-value pairs"),
      description: z.string().optional().describe("Alarm description"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      alarm_name: z.string().describe("Alarm name"),
      alarm_arn: z.string().describe("Alarm ARN"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const p: Record<string, string> = {
        Action: "PutMetricAlarm",
        AlarmName: params.alarm_name,
        Namespace: params.namespace,
        MetricName: params.metric_name,
        Threshold: String(params.threshold),
        ComparisonOperator: params.comparison,
        EvaluationPeriods: String(params.evaluation_periods),
        Period: String(params.period),
        Statistic: params.statistic,
      };
      if (params.description) p["AlarmDescription"] = params.description;
      if (params.actions) {
        params.actions.forEach((a, i) => { p[`AlarmActions.member.${i + 1}`] = a; });
      }
      if (params.dimensions) {
        const dims = JSON.parse(params.dimensions) as Record<string, string>;
        let idx = 1;
        for (const [k, v] of Object.entries(dims)) {
          p[`Dimensions.member.${idx}.Name`] = k;
          p[`Dimensions.member.${idx}.Value`] = v;
          idx++;
        }
      }
      await cwQuery(ctx, region, p);
      return { alarm_name: params.alarm_name, alarm_arn: `arn:aws:cloudwatch:${region}:alarm:${params.alarm_name}` };
    },
  },

  list_alarms: {
    description: "List CloudWatch metric alarms.",
    params: z.object({
      state: z.enum(["OK", "ALARM", "INSUFFICIENT_DATA", "all"]).default("all").describe("Alarm state filter"),
      prefix: z.string().optional().describe("Alarm name prefix filter"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.array(z.object({
      alarm_name: z.string().describe("Alarm name"),
      alarm_arn: z.string().describe("Alarm ARN"),
      state: z.string().describe("Current state"),
      metric_name: z.string().describe("Metric name"),
      namespace: z.string().describe("Namespace"),
      threshold: z.number().describe("Threshold"),
      comparison: z.string().describe("Comparison operator"),
      evaluation_periods: z.number().describe("Evaluation periods"),
      updated_at: z.string().describe("Last updated"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const p: Record<string, string> = { Action: "DescribeAlarms" };
      if (params.state !== "all") p["StateValue"] = params.state;
      if (params.prefix) p["AlarmNamePrefix"] = params.prefix;
      const xml = await cwQuery(ctx, region, p);
      return xmlBlocks(xml, "member").filter((m) => m.includes("<AlarmName>")).map((m) => ({
        alarm_name: xmlVal(m, "AlarmName"),
        alarm_arn: xmlVal(m, "AlarmArn"),
        state: xmlVal(m, "StateValue"),
        metric_name: xmlVal(m, "MetricName"),
        namespace: xmlVal(m, "Namespace"),
        threshold: parseFloat(xmlVal(m, "Threshold") || "0"),
        comparison: xmlVal(m, "ComparisonOperator"),
        evaluation_periods: parseInt(xmlVal(m, "EvaluationPeriods") || "0", 10),
        updated_at: xmlVal(m, "StateUpdatedTimestamp") || xmlVal(m, "AlarmConfigurationUpdatedTimestamp"),
      }));
    },
  },

  get_log_events: {
    description: "Get CloudWatch log events from a log group.",
    params: z.object({
      log_group: z.string().describe("CloudWatch log group name"),
      log_stream: z.string().optional().describe("Specific log stream name"),
      start_time: z.number().optional().describe("Start time (epoch ms)"),
      end_time: z.number().optional().describe("End time (epoch ms)"),
      limit: z.number().min(1).max(10000).default(100).describe("Maximum events to return"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.array(z.object({
      timestamp: z.number().describe("Event timestamp (epoch ms)"),
      message: z.string().describe("Log message"),
      ingestion_time: z.number().describe("Ingestion time (epoch ms)"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      if (params.log_stream) {
        const payload: Record<string, any> = {
          logGroupName: params.log_group,
          logStreamName: params.log_stream,
          limit: params.limit,
        };
        if (params.start_time) payload.startTime = params.start_time;
        if (params.end_time) payload.endTime = params.end_time;
        const data = await cwlFetch(ctx, region, "GetLogEvents", payload);
        return (data.events ?? []).map((e: any) => ({
          timestamp: e.timestamp,
          message: e.message,
          ingestion_time: e.ingestionTime,
        }));
      } else {
        const payload: Record<string, any> = {
          logGroupName: params.log_group,
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
      }
    },
  },
};
