import { source } from "@/lib/source";
import { DocsLayout } from "fumadocs-ui/layouts/docs";
import type { CSSProperties, ReactNode } from "react";
import { docsSidebar } from "./docs-sidebar";

const docsLayoutStyle = {
  gridTemplate: `"sidebar header toc"
"sidebar toc-popover toc"
"sidebar main toc" 1fr / var(--fd-sidebar-col) minmax(0, 1fr) var(--fd-toc-width)`,
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
