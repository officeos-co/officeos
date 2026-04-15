import { docs } from "@/.source"
import { ThemeToggle } from "@/components/theme-toggle"
import { formatDate } from "@/lib/utils"

interface ChangelogEntry {
  title: string
  date: string
  version?: string
  tags?: string[]
  body: React.ComponentType
  _mdx: { path: string }
}

export default function HomePage() {
  const entries = [...docs.getPages()] as unknown as { data: ChangelogEntry }[]
  const sorted = entries.sort((a, b) => {
    return new Date(b.data.date).getTime() - new Date(a.data.date).getTime()
  })

  return (
    <div className="min-h-screen bg-background relative">
      {/* Header */}
      <div className="border-b border-border/50">
        <div className="max-w-5xl mx-auto relative">
          <div className="p-3 flex items-center justify-between">
            <div className="flex items-center gap-3">
              <a
                href="https://officeos.co"
                className="text-sm text-muted-foreground hover:text-primary transition-colors"
              >
                &larr; officeos.co
              </a>
              <span className="text-border">|</span>
              <h1 className="text-3xl font-semibold tracking-tight">
                Changelog
              </h1>
            </div>
            <ThemeToggle />
          </div>
        </div>
      </div>

      {/* Timeline */}
      <div className="max-w-5xl mx-auto px-6 lg:px-10 pt-10">
        <div className="relative">
          {sorted.map((entry, i) => {
            const MDX = entry.data.body
            const date = new Date(entry.data.date)
            const formattedDate = formatDate(date)

            return (
              <div key={i} className="relative">
                <div className="flex flex-col md:flex-row gap-y-6">
                  <div className="md:w-48 flex-shrink-0">
                    <div className="md:sticky md:top-8 pb-10">
                      <time className="text-sm font-medium text-muted-foreground block mb-3">
                        {formattedDate}
                      </time>

                      {entry.data.version && (
                        <div className="inline-flex relative z-10 items-center justify-center w-10 h-10 text-foreground border border-border rounded-lg text-sm font-bold">
                          {entry.data.version}
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Right side - Content */}
                  <div className="flex-1 md:pl-8 relative pb-10">
                    {/* Vertical timeline line */}
                    <div className="hidden md:block absolute top-2 left-0 w-px h-full bg-border">
                      {/* Timeline dot */}
                      <div className="hidden md:block absolute -translate-x-1/2 size-3 bg-primary rounded-full z-10" />
                    </div>

                    <div className="space-y-6">
                      <div className="relative z-10 flex flex-col gap-2">
                        <h2 className="text-2xl font-semibold tracking-tight text-balance">
                          {entry.data.title}
                        </h2>

                        {/* Tags */}
                        {entry.data.tags && entry.data.tags.length > 0 && (
                          <div className="flex flex-wrap gap-2">
                            {entry.data.tags.map((tag: string) => (
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
                      <div className="prose dark:prose-invert max-w-none prose-headings:scroll-mt-8 prose-headings:font-semibold prose-a:no-underline prose-headings:tracking-tight prose-headings:text-balance prose-p:tracking-tight prose-p:text-balance">
                        <MDX />
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}
