import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ec2Query, xmlVal, xmlAll, xmlBlocks, resolveRegion } from "../core/client.ts";

export const ec2: Record<string, ActionDefinition> = {
  list_instances: {
    description: "List EC2 instances, optionally filtered by state.",
    params: z.object({
      region: z.string().optional().describe("AWS region"),
      state: z.enum(["running", "stopped", "pending", "terminated", "all"]).default("all").describe("Instance state filter"),
      per_page: z.number().min(1).max(1000).default(50).describe("Results per page"),
      filters: z.string().optional().describe("JSON array of Name/Values filters"),
    }),
    returns: z.array(z.object({
      instance_id: z.string().describe("Instance ID"),
      instance_type: z.string().describe("Instance type"),
      state: z.string().describe("Current state"),
      public_ip: z.string().nullable().describe("Public IP address"),
      private_ip: z.string().nullable().describe("Private IP address"),
      launch_time: z.string().describe("Launch time"),
      tags: z.record(z.string()).describe("Instance tags"),
      vpc_id: z.string().nullable().describe("VPC ID"),
      subnet_id: z.string().nullable().describe("Subnet ID"),
      security_groups: z.array(z.string()).describe("Security group IDs"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const p: Record<string, string> = { Action: "DescribeInstances", "MaxResults": String(params.per_page) };
      let filterIdx = 1;
      if (params.state !== "all") {
        p[`Filter.${filterIdx}.Name`] = "instance-state-name";
        p[`Filter.${filterIdx}.Value.1`] = params.state;
        filterIdx++;
      }
      if (params.filters) {
        const filters = JSON.parse(params.filters) as { Name: string; Values: string[] }[];
        for (const f of filters) {
          p[`Filter.${filterIdx}.Name`] = f.Name;
          f.Values.forEach((v, i) => { p[`Filter.${filterIdx}.Value.${i + 1}`] = v; });
          filterIdx++;
        }
      }
      const xml = await ec2Query(ctx, region, p);
      const items = xmlBlocks(xml, "item").filter((b) => b.includes("<instanceId>"));
      return items.map((b) => {
        const tags: Record<string, string> = {};
        xmlBlocks(b, "item").filter((t) => t.includes("<key>")).forEach((t) => { tags[xmlVal(t, "key")] = xmlVal(t, "value"); });
        return {
          instance_id: xmlVal(b, "instanceId"),
          instance_type: xmlVal(b, "instanceType"),
          state: xmlVal(xmlVal(b, "instanceState") || b, "name"),
          public_ip: xmlVal(b, "ipAddress") || null,
          private_ip: xmlVal(b, "privateIpAddress") || null,
          launch_time: xmlVal(b, "launchTime"),
          tags,
          vpc_id: xmlVal(b, "vpcId") || null,
          subnet_id: xmlVal(b, "subnetId") || null,
          security_groups: xmlBlocks(b, "groupId").map((g) => xmlVal(g, "groupId") || g.replace(/<\/?groupId>/g, "")),
        };
      });
    },
  },

  get_instance: {
    description: "Get detailed information about a single EC2 instance.",
    params: z.object({
      instance_id: z.string().describe("EC2 instance ID"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      instance_id: z.string().describe("Instance ID"),
      instance_type: z.string().describe("Instance type"),
      state: z.string().describe("Current state"),
      public_ip: z.string().nullable().describe("Public IP"),
      private_ip: z.string().nullable().describe("Private IP"),
      launch_time: z.string().describe("Launch time"),
      tags: z.record(z.string()).describe("Instance tags"),
      vpc_id: z.string().nullable().describe("VPC ID"),
      subnet_id: z.string().nullable().describe("Subnet ID"),
      security_groups: z.array(z.string()).describe("Security group IDs"),
      iam_instance_profile: z.string().nullable().describe("IAM instance profile ARN"),
      block_device_mappings: z.array(z.string()).describe("Attached volume IDs"),
      network_interfaces: z.array(z.string()).describe("Network interface IDs"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const xml = await ec2Query(ctx, region, { Action: "DescribeInstances", "InstanceId.1": params.instance_id });
      const b = xml;
      const tags: Record<string, string> = {};
      const tagBlocks = xmlBlocks(b, "item").filter((t) => t.includes("<key>") && t.includes("<value>"));
      tagBlocks.forEach((t) => { tags[xmlVal(t, "key")] = xmlVal(t, "value"); });
      return {
        instance_id: xmlVal(b, "instanceId"),
        instance_type: xmlVal(b, "instanceType"),
        state: xmlVal(b, "name"),
        public_ip: xmlVal(b, "ipAddress") || null,
        private_ip: xmlVal(b, "privateIpAddress") || null,
        launch_time: xmlVal(b, "launchTime"),
        tags,
        vpc_id: xmlVal(b, "vpcId") || null,
        subnet_id: xmlVal(b, "subnetId") || null,
        security_groups: xmlAll(b, "groupId"),
        iam_instance_profile: xmlVal(b, "arn") || null,
        block_device_mappings: xmlAll(b, "volumeId"),
        network_interfaces: xmlAll(b, "networkInterfaceId"),
      };
    },
  },

  start_instance: {
    description: "Start a stopped EC2 instance.",
    params: z.object({
      instance_id: z.string().describe("EC2 instance ID"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      instance_id: z.string().describe("Instance ID"),
      previous_state: z.string().describe("Previous state"),
      current_state: z.string().describe("Current state"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const xml = await ec2Query(ctx, region, { Action: "StartInstances", "InstanceId.1": params.instance_id });
      return {
        instance_id: params.instance_id,
        previous_state: xmlVal(xmlVal(xml, "previousState") || xml, "name"),
        current_state: xmlVal(xmlVal(xml, "currentState") || xml, "name"),
      };
    },
  },

  stop_instance: {
    description: "Stop a running EC2 instance.",
    params: z.object({
      instance_id: z.string().describe("EC2 instance ID"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      instance_id: z.string().describe("Instance ID"),
      previous_state: z.string().describe("Previous state"),
      current_state: z.string().describe("Current state"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const xml = await ec2Query(ctx, region, { Action: "StopInstances", "InstanceId.1": params.instance_id });
      return {
        instance_id: params.instance_id,
        previous_state: xmlVal(xmlVal(xml, "previousState") || xml, "name"),
        current_state: xmlVal(xmlVal(xml, "currentState") || xml, "name"),
      };
    },
  },

  terminate_instance: {
    description: "Terminate an EC2 instance permanently.",
    params: z.object({
      instance_id: z.string().describe("EC2 instance ID"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      instance_id: z.string().describe("Instance ID"),
      previous_state: z.string().describe("Previous state"),
      current_state: z.string().describe("Current state"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const xml = await ec2Query(ctx, region, { Action: "TerminateInstances", "InstanceId.1": params.instance_id });
      return {
        instance_id: params.instance_id,
        previous_state: xmlVal(xmlVal(xml, "previousState") || xml, "name"),
        current_state: xmlVal(xmlVal(xml, "currentState") || xml, "name"),
      };
    },
  },

  create_instance: {
    description: "Launch a new EC2 instance.",
    params: z.object({
      ami: z.string().describe("AMI ID"),
      instance_type: z.string().describe("Instance type (e.g. t3.micro)"),
      key_name: z.string().optional().describe("SSH key pair name"),
      security_groups: z.array(z.string()).optional().describe("Security group IDs"),
      subnet: z.string().optional().describe("Subnet ID"),
      tags: z.string().optional().describe("JSON object of key-value tags"),
      user_data: z.string().optional().describe("Base64-encoded user data script"),
      count: z.number().min(1).default(1).describe("Number of instances to launch"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      instance_id: z.string().describe("Instance ID"),
      instance_type: z.string().describe("Instance type"),
      state: z.string().describe("Current state"),
      public_ip: z.string().nullable().describe("Public IP"),
      private_ip: z.string().nullable().describe("Private IP"),
      launch_time: z.string().describe("Launch time"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const p: Record<string, string> = {
        Action: "RunInstances",
        ImageId: params.ami,
        InstanceType: params.instance_type,
        MinCount: String(params.count),
        MaxCount: String(params.count),
      };
      if (params.key_name) p["KeyName"] = params.key_name;
      if (params.subnet) p["SubnetId"] = params.subnet;
      if (params.user_data) p["UserData"] = params.user_data;
      if (params.security_groups) {
        params.security_groups.forEach((sg, i) => { p[`SecurityGroupId.${i + 1}`] = sg; });
      }
      if (params.tags) {
        const t = JSON.parse(params.tags) as Record<string, string>;
        let idx = 1;
        for (const [k, v] of Object.entries(t)) {
          p[`TagSpecification.1.ResourceType`] = "instance";
          p[`TagSpecification.1.Tag.${idx}.Key`] = k;
          p[`TagSpecification.1.Tag.${idx}.Value`] = v;
          idx++;
        }
      }
      const xml = await ec2Query(ctx, region, p);
      return {
        instance_id: xmlVal(xml, "instanceId"),
        instance_type: xmlVal(xml, "instanceType"),
        state: xmlVal(xml, "name"),
        public_ip: xmlVal(xml, "ipAddress") || null,
        private_ip: xmlVal(xml, "privateIpAddress") || null,
        launch_time: xmlVal(xml, "launchTime"),
      };
    },
  },
};
