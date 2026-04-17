import { Skeleton } from "@/components/ui/skeleton"

export default function Loading() {
  return (
    <div className="min-h-svh p-6 md:p-10">
      <Skeleton className="h-9 w-20 mb-8" />
      <div className="max-w-4xl mx-auto space-y-8">
        <div className="text-center space-y-2">
          <Skeleton className="h-8 w-48 mx-auto" />
          <Skeleton className="h-4 w-72 mx-auto" />
        </div>
        <Skeleton className="h-10 w-64 mx-auto rounded-lg" />
        <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
          {Array.from({ length: 2 }).map((_, i) => (
            <div key={i} className="rounded-xl border border-border p-6 space-y-4">
              <Skeleton className="size-10 rounded-lg" />
              <Skeleton className="h-6 w-24" />
              <Skeleton className="h-4 w-48" />
              <Skeleton className="h-8 w-20" />
              <Skeleton className="h-10 w-full rounded-lg" />
              <div className="space-y-2 pt-2">
                {Array.from({ length: 5 }).map((_, j) => (
                  <div key={j} className="flex items-center gap-2">
                    <Skeleton className="size-4 rounded-full" />
                    <Skeleton className="h-4 w-40" />
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
