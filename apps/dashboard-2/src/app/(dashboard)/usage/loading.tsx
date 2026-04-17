import { Skeleton } from "@/components/ui/skeleton"

export default function Loading() {
  return (
    <>
      <div className="flex h-16 shrink-0 items-center gap-2 px-4">
        <Skeleton className="h-4 w-28" />
      </div>
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0 max-w-4xl mx-auto w-full">
        <div className="flex items-center gap-2 flex-wrap">
          <Skeleton className="h-9 w-[160px]" />
          <Skeleton className="h-9 w-[120px]" />
          <div className="flex items-center gap-1 ml-auto">
            <Skeleton className="h-8 w-8" />
            <Skeleton className="h-9 w-[160px]" />
            <Skeleton className="h-8 w-8" />
          </div>
        </div>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="rounded-xl border border-border p-4 space-y-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-7 w-16" />
            </div>
          ))}
        </div>
        <div className="rounded-xl border border-border p-4">
          <Skeleton className="h-4 w-24 mb-1" />
          <Skeleton className="h-3 w-40 mb-4" />
          <Skeleton className="h-[240px] w-full" />
        </div>
        <div className="rounded-xl border border-border p-4">
          <Skeleton className="h-4 w-36 mb-1" />
          <Skeleton className="h-3 w-52 mb-4" />
          <Skeleton className="h-[180px] w-full" />
        </div>
      </div>
    </>
  )
}
