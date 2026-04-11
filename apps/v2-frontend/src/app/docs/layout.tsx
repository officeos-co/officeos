import type { ReactNode } from "react";
import { listDocs } from "@/lib/docs";
import { DocsSidebar } from "@/components/DocsSidebar";

export default function DocsLayout({ children }: { children: ReactNode }) {
  const docs = listDocs();

  return (
    <div className="flex h-full">
      <DocsSidebar docs={docs} />
      <div className="flex-1 overflow-y-auto">{children}</div>
    </div>
  );
}
