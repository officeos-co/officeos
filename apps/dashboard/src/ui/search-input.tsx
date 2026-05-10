"use client"

import { SearchIcon } from "lucide-react"
import { Input } from "@/ui/input"
import { cn } from "@/lib/utils"

export function SearchInput({
  value,
  onChange,
  placeholder = "Search...",
  className,
}: {
  value: string
  onChange: (value: string) => void
  placeholder?: string
  className?: string
}) {
  return (
    <div className={cn("relative flex-1 max-w-sm", className)}>
      <SearchIcon className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
      <Input
        placeholder={placeholder}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="pl-8"
      />
    </div>
  )
}
