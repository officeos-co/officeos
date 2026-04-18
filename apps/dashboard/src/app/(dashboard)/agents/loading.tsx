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
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b text-left">
              <th className="px-4 py-3"><Skeleton className="h-3 w-6" /></th>
              <th className="px-4 py-3"><Skeleton className="h-3 w-12" /></th>
              <th className="px-4 py-3"><Skeleton className="h-3 w-12" /></th>
              <th className="px-4 py-3 text-center"><Skeleton className="h-3 w-12 mx-auto" /></th>
              <th className="px-4 py-3"><Skeleton className="h-3 w-16" /></th>
              <th className="px-4 py-3"><Skeleton className="h-3 w-20" /></th>
              <th className="px-4 py-3 w-10" />
            </tr>
          </thead>
          <tbody>
            {Array.from({ length: 3 }).map((_, i) => (
              <tr key={i} className="border-b">
                <td className="px-4 py-3"><Skeleton className="h-4 w-8" /></td>
                <td className="px-4 py-3"><Skeleton className="h-4 w-28" /></td>
                <td className="px-4 py-3"><Skeleton className="h-4 w-32" /></td>
                <td className="px-4 py-3 text-center"><Skeleton className="h-6 w-16 rounded-full mx-auto" /></td>
                <td className="px-4 py-3"><Skeleton className="h-4 w-24" /></td>
                <td className="px-4 py-3"><Skeleton className="h-4 w-24" /></td>
                <td className="px-4 py-3"><Skeleton className="size-6 rounded" /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}
