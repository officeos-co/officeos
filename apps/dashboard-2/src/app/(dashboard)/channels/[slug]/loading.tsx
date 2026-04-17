import { Skeleton } from "@/components/ui/skeleton"

export default function Loading() {
  return (
    <>
      <div className="flex h-16 shrink-0 items-center gap-2 px-4">
        <Skeleton className="h-4 w-48" />
      </div>
      <div className="flex flex-1 flex-col gap-6 p-4 pt-0 max-w-3xl mx-auto w-full">
        <div className="flex items-start gap-4">
          <Skeleton className="size-12 rounded-xl shrink-0" />
          <div className="flex-1 space-y-2">
            <div className="flex items-center gap-2">
              <Skeleton className="h-6 w-32" />
              <Skeleton className="h-5 w-16 rounded-full" />
            </div>
            <Skeleton className="h-4 w-64" />
            <div className="flex gap-4 pt-1">
              <Skeleton className="h-3 w-12" />
              <Skeleton className="h-3 w-16" />
              <Skeleton className="h-3 w-20" />
            </div>
          </div>
        </div>
        <div className="rounded-xl border border-border p-4 space-y-3">
          <Skeleton className="h-5 w-24" />
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-4 w-48" />
          ))}
        </div>
        <div className="rounded-xl border border-border p-4 space-y-3">
          <Skeleton className="h-5 w-28" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-5/6" />
          <Skeleton className="h-4 w-4/6" />
          <Skeleton className="h-4 w-full" />
        </div>
      </div>
    </>
  )
}
