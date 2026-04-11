import Link from "next/link";
import { listDocs } from "@/lib/docs";

export default function DocsPage() {
  const docs = listDocs();

  return (
    <div>
      <div className="sticky top-0 z-10 border-b border-[var(--eaos-border)] bg-[var(--eaos-bg)]/90 px-8 py-6 backdrop-blur">
        <h1 className="text-2xl font-semibold">Documentation</h1>
        <p className="mt-1 text-sm text-[var(--eaos-text-muted)]">
          Architecture and system design for EnterpriseAgentOS.
        </p>
      </div>

      <div className="px-8 py-6">
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          {docs.map((doc) => (
            <Link
              key={doc.slug}
              href={`/docs/${doc.slug}`}
              className="flex flex-col rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)] p-5 transition-colors hover:bg-black/30"
            >
              <div className="text-sm font-semibold">{doc.title}</div>
              {doc.subtitle && (
                <p className="mt-2 line-clamp-2 text-xs text-[var(--eaos-text-muted)]">
                  {doc.subtitle}
                </p>
              )}
            </Link>
          ))}
        </div>
      </div>
    </div>
  );
}
