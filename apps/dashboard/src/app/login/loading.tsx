import { Skeleton } from "@/ui/skeleton"

export default function Loading() {
  return (
    <div className="grid min-h-svh lg:grid-cols-2">
      <div className="flex flex-col gap-4 p-6 md:p-10">
        <div className="flex flex-1 items-center justify-center">
          <div className="w-full max-w-xs space-y-6">
            <div className="space-y-2 text-center">
              <Skeleton className="h-7 w-32 mx-auto" />
              <Skeleton className="h-4 w-48 mx-auto" />
            </div>
            <div className="space-y-4">
              <div className="space-y-2">
                <Skeleton className="h-4 w-12" />
                <Skeleton className="h-9 w-full" />
              </div>
              <div className="space-y-2">
                <Skeleton className="h-4 w-16" />
                <Skeleton className="h-9 w-full" />
              </div>
              <Skeleton className="h-9 w-full" />
            </div>
          </div>
        </div>
      </div>
      <div className="relative hidden lg:block bg-muted" />
    </div>
  )
}
