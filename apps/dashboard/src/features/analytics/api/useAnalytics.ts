"use client";

import { useMutation, gql } from "@apollo/client";

const TRACK_PAGE_VIEW = gql`
  mutation TrackPageView($input: TrackPageViewInput!) {
    trackPageView(input: $input)
  }
`;
const TRACK_NAV_CLICKED = gql`
  mutation TrackNavClicked($input: TrackNavClickedInput!) {
    trackNavClicked(input: $input)
  }
`;
const TRACK_CHANNEL_CONNECTED = gql`
  mutation TrackChannelConnected($input: TrackChannelConnectedInput!) {
    trackChannelConnected(input: $input)
  }
`;
const TRACK_AGENT_CREATED = gql`
  mutation TrackAgentCreated($input: TrackAgentCreatedInput!) {
    trackAgentCreated(input: $input)
  }
`;

type AgentCreatedInput = {
  agentName: string;
  provider: string;
  skillCount: number;
  allowSkills: number;
  denySkills: number;
};

export function useAnalytics() {
  const [trackPageViewMutation] = useMutation(TRACK_PAGE_VIEW);
  const [trackNavClickedMutation] = useMutation(TRACK_NAV_CLICKED);
  const [trackChannelConnectedMutation] = useMutation(TRACK_CHANNEL_CONNECTED);
  const [trackAgentCreatedMutation] = useMutation(TRACK_AGENT_CREATED);

  const run = async (label: string, fire: () => Promise<unknown>) => {
    try {
      await fire();
    } catch {
      // Analytics must never surface to the user.
    }
  };

  return {
    trackPageView: (path: string) =>
      run("$pageview", () =>
        trackPageViewMutation({ variables: { input: { path } } }),
      ),
    trackNavClicked: (destination: string) =>
      run("nav_clicked", () =>
        trackNavClickedMutation({ variables: { input: { destination } } }),
      ),
    trackChannelConnected: (channelSlug: string) =>
      run("channel_connected", () =>
        trackChannelConnectedMutation({
          variables: { input: { channelSlug } },
        }),
      ),
    trackAgentCreated: (input: AgentCreatedInput) =>
      run("agent_created", () =>
        trackAgentCreatedMutation({ variables: { input } }),
      ),
  };
}
