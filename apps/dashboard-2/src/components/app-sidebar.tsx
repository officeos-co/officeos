"use client"

import * as React from "react"
import Link from "next/link"

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
  SparklesIcon,
} from "lucide-react"

const data = {
  org: {
    name: "AgentOS",
    plan: "Free plan",
  },
  user: {
    name: "Harro Krog",
    email: "harro@officeos.co",
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
      ],
    },
    {
      title: "Analytics",
      url: "#",
      icon: <ActivityIcon />,
      items: [
        { title: "Logs", url: "/logs" },
        { title: "Usage & Cost", url: "/usage" },
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
        <TeamSwitcher org={data.org} />
      </SidebarHeader>
      <SidebarContent>
        <NavMain items={data.navMain} />
      </SidebarContent>
      <SidebarFooter>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton
              render={<Link href="/billing" />}
              tooltip="Upgrade to Pro"
              className="text-muted-foreground"
            >
              <SparklesIcon className="text-secondary" />
              <span>Free plan · <span className="text-secondary font-medium">Upgrade</span></span>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
        <NavUser user={data.user} />
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  )
}
