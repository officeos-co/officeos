"use client";

import { AppSidebar } from "@/shell/app-sidebar";
import { SidebarInset, SidebarProvider } from "@/ui/sidebar";
import { AnalyticsPageview } from "@/shell/analytics-pageview";
import { AuthGuard } from "@/shell/auth-guard";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AuthGuard>
      <SidebarProvider>
        <AnalyticsPageview />
        <AppSidebar />
        <SidebarInset>
          <div className="mx-auto min-h-svh w-full max-w-[1600px]">
            {children}
          </div>
        </SidebarInset>
      </SidebarProvider>
    </AuthGuard>
  );
}
