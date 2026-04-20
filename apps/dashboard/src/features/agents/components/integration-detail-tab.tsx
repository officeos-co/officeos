import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { PROSE_CLASSES } from "./integration-prose";

interface Tool {
  name: string;
  description: string;
}

interface IntegrationDetailTabProps {
  tools: Tool[];
  doc: string | null;
}

export function IntegrationDetailTab({ tools, doc }: IntegrationDetailTabProps) {
  return (
    <div className="space-y-6">
      {/* Tools card */}
      <div className="rounded-xl border border-border bg-card">
        <div className="px-4 py-3 border-b border-border">
          <span className="text-sm font-medium">Tools</span>
          <span className="ml-2 text-xs text-muted-foreground">
            {tools.length}
          </span>
        </div>
        {tools.map((tool, i) => (
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
      {doc && (
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
      )}
    </div>
  );
}
