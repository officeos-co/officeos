"use client";

import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { ChangelogEntry } from "@/lib/changelog";

function formatDate(date: Date): string {
  return date.toLocaleDateString("en-US", {
    year: "numeric",
    month: "long",
    day: "numeric",
  });
}

export function ChangelogTimeline({ entries }: { entries: ChangelogEntry[] }) {
  return (
    <div className="relative">
      {entries.map((entry, i) => (
        <div key={i} className="relative">
          <div className="flex flex-col md:flex-row gap-y-6">
            <div className="md:w-48 flex-shrink-0">
              <div className="md:sticky md:top-8 pb-10">
                <time className="text-sm font-medium text-muted-foreground block mb-3">
                  {formatDate(new Date(entry.date))}
                </time>
                {entry.version && (
                  <div className="inline-flex items-center justify-center w-10 h-10 text-primary border border-border rounded-lg text-sm font-bold">
                    {entry.version}
                  </div>
                )}
              </div>
            </div>

            <div className="flex-1 md:pl-8 relative pb-10">
              <div className="hidden md:block absolute top-2 left-0 w-px h-full bg-border">
                <div className="absolute -translate-x-1/2 size-3 bg-primary rounded-full" />
              </div>

              <div className="space-y-6">
                <div className="flex flex-col gap-2">
                  <h2 className="text-2xl font-semibold tracking-tight text-balance">
                    {entry.title}
                  </h2>
                  {entry.tags && entry.tags.length > 0 && (
                    <div className="flex flex-wrap gap-2">
                      {entry.tags.map((tag) => (
                        <span
                          key={tag}
                          className="h-6 w-fit px-2 text-xs font-medium bg-muted text-muted-foreground rounded-full border flex items-center justify-center"
                        >
                          {tag}
                        </span>
                      ))}
                    </div>
                  )}
                </div>

                <div className="prose max-w-none prose-headings:font-semibold prose-headings:tracking-tight prose-headings:text-balance prose-p:tracking-tight prose-p:text-muted-foreground prose-p:leading-relaxed prose-a:no-underline prose-li:text-muted-foreground">
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>
                    {entry.content}
                  </ReactMarkdown>
                </div>
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
