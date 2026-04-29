"use client"

import {
  Avatar,
  AvatarFallback,
  AvatarImage,
} from "@/components/ui/avatar"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  useSidebar,
} from "@/components/ui/sidebar"
import {
  ChevronsUpDownIcon,
  LogOutIcon,
  HelpCircleIcon,
  ExternalLinkIcon,
  ScaleIcon,
  SparklesIcon,
} from "lucide-react"
import { useAuthContext } from "@/contexts/AuthContext"
import { isDevelopment } from "@/lib/env"

function Initials(name: string) {
  return name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .toUpperCase()
    .slice(0, 2)
}

export function NavUser({
  user,
}: {
  user: {
    name: string
    plan: string
    avatar: string
  }
}) {
  const { isMobile } = useSidebar()
  const { logout } = useAuthContext()

  return (
    <SidebarMenu>
      <SidebarMenuItem>
        <DropdownMenu>
          <DropdownMenuTrigger
            render={
              <SidebarMenuButton
                size="lg"
                className="aria-expanded:bg-muted"
                onClick={(e) => e.preventDefault()}
              />
            }
          >
            <Avatar className="h-8 w-8 rounded-lg">
              <AvatarImage src={user.avatar} alt={user.name} />
              <AvatarFallback className="rounded-lg">
                {Initials(user.name)}
              </AvatarFallback>
            </Avatar>
            <div className="grid flex-1 text-left text-sm leading-tight">
              <span className="truncate font-medium">{user.name}</span>
              <span className="truncate text-xs text-muted-foreground">{user.plan}</span>
            </div>
            <ChevronsUpDownIcon className="ml-auto size-4" />
          </DropdownMenuTrigger>
          <DropdownMenuContent
            className="min-w-56 rounded-lg"
            side={isMobile ? "bottom" : "right"}
            align="end"
            sideOffset={4}
          >
            <DropdownMenuGroup>
              <DropdownMenuLabel className="text-xs text-muted-foreground font-normal px-2">
                {user.name}
              </DropdownMenuLabel>
            </DropdownMenuGroup>
            <DropdownMenuSeparator />
            {!isDevelopment() && (
              <>
                <DropdownMenuGroup>
                  <DropdownMenuItem disabled>
                    <SparklesIcon />
                    Upgrade Plan — Coming Soon
                  </DropdownMenuItem>
                </DropdownMenuGroup>
                <DropdownMenuSeparator />
              </>
            )}
            <DropdownMenuGroup>
              <DropdownMenuItem onClick={() => window.open("https://www.officeos.co/support", "_blank")}>
                <HelpCircleIcon />
                Support
                <ExternalLinkIcon className="ml-auto size-3 text-muted-foreground" />
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => window.open("https://officeos.co/privacy", "_blank")}>
                <ScaleIcon />
                Privacy Policy
                <ExternalLinkIcon className="ml-auto size-3 text-muted-foreground" />
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => window.open("https://officeos.co/terms", "_blank")}>
                <ScaleIcon />
                Terms of Service
                <ExternalLinkIcon className="ml-auto size-3 text-muted-foreground" />
              </DropdownMenuItem>
            </DropdownMenuGroup>
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={() => logout()}>
              <LogOutIcon />
              Log out
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </SidebarMenuItem>
    </SidebarMenu>
  )
}
