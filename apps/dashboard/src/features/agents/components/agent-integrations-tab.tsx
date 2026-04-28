"use client";

import { useState, useEffect, useRef } from "react";
import Link from "next/link";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import {
  ToolPermissionCard,
  ChannelPermissionCard,
  type ToolPermission,
} from "@/components/permission-cards";
import { builtInTools } from "@/features/agents/data/integrations";
import type { Channel, ChannelPermissions } from "@/features/agents/data/channels";
import {
  useIntegrations,
  useInstallSkill,
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
  useAssignSkillToAgent,
  useUnassignSkillFromAgent,
} from "@/features/agents/api/useAgentSkills";
import { IntegrationCard } from "./integration-card";
import { ChannelCard } from "./channel-card";
import { CredentialDialog } from "./credential-dialog";
import { ChannelOnboardingDialog } from "./channel-onboarding-dialog";
import { TerminalIcon, AlertTriangleIcon, ExternalLinkIcon } from "lucide-react";

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
  const assignSkill = useAssignSkillToAgent();
  const unassignSkill = useUnassignSkillFromAgent();
  const installSkill = useInstallSkill();
  const setSkillCredentials = useSetSkillCredentials();
  const initializedRef = useRef(false);
  const [selectedIntegrations, setSelectedIntegrations] = useState<Set<string>>(
    new Set(),
  );
  const [selectedChannels, setSelectedChannels] = useState<Set<string>>(
    new Set(),
  );
  const [installingSlug, setInstallingSlug] = useState<string | null>(null);
  const [configureSlug, setConfigureSlug] = useState<string | null>(null);
  const [onboardChannel, setOnboardChannel] = useState<Channel | null>(null);

  // Sync from backend bindings once loaded
  useEffect(() => {
    if (!bindingsLoading && !initializedRef.current) {
      initializedRef.current = true;
      if (skillSlugs.length > 0) setSelectedIntegrations(new Set(skillSlugs));
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

  async function toggleIntegration(slug: string) {
    const wasSelected = selectedIntegrations.has(slug);

    // Optimistic UI
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
    } catch (e) {
      console.error("Failed to toggle skill", e);
      // Revert on failure
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

    // Guard: skip mutation if state already matches backend
    const alreadyBound = channelSlugs.includes(slug);
    if (!wasSelected && alreadyBound) {
      // Just update local state, no mutation needed
      setSelectedChannels((prev) => new Set(prev).add(slug));
      if (ch) setChannelPerms((cp) => ({ ...cp, [slug]: { ...ch.defaultPermissions } }));
      return;
    }
    if (wasSelected && !alreadyBound) {
      // Just update local state, no mutation needed
      setSelectedChannels((prev) => {
        const next = new Set(prev);
        next.delete(slug);
        return next;
      });
      setChannelPerms((cp) => { const n = { ...cp }; delete n[slug]; return n; });
      return;
    }

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

  const sortedIntegrations = sortIntegrations(integrations);
  const sortedChannels = [...channels].sort((a, b) => (a.added === b.added ? 0 : a.added ? -1 : 1));
  const activeIntegrations = integrations.filter((i) =>
    selectedIntegrations.has(i.slug),
  );
  const activeChannels = channels.filter((c) => selectedChannels.has(c.slug));

  return (
    <>
      <div className="pt-4 space-y-6">
        {/* Integrations */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <div>
              <Label>Integrations</Label>
              <p className="text-xs text-muted-foreground">API-based tools the agent can call.</p>
            </div>
            <Link href="/integrations" className="text-xs text-muted-foreground hover:text-foreground">Manage integrations →</Link>
          </div>
          <div className="grid grid-cols-3 gap-2">
            {sortedIntegrations.map((i) => (
              <IntegrationCard
                key={i.slug}
                integration={i}
                selected={selectedIntegrations.has(i.slug)}
                installing={installingSlug === i.slug}
                onInstall={async () => {
                  setInstallingSlug(i.slug);
                  try { await installSkill(i.slug); }
                  finally { setInstallingSlug(null); }
                }}
                onConfigure={() => setConfigureSlug(i.slug)}
                onToggle={() => toggleIntegration(i.slug)}
              />
            ))}
          </div>
        </div>

        {/* Channels */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <div>
              <Label>Channels</Label>
              <p className="text-xs text-muted-foreground">Messaging platforms that connect to the agent's session via WebSocket.</p>
            </div>
            <Link href="/channels" className="text-xs text-muted-foreground hover:text-foreground">Manage channels →</Link>
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
          const unconfigured = activeIntegrations.filter((i) => i.credentialFields.length > 0 && !i.configured);
          if (unconfigured.length === 0) return null;
          return (
            <div className="rounded-xl border border-amber-300 bg-amber-50 p-4">
              <div className="flex items-start gap-3">
                <AlertTriangleIcon className="size-5 text-amber-600 shrink-0 mt-0.5" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-amber-800">
                    {unconfigured.length === 1
                      ? `${unconfigured[0].name} requires credentials before it can be used.`
                      : `${unconfigured.length} integrations require credentials before they can be used.`}
                  </p>
                  <p className="text-xs text-amber-700 mt-1">
                    The agent will not be able to use unconfigured integrations. Set up credentials on the integration page.
                  </p>
                  <div className="flex flex-wrap gap-2 mt-3">
                    {unconfigured.map((i) => (
                      <Link key={i.slug} href={`/integrations/${i.slug}`}
                        className="inline-flex items-center gap-1.5 rounded-md bg-amber-100 px-2.5 py-1 text-xs font-medium text-amber-800 hover:bg-amber-200 transition-colors">
                        <div className="size-3.5 shrink-0 [&>svg]:size-3.5" dangerouslySetInnerHTML={{ __html: i.logo }} />
                        {i.name}
                        <ExternalLinkIcon className="size-3" />
                      </Link>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          );
        })()}

        {/* Tool permissions — coming soon */}
        <div className="space-y-3 pointer-events-none opacity-50">
          <Label className="flex items-center gap-2">
            Tool permissions
            <span className="rounded-full bg-muted px-2 py-0.5 text-[10px] font-medium text-muted-foreground">Coming soon</span>
          </Label>
          <ToolPermissionCard
            title="Built-in tools"
            subtitle="agent_toolset"
            icon={<TerminalIcon className="size-4" />}
            tools={builtInTools}
            permissions={toolPermissions}
            onToggle={() => {}}
            groupPerm={groupPermissions["builtin"] ?? "allow"}
            onGroupPerm={() => {}}
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
              onToggle={() => {}}
              groupPerm={groupPermissions[i.slug] ?? "allow"}
              onGroupPerm={() => {}}
              prefix={i.slug}
            />
          ))}
        </div>

        {/* Channel permissions — coming soon */}
        {activeChannels.length > 0 && (
          <div className="space-y-3 pointer-events-none opacity-50">
            <Label className="flex items-center gap-2">
              Channel permissions
              <span className="rounded-full bg-muted px-2 py-0.5 text-[10px] font-medium text-muted-foreground">Coming soon</span>
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
      {configureSlug && (() => {
        const i = integrations.find((x) => x.slug === configureSlug);
        if (!i) return null;
        return (
          <CredentialDialog
            open
            onOpenChange={(open) => { if (!open) setConfigureSlug(null); }}
            name={i.name}
            slug={i.slug}
            logo={i.logo}
            credentials={i.credentialFields}
            onSave={(values) => {
              setSkillCredentials(i.slug, values);
              setConfigureSlug(null);
            }}
          />
        );
      })()}

      {/* Channel onboarding dialog for inline connect */}
      {onboardChannel && (
        <ChannelOnboardingDialog
          open
          onOpenChange={(open) => { if (!open) setOnboardChannel(null); }}
          channel={onboardChannel}
          onComplete={() => setOnboardChannel(null)}
        />
      )}
    </>
  );
}
