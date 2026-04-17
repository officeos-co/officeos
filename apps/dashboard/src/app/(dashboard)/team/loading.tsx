import { Skeleton } from "@/components/ui/skeleton"

export default function Loading() {
  return (
    <>
      <div className="flex h-16 shrink-0 items-center gap-2 px-4">
        <Skeleton className="h-4 w-24" />
      </div>
      <div className="flex flex-1 flex-col gap-6 p-4 pt-0 max-w-3xl mx-auto w-full">
        <section>
          <Skeleton className="h-4 w-24 mb-3" />
          <div className="space-y-2 max-w-sm">
            <Skeleton className="h-4 w-28" />
            <Skeleton className="h-9 w-full" />
          </div>
        </section>
        <Skeleton className="h-px w-full" />
        <section>
          <Skeleton className="h-4 w-28 mb-3" />
          <div className="w-full">
            <div className="border-b py-2.5 flex gap-8">
              <Skeleton className="h-4 w-16" />
              <Skeleton className="h-4 w-32" />
              <Skeleton className="h-4 w-14" />
              <Skeleton className="h-4 w-16" />
              <Skeleton className="h-4 w-6" />
            </div>
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="border-b last:border-0 py-2.5 flex items-center gap-8">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-4 w-40" />
                <Skeleton className="h-5 w-16 rounded" />
                <Skeleton className="h-4 w-20" />
                <Skeleton className="h-7 w-7" />
              </div>
            ))}
          </div>
        </section>
      </div>
    </>
  )
}
