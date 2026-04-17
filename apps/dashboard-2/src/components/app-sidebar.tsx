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
  SidebarRail,
} from "@/components/ui/sidebar";
import {
  BotIcon,
  ActivityIcon,
  SettingsIcon,
  BookOpenIcon,
} from "lucide-react";
import { useAuthContext } from "@/contexts/AuthContext";
import { useBilling } from "@/hooks/useBilling";

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
        { title: "Cost", url: "/cost" },
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

  return (
    <Sidebar collapsible="icon" {...props}>
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
        </SidebarMenu>
        {!authLoading && !billingLoading && user && billing && (
          <NavUser
            user={{
              name: user.name ?? user.email,
              plan: billing.plan,
              avatar: user.avatarUrl ?? "",
            }}
          />
        )}
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  );
}
