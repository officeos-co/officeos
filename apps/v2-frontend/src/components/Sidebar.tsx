"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const navItems = [
  { href: "/providers", label: "Providers" },
  { href: "/skills", label: "Skills" },
  { href: "/agents", label: "Agents" },
];

const secondaryItems = [
  { href: "/docs", label: "Documentation" },
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="flex h-full w-[260px] shrink-0 flex-col border-r border-[var(--eaos-border)] bg-[var(--eaos-sidebar)] px-3 py-4">
      <div className="mb-6 flex items-center gap-2 px-2">
        <div className="h-7 w-7 rounded-md bg-white text-black grid place-items-center text-xs font-bold">
          E
        </div>
        <span className="text-sm font-semibold">EnterpriseAgentOS</span>
      </div>

      <nav className="flex flex-col gap-1">
        <div className="px-2 pb-1 text-[11px] uppercase tracking-wider text-[var(--eaos-text-muted)]">
          Workspace
        </div>
        {navItems.map((item) => {
          const isActive =
            pathname === item.href || pathname.startsWith(`${item.href}/`);
          return (
            <Link
              key={item.href}
              href={item.href}
              className={[
                "rounded-md px-3 py-2 text-sm transition-colors",
                isActive
                  ? "bg-black text-white"
                  : "text-[var(--eaos-text-muted)] hover:bg-[var(--eaos-panel)] hover:text-white",
              ].join(" ")}
            >
              {item.label}
            </Link>
          );
        })}
        <div className="mt-4 px-2 pb-1 text-[11px] uppercase tracking-wider text-[var(--eaos-text-muted)]">
          Resources
        </div>
        {secondaryItems.map((item) => {
          const isActive =
            pathname === item.href || pathname.startsWith(`${item.href}/`);
          return (
            <Link
              key={item.href}
              href={item.href}
              className={[
                "rounded-md px-3 py-2 text-sm transition-colors",
                isActive
                  ? "bg-black text-white"
                  : "text-[var(--eaos-text-muted)] hover:bg-[var(--eaos-panel)] hover:text-white",
              ].join(" ")}
            >
              {item.label}
            </Link>
          );
        })}
      </nav>

      <div className="mt-auto flex items-center gap-2 rounded-md border border-[var(--eaos-border)] bg-[var(--eaos-panel)] px-3 py-2">
        <div className="h-6 w-6 rounded-full bg-gradient-to-br from-fuchsia-500 to-indigo-500" />
        <div className="text-xs">
          <div className="font-medium">Local dev</div>
          <div className="text-[var(--eaos-text-muted)]">single-tenant</div>
        </div>
      </div>
    </aside>
  );
}
