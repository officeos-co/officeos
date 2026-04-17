import Link from "next/link"
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb"
import { Separator } from "@/components/ui/separator"
import { SidebarTrigger } from "@/components/ui/sidebar"

const groupRoutes: Record<string, string> = {
  "Managed Agents": "/agents",
  "Agents": "/agents",
  "Analytics": "/logs",
  "Manage": "/billing",
  "Integrations": "/integrations",
  "Channels": "/channels",
}

export function PageHeader({
  group,
  page,
  action,
}: {
  group?: string
  page: string
  action?: React.ReactNode
}) {
  const groupHref = group ? groupRoutes[group] ?? "#" : "#"

  return (
    <header className="flex h-16 shrink-0 items-center gap-2 transition-[width,height] ease-linear group-has-data-[collapsible=icon]/sidebar-wrapper:h-12">
      <div className="flex items-center gap-2 px-4 flex-1">
        <SidebarTrigger className="-ml-1" />
        <Separator
          orientation="vertical"
          className="mr-2 data-vertical:h-4 data-vertical:self-auto"
        />
        <Breadcrumb>
          <BreadcrumbList>
            {group && (
              <>
                <BreadcrumbItem className="hidden md:block">
                  <BreadcrumbLink render={<Link href={groupHref} />}>
                    {group}
                  </BreadcrumbLink>
                </BreadcrumbItem>
                <BreadcrumbSeparator className="hidden md:block" />
              </>
            )}
            <BreadcrumbItem>
              <BreadcrumbPage>{page}</BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>
        {action && <div className="ml-auto">{action}</div>}
      </div>
    </header>
  )
}
