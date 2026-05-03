"use client";

import {
  Sidebar,
  SidebarProvider,
  SidebarTrigger,
  useSidebar,
  type SidebarProps,
} from "fumadocs-ui/layouts/docs/slots/sidebar";

function DocsSidebar(props: SidebarProps) {
  return <Sidebar {...props} collapsible={false} />;
}

export const docsSidebar = {
  provider: SidebarProvider,
  root: DocsSidebar,
  trigger: SidebarTrigger,
  useSidebar,
};
