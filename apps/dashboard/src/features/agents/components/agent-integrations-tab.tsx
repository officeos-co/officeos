"use client";

import { useState, useEffect, useRef } from "react";
import { toast } from "sonner";
import Link from "next/link";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import {
  ToolPermissionCard,
  ChannelPermissionCard,
  type ToolPermission,
} from "@/components/permission-cards";
import type {
  Channel,
  ChannelPermissions,
} from "@/features/agents/data/channels";
import {
  useIntegrations,
  useSetSkillCredentials,
  sortIntegrations,
} from "@/features/agents/api/useIntegrations";
import {
  useChannels,
  useBindChannelToAgent,
  useUnbindChannelFromAgent,
} from "@/features/agents/api/useChannels";
import { useAgentBindings } from "@/features/agents/api/useAgentBindings";
import {
  useAgentToolCatalog,
  useAgentToolPermissions,
  useSetAgentToolPermissions,
} from "@/features/agents/api/useAgents";
import {
  useAssignSkillToAgent,
  useUnassignSkillFromAgent,
} from "@/features/agents/api/useAgentSkills";
import { IntegrationCard } from "./integration-card";
import { ChannelCard } from "./channel-card";
import { CredentialDialog } from "./credential-dialog";
import { ChannelOnboardingDialog } from "./channel-onboarding-dialog";
import {
  TerminalIcon,
  AlertTriangleIcon,
  ExternalLinkIcon,
} from "lucide-react";

function graphQLErrorMessage(error: unknown, fallback: string) {
  if (
    typeof error === "object" &&
    error !== null &&
    "graphQLErrors" in error &&
    Array.isArray(error.graphQLErrors)
  ) {
    const first = error.graphQLErrors[0] as { message?: unknown } | undefined;
    if (typeof first?.message === "string") return first.message;
  }
  return fallback;
}

export function AgentIntegrationsTab({ agentId }: { agentId: string }) {
  const { integrations } = useIntegrations();
  const { channels } = useChannels();
  const {
    skillSlugs,
    channelSlugs,
    loading: bindingsLoading,
  } = useAgentBindings(agentId);
  const { bindChannelToAgent } = useBindChannelToAgent();
  const { unbindChannelFromAgent } = useUnbindChannelFromAgent();
  const { tools: toolCatalog } = useAgentToolCatalog(agentId);
  const { permissions: savedToolPermissions } =
    useAgentToolPermissions(agentId);
  const { setAgentToolPermissions } = useSetAgentToolPermissions();
  const assignSkill = useAssignSkillToAgent();
  const unassignSkill = useUnassignSkillFromAgent();
  const setSkillCredentials = useSetSkillCredentials();
  const [selectedIntegrations, setSelectedIntegrations] = useState<Set<string>>(
    new Set(),
  );
  const [selectedChannels, setSelectedChannels] = useState<Set<string>>(
    new Set(),
  );
  const [toolPermissions, setToolPermissions] = useState<
    Record<string, ToolPermission>
  >({});
  const [groupPermissions, setGroupPermissions] = useState<
    Record<string, ToolPermission>
  >({});
  const [channelPerms, setChannelPerms] = useState<
    Record<string, ChannelPermissions>
  >({});
  const lastSyncedSkillsRef = useRef<string>("");
  const lastSyncedChannelsRef = useRef<string>("");
  const [configureSlug, setConfigureSlug] = useState<string | null>(null);
  const [onboardChannel, setOnboardChannel] = useState<Channel | null>(null);
  const lastSyncedPermissionsRef = useRef<string>("");

  // Sync from backend bindings whenever they change
  useEffect(() => {
    if (bindingsLoading) return;
    const skillsKey = [...skillSlugs].sort().join(",");
    const channelsKey = [...channelSlugs].sort().join(",");

    if (skillsKey !== lastSyncedSkillsRef.current) {
      lastSyncedSkillsRef.current = skillsKey;
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setSelectedIntegrations(new Set(skillSlugs));
    }

    if (channelsKey !== lastSyncedChannelsRef.current) {
      lastSyncedChannelsRef.current = channelsKey;
      setSelectedChannels(new Set(channelSlugs));
      const cp: Record<string, ChannelPermissions> = {};
      for (const slug of channelSlugs) {
        const ch = channels.find((c) => c.slug === slug);
        if (ch) cp[slug] = { ...ch.defaultPermissions };
      }
      setChannelPerms(cp);
    }
  }, [bindingsLoading, skillSlugs, channelSlugs, channels]);

  useEffect(() => {
    const key = savedToolPermissions
      .map((p) => `${p.skillName}:${p.toolName}:${p.mode}`)
      .sort()
      .join("|");
    if (key === lastSyncedPermissionsRef.current) return;
    lastSyncedPermissionsRef.current = key;

    const nextTools: Record<string, ToolPermission> = {};
    const nextGroups: Record<string, ToolPermission> = {};
    for (const permission of savedToolPermissions) {
      const mode = permission.mode === "DENY" ? "deny" : "allow";
      if (permission.toolName) {
        nextTools[`${permission.skillName}:${permission.toolName}`] = mode;
      } else {
        nextGroups[permission.skillName] = mode;
      }
    }
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setToolPermissions(nextTools);
    setGroupPermissions(nextGroups);
  }, [savedToolPermissions]);

  async function persistToolPermissions(
    nextTools: Record<string, ToolPermission>,
    nextGroups: Record<string, ToolPermission>,
  ) {
    const entries: Array<{
      skill: string;
      tool: string;
      mode: "ALLOW" | "DENY";
    }> = [];

    for (const [key, permission] of Object.entries(nextTools)) {
      const [skill, ...toolParts] = key.split(":");
      entries.push({
        skill,
        tool: toolParts.join(":"),
        mode: permission === "deny" ? "DENY" : "ALLOW",
      });
    }

    for (const [skill, permission] of Object.entries(nextGroups)) {
      entries.push({
        skill,
        tool: "",
        mode: permission === "deny" ? "DENY" : "ALLOW",
      });
    }

    try {
      await setAgentToolPermissions(agentId, entries);
    } catch (error) {
      toast.error(graphQLErrorMessage(error, "Failed to save tool permissions"));
    }
  }

  function updateToolPermission(key: string, permission: ToolPermission) {
    const next = { ...toolPermissions, [key]: permission };
    setToolPermissions(next);
    void persistToolPermissions(next, groupPermissions);
  }

  function updateGroupPermission(skill: string, permission: ToolPermission) {
    const next = { ...groupPermissions, [skill]: permission };
    setGroupPermissions(next);
    void persistToolPermissions(toolPermissions, next);
  }

  async function toggleIntegration(slug: string) {
    const wasSelected = selectedIntegrations.has(slug);

    setSelectedIntegrations((prev) => {
      const next = new Set(prev);
      if (wasSelected) next.delete(slug);
      else next.add(slug);
      return next;
    });

    try {
      if (wasSelected) {
        await unassignSkill(agentId, slug);
      } else {
        await assignSkill(agentId, slug);
      }
    } catch (e: unknown) {
      const msg = graphQLErrorMessage(e, "Failed to toggle MCP server");
      toast.error(msg);
      setSelectedIntegrations((prev) => {
        const next = new Set(prev);
        if (wasSelected) next.add(slug);
        else next.delete(slug);
        return next;
      });
    }
  }

  async function toggleChannel(slug: string) {
    const wasSelected = selectedChannels.has(slug);
    const ch = channels.find((c) => c.slug === slug);

    const alreadyBound = channelSlugs.includes(slug);
    if (!wasSelected && alreadyBound) {
      setSelectedChannels((prev) => new Set(prev).add(slug));
      if (ch)
        setChannelPerms((cp) => ({
          ...cp,
          [slug]: { ...ch.defaultPermissions },
        }));
      return;
    }
    if (wasSelected && !alreadyBound) {
      setSelectedChannels((prev) => {
        const next = new Set(prev);
        next.delete(slug);
        return next;
      });
      setChannelPerms((cp) => {
        const n = { ...cp };
        delete n[slug];
        return n;
      });
      return;
    }

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
          } catch (e: unknown) {
            const msg = graphQLErrorMessage(e, "Failed to bind channel");
            toast.error(msg);
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

  const sortedIntegrations = sortIntegrations(integrations);
  const sortedChannels = [...channels].sort((a, b) =>
    a.added === b.added ? 0 : a.added ? -1 : 1,
  );
  const activeIntegrations = integrations.filter((i) =>
    selectedIntegrations.has(i.name),
  );
  const activeChannels = channels.filter((c) => selectedChannels.has(c.slug));
  const backendBuiltInTools = toolCatalog
    .filter((tool) => tool.group === "builtin")
    .map((tool) => ({
      name: tool.permissionTool || tool.runtimeName,
      description: tool.description,
    }));

  return (
    <>
      <div className="pt-4 space-y-6">
        {/* Integrations */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <div>
              <Label>MCP Servers</Label>
              <p className="text-xs text-muted-foreground">
                MCP servers the agent can use.
              </p>
            </div>
            <Link
              href="/integrations"
              className="text-xs text-muted-foreground hover:text-foreground"
            >
              Manage servers →
            </Link>
          </div>
          <div className="grid grid-cols-3 gap-2">
            {sortedIntegrations.map((i) => (
              <IntegrationCard
                key={i.name}
                integration={i}
                selected={selectedIntegrations.has(i.name)}
                onConfigure={() => setConfigureSlug(i.name)}
                onToggle={() => toggleIntegration(i.name)}
              />
            ))}
          </div>
        </div>

        {/* Channels */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <div>
              <Label>Channels</Label>
              <p className="text-xs text-muted-foreground">
                Messaging platforms that connect to the agent&apos;s session via
                WebSocket.
              </p>
            </div>
            <Link
              href="/channels"
              className="text-xs text-muted-foreground hover:text-foreground"
            >
              Manage channels →
            </Link>
          </div>
          <div className="grid grid-cols-3 gap-2">
            {sortedChannels.map((c) => (
              <ChannelCard
                key={c.slug}
                channel={c}
                selected={selectedChannels.has(c.slug)}
                onConnect={() => setOnboardChannel(c)}
                onToggle={() => toggleChannel(c.slug)}
              />
            ))}
          </div>
        </div>

        <Separator />

        {/* Unconfigured integrations warning */}
        {(() => {
          const unconfigured = activeIntegrations.filter(
            (i) => i.credentialFields.length > 0 && !i.configured,
          );
          if (unconfigured.length === 0) return null;
          return (
            <div className="rounded-xl border border-amber-300 bg-amber-50 p-4">
              <div className="flex items-start gap-3">
                <AlertTriangleIcon className="size-5 text-amber-600 shrink-0 mt-0.5" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-amber-800">
                    {unconfigured.length === 1
                      ? `${unconfigured[0].title} requires credentials before it can be used.`
                      : `${unconfigured.length} MCP servers require credentials before they can be used.`}
                  </p>
                  <p className="text-xs text-amber-700 mt-1">
                    The agent will not be able to use unconfigured servers.
                    Set up credentials on the server page.
                  </p>
                  <div className="flex flex-wrap gap-2 mt-3">
                    {unconfigured.map((i) => (
                      <Link
                        key={i.name}
                        href={`/integrations/${i.name}`}
                        className="inline-flex items-center gap-1.5 rounded-md bg-amber-100 px-2.5 py-1 text-xs font-medium text-amber-800 hover:bg-amber-200 transition-colors"
                      >
                        <div
                          className="size-3.5 shrink-0 [&>svg]:size-3.5"
                          dangerouslySetInnerHTML={{ __html: i.logo }}
                        />
                        {i.title}
                        <ExternalLinkIcon className="size-3" />
                      </Link>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          );
        })()}

        {/* Tool permissions */}
        <div className="space-y-3">
          <Label>Tool permissions</Label>
          <ToolPermissionCard
            title="Built-in tools"
            subtitle="agent_toolset"
            icon={<TerminalIcon className="size-4" />}
            tools={backendBuiltInTools}
            permissions={toolPermissions}
            onToggle={updateToolPermission}
            groupPerm={groupPermissions["builtin"] ?? "allow"}
            onGroupPerm={(permission) => updateGroupPermission("builtin", permission)}
            prefix="builtin"
          />
          {activeIntegrations.map((i) => (
            <ToolPermissionCard
              key={i.name}
              title={i.title}
              subtitle={i.name}
              icon={
                <div
                  className="size-4 [&>svg]:size-4"
                  dangerouslySetInnerHTML={{ __html: i.logo }}
                />
              }
              tools={i.tools}
              permissions={toolPermissions}
              onToggle={updateToolPermission}
              groupPerm={groupPermissions[i.name] ?? "allow"}
              onGroupPerm={(permission) => updateGroupPermission(i.name, permission)}
              prefix={i.name}
            />
          ))}
        </div>

        {/* Channel permissions — coming soon */}
        {activeChannels.length > 0 && (
          <div className="space-y-3 pointer-events-none opacity-50">
            <Label className="flex items-center gap-2">
              Channel permissions
              <span className="rounded-full bg-muted px-2 py-0.5 text-[10px] font-medium text-muted-foreground">
                Coming soon
              </span>
            </Label>
            {activeChannels.map((c) => (
              <ChannelPermissionCard
                key={c.slug}
                channel={c}
                perms={channelPerms[c.slug] ?? c.defaultPermissions}
                onChange={() => {}}
              />
            ))}
          </div>
        )}

        <div className="pb-8" />
      </div>

      {/* Credential dialog for inline configure */}
      {configureSlug &&
        (() => {
          const i = integrations.find((x) => x.name === configureSlug);
          if (!i) return null;
          return (
            <CredentialDialog
              open
              onOpenChange={(open) => {
                if (!open) setConfigureSlug(null);
              }}
              name={i.title}
              slug={i.name}
              logo={i.logo}
              credentials={i.credentialFields}
              onSave={(values) => {
                setSkillCredentials(i.name, values);
                setConfigureSlug(null);
              }}
            />
          );
        })()}

      {/* Channel onboarding dialog for inline connect */}
      {onboardChannel && (
        <ChannelOnboardingDialog
          open
          onOpenChange={(open) => {
            if (!open) setOnboardChannel(null);
          }}
          channel={onboardChannel}
          onComplete={() => setOnboardChannel(null)}
        />
      )}
    </>
  );
}
