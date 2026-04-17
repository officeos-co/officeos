import { Skeleton } from "@/components/ui/skeleton"

export default function Loading() {
  return (
    <>
      <div className="flex h-16 shrink-0 items-center gap-2 px-4">
        <Skeleton className="h-4 w-28" />
      </div>
      <div className="flex flex-1 flex-col gap-8 p-4 pt-0 max-w-3xl mx-auto w-full">
        <section className="space-y-4">
          <Skeleton className="h-5 w-16" />
          <div className="grid grid-cols-[1fr_1fr] gap-4">
            <div className="space-y-2">
              <Skeleton className="h-4 w-20" />
              <div className="flex items-center gap-3">
                <Skeleton className="size-10 rounded-full shrink-0" />
                <Skeleton className="h-9 w-full" />
              </div>
            </div>
            <div className="space-y-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-9 w-full" />
            </div>
          </div>
          <div className="space-y-2">
            <Skeleton className="h-4 w-20" />
            <Skeleton className="h-9 w-full" />
          </div>
          <div className="space-y-2">
            <Skeleton className="h-4 w-64" />
            <Skeleton className="h-3 w-52" />
            <Skeleton className="h-24 w-full" />
          </div>
        </section>
        <Skeleton className="h-px w-full" />
        <section className="space-y-0">
          <Skeleton className="h-5 w-24 mb-4" />
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="flex items-start justify-between py-4 border-b border-border last:border-0">
              <div className="space-y-1">
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-3 w-64" />
              </div>
              <Skeleton className="h-5 w-9 rounded-full" />
            </div>
          ))}
        </section>
      </div>
    </>
  )
}
