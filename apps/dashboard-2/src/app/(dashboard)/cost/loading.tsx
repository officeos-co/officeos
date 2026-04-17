import { Skeleton } from "@/components/ui/skeleton"

export default function Loading() {
  return (
    <>
      <div className="flex h-16 shrink-0 items-center gap-2 px-4">
        <Skeleton className="h-4 w-24" />
      </div>
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0 max-w-4xl mx-auto w-full">
        <div className="flex items-center gap-2 flex-wrap">
          <Skeleton className="h-9 w-[140px]" />
          <Skeleton className="h-9 w-[160px]" />
          <div className="flex items-center gap-1 ml-auto">
            <Skeleton className="h-8 w-8" />
            <Skeleton className="h-9 w-[160px]" />
            <Skeleton className="h-8 w-8" />
          </div>
        </div>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="rounded-xl border border-border p-4 space-y-2">
              <Skeleton className="h-4 w-28" />
              {i > 0 && <Skeleton className="h-3 w-32" />}
              <Skeleton className="h-7 w-16" />
            </div>
          ))}
        </div>
        <div className="rounded-xl border border-border p-4">
          <Skeleton className="h-4 w-36 mb-1" />
          <Skeleton className="h-3 w-24 mb-4" />
          <Skeleton className="h-[280px] w-full" />
        </div>
      </div>
    </>
  )
}
