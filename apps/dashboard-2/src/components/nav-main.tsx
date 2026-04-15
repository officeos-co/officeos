"use client"

import { useState, useEffect } from "react"
import Link from "next/link"
import { usePathname } from "next/navigation"
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible"
import {
  SidebarGroup,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
} from "@/components/ui/sidebar"
import { ChevronRightIcon } from "lucide-react"

export function NavMain({
  items,
}: {
  items: {
    title: string
    url: string
    icon?: React.ReactNode
    isActive?: boolean
    items?: {
      title: string
      url: string
    }[]
  }[]
}) {
  const pathname = usePathname()

  // Track which groups are open — auto-open the one containing the active route
  const [openGroups, setOpenGroups] = useState<Set<string>>(() => {
    const initial = new Set<string>()
    for (const item of items) {
      const hasActive = item.items?.some(
        (sub) => pathname === sub.url || pathname.startsWith(sub.url + "/"),
      )
      if (hasActive || item.isActive) initial.add(item.title)
    }
    return initial
  })

  // When pathname changes, auto-open the group containing the new route
  useEffect(() => {
    for (const item of items) {
      const hasActive = item.items?.some(
        (sub) => pathname === sub.url || pathname.startsWith(sub.url + "/"),
      )
      if (hasActive) {
        setOpenGroups((prev) => {
          if (prev.has(item.title)) return prev
          return new Set([...prev, item.title])
        })
      }
    }
  }, [pathname, items])

  function toggleGroup(title: string) {
    setOpenGroups((prev) => {
      const next = new Set(prev)
      if (next.has(title)) next.delete(title)
      else next.add(title)
      return next
    })
  }

  return (
    <SidebarGroup>
      <SidebarMenu>
        {items.map((item) => {
          const isOpen = openGroups.has(item.title)
          return (
            <Collapsible
              key={item.title}
              open={isOpen}
              onOpenChange={() => toggleGroup(item.title)}
              className="group/collapsible"
              render={<SidebarMenuItem />}
            >
              <CollapsibleTrigger
                render={<SidebarMenuButton tooltip={item.title} />}
              >
                {item.icon}
                <span>{item.title}</span>
                <ChevronRightIcon className="ml-auto transition-transform duration-200 group-data-open/collapsible:rotate-90" />
              </CollapsibleTrigger>
              <CollapsibleContent>
                <SidebarMenuSub>
                  {item.items?.map((subItem) => {
                    const isActive =
                      pathname === subItem.url ||
                      pathname.startsWith(subItem.url + "/")
                    return (
                      <SidebarMenuSubItem key={subItem.title}>
                        <SidebarMenuSubButton
                          isActive={isActive}
                          render={<Link href={subItem.url} />}
                        >
                          <span>{subItem.title}</span>
                        </SidebarMenuSubButton>
                      </SidebarMenuSubItem>
                    )
                  })}
                </SidebarMenuSub>
              </CollapsibleContent>
            </Collapsible>
          )
        })}
      </SidebarMenu>
    </SidebarGroup>
  )
}
