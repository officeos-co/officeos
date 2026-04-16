import { AppSidebar } from "@/components/app-sidebar";
import { SidebarInset, SidebarProvider } from "@/components/ui/sidebar";
import { AnalyticsPageview } from "@/components/analytics-pageview";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <SidebarProvider>
      <AnalyticsPageview />
      <AppSidebar />
      <SidebarInset>
        <div className="mx-auto w-full max-w-[1600px]">{children}</div>
      </SidebarInset>
    </SidebarProvider>
  );
}
