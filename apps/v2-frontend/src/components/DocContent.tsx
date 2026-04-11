"use client";

import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { Components } from "react-markdown";

const components: Components = {
  h1: ({ children }) => (
    <h1 className="mb-4 mt-8 text-2xl font-semibold first:mt-0">{children}</h1>
  ),
  h2: ({ children }) => (
    <h2 className="mb-3 mt-8 border-b border-[var(--eaos-border)] pb-2 text-lg font-semibold">
      {children}
    </h2>
  ),
  h3: ({ children }) => (
    <h3 className="mb-2 mt-6 text-base font-semibold">{children}</h3>
  ),
  h4: ({ children }) => (
    <h4 className="mb-2 mt-4 text-sm font-semibold">{children}</h4>
  ),
  p: ({ children }) => (
    <p className="mb-4 text-sm leading-relaxed text-[var(--eaos-text)]">{children}</p>
  ),
  ul: ({ children }) => (
    <ul className="mb-4 list-disc space-y-1 pl-6 text-sm text-[var(--eaos-text)]">{children}</ul>
  ),
  ol: ({ children }) => (
    <ol className="mb-4 list-decimal space-y-1 pl-6 text-sm text-[var(--eaos-text)]">{children}</ol>
  ),
  li: ({ children }) => <li className="leading-relaxed">{children}</li>,
  a: ({ href, children }) => (
    <a
      href={href}
      className="text-blue-400 underline decoration-blue-400/30 hover:decoration-blue-400"
      target={href?.startsWith("http") ? "_blank" : undefined}
      rel={href?.startsWith("http") ? "noopener noreferrer" : undefined}
    >
      {children}
    </a>
  ),
  blockquote: ({ children }) => (
    <blockquote className="mb-4 border-l-2 border-[var(--eaos-text-muted)] pl-4 text-sm italic text-[var(--eaos-text-muted)]">
      {children}
    </blockquote>
  ),
  code: ({ className, children }) => {
    const isBlock = className?.startsWith("language-");
    if (isBlock) {
      return (
        <code className={`text-xs ${className ?? ""}`}>{children}</code>
      );
    }
    return (
      <code className="rounded bg-[var(--eaos-panel)] border border-[var(--eaos-border)] px-1.5 py-0.5 font-mono text-xs text-emerald-400">
        {children}
      </code>
    );
  },
  pre: ({ children }) => (
    <pre className="mb-4 overflow-x-auto rounded-lg border border-[var(--eaos-border)] bg-[var(--eaos-sidebar)] p-4 font-mono text-xs leading-relaxed">
      {children}
    </pre>
  ),
  table: ({ children }) => (
    <div className="mb-4 overflow-x-auto">
      <table className="w-full border-collapse text-sm">{children}</table>
    </div>
  ),
  thead: ({ children }) => (
    <thead className="border-b border-[var(--eaos-border)] text-left text-[11px] uppercase tracking-wider text-[var(--eaos-text-muted)]">
      {children}
    </thead>
  ),
  th: ({ children }) => <th className="px-3 py-2 font-medium">{children}</th>,
  td: ({ children }) => (
    <td className="border-b border-[var(--eaos-border)]/50 px-3 py-2 text-[var(--eaos-text)]">
      {children}
    </td>
  ),
  tr: ({ children }) => <tr className="hover:bg-[var(--eaos-panel)]">{children}</tr>,
  hr: () => <hr className="my-6 border-[var(--eaos-border)]" />,
  strong: ({ children }) => <strong className="font-semibold">{children}</strong>,
};

export function DocContent({ content }: { content: string }) {
  // Strip the title (first # heading) and blockquote subtitle since they're in the TopBar
  const lines = content.split("\n");
  let startIndex = 0;
  for (let i = 0; i < lines.length; i++) {
    const trimmed = lines[i].trim();
    if (trimmed.startsWith("# ")) {
      startIndex = i + 1;
      continue;
    }
    if (startIndex > 0 && trimmed === "") continue;
    if (startIndex > 0 && trimmed.startsWith(">")) {
      startIndex = i + 1;
      continue;
    }
    if (startIndex > 0) break;
  }
  const body = lines.slice(startIndex).join("\n").trimStart();

  return (
    <div className="max-w-3xl">
      <ReactMarkdown remarkPlugins={[remarkGfm]} components={components}>
        {body}
      </ReactMarkdown>
    </div>
  );
}
