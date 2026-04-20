"use client";

import { useState } from "react";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import {
  ToolPermissionCard,
  ChannelPermissionCard,
  type ToolPermission,
} from "@/components/permission-cards";
import { builtInTools } from "@/features/agents/data/integrations";
import { type ChannelPermissions } from "@/features/agents/data/channels";
import { useAgent } from "@/features/agents/api/useAgents";
import { useIntegrations } from "@/features/agents/api/useIntegrations";
import { useChannels } from "@/features/agents/api/useChannels";
import { CheckIcon, TerminalIcon } from "lucide-react";

export function AgentIntegrationsTab({ agentId }: { agentId: string }) {
  const { agent } = useAgent(agentId);
  const { integrations } = useIntegrations();
  const { channels } = useChannels();
  const current = agent ?? {
    id: agentId,
    name: "",
    model: "",
    status: "stopped",
    prompt: "",
    integrations: [] as string[],
    channels: [] as string[],
    createdAt: Date.now(),
  };
  const [selectedIntegrations, setSelectedIntegrations] = useState<Set<string>>(
    new Set(current.integrations),
  );
  const [selectedChannels, setSelectedChannels] = useState<Set<string>>(
    new Set(current.channels),
  );
  const [toolPermissions, setToolPermissions] = useState<
    Record<string, ToolPermission>
  >({});
  const [groupPermissions, setGroupPermissions] = useState<
    Record<string, ToolPermission>
  >({});
  const [channelPerms, setChannelPerms] = useState<
    Record<string, ChannelPermissions>
  >(() => {
    const cp: Record<string, ChannelPermissions> = {};
    for (const slug of current.channels) {
      const ch = channels.find((c) => c.slug === slug);
      if (ch) cp[slug] = { ...ch.defaultPermissions };
    }
    return cp;
  });

  function toggleIntegration(slug: string) {
    setSelectedIntegrations((prev) => {
      const next = new Set(prev);
      if (next.has(slug)) next.delete(slug);
      else next.add(slug);
      return next;
    });
  }

  function toggleChannel(slug: string) {
    setSelectedChannels((prev) => {
      const next = new Set(prev);
      if (next.has(slug)) {
        next.delete(slug);
        setChannelPerms((cp) => {
          const n = { ...cp };
          delete n[slug];
          return n;
        });
      } else {
        next.add(slug);
        const ch = channels.find((c) => c.slug === slug);
        if (ch)
          setChannelPerms((cp) => ({
            ...cp,
            [slug]: { ...ch.defaultPermissions },
          }));
      }
      return next;
    });
  }

  const activeIntegrations = integrations.filter((i) =>
    selectedIntegrations.has(i.slug),
  );
  const activeChannels = channels.filter((c) => selectedChannels.has(c.slug));

  return (
    <div className="pt-4 space-y-6">
      {/* Integrations */}
      <div className="space-y-3">
        <Label>Integrations</Label>
        <div className="grid grid-cols-3 gap-2">
          {integrations.map((i) => {
            const active = selectedIntegrations.has(i.slug);
            return (
              <button
                key={i.slug}
                type="button"
                onClick={() => toggleIntegration(i.slug)}
                className={`flex items-center gap-2.5 rounded-lg border px-3 py-2 text-left text-sm transition-colors ${active ? "border-primary bg-primary/5" : "border-border hover:bg-muted/50"}`}
              >
                <div
                  className="size-[18px] shrink-0 [&>svg]:size-[18px]"
                  dangerouslySetInnerHTML={{ __html: i.logo }}
                />
                <span className="flex-1 truncate">{i.name}</span>
                {active && (
                  <CheckIcon className="size-3.5 text-primary shrink-0" />
                )}
              </button>
            );
          })}
        </div>
      </div>

      {/* Channels */}
      <div className="space-y-3">
        <Label>Channels</Label>
        <div className="grid grid-cols-3 gap-2">
          {channels.map((c) => {
            const active = selectedChannels.has(c.slug);
            return (
              <button
                key={c.slug}
                type="button"
                onClick={() => toggleChannel(c.slug)}
                className={`flex items-center gap-2.5 rounded-lg border px-3 py-2 text-left text-sm transition-colors ${active ? "border-primary bg-primary/5" : "border-border hover:bg-muted/50"}`}
              >
                <div
                  className="size-[18px] shrink-0 [&>svg]:size-[18px]"
                  dangerouslySetInnerHTML={{ __html: c.logo }}
                />
                <span className="flex-1 truncate">{c.name}</span>
                {active && (
                  <CheckIcon className="size-3.5 text-primary shrink-0" />
                )}
              </button>
            );
          })}
        </div>
      </div>

      <Separator />

      {/* Tool permissions */}
      <div className="space-y-3">
        <Label>Tool permissions</Label>
        <ToolPermissionCard
          title="Built-in tools"
          subtitle="agent_toolset"
          icon={<TerminalIcon className="size-4" />}
          tools={builtInTools}
          permissions={toolPermissions}
          onToggle={(k, p) =>
            setToolPermissions((prev) => ({ ...prev, [k]: p }))
          }
          groupPerm={groupPermissions["builtin"] ?? "allow"}
          onGroupPerm={(p) =>
            setGroupPermissions((prev) => ({ ...prev, builtin: p }))
          }
          prefix="builtin"
        />
        {activeIntegrations.map((i) => (
          <ToolPermissionCard
            key={i.slug}
            title={i.name}
            subtitle={i.sourceCodeUrl.replace("https://github.com/", "")}
            icon={
              <div
                className="size-4 [&>svg]:size-4"
                dangerouslySetInnerHTML={{ __html: i.logo }}
              />
            }
            tools={i.tools}
            permissions={toolPermissions}
            onToggle={(k, p) =>
              setToolPermissions((prev) => ({ ...prev, [k]: p }))
            }
            groupPerm={groupPermissions[i.slug] ?? "allow"}
            onGroupPerm={(p) =>
              setGroupPermissions((prev) => ({ ...prev, [i.slug]: p }))
            }
            prefix={i.slug}
          />
        ))}
      </div>

      {/* Channel permissions */}
      {activeChannels.length > 0 && (
        <div className="space-y-3">
          <Label>Channel permissions</Label>
          {activeChannels.map((c) => (
            <ChannelPermissionCard
              key={c.slug}
              channel={c}
              perms={channelPerms[c.slug] ?? c.defaultPermissions}
              onChange={(p) =>
                setChannelPerms((prev) => ({ ...prev, [c.slug]: p }))
              }
            />
          ))}
        </div>
      )}

      <div className="flex items-center gap-3 pb-8">
        <Button size="sm">Save changes</Button>
      </div>
    </div>
  );
}
