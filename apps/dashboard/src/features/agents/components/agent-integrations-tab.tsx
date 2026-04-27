"use client";

import { useState, useEffect, useRef } from "react";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import {
  ToolPermissionCard,
  ChannelPermissionCard,
  type ToolPermission,
} from "@/components/permission-cards";
import { builtInTools } from "@/features/agents/data/integrations";
import { type ChannelPermissions } from "@/features/agents/data/channels";
import { useIntegrations } from "@/features/agents/api/useIntegrations";
import { useChannels, useBindChannelToAgent, useUnbindChannelFromAgent } from "@/features/agents/api/useChannels";
import { useAgentBindings } from "@/features/agents/api/useAgentBindings";
import { CheckIcon, TerminalIcon, PackageCheckIcon } from "lucide-react";

export function AgentIntegrationsTab({ agentId }: { agentId: string }) {
  const { integrations } = useIntegrations();
  const { channels } = useChannels();
  const { skillSlugs, channelSlugs, loading: bindingsLoading } =
    useAgentBindings(agentId);
  const { bindChannelToAgent } = useBindChannelToAgent();
  const { unbindChannelFromAgent } = useUnbindChannelFromAgent();
  const initializedRef = useRef(false);
  const [selectedIntegrations, setSelectedIntegrations] = useState<Set<string>>(
    new Set(),
  );
  const [selectedChannels, setSelectedChannels] = useState<Set<string>>(
    new Set(),
  );

  // Sync from backend bindings once loaded
  useEffect(() => {
    if (!bindingsLoading && !initializedRef.current) {
      initializedRef.current = true;
      if (skillSlugs.length > 0)
        setSelectedIntegrations(new Set(skillSlugs));
      if (channelSlugs.length > 0) {
        const slugSet = new Set(channelSlugs);
        setSelectedChannels(slugSet);
        const cp: Record<string, ChannelPermissions> = {};
        for (const slug of channelSlugs) {
          const ch = channels.find((c) => c.slug === slug);
          if (ch) cp[slug] = { ...ch.defaultPermissions };
        }
        setChannelPerms(cp);
      }
    }
  }, [bindingsLoading, skillSlugs, channelSlugs, channels]);
  const [toolPermissions, setToolPermissions] = useState<
    Record<string, ToolPermission>
  >({});
  const [groupPermissions, setGroupPermissions] = useState<
    Record<string, ToolPermission>
  >({});
  const [channelPerms, setChannelPerms] = useState<
    Record<string, ChannelPermissions>
  >({});

  function toggleIntegration(slug: string) {
    setSelectedIntegrations((prev) => {
      const next = new Set(prev);
      if (next.has(slug)) next.delete(slug);
      else next.add(slug);
      return next;
    });
  }

  async function toggleChannel(slug: string) {
    const wasSelected = selectedChannels.has(slug);
    const ch = channels.find((c) => c.slug === slug);

    // Optimistically update UI
    setSelectedChannels((prev) => {
      const next = new Set(prev);
      if (wasSelected) next.delete(slug);
      else next.add(slug);
      return next;
    });

    if (wasSelected) {
      setChannelPerms((cp) => {
        const n = { ...cp };
        delete n[slug];
        return n;
      });
      if (ch?.connectionId) {
        try {
          await unbindChannelFromAgent(ch.connectionId, agentId);
        } catch (e) {
          console.error("Failed to unbind channel", e);
          setSelectedChannels((prev) => new Set(prev).add(slug));
        }
      }
    } else {
      if (ch) {
        setChannelPerms((cp) => ({
          ...cp,
          [slug]: { ...ch.defaultPermissions },
        }));
        if (ch.connectionId) {
          try {
            await bindChannelToAgent(ch.connectionId, agentId);
          } catch (e) {
            console.error("Failed to bind channel", e);
            setSelectedChannels((prev) => {
              const next = new Set(prev);
              next.delete(slug);
              return next;
            });
          }
        }
      }
    }
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
                {i.installed && (
                  <span className="flex items-center gap-1 rounded-full bg-emerald-500/10 px-1.5 py-0.5 text-[10px] font-medium text-emerald-600 shrink-0">
                    <PackageCheckIcon className="size-3" />
                    Installed
                  </span>
                )}
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
                {c.added && (
                  <span className="flex items-center gap-1 rounded-full bg-emerald-500/10 px-1.5 py-0.5 text-[10px] font-medium text-emerald-600 shrink-0">
                    Connected
                  </span>
                )}
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

      <div className="pb-8" />
    </div>
  );
}
