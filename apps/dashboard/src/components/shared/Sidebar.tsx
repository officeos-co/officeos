"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import posthog from "posthog-js";
import { useAuth } from "@/hooks/useAuth";
import { useAgents, type Agent } from "@/hooks/useAgents";
import { cn } from "@/lib/utils";
import {
  Bot,
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
  Sparkles,
  Plug,
  Bell,
} from "lucide-react";
import { useState, useRef, useEffect } from "react";
import { useSystemEvents } from "@/hooks/useSystemEvents";

function statusDot(status: string) {
  if (status === "running" || status === "ready" || status === "online")
    return "bg-emerald-500";
  if (status === "pending" || status === "building") return "bg-amber-400";
  if (status === "failed" || status === "not_found" || status === "offline")
    return "bg-red-400";
  return "bg-muted-foreground/40";
}

function ProfileMenu({
  open,
  onClose,
  onOpenOrgSettings,
}: {
  open: boolean;
  onClose: () => void;
  onOpenOrgSettings: () => void;
}) {
  const { logout } = useAuth();
  const router = useRouter();
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    function handleClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) onClose();
    }
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div
      ref={ref}
      className="absolute bottom-full left-0 right-0 mb-2 mx-3 rounded-xl border border-border bg-popover shadow-lg py-1.5 z-50"
    >
      <button
        type="button"
        onClick={() => {
          onClose();
          router.push("/pricing");
        }}
        className="flex w-full items-center gap-2.5 px-3 py-2 text-[13px] font-medium text-sidebar-foreground transition-colors hover:bg-sidebar-accent/60"
      >
        <Sparkles className="h-4 w-4 shrink-0 text-primary" />
        Upgrade plan
      </button>
      <button
        type="button"
        onClick={() => {
          onClose();
          onOpenOrgSettings();
        }}
        className="flex w-full items-center gap-2.5 px-3 py-2 text-[13px] font-medium text-muted-foreground transition-colors hover:bg-sidebar-accent/60 hover:text-sidebar-foreground"
      >
        <Settings className="h-4 w-4 shrink-0" />
        Org Settings
      </button>
      <div className="mx-3 my-1 border-t border-border" />
      <button
        type="button"
        onClick={() => {
          onClose();
          logout();
        }}
        className="flex w-full items-center gap-2.5 px-3 py-2 text-[13px] font-medium text-muted-foreground transition-colors hover:bg-sidebar-accent/60 hover:text-sidebar-foreground"
      >
        <LogOut className="h-4 w-4 shrink-0" />
        Sign out
      </button>
    </div>
  );
}

function MainSidebar({
  onOpenOrgSettings,
}: {
  onOpenOrgSettings: () => void;
}) {
  const pathname = usePathname();
  const { user } = useAuth();
  const { agents } = useAgents();
  const [profileMenuOpen, setProfileMenuOpen] = useState(false);

  const { unacknowledgedCount: errorCount } = useSystemEvents(50);

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
            { href: "/skills", label: "Tools", icon: Wrench },
            { href: "/system-events", label: "Events", icon: Bell },
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
                onClick={() => posthog.capture("nav_clicked", { destination: item.href })}
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
                {item.href === "/system-events" && errorCount > 0 && (
                  <span className="ml-auto flex h-5 min-w-5 items-center justify-center rounded-full bg-red-500 px-1.5 text-[10px] font-semibold text-white">
                    {errorCount > 99 ? "99+" : errorCount}
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
        <a
          href="https://status.harrokrog.com"
          target="_blank"
          rel="noopener noreferrer"
          className="mb-2 flex items-center gap-2.5 rounded-lg px-3 py-2 text-[13px] font-medium text-muted-foreground transition-colors hover:bg-sidebar-accent/60 hover:text-sidebar-foreground"
        >
          <span className="relative flex h-4 w-4 items-center justify-center">
            <span className="absolute h-2 w-2 rounded-full bg-emerald-500" />
            <span className="absolute h-2 w-2 animate-ping rounded-full bg-emerald-500 opacity-75" />
          </span>
          System Status
        </a>
        <a
          href="https://docs.harrokrog.com"
          target="_blank"
          rel="noopener noreferrer"
          className="mb-2 flex items-center gap-2.5 rounded-lg px-3 py-2 text-[13px] font-medium text-muted-foreground transition-colors hover:bg-sidebar-accent/60 hover:text-sidebar-foreground"
        >
          <BookOpen className="h-4 w-4 shrink-0" />
          Documentation
        </a>

        {/* User card — clickable, opens profile menu */}
        <div className="relative">
          <ProfileMenu
            open={profileMenuOpen}
            onClose={() => setProfileMenuOpen(false)}
            onOpenOrgSettings={onOpenOrgSettings}
          />
          <button
            type="button"
            onClick={() => setProfileMenuOpen((v) => !v)}
            className="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 transition-colors hover:bg-sidebar-accent/60"
          >
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
            <div className="flex-1 truncate text-left">
              <div className="truncate text-[13px] font-medium text-sidebar-foreground">
                {user?.name ?? "Local dev"}
              </div>
              <div className="truncate text-[11px] text-muted-foreground">
                {user?.email ?? "single-tenant"}
              </div>
            </div>
          </button>
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
    { href: "/runners", label: "Runners", icon: Radio },
    { href: "/settings/channels", label: "Channels", icon: Plug },
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
          href="https://status.harrokrog.com"
          target="_blank"
          rel="noopener noreferrer"
          className="mb-2 flex items-center gap-2.5 rounded-lg px-3 py-2 text-[13px] font-medium text-muted-foreground transition-colors hover:bg-sidebar-accent/60 hover:text-sidebar-foreground"
        >
          <span className="relative flex h-4 w-4 items-center justify-center">
            <span className="absolute h-2 w-2 rounded-full bg-emerald-500" />
            <span className="absolute h-2 w-2 animate-ping rounded-full bg-emerald-500 opacity-75" />
          </span>
          System Status
        </a>
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
        <MainSidebar onOpenOrgSettings={() => {
          posthog.capture("settings_opened");
          setShowOrgSettings(true);
        }} />
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
