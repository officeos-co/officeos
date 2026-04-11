"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { cn } from "@/lib/utils";
import type { DocEntry } from "@/lib/docs";

type Props = {
  docs: DocEntry[];
};

export function DocsSidebar({ docs }: Props) {
  const pathname = usePathname();

  return (
    <aside className="flex h-full w-[240px] shrink-0 flex-col border-r border-sidebar-border bg-sidebar px-3 py-4">
      <div className="mb-2 flex items-center gap-2.5 px-2">
        <div className="grid h-7 w-7 place-items-center rounded-lg bg-primary text-xs font-bold text-primary-foreground">
          E
        </div>
        <span className="text-sm font-semibold tracking-tight text-sidebar-foreground">
          Docs
        </span>
      </div>

      <Link
        href="/"
        className="mb-4 flex items-center gap-2 rounded-md px-2.5 py-1.5 text-xs text-muted-foreground transition-colors hover:bg-sidebar-accent/50 hover:text-sidebar-foreground"
      >
        <ArrowLeft className="h-3 w-3" />
        Back to dashboard
      </Link>

      <nav className="flex flex-col gap-0.5">
        <span className="mb-1 px-2 text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
          Guides
        </span>
        {docs.map((doc) => {
          const href = `/docs/${doc.slug}`;
          const isActive = pathname === href;
          return (
            <Link
              key={doc.slug}
              href={href}
              className={cn(
                "rounded-md px-2.5 py-1.5 text-[13px] font-medium transition-colors",
                isActive
                  ? "bg-sidebar-accent text-sidebar-foreground"
                  : "text-muted-foreground hover:bg-sidebar-accent/50 hover:text-sidebar-foreground"
              )}
            >
              {doc.title}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
