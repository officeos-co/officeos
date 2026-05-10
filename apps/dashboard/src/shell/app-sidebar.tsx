"use client";

import * as React from "react";

import { NavMain } from "./nav-main";
import { NavUser } from "./nav-user";
import { TeamSwitcher } from "./team-switcher";
import { WorkspaceSwitcher } from "./workspace-switcher";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/ui/sidebar";
import {
  BotIcon,
  ActivityIcon,
  SettingsIcon,
  BookOpenIcon,
} from "lucide-react";
import { useAuthContext } from "@/contexts/AuthContext";
import { useBilling } from "@/features/manage";

const data = {
  navMain: [
    {
      title: "Managed Agents",
      url: "#",
      icon: <BotIcon />,
      isActive: true,
      items: [
        { title: "Agents", url: "/agents" },
        { title: "Browser", url: "/browser" },
        { title: "Cron Jobs", url: "/cron-jobs" },
        { title: "Integrations", url: "/integrations" },
        { title: "Channels", url: "/channels" },
        { title: "Memory Store", url: "/memory-stores" },
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
      title: "Manage",
      url: "#",
      icon: <SettingsIcon />,
      items: [
        { title: "Profile", url: "/profile" },
        { title: "Team", url: "/team" },
        { title: "Billing", url: "/billing" },
      ],
    },
  ],
};

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const { user, loading: authLoading } = useAuthContext();
  const { billing, loading: billingLoading } = useBilling();
  const navMain = React.useMemo(() => {
    const showProviders = billing?.plan?.toLowerCase() === "enterprise";
    return data.navMain.map((section) =>
      section.title === "Manage" && showProviders
        ? { ...section, items: [...section.items, { title: "Providers", url: "/providers" }] }
        : section
    );
  }, [billing?.plan]);

  return (
    <Sidebar {...props}>
      <SidebarHeader>
        <TeamSwitcher />
        <WorkspaceSwitcher />
      </SidebarHeader>
      <SidebarContent>
        <NavMain items={navMain} />
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
