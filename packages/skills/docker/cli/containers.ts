import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { dFetch, dPost, dDelete, dockerUrl, hdrs, enc } from "../core/client.ts";

export const containers: Record<string, ActionDefinition> = {
  list_containers: {
    description: "List containers.",
    params: z.object({
      all: z.boolean().default(false).describe("Include stopped containers"),
      limit: z.number().default(0).describe("Max containers to return (0 = all)"),
      filter: z.string().optional().describe("Filter expression (e.g. status=running, name=myapp)"),
    }),
    returns: z.array(
      z.object({
        id: z.string().describe("Container ID"),
        name: z.string().describe("Container name"),
        image: z.string().describe("Image name"),
        status: z.string().describe("Human-readable status"),
        state: z.string().describe("Container state"),
        ports: z.any().describe("Port bindings"),
        created: z.number().describe("Unix timestamp"),
      }),
    ),
    execute: async (params, ctx) => {
      const qs = new URLSearchParams();
      qs.set("all", String(params.all));
      if (params.limit > 0) qs.set("limit", String(params.limit));
      if (params.filter) {
        const [k, v] = params.filter.split("=");
        qs.set("filters", JSON.stringify({ [k]: [v] }));
      }
      const data = await dFetch(ctx, `/containers/json?${qs}`);
      return data.map((c: any) => ({
        id: c.Id,
        name: (c.Names ?? [])[0]?.replace(/^\//, "") ?? "",
        image: c.Image,
        status: c.Status,
        state: c.State,
        ports: c.Ports,
        created: c.Created,
      }));
    },
  },

  get_container: {
    description: "Get detailed information about a container.",
    params: z.object({
      container_id: z.string().describe("Container ID or name"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      config: z.any().describe("Container config"),
      network_settings: z.any().describe("Network settings"),
      mounts: z.any().describe("Mounts"),
      state: z.any().describe("Container state"),
    }),
    execute: async (params, ctx) => {
      const c = await dFetch(ctx, `/containers/${enc(params.container_id)}/json`);
      return {
        id: c.Id,
        name: c.Name?.replace(/^\//, "") ?? "",
        config: c.Config,
        network_settings: c.NetworkSettings,
        mounts: c.Mounts,
        state: c.State,
      };
    },
  },

  run: {
    description: "Create and start a container from an image.",
    params: z.object({
      image: z.string().describe("Image to run (e.g. nginx:latest)"),
      name: z.string().optional().describe("Container name"),
      ports: z.array(z.string()).optional().describe("Port mappings (host:container)"),
      env: z.array(z.string()).optional().describe("Environment variables (KEY=VALUE)"),
      volumes: z.array(z.string()).optional().describe("Volume mounts (host:container[:ro])"),
      detach: z.boolean().default(true).describe("Run in background"),
      network: z.string().optional().describe("Network to connect to"),
      command: z.string().optional().describe("Override default command"),
    }),
    returns: z.object({
      container_id: z.string().describe("Created container ID"),
      name: z.string().describe("Container name"),
      status: z.string().describe("Container status"),
    }),
    execute: async (params, ctx) => {
      const portBindings: Record<string, Array<{ HostPort: string }>> = {};
      const exposedPorts: Record<string, object> = {};
      if (params.ports) {
        for (const p of params.ports) {
          const [hostPort, containerPort] = p.split(":");
          const key = `${containerPort}/tcp`;
          exposedPorts[key] = {};
          portBindings[key] = [{ HostPort: hostPort }];
        }
      }
      const binds = params.volumes ?? [];
      const body: any = {
        Image: params.image,
        Env: params.env,
        ExposedPorts: Object.keys(exposedPorts).length ? exposedPorts : undefined,
        HostConfig: {
          PortBindings: Object.keys(portBindings).length ? portBindings : undefined,
          Binds: binds.length ? binds : undefined,
          NetworkMode: params.network,
        },
      };
      if (params.command) body.Cmd = params.command.split(" ");
      const qs = params.name ? `?name=${enc(params.name)}` : "";
      const created = await dPost(ctx, `/containers/create${qs}`, body);
      await dPost(ctx, `/containers/${created.Id}/start`);
      return { container_id: created.Id, name: params.name ?? created.Id.slice(0, 12), status: "started" };
    },
  },

  stop: {
    description: "Stop a running container.",
    params: z.object({
      container_id: z.string().describe("Container ID or name"),
      timeout: z.number().default(10).describe("Seconds to wait before killing"),
    }),
    returns: z.object({ container_id: z.string(), status: z.string() }),
    execute: async (params, ctx) => {
      await dPost(ctx, `/containers/${enc(params.container_id)}/stop?t=${params.timeout}`);
      return { container_id: params.container_id, status: "stopped" };
    },
  },

  start: {
    description: "Start a stopped container.",
    params: z.object({
      container_id: z.string().describe("Container ID or name"),
    }),
    returns: z.object({ container_id: z.string(), status: z.string() }),
    execute: async (params, ctx) => {
      await dPost(ctx, `/containers/${enc(params.container_id)}/start`);
      return { container_id: params.container_id, status: "started" };
    },
  },

  restart: {
    description: "Restart a container.",
    params: z.object({
      container_id: z.string().describe("Container ID or name"),
      timeout: z.number().default(10).describe("Seconds to wait before killing"),
    }),
    returns: z.object({ container_id: z.string(), status: z.string() }),
    execute: async (params, ctx) => {
      await dPost(ctx, `/containers/${enc(params.container_id)}/restart?t=${params.timeout}`);
      return { container_id: params.container_id, status: "restarted" };
    },
  },

  rm: {
    description: "Remove a container.",
    params: z.object({
      container_id: z.string().describe("Container ID or name"),
      force: z.boolean().default(false).describe("Force remove running container"),
      volumes: z.boolean().default(false).describe("Remove associated anonymous volumes"),
    }),
    returns: z.object({ container_id: z.string() }),
    execute: async (params, ctx) => {
      const qs = new URLSearchParams();
      qs.set("force", String(params.force));
      qs.set("v", String(params.volumes));
      await dDelete(ctx, `/containers/${enc(params.container_id)}?${qs}`);
      return { container_id: params.container_id };
    },
  },

  logs: {
    description: "Get container logs.",
    params: z.object({
      container_id: z.string().describe("Container ID or name"),
      tail: z.number().default(200).describe("Number of lines from the end"),
      follow: z.boolean().default(false).describe("Stream logs in real time"),
      timestamps: z.boolean().default(false).describe("Show timestamps"),
      since: z.string().optional().describe("Logs since timestamp or duration (e.g. 10m, 2024-01-01T00:00:00Z)"),
    }),
    returns: z.string().describe("Log output as text"),
    execute: async (params, ctx) => {
      const qs = new URLSearchParams({
        stdout: "true",
        stderr: "true",
        tail: String(params.tail),
        timestamps: String(params.timestamps),
      });
      if (params.since) qs.set("since", params.since);
      const res = await ctx.fetch(dockerUrl(ctx.credentials.host, `/containers/${enc(params.container_id)}/logs?${qs}`));
      if (!res.ok) throw new Error(`Docker API ${res.status}: ${await res.text()}`);
      return res.text();
    },
  },

  exec: {
    description: "Execute a command inside a running container.",
    params: z.object({
      container_id: z.string().describe("Container ID or name"),
      command: z.string().describe("Command to execute"),
      workdir: z.string().optional().describe("Working directory inside container"),
    }),
    returns: z.object({
      exit_code: z.number().describe("Exit code"),
      stdout: z.string().describe("Standard output"),
      stderr: z.string().describe("Standard error"),
    }),
    execute: async (params, ctx) => {
      const execCreate = await dPost(ctx, `/containers/${enc(params.container_id)}/exec`, {
        AttachStdout: true,
        AttachStderr: true,
        Cmd: params.command.split(" "),
        WorkingDir: params.workdir,
      });
      const res = await ctx.fetch(dockerUrl(ctx.credentials.host, `/exec/${execCreate.Id}/start`), {
        method: "POST",
        headers: hdrs(),
        body: JSON.stringify({ Detach: false, Tty: false }),
      });
      const output = await res.text();
      const inspect = await dFetch(ctx, `/exec/${execCreate.Id}/json`);
      return { exit_code: inspect.ExitCode ?? 0, stdout: output, stderr: "" };
    },
  },

  inspect: {
    description: "Inspect a container (full JSON details).",
    params: z.object({
      container_id: z.string().describe("Container ID or name"),
    }),
    returns: z.object({
      config: z.any().describe("Container config"),
      network_settings: z.any().describe("Network settings"),
      mounts: z.any().describe("Mounts"),
      state: z.any().describe("Container state"),
      host_config: z.any().describe("Host config"),
    }),
    execute: async (params, ctx) => {
      const c = await dFetch(ctx, `/containers/${enc(params.container_id)}/json`);
      return { config: c.Config, network_settings: c.NetworkSettings, mounts: c.Mounts, state: c.State, host_config: c.HostConfig };
    },
  },

  stats: {
    description: "Get live resource usage statistics for a container.",
    params: z.object({
      container_id: z.string().describe("Container ID or name"),
    }),
    returns: z.object({
      cpu_percent: z.number().describe("CPU usage percentage"),
      memory_usage: z.number().describe("Memory usage in bytes"),
      memory_limit: z.number().describe("Memory limit in bytes"),
      memory_percent: z.number().describe("Memory usage percentage"),
      network_rx: z.number().describe("Network bytes received"),
      network_tx: z.number().describe("Network bytes transmitted"),
      block_read: z.number().describe("Block I/O read bytes"),
      block_write: z.number().describe("Block I/O write bytes"),
      pids: z.number().describe("Number of PIDs"),
    }),
    execute: async (params, ctx) => {
      const s = await dFetch(ctx, `/containers/${enc(params.container_id)}/stats?stream=false`);
      const cpuDelta = (s.cpu_stats?.cpu_usage?.total_usage ?? 0) - (s.precpu_stats?.cpu_usage?.total_usage ?? 0);
      const sysDelta = (s.cpu_stats?.system_cpu_usage ?? 0) - (s.precpu_stats?.system_cpu_usage ?? 0);
      const cpuCount = s.cpu_stats?.online_cpus ?? 1;
      const cpuPercent = sysDelta > 0 ? (cpuDelta / sysDelta) * cpuCount * 100 : 0;
      const memUsage = s.memory_stats?.usage ?? 0;
      const memLimit = s.memory_stats?.limit ?? 1;
      let netRx = 0, netTx = 0;
      if (s.networks) {
        for (const iface of Object.values(s.networks) as any[]) {
          netRx += iface.rx_bytes ?? 0;
          netTx += iface.tx_bytes ?? 0;
        }
      }
      let blockR = 0, blockW = 0;
      for (const entry of s.blkio_stats?.io_service_bytes_recursive ?? []) {
        if (entry.op === "read" || entry.op === "Read") blockR += entry.value;
        if (entry.op === "write" || entry.op === "Write") blockW += entry.value;
      }
      return {
        cpu_percent: Math.round(cpuPercent * 100) / 100,
        memory_usage: memUsage,
        memory_limit: memLimit,
        memory_percent: Math.round((memUsage / memLimit) * 10000) / 100,
        network_rx: netRx,
        network_tx: netTx,
        block_read: blockR,
        block_write: blockW,
        pids: s.pids_stats?.current ?? 0,
      };
    },
  },
};
