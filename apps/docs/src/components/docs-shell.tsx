import { source } from "@/lib/source";
import { DocsLayout } from "fumadocs-ui/layouts/docs";
import type { CSSProperties, ReactNode } from "react";
import { docsSidebar } from "./docs-sidebar";

const docsLayoutStyle = {
  "--fd-layout-width": "118rem",
  gridTemplate: `"sidebar header toc ."
"sidebar toc-popover toc ."
"sidebar main toc ." 1fr / var(--fd-sidebar-col) minmax(0, calc(var(--fd-layout-width) - var(--fd-sidebar-width) - var(--fd-toc-width))) var(--fd-toc-width) minmax(0, 1fr)`,
} as CSSProperties;

export function DocsShell({ children }: { children: ReactNode }) {
  return (
    <DocsLayout
      tree={source.pageTree}
      slots={{ sidebar: docsSidebar }}
      sidebar={{ collapsible: false }}
      containerProps={{
        className: "docs-layout",
        style: docsLayoutStyle,
      }}
    >
      {children}
    </DocsLayout>
  );
}
