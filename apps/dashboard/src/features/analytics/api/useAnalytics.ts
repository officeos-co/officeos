"use client"

import { useMutation, gql } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"

// One typed mutation per use case. Matches backend `Entities/PostHog/` —
// the GraphQL schema is the source of truth for which events exist.

const TRACK_PAGE_VIEW = gql`
  mutation TrackPageView($input: TrackPageViewInput!) {
    trackPageView(input: $input)
  }
`
const TRACK_NAV_CLICKED = gql`
  mutation TrackNavClicked($input: TrackNavClickedInput!) {
    trackNavClicked(input: $input)
  }
`
const TRACK_SKILL_INSTALLED = gql`
  mutation TrackSkillInstalled($input: TrackSkillInstalledInput!) {
    trackSkillInstalled(input: $input)
  }
`
const TRACK_SKILL_CONFIGURED = gql`
  mutation TrackSkillConfigured($input: TrackSkillConfiguredInput!) {
    trackSkillConfigured(input: $input)
  }
`
const TRACK_CHANNEL_CONNECTED = gql`
  mutation TrackChannelConnected($input: TrackChannelConnectedInput!) {
    trackChannelConnected(input: $input)
  }
`
const TRACK_AGENT_CREATED = gql`
  mutation TrackAgentCreated($input: TrackAgentCreatedInput!) {
    trackAgentCreated(input: $input)
  }
`

type AgentCreatedInput = {
  agentName: string
  provider: string
  template: string
  skillCount: number
  allowSkills: number
  denySkills: number
}

export function useAnalytics() {
  const [trackPageViewMutation] = useMutation(TRACK_PAGE_VIEW)
  const [trackNavClickedMutation] = useMutation(TRACK_NAV_CLICKED)
  const [trackSkillInstalledMutation] = useMutation(TRACK_SKILL_INSTALLED)
  const [trackSkillConfiguredMutation] = useMutation(TRACK_SKILL_CONFIGURED)
  const [trackChannelConnectedMutation] = useMutation(TRACK_CHANNEL_CONNECTED)
  const [trackAgentCreatedMutation] = useMutation(TRACK_AGENT_CREATED)

  const run = async (label: string, fire: () => Promise<unknown>) => {
    if (USE_MOCKS) {
      // eslint-disable-next-line no-console
      console.debug("[posthog:mock]", label)
      return
    }
    try {
      await fire()
    } catch {
      // Analytics must never surface to the user.
    }
  }

  return {
    trackPageView: (path: string) =>
      run("$pageview", () => trackPageViewMutation({ variables: { input: { path } } })),
    trackNavClicked: (destination: string) =>
      run("nav_clicked", () => trackNavClickedMutation({ variables: { input: { destination } } })),
    trackSkillInstalled: (skillName: string) =>
      run("skill_installed", () =>
        trackSkillInstalledMutation({ variables: { input: { skillName } } })),
    trackSkillConfigured: (skillName: string) =>
      run("skill_configured", () =>
        trackSkillConfiguredMutation({ variables: { input: { skillName } } })),
    trackChannelConnected: (channelSlug: string) =>
      run("channel_connected", () =>
        trackChannelConnectedMutation({ variables: { input: { channelSlug } } })),
    trackAgentCreated: (input: AgentCreatedInput) =>
      run("agent_created", () => trackAgentCreatedMutation({ variables: { input } })),
  }
}
