import { Skeleton } from "@/components/ui/skeleton"

export default function Loading() {
  return (
    <>
      <div className="flex h-16 shrink-0 items-center gap-2 px-4">
        <Skeleton className="h-4 w-36" />
      </div>
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="flex items-center gap-2">
          <Skeleton className="h-8 w-56 rounded-lg" />
        </div>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="flex flex-col gap-3 rounded-xl border border-border p-4">
              <div className="flex items-start gap-3">
                <Skeleton className="size-8 rounded shrink-0" />
                <div className="flex-1 space-y-1">
                  <Skeleton className="h-4 w-20" />
                  <Skeleton className="h-3 w-14" />
                </div>
                <Skeleton className="h-7 w-18 rounded" />
              </div>
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-2/3" />
              <div className="flex gap-1.5">
                <Skeleton className="h-5 w-16 rounded" />
                <Skeleton className="h-5 w-20 rounded" />
                <Skeleton className="h-5 w-14 rounded" />
              </div>
            </div>
          ))}
        </div>
      </div>
    </>
  )
}
