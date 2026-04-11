import Link from "next/link";
import { listDocs } from "@/lib/docs";

export default function DocsPage() {
  const docs = listDocs();

  return (
    <>
      <div className="sticky top-0 z-10 border-b border-border bg-background/90 px-8 py-6 backdrop-blur">
        <h1 className="text-2xl font-semibold">Documentation</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Architecture and system design for EnterpriseAgentOS.
        </p>
      </div>

      <div className="px-8 py-6">
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          {docs.map((doc) => (
            <Link
              key={doc.slug}
              href={`/docs/${doc.slug}`}
              className="flex flex-col rounded-xl border border-border bg-card p-5 transition-colors hover:bg-muted"
            >
              <div className="text-sm font-semibold">{doc.title}</div>
              {doc.subtitle && (
                <p className="mt-2 line-clamp-2 text-xs text-muted-foreground">
                  {doc.subtitle}
                </p>
              )}
            </Link>
          ))}
        </div>
      </div>
    </>
  );
}
