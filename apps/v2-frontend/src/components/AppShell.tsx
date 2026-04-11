"use client";

import { usePathname } from "next/navigation";
import { Sidebar } from "./Sidebar";
import { AuthGuard } from "./AuthGuard";
import type { ReactNode } from "react";

export function AppShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const isDocs = pathname.startsWith("/docs");
  const isLogin = pathname === "/login";

  if (isLogin) {
    return <>{children}</>;
  }

  return (
    <AuthGuard>
      <div className="flex h-screen w-screen overflow-hidden bg-[var(--eaos-bg)] text-[var(--eaos-text)]">
        {!isDocs && <Sidebar />}
        <main className="flex-1 overflow-y-auto">{children}</main>
      </div>
    </AuthGuard>
  );
}
