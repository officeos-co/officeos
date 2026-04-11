"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { DocEntry } from "@/lib/docs";

type Props = {
  docs: DocEntry[];
};

export function DocsSidebar({ docs }: Props) {
  const pathname = usePathname();

  return (
    <aside className="flex h-full w-[260px] shrink-0 flex-col border-r border-[var(--eaos-border)] bg-[var(--eaos-sidebar)] px-3 py-4">
      <div className="mb-2 flex items-center gap-2 px-2">
        <div className="h-7 w-7 rounded-md bg-white text-black grid place-items-center text-xs font-bold">
          E
        </div>
        <span className="text-sm font-semibold">Docs</span>
      </div>

      <Link
        href="/"
        className="mb-4 rounded-md px-3 py-1.5 text-xs text-[var(--eaos-text-muted)] transition-colors hover:bg-[var(--eaos-panel)] hover:text-white"
      >
        &larr; Back to dashboard
      </Link>

      <nav className="flex flex-col gap-0.5">
        <div className="px-2 pb-1 text-[11px] uppercase tracking-wider text-[var(--eaos-text-muted)]">
          Guides
        </div>
        {docs.map((doc) => {
          const href = `/docs/${doc.slug}`;
          const isActive = pathname === href;
          return (
            <Link
              key={doc.slug}
              href={href}
              className={[
                "rounded-md px-3 py-2 text-sm transition-colors",
                isActive
                  ? "bg-black text-white"
                  : "text-[var(--eaos-text-muted)] hover:bg-[var(--eaos-panel)] hover:text-white",
              ].join(" ")}
            >
              {doc.title}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
