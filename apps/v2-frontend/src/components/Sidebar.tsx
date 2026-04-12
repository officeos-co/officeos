"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { useAgents, type Agent } from "@/hooks/useAgents";
import { cn } from "@/lib/utils";
import {
  Bot,
  KeyRound,
  Radio,
  BookOpen,
  LogOut,
  LayoutDashboard,
  Settings,
  ArrowLeft,
  User,
  Building2,
  Wrench,
  CreditCard,
  Gauge,
  Key,
  Network,
  ChevronRight,
} from "lucide-react";
import { useState } from "react";

function statusDot(status: string) {
  if (status === "running" || status === "ready" || status === "online")
    return "bg-emerald-500";
  if (status === "pending" || status === "building") return "bg-amber-400";
  if (status === "failed" || status === "not_found" || status === "offline")
    return "bg-red-400";
  return "bg-muted-foreground/40";
}

function MainSidebar({
  onOpenOrgSettings,
}: {
  onOpenOrgSettings: () => void;
}) {
  const pathname = usePathname();
  const { user, logout } = useAuth();
  const { agents } = useAgents();

  const runningCount = agents.filter(
    (a) => a.status === "running" || a.status === "ready" || a.status === "online",
  ).length;

  return (
    <aside className="flex h-full w-[260px] shrink-0 flex-col border-r border-sidebar-border bg-sidebar">
      {/* Brand */}
      <div className="flex items-center gap-3 px-5 py-5">
        <div className="grid h-9 w-9 place-items-center rounded-xl bg-primary text-sm font-bold text-primary-foreground">
          E
        </div>
        <span className="text-[15px] font-semibold tracking-tight text-sidebar-foreground">
          AgentOS
        </span>
      </div>

      <div className="flex flex-1 flex-col gap-6 overflow-y-auto px-4 pb-4">
        {/* Main nav */}
        <nav className="flex flex-col gap-0.5">
          {[
            { href: "/", label: "Dashboard", icon: LayoutDashboard },
            { href: "/agents", label: "Agents", icon: Bot },
          ].map((item) => {
            const isActive =
              item.href === "/"
                ? pathname === "/"
                : pathname === item.href || pathname.startsWith(`${item.href}/`);
            const Icon = item.icon;
            return (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  "flex items-center gap-2.5 rounded-lg px-3 py-2 text-[13px] font-medium transition-colors",
                  isActive
                    ? "bg-sidebar-accent text-sidebar-foreground"
                    : "text-muted-foreground hover:bg-sidebar-accent/60 hover:text-sidebar-foreground",
                )}
              >
                <Icon className="h-4 w-4 shrink-0" />
                {item.label}
                {item.href === "/agents" && runningCount > 0 && (
                  <span className="ml-auto flex items-center gap-1.5 text-[11px] text-emerald-600">
                    <span className="h-1.5 w-1.5 rounded-full bg-emerald-500" />
                    {runningCount} live
                  </span>
                )}
              </Link>
            );
          })}

          {/* Knowledge Graph placeholder */}
          <div className="flex items-center justify-between gap-2.5 rounded-lg px-3 py-2 text-[13px] font-medium text-muted-foreground/50 cursor-not-allowed">
            <div className="flex items-center gap-2.5">
              <Network className="h-4 w-4 shrink-0" />
              Knowledge Graph
            </div>
            <span className="rounded border border-border px-1.5 py-0.5 text-[10px] text-muted-foreground/50">
              Soon
            </span>
          </div>
        </nav>

        {/* Active agents list */}
        {agents.length > 0 && (
          <div>
            <div className="mb-2 px-3 text-[11px] font-semibold uppercase tracking-widest text-muted-foreground/70">
              Active Agents
            </div>
            <nav className="flex flex-col gap-0.5">
              {agents.slice(0, 8).map((agent: Agent) => {
                const isActive = pathname === `/agents/${agent.id}`;
                return (
                  <Link
                    key={agent.id}
                    href={`/agents/${agent.id}`}
                    className={cn(
                      "flex items-center gap-2.5 rounded-lg px-3 py-1.5 text-[13px] transition-colors",
                      isActive
                        ? "bg-sidebar-accent font-medium text-sidebar-foreground"
                        : "text-muted-foreground hover:bg-sidebar-accent/60 hover:text-sidebar-foreground",
                    )}
                  >
                    <span className={cn("h-2 w-2 shrink-0 rounded-full", statusDot(agent.status))} />
                    <span className="truncate">{agent.name}</span>
                  </Link>
                );
              })}
              {agents.length > 8 && (
                <Link
                  href="/agents"
                  className="px-3 py-1 text-[11px] text-muted-foreground hover:text-sidebar-foreground"
                >
                  +{agents.length - 8} more
                </Link>
              )}
            </nav>
          </div>
        )}
      </div>

      {/* Bottom section */}
      <div className="mt-auto border-t border-sidebar-border px-4 py-3">
        <Link
          href="/pricing"
          className={cn(
            "mb-1 flex items-center gap-2.5 rounded-lg px-3 py-2 text-[13px] font-medium transition-colors",
            pathname === "/pricing"
              ? "bg-sidebar-accent text-sidebar-foreground"
              : "text-muted-foreground hover:bg-sidebar-accent/60 hover:text-sidebar-foreground",
          )}
        >
          <CreditCard className="h-4 w-4 shrink-0" />
          Pricing
        </Link>
        <button
          type="button"
          onClick={onOpenOrgSettings}
          className="mb-2 flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-[13px] font-medium text-muted-foreground transition-colors hover:bg-sidebar-accent/60 hover:text-sidebar-foreground"
        >
          <Settings className="h-4 w-4 shrink-0" />
          Org Settings
          <ChevronRight className="ml-auto h-3.5 w-3.5" />
        </button>

        {/* User card */}
        <div className="flex items-center gap-2.5 rounded-lg px-3 py-2">
          {user?.avatarUrl ? (
            <img
              src={user.avatarUrl}
              alt=""
              className="h-8 w-8 rounded-full ring-1 ring-border"
            />
          ) : (
            <div className="grid h-8 w-8 place-items-center rounded-full bg-muted text-xs font-medium text-muted-foreground ring-1 ring-border">
              {(user?.name ?? "U").charAt(0).toUpperCase()}
            </div>
          )}
          <div className="flex-1 truncate">
            <div className="truncate text-[13px] font-medium text-sidebar-foreground">
              {user?.name ?? "Local dev"}
            </div>
            <div className="truncate text-[11px] text-muted-foreground">
              {user?.email ?? "single-tenant"}
            </div>
          </div>
          {user && (
            <button
              onClick={logout}
              className="rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-sidebar-accent hover:text-sidebar-foreground"
              title="Sign out"
            >
              <LogOut className="h-3.5 w-3.5" />
            </button>
          )}
        </div>
      </div>
    </aside>
  );
}

function OrgSettingsSidebar({ onBack }: { onBack: () => void }) {
  const pathname = usePathname();

  const orgNavItems = [
    { href: "/settings/profile", label: "Profile", icon: User },
    { href: "/settings/organization", label: "Organization", icon: Building2 },
    { href: "/providers", label: "Providers", icon: KeyRound },
    { href: "/skills", label: "Tools", icon: Wrench },
    { href: "/runners", label: "Runners", icon: Radio },
    { href: "/settings/billing", label: "Billing", icon: CreditCard },
    { href: "/settings/limits", label: "Limits", icon: Gauge, placeholder: true },
    { href: "/settings/api-keys", label: "API Keys", icon: Key, placeholder: true },
  ];

  return (
    <aside className="flex h-full w-[260px] shrink-0 flex-col border-r border-sidebar-border bg-sidebar">
      {/* Header */}
      <div className="flex items-center gap-3 px-5 py-5">
        <div className="grid h-9 w-9 place-items-center rounded-xl bg-primary text-sm font-bold text-primary-foreground">
          E
        </div>
        <span className="text-[15px] font-semibold tracking-tight text-sidebar-foreground">
          Org Settings
        </span>
      </div>

      <div className="flex flex-1 flex-col overflow-y-auto px-4 pb-4">
        {/* Back button */}
        <button
          type="button"
          onClick={onBack}
          className="mb-4 flex items-center gap-2 rounded-lg px-3 py-2 text-[13px] font-medium text-muted-foreground transition-colors hover:bg-sidebar-accent/60 hover:text-sidebar-foreground"
        >
          <ArrowLeft className="h-4 w-4 shrink-0" />
          Back to main app
        </button>

        <nav className="flex flex-col gap-0.5">
          {orgNavItems.map((item) => {
            const isActive =
              pathname === item.href || pathname.startsWith(`${item.href}/`);
            const Icon = item.icon;
            return (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  "flex items-center gap-2.5 rounded-lg px-3 py-2 text-[13px] font-medium transition-colors",
                  item.placeholder
                    ? "cursor-not-allowed text-muted-foreground/50 pointer-events-none"
                    : isActive
                      ? "bg-sidebar-accent text-sidebar-foreground"
                      : "text-muted-foreground hover:bg-sidebar-accent/60 hover:text-sidebar-foreground",
                )}
                aria-disabled={item.placeholder}
                tabIndex={item.placeholder ? -1 : undefined}
              >
                <Icon className="h-4 w-4 shrink-0" />
                {item.label}
                {item.placeholder && (
                  <span className="ml-auto rounded border border-border px-1.5 py-0.5 text-[10px] text-muted-foreground/50">
                    Soon
                  </span>
                )}
              </Link>
            );
          })}
        </nav>
      </div>

      <div className="border-t border-sidebar-border px-4 py-3">
        <a
          href="https://docs.harrokrog.com"
          target="_blank"
          rel="noopener noreferrer"
          className="flex items-center gap-2.5 rounded-lg px-3 py-2 text-[13px] font-medium text-muted-foreground transition-colors hover:bg-sidebar-accent/60 hover:text-sidebar-foreground"
        >
          <BookOpen className="h-4 w-4 shrink-0" />
          Documentation
        </a>
      </div>
    </aside>
  );
}

export function Sidebar() {
  const [showOrgSettings, setShowOrgSettings] = useState(false);

  return (
    <div className="relative h-full w-[260px] shrink-0 overflow-hidden">
      <div
        className={cn(
          "absolute inset-0 transition-transform duration-300 ease-in-out",
          showOrgSettings ? "-translate-x-full" : "translate-x-0",
        )}
      >
        <MainSidebar onOpenOrgSettings={() => setShowOrgSettings(true)} />
      </div>
      <div
        className={cn(
          "absolute inset-0 transition-transform duration-300 ease-in-out",
          showOrgSettings ? "translate-x-0" : "translate-x-full",
        )}
      >
        <OrgSettingsSidebar onBack={() => setShowOrgSettings(false)} />
      </div>
    </div>
  );
}
