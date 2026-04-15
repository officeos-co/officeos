import { PageHeader } from "@/components/page-header"

export default function LogsPage() {
  return (
    <>
      <PageHeader group="Analytics" page="Logs" />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="min-h-[50vh] flex-1 rounded-xl bg-muted/50" />
      </div>
    </>
  )
}
