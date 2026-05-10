import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { Skeleton } from "@/ui/skeleton";
import { PROSE_CLASSES } from "./integration-prose";

interface Tool {
  name: string;
  description: string;
}

interface IntegrationDetailTabProps {
  tools: Tool[];
  doc: string | null;
  loading?: boolean;
}

export function IntegrationDetailTab({
  tools,
  doc,
  loading = false,
}: IntegrationDetailTabProps) {
  return (
    <div className="space-y-6">
      {/* Tools card */}
      <div className="rounded-xl border border-border bg-card">
        <div className="px-4 py-3 border-b border-border">
          <span className="text-sm font-medium">Tools</span>
          <span className="ml-2 text-xs text-muted-foreground">
            {loading ? " " : tools.length}
          </span>
        </div>
        {loading
          ? Array.from({ length: 20 }).map((_, index) => (
              <div
                key={`tool-skeleton-${index}`}
                className="flex items-center gap-4 border-b border-border px-4 py-3 last:border-b-0"
                aria-hidden="true"
              >
                <Skeleton className="h-6 w-28" />
                <Skeleton
                  className={
                    index % 3 === 0
                      ? "h-4 flex-1"
                      : index % 3 === 1
                        ? "h-4 w-2/3"
                        : "h-4 w-1/2"
                  }
                />
              </div>
            ))
          : tools.map((tool, i) => (
              <div
                key={tool.name}
                className={`flex items-center gap-4 px-4 py-3${
                  i < tools.length - 1 ? " border-b border-border" : ""
                }`}
              >
                <code className="rounded bg-muted px-2 py-1 font-mono text-xs">
                  {tool.name}
                </code>
                <span className="text-sm text-muted-foreground">
                  {tool.description}
                </span>
              </div>
            ))}
      </div>

      {/* Documentation */}
      {loading ? (
        <div className="rounded-xl border border-border bg-card">
          <div className="px-4 py-3 border-b border-border">
            <span className="text-sm font-medium">Documentation</span>
          </div>
          <div className="space-y-3 p-6" aria-hidden="true">
            <Skeleton className="h-5 w-40" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-5/6" />
            <Skeleton className="h-4 w-3/4" />
          </div>
        </div>
      ) : doc ? (
        <div className="rounded-xl border border-border bg-card">
          <div className="px-4 py-3 border-b border-border">
            <span className="text-sm font-medium">Documentation</span>
          </div>
          <div className="p-6">
            <div className={PROSE_CLASSES}>
              <ReactMarkdown remarkPlugins={[remarkGfm]}>
                {doc}
              </ReactMarkdown>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
