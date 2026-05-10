"use client";

import Image from "next/image";
import Link from "next/link";
import { useSidebar } from "@/ui/sidebar";

export function TeamSwitcher() {
  const { state } = useSidebar();
  const collapsed = state === "collapsed";

  return (
    <Link
      href="/"
      aria-label="Go to overview"
      className={`flex items-center gap-2 rounded-lg text-sidebar-foreground transition-colors hover:bg-sidebar-accent/50 hover:text-sidebar-accent-foreground/80 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sidebar-ring focus-visible:ring-offset-2 ${collapsed ? "justify-center py-2" : "px-3 py-2"}`}
    >
      <Image
        src="/icon-512.png"
        alt="OfficeOS"
        width={collapsed ? 24 : 22}
        height={collapsed ? 24 : 22}
        className="shrink-0"
      />
      {!collapsed && (
        <span className="text-lg font-semibold tracking-tight">OfficeOS</span>
      )}
    </Link>
  );
}
