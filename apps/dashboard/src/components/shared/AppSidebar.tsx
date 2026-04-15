"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import posthog from "posthog-js";
import { useAuth } from "@/hooks/useAuth";
import { useAgents } from "@/hooks/useAgents";
import { useSystemEvents } from "@/hooks/useSystemEvents";
import { useApprovalRequests } from "@/hooks/useApprovalRequests";
import { cn } from "@/lib/utils";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
  SidebarTrigger,
} from "@/components/ui/sidebar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Bot,
  BookOpen,
  LogOut,
  LayoutDashboard,
  Settings,
  Wrench,
  Bell,
  CheckSquare,
  ChevronsUpDown,
  Server,
  KeyRound,
  CreditCard,
  Link2,
  User,
  Gauge,
  PanelLeft,
} from "lucide-react";

export function AppSidebar() {
  const pathname = usePathname();
  const router = useRouter();
  const { user, logout } = useAuth();
  const { agents } = useAgents();
  const { unacknowledgedCount: errorCount } = useSystemEvents(50);
  const { pendingCount } = useApprovalRequests();

  const runningCount = agents.filter(
    (a) =>
      a.status === "running" || a.status === "ready" || a.status === "online",
  ).length;

  const mainNavItems = [
    { href: "/", label: "Overview", icon: LayoutDashboard },
    { href: "/agents", label: "Agents", icon: Bot },
    { href: "/skills", label: "Tools", icon: Wrench },
    { href: "/providers", label: "Providers", icon: KeyRound },
    { href: "/runners", label: "Runners", icon: Server },
  ];

  const monitorItems = [
    { href: "/system-events", label: "Events", icon: Bell },
    { href: "/approvals", label: "Approvals", icon: CheckSquare },
  ];

  const settingsItems = [
    { href: "/settings/organization", label: "Organization", icon: Settings },
    { href: "/settings/profile", label: "Profile", icon: User },
    { href: "/settings/billing", label: "Billing", icon: CreditCard },
    { href: "/settings/channels", label: "Channels", icon: Link2 },
    { href: "/settings/api-keys", label: "API Keys", icon: KeyRound },
    { href: "/settings/limits", label: "Limits", icon: Gauge },
  ];

  function isActive(href: string) {
    if (href === "/") return pathname === "/";
    return pathname === href || pathname.startsWith(`${href}/`);
  }

  function NavItem({ href, label, icon: Icon }: { href: string; label: string; icon: React.ComponentType<{ className?: string }> }) {
    const active = isActive(href);
    return (
      <SidebarMenuItem>
        <SidebarMenuButton
          isActive={active}
          tooltip={label}
          onClick={() => posthog.capture("nav_clicked", { destination: href })}
          render={<Link href={href} />}
          className="h-8"
        >
          <Icon className="!h-4 !w-4" />
          <span className="text-[13px]">{label}</span>
        </SidebarMenuButton>
        {href === "/agents" && runningCount > 0 && (
          <SidebarMenuBadge className="bg-emerald-500/15 text-emerald-600 text-[10px] font-medium">
            {runningCount}
          </SidebarMenuBadge>
        )}
        {href === "/system-events" && errorCount > 0 && (
          <SidebarMenuBadge className="bg-red-500/15 text-red-600 text-[10px] font-medium">
            {errorCount > 99 ? "99+" : errorCount}
          </SidebarMenuBadge>
        )}
        {href === "/approvals" && pendingCount > 0 && (
          <SidebarMenuBadge className="bg-amber-500/15 text-amber-600 text-[10px] font-medium">
            {pendingCount > 99 ? "99+" : pendingCount}
          </SidebarMenuBadge>
        )}
      </SidebarMenuItem>
    );
  }

  return (
    <Sidebar collapsible="icon">
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <div className="flex items-center justify-between px-2 py-1">
              <SidebarMenuButton
                size="lg"
                render={<Link href="/" />}
                className="h-8 !p-0"
              >
                <span className="text-[14px] font-semibold tracking-tight text-foreground">
                  AgentOS
                </span>
              </SidebarMenuButton>
              <SidebarTrigger className="h-6 w-6 text-muted-foreground hover:text-foreground group-data-[collapsible=icon]:hidden">
                <PanelLeft className="h-4 w-4" />
              </SidebarTrigger>
            </div>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>

      <SidebarContent>
        <SidebarGroup>
          <SidebarMenu>
            {mainNavItems.map((item) => (
              <NavItem key={item.href} {...item} />
            ))}
          </SidebarMenu>
        </SidebarGroup>

        <SidebarGroup>
          <SidebarGroupLabel className="text-[11px] uppercase tracking-wider text-muted-foreground/70">
            Monitor
          </SidebarGroupLabel>
          <SidebarMenu>
            {monitorItems.map((item) => (
              <NavItem key={item.href} {...item} />
            ))}
          </SidebarMenu>
        </SidebarGroup>

        <SidebarGroup>
          <SidebarGroupLabel className="text-[11px] uppercase tracking-wider text-muted-foreground/70">
            Settings
          </SidebarGroupLabel>
          <SidebarMenu>
            {settingsItems.map((item) => (
              <NavItem key={item.href} {...item} />
            ))}
          </SidebarMenu>
        </SidebarGroup>
      </SidebarContent>

      <SidebarFooter>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton
              tooltip="Documentation"
              className="h-8"
              render={
                <a
                  href="https://docs.officeos.co"
                  target="_blank"
                  rel="noopener noreferrer"
                />
              }
            >
              <BookOpen className="!h-4 !w-4" />
              <span className="text-[13px]">Docs</span>
            </SidebarMenuButton>
          </SidebarMenuItem>

          <SidebarMenuItem>
            <DropdownMenu>
              <DropdownMenuTrigger
                render={
                  <SidebarMenuButton
                    size="lg"
                    className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground h-10"
                  />
                }
              >
                {user?.avatarUrl ? (
                  <img
                    src={user.avatarUrl}
                    alt=""
                    className="h-7 w-7 rounded-full"
                  />
                ) : (
                  <div className="grid h-7 w-7 place-items-center rounded-full bg-muted text-[11px] font-medium text-muted-foreground">
                    {(user?.name ?? "U").charAt(0).toUpperCase()}
                  </div>
                )}
                <div className="flex-1 truncate text-left">
                  <div className="truncate text-[13px] font-medium leading-tight">
                    {user?.name ?? "Local dev"}
                  </div>
                  <div className="truncate text-[11px] text-muted-foreground leading-tight">
                    {user?.email ?? "single-tenant"}
                  </div>
                </div>
                <ChevronsUpDown className="ml-auto size-3.5 shrink-0 text-muted-foreground" />
              </DropdownMenuTrigger>
              <DropdownMenuContent
                className="w-[--radix-dropdown-menu-trigger-width] min-w-48"
                side="bottom"
                align="end"
                sideOffset={4}
              >
                <DropdownMenuItem
                  onSelect={() => router.push("/settings/profile")}
                >
                  <User className="mr-2 h-4 w-4" />
                  Profile
                </DropdownMenuItem>
                <DropdownMenuItem
                  onSelect={() => router.push("/settings/billing")}
                >
                  <CreditCard className="mr-2 h-4 w-4" />
                  Billing
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem
                  onSelect={() => logout()}
                  className="text-destructive focus:text-destructive"
                >
                  <LogOut className="mr-2 h-4 w-4" />
                  Sign out
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>

      <SidebarRail />
    </Sidebar>
  );
}
