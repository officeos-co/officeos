import Link from "next/link";
import type { DocEntry } from "@/lib/docs";

type Props = {
  docs: DocEntry[];
  activeSlug: string;
};

export function DocSidebar({ docs, activeSlug }: Props) {
  return (
    <aside className="hidden w-[220px] shrink-0 border-r border-[var(--eaos-border)] bg-[var(--eaos-sidebar)] lg:block">
      <div className="px-4 pt-6 pb-2 text-[11px] uppercase tracking-wider text-[var(--eaos-text-muted)]">
        Documentation
      </div>
      <nav className="flex flex-col gap-0.5 px-2 pb-6">
        {docs.map((doc) => (
          <Link
            key={doc.slug}
            href={`/docs/${doc.slug}`}
            className={[
              "rounded-md px-3 py-2 text-sm transition-colors",
              doc.slug === activeSlug
                ? "bg-black text-white"
                : "text-[var(--eaos-text-muted)] hover:bg-[var(--eaos-panel)] hover:text-white",
            ].join(" ")}
          >
            {doc.title}
          </Link>
        ))}
      </nav>
    </aside>
  );
}
