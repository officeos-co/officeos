import { cn } from "@/lib/utils"

export function EmptyState({
  message,
  className,
}: {
  message: string
  className?: string
}) {
  return (
    <div className={cn("py-8 text-center text-sm text-muted-foreground", className)}>
      {message}
    </div>
  )
}
