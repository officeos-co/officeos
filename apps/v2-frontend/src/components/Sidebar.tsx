"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { cn } from "@/lib/utils";
import { Bot, KeyRound, Puzzle, Radio, BookOpen, LogOut } from "lucide-react";

const navItems = [
  { href: "/providers", label: "Providers", icon: KeyRound },
  { href: "/skills", label: "Skills", icon: Puzzle },
  { href: "/agents", label: "Agents", icon: Bot },
  { href: "/runners", label: "Runners", icon: Radio },
];

export function Sidebar() {
  const pathname = usePathname();
  const { user, logout } = useAuth();

  return (
    <aside className="flex h-full w-[240px] shrink-0 flex-col border-r border-sidebar-border bg-sidebar px-3 py-4">
      {/* Logo */}
      <div className="mb-6 flex items-center gap-2.5 px-2">
        <div className="grid h-7 w-7 place-items-center rounded-lg bg-primary text-xs font-bold text-primary-foreground">
          E
        </div>
        <span className="text-sm font-semibold tracking-tight text-sidebar-foreground">
          EnterpriseAgentOS
        </span>
      </div>

      {/* Main nav */}
      <nav className="flex flex-col gap-0.5">
        <span className="mb-1 px-2 text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
          Workspace
        </span>
        {navItems.map((item) => {
          const isActive =
            pathname === item.href || pathname.startsWith(`${item.href}/`);
          const Icon = item.icon;
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-2.5 rounded-md px-2.5 py-1.5 text-[13px] font-medium transition-colors",
                isActive
                  ? "bg-sidebar-accent text-sidebar-foreground"
                  : "text-muted-foreground hover:bg-sidebar-accent/50 hover:text-sidebar-foreground"
              )}
            >
              <Icon className="h-4 w-4 shrink-0" />
              {item.label}
            </Link>
          );
        })}
      </nav>

      {/* Bottom section */}
      <div className="mt-auto flex flex-col gap-1">
        <Link
          href="/docs"
          target="_blank"
          className="flex items-center gap-2.5 rounded-md px-2.5 py-1.5 text-[13px] font-medium text-muted-foreground transition-colors hover:bg-sidebar-accent/50 hover:text-sidebar-foreground"
        >
          <BookOpen className="h-4 w-4 shrink-0" />
          Documentation
        </Link>

        {/* User card */}
        <div className="mt-1 flex items-center gap-2.5 rounded-lg border border-sidebar-border bg-sidebar-accent/30 px-2.5 py-2">
          {user?.avatarUrl ? (
            <img
              src={user.avatarUrl}
              alt=""
              className="h-7 w-7 rounded-full ring-1 ring-sidebar-border"
            />
          ) : (
            <div className="h-7 w-7 rounded-full bg-gradient-to-br from-primary/80 to-primary/40 ring-1 ring-sidebar-border" />
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
              className="rounded-md p-1 text-muted-foreground transition-colors hover:bg-sidebar-accent hover:text-sidebar-foreground"
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
