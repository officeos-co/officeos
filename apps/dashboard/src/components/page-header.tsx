import Link from "next/link";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { SidebarTrigger } from "@/components/ui/sidebar";
import {
  getPageWidthClassName,
  type PageWidth,
} from "@/components/page-container";
import { cn } from "@/lib/utils";

const groupRoutes: Record<string, string> = {
  "Managed Agents": "/agents",
  Agents: "/agents",
  Analytics: "/logs",
  Manage: "/billing",
  Integrations: "/integrations",
  "MCP Servers": "/integrations",
  Channels: "/channels",
};

export function PageHeader({
  group,
  page,
  subtitle,
  action,
  width = "full",
  contentClassName,
}: {
  group?: string;
  page: string;
  subtitle?: string;
  action?: React.ReactNode;
  width?: PageWidth;
  contentClassName?: string;
}) {
  const groupHref = group ? (groupRoutes[group] ?? "#") : "#";
  const showBreadcrumb = group && !subtitle;

  return (
    <header className="flex shrink-0 items-center gap-2 py-4">
      <div
        className={getPageWidthClassName(
          width,
          cn("flex flex-1 items-start justify-between gap-4", contentClassName),
        )}
      >
        <SidebarTrigger className="md:hidden" />
        <div className="min-w-0">
          {showBreadcrumb && (
            <Breadcrumb className="mb-1">
              <BreadcrumbList>
                <BreadcrumbItem className="hidden md:block">
                  <BreadcrumbLink render={<Link href={groupHref} />}>
                    {group}
                  </BreadcrumbLink>
                </BreadcrumbItem>
                <BreadcrumbSeparator className="hidden md:block" />
                <BreadcrumbItem>
                  <BreadcrumbPage>{page}</BreadcrumbPage>
                </BreadcrumbItem>
              </BreadcrumbList>
            </Breadcrumb>
          )}
          <h1 className="truncate text-xl font-semibold tracking-tight">
            {page}
          </h1>
          {subtitle && (
            <p className="mt-1 text-sm text-muted-foreground">{subtitle}</p>
          )}
        </div>
        {action && <div className="ml-auto">{action}</div>}
      </div>
    </header>
  );
}
