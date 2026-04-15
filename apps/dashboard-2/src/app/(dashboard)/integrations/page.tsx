"use client"

import { useState } from "react"
import Image from "next/image"
import Link from "next/link"
import { PageHeader } from "@/components/page-header"
import { Input } from "@/components/ui/input"
import { integrations } from "@/data/integrations"
import { SearchIcon, HeartIcon } from "lucide-react"

export default function IntegrationsPage() {
  const [search, setSearch] = useState("")

  const filtered = integrations.filter((i) =>
    !search || i.name.toLowerCase().includes(search.toLowerCase())
  )

  return (
    <>
      <PageHeader group="Managed Agents" page="Integrations" />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="relative max-w-sm">
          <SearchIcon className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
          <Input
            placeholder="Search integrations..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-8"
          />
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
                  <span className="font-medium text-sm">{integration.name}</span>
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
