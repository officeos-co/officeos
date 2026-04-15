"use client"

import { useState } from "react"
import Image from "next/image"
import Link from "next/link"
import { PageHeader } from "@/components/page-header"
import { Input } from "@/components/ui/input"
import { integrations } from "@/data/integrations"
import { SearchIcon, HeartIcon } from "lucide-react"

type TypeFilter = "all" | "tool" | "channel"


export default function IntegrationsPage() {
  const [search, setSearch] = useState("")
  const [typeFilter, setTypeFilter] = useState<TypeFilter>("all")

  const filtered = integrations.filter((i) => {
    if (search && !i.name.toLowerCase().includes(search.toLowerCase())) return false
    if (typeFilter !== "all" && i.type !== typeFilter) return false
    return true
  })

  return (
    <>
      <PageHeader group="Managed Agents" page="Integrations" />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="flex items-center gap-2">
          <div className="relative flex-1 max-w-sm">
            <SearchIcon className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
            <Input
              placeholder="Search integrations..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-8"
            />
          </div>
          <div className="flex items-center rounded-lg border border-border">
            {(["all", "tool", "channel"] as const).map((t) => (
              <button
                key={t}
                type="button"
                onClick={() => setTypeFilter(t)}
                className={`px-3 py-1.5 text-xs font-medium capitalize transition-colors ${
                  typeFilter === t
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:text-foreground"
                } ${t === "all" ? "rounded-l-md" : ""} ${t === "channel" ? "rounded-r-md" : ""}`}
              >
                {t === "all" ? "All" : t === "tool" ? "Tools" : "Channels"}
              </button>
            ))}
          </div>
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {filtered.map((integration) => (
            <Link
              key={integration.slug}
              href={`/integrations/${integration.slug}`}
              className="flex flex-col gap-3 rounded-xl border border-border p-4 text-left transition-colors hover:bg-muted/50"
            >
              <div className="flex items-start gap-3">
                <Image
                  src={integration.logo}
                  alt={integration.name}
                  width={32}
                  height={32}
                  className="shrink-0"
                />
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-sm">{integration.name}</span>
                    <span className={`rounded-full px-2 py-0.5 text-[10px] font-medium uppercase tracking-wider ${
                      integration.type === "channel"
                        ? "bg-blue-100 text-blue-700"
                        : "bg-zinc-100 text-zinc-600"
                    }`}>
                      {integration.type}
                    </span>
                  </div>
                </div>
              </div>
              <p className="text-sm line-clamp-2 text-muted-foreground">{integration.description}</p>
              <div className="flex items-center gap-3 text-xs text-muted-foreground">
                <span>{integration.tools.length} tools</span>
                <span>·</span>
                <span className="flex items-center gap-1">
                  <HeartIcon className="size-3" />
                  {integration.likes}
                </span>
                <span>·</span>
                <span>{integration.updatedAgo}</span>
              </div>
            </Link>
          ))}
        </div>

        {filtered.length === 0 && (
          <div className="py-8 text-center text-sm text-muted-foreground">
            No integrations found.
          </div>
        )}
      </div>
    </>
  )
}
