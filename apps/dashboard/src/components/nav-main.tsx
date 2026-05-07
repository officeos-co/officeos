"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import {
  SidebarGroup,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  useSidebar,
} from "@/components/ui/sidebar";
import { ChevronRightIcon } from "lucide-react";
import { useAnalytics } from "@/features/analytics";

export function NavMain({
  items,
}: {
  items: {
    title: string;
    url: string;
    icon?: React.ReactNode;
    isActive?: boolean;
    items?: {
      title: string;
      url: string;
    }[];
  }[];
}) {
  const pathname = usePathname();
  const { trackNavClicked } = useAnalytics();
  const { state } = useSidebar();
  const collapsed = state === "collapsed";

  // All groups open by default
  const [openGroups, setOpenGroups] = useState<Set<string>>(
    () => new Set(items.map((item) => item.title)),
  );

  function isRouteActive(
    url: string,
    siblings: { title: string; url: string }[] = [],
  ) {
    if (pathname === url) return true;

    const hasMoreSpecificSibling = siblings.some(
      (sibling) => sibling.url !== url && sibling.url.startsWith(url + "/"),
    );

    return !hasMoreSpecificSibling && pathname.startsWith(url + "/");
  }

  function toggleGroup(title: string) {
    setOpenGroups((prev) => {
      const next = new Set(prev);
      if (next.has(title)) next.delete(title);
      else next.add(title);
      return next;
    });
  }

  return (
    <SidebarGroup className="gap-1 px-3 pt-2">
      <SidebarMenu className="gap-0">
        {items.map((item) => {
          const hasActiveChild = item.items?.some((sub) =>
            isRouteActive(sub.url, item.items),
          );
          const isOpen = openGroups.has(item.title);

          if (collapsed) {
            return (
              <SidebarMenuItem key={item.title} className="group/collapsed relative">
                <SidebarMenuButton
                  className={`h-10 px-3 text-sm font-medium [&_svg]:size-4 [&_svg]:stroke-[1.5] ${hasActiveChild ? "bg-sidebar-primary/10 text-sidebar-primary" : ""}`}
                >
                  {item.icon}
                </SidebarMenuButton>
                <div className="pointer-events-none invisible absolute left-full top-0 z-[9999] ml-1 min-w-[180px] rounded-lg border border-sidebar-border bg-sidebar p-2 shadow-xl group-hover/collapsed:pointer-events-auto group-hover/collapsed:visible">
                  <p className="mb-1.5 px-2 text-xs font-semibold text-sidebar-foreground/70">
                    {item.title}
                  </p>
                  {item.items?.map((subItem) => {
                    const isActive = isRouteActive(subItem.url, item.items);
                    return (
                      <Link
                        key={subItem.title}
                        href={subItem.url}
                        onClick={() => trackNavClicked(subItem.url)}
                        className={`block rounded-lg px-2 py-1.5 text-sm text-sidebar-foreground/80 hover:bg-sidebar-accent/50 hover:text-sidebar-accent-foreground ${isActive ? "bg-sidebar-primary/10 font-medium text-sidebar-primary" : ""}`}
                      >
                        {subItem.title}
                      </Link>
                    );
                  })}
                </div>
              </SidebarMenuItem>
            );
          }

          return (
            <Collapsible
              key={item.title}
              open={isOpen}
              onOpenChange={() => toggleGroup(item.title)}
              className="group/collapsible flex flex-col"
              render={<SidebarMenuItem />}
            >
              <CollapsibleTrigger
                render={
                  <SidebarMenuButton
                    tooltip={item.title}
                    className="h-10 px-3 text-sm font-medium [&_svg]:size-4 [&_svg]:stroke-[1.5]"
                  />
                }
              >
                {item.icon}
                <span>{item.title}</span>
                <ChevronRightIcon className="ml-auto !size-3.5 !text-sidebar-foreground/60 transition-transform duration-200 group-data-open/collapsible:rotate-90" />
              </CollapsibleTrigger>
              <CollapsibleContent>
                <SidebarMenuSub className="gap-0.5">
                  {item.items?.map((subItem) => {
                    const isActive = isRouteActive(subItem.url, item.items);
                    return (
                      <SidebarMenuSubItem key={subItem.title}>
                        <SidebarMenuSubButton
                          isActive={isActive}
                          render={<Link href={subItem.url} />}
                          onClick={() => trackNavClicked(subItem.url)}
                          className="h-9 pl-9 text-sm data-active:font-medium"
                        >
                          <span>{subItem.title}</span>
                        </SidebarMenuSubButton>
                      </SidebarMenuSubItem>
                    );
                  })}
                </SidebarMenuSub>
              </CollapsibleContent>
            </Collapsible>
          );
        })}
      </SidebarMenu>
    </SidebarGroup>
  );
}
