"use client";

import Image from "next/image";
import Link from "next/link";
import { useSidebar } from "@/components/ui/sidebar";

export function TeamSwitcher() {
  const { state } = useSidebar();
  const collapsed = state === "collapsed";

  return (
    <Link
      href="/agents"
      aria-label="Go to agents"
      className={`flex items-center gap-2 rounded-md transition-colors hover:bg-muted/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 ${collapsed ? "justify-center py-3" : "px-4 pt-5 pb-4"}`}
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
