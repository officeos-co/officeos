"use client";

import { useEffect, type ReactNode } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";

export function AuthGuard({ children }: { children: ReactNode }) {
  const { isAuthenticated, loading } = useAuth();
  const pathname = usePathname();
  const router = useRouter();

  useEffect(() => {
    if (!loading && !isAuthenticated && pathname !== "/login") {
      router.push("/login");
    }
  }, [loading, isAuthenticated, pathname, router]);

  if (pathname === "/login") return <>{children}</>;

  if (loading) {
    return (
      <div className="flex h-screen w-screen items-center justify-center bg-[var(--eaos-bg)] text-[var(--eaos-text-muted)]">
        <span className="text-sm">Loading...</span>
      </div>
    );
  }

  if (!isAuthenticated) return null;

  return <>{children}</>;
}
