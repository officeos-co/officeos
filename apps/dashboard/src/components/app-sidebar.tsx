"use client";

import * as React from "react";

import { NavMain } from "@/components/nav-main";
import { NavUser } from "@/components/nav-user";
import { TeamSwitcher } from "@/components/team-switcher";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar";
import {
  BotIcon,
  ActivityIcon,
  SettingsIcon,
  BookOpenIcon,
  DatabaseIcon,
} from "lucide-react";
import { useAuthContext } from "@/contexts/AuthContext";
import { useBilling } from "@/features/manage";
import { isDevelopment } from "@/lib/env";

const data = {
  navMain: [
    {
      title: "Managed Agents",
      url: "#",
      icon: <BotIcon />,
      isActive: true,
      items: [
        { title: "Quickstart", url: "/quickstart" },
        { title: "Agents", url: "/agents" },
        { title: "Browser", url: "/browser" },
        { title: "Memory Store", url: "/memory-stores" },
        { title: "Cron Jobs", url: "/cron-jobs" },
        { title: "Integrations", url: "/integrations" },
        { title: "Channels", url: "/channels" },
      ],
    },
    {
      title: "Analytics",
      url: "#",
      icon: <ActivityIcon />,
      items: [
        { title: "Logs", url: "/logs" },
        { title: "Usage", url: "/usage" },
      ],
    },
    {
      title: "Data",
      url: "#",
      icon: <DatabaseIcon />,
      items: [
        { title: "Connectors", url: "/atlas/connectors" },
        { title: "History", url: "/atlas/history" },
      ],
    },
    {
      title: "Manage",
      url: "#",
      icon: <SettingsIcon />,
      items: [
        { title: "Profile", url: "/profile" },
        { title: "Team", url: "/team" },
        ...(!isDevelopment() ? [{ title: "Billing", url: "/billing" }] : []),
        ...(isDevelopment() ? [{ title: "Providers", url: "/providers" }] : []),
      ],
    },
  ],
};

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const { user, loading: authLoading } = useAuthContext();
  const { billing, loading: billingLoading } = useBilling();

  return (
    <Sidebar {...props}>
      <SidebarHeader>
        <TeamSwitcher />
      </SidebarHeader>
      <SidebarContent>
        <NavMain items={data.navMain} />
      </SidebarContent>
      <SidebarFooter>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton
              tooltip="Documentation"
              render={
                <a
                  href="https://docs.officeos.co"
                  target="_blank"
                  rel="noopener noreferrer"
                />
              }
            >
              <BookOpenIcon />
              <span>Documentation</span>
            </SidebarMenuButton>
          </SidebarMenuItem>
          {/* TODO: Add OfficeOS Cloud when ready */}
          {/* {isDevelopment() && (
            <SidebarMenuItem>
              <SidebarMenuButton
                tooltip="Try OfficeOS Cloud"
                render={
                  <a
                    href="https://dashboard.officeos.co"
                    target="_blank"
                    rel="noopener noreferrer"
                  />
                }
              >
                <CloudIcon />
                <span>Try OfficeOS Cloud</span>
              </SidebarMenuButton>
            </SidebarMenuItem>
          )} */}
        </SidebarMenu>
        {!authLoading && !billingLoading && user && (
          <NavUser
            user={{
              name: user.name ?? user.email,
              plan: billing?.plan ?? "self-hosted",
              avatar: user.avatarUrl ?? "",
            }}
          />
        )}
      </SidebarFooter>
    </Sidebar>
  );
}
