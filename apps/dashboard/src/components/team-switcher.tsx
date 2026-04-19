"use client";

import Image from "next/image";
import { useSidebar } from "@/components/ui/sidebar";

export function TeamSwitcher() {
  const { state } = useSidebar();
  const collapsed = state === "collapsed";

  return (
    <div className={`flex items-center gap-2 ${collapsed ? "justify-center py-3" : "px-4 pt-5 pb-4"}`}>
      <Image
        src="/icon-512.png"
        alt="AgentOS"
        width={collapsed ? 24 : 22}
        height={collapsed ? 24 : 22}
        className="shrink-0"
      />
      {!collapsed && (
        <span className="text-lg font-semibold tracking-tight">AgentOS</span>
      )}
    </div>
  );
}
