"use client"

import * as React from "react"

import { NavMain } from "@/components/nav-main"
import { NavUser } from "@/components/nav-user"
import { TeamSwitcher } from "@/components/team-switcher"
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
} from "@/components/ui/sidebar"
import {
  BotIcon,
  ActivityIcon,
  SettingsIcon,
  BookOpenIcon,
} from "lucide-react"

const data = {
  user: {
    name: "Harro Krog",
    plan: "Free plan",
    avatar: "",
  },
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
        { title: "Providers", url: "/providers" },
        { title: "API Keys", url: "/api-keys" },
        { title: "Team", url: "/team" },
        { title: "Billing", url: "/billing" },
      ],
    },
  ],
}

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
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
        <NavUser user={data.user} />
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  )
}
