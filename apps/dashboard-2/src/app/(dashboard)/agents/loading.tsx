import { Skeleton } from "@/components/ui/skeleton"

export default function Loading() {
  return (
    <>
      <div className="flex h-16 shrink-0 items-center gap-2 px-4">
        <Skeleton className="h-4 w-32" />
      </div>
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="flex items-center gap-2">
          <Skeleton className="h-9 w-64" />
          <Skeleton className="h-9 w-24" />
        </div>
        <div className="w-full">
          <div className="border-b py-3 px-4 flex gap-4">
            {Array.from({ length: 7 }).map((_, i) => (
              <Skeleton key={i} className="h-3 w-20" />
            ))}
          </div>
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="border-b py-3 px-4 flex items-center gap-4">
              <Skeleton className="h-4 w-8" />
              <Skeleton className="h-4 w-28" />
              <Skeleton className="h-4 w-32" />
              <Skeleton className="h-6 w-16 rounded-full" />
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-6" />
            </div>
          ))}
        </div>
      </div>
    </>
  )
}
