import { Skeleton } from "@/components/ui/skeleton"

export default function Loading() {
  return (
    <>
      <div className="flex h-16 shrink-0 items-center gap-2 px-4">
        <Skeleton className="h-4 w-28" />
      </div>
      <div className="flex flex-1 flex-col gap-6 p-4 pt-0 max-w-3xl mx-auto w-full">
        <section className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Skeleton className="size-12 rounded-xl" />
            <div className="space-y-1">
              <Skeleton className="h-5 w-28" />
              <Skeleton className="h-4 w-48" />
            </div>
          </div>
          <Skeleton className="h-9 w-24" />
        </section>
        <Skeleton className="h-px w-full" />
        <section className="space-y-3">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-6 w-32" />
          <Skeleton className="h-3 w-48" />
        </section>
        <Skeleton className="h-px w-full" />
        <section className="space-y-3">
          <Skeleton className="h-4 w-16" />
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <Skeleton className="size-5" />
              <Skeleton className="h-4 w-32" />
            </div>
            <Skeleton className="h-9 w-20" />
          </div>
        </section>
        <Skeleton className="h-px w-full" />
        <section className="space-y-3">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-3 w-72" />
          <div className="flex items-center justify-between">
            <div className="space-y-1">
              <Skeleton className="h-4 w-20" />
              <Skeleton className="h-3 w-48" />
            </div>
            <Skeleton className="h-5 w-9 rounded-full" />
          </div>
        </section>
        <Skeleton className="h-px w-full" />
        <section className="space-y-3">
          <Skeleton className="h-4 w-16" />
          <div className="w-full">
            <div className="border-b py-2.5 flex gap-8">
              <Skeleton className="h-4 w-12" />
              <Skeleton className="h-4 w-12" />
              <Skeleton className="h-4 w-14" />
              <Skeleton className="h-4 w-16" />
            </div>
            {Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className="border-b last:border-0 py-2.5 flex items-center gap-8">
                <Skeleton className="h-4 w-20" />
                <Skeleton className="h-4 w-14" />
                <Skeleton className="h-4 w-12" />
                <Skeleton className="h-4 w-10" />
              </div>
            ))}
          </div>
        </section>
      </div>
    </>
  )
}
