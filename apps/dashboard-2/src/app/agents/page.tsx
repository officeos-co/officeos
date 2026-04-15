import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { PlusIcon } from "lucide-react"

const agents = [
  { id: "1", name: "Research Assistant", provider: "anthropic", model: "claude-sonnet-4-6", status: "running", created: "2 days ago" },
  { id: "2", name: "Code Reviewer", provider: "anthropic", model: "claude-opus-4-6", status: "running", created: "5 days ago" },
  { id: "3", name: "Customer Support Bot", provider: "google", model: "gemini-2.5-pro", status: "running", created: "10 days ago" },
  { id: "4", name: "Data Pipeline Monitor", provider: "anthropic", model: "claude-haiku-4-5", status: "pending", created: "1 day ago" },
  { id: "5", name: "Content Writer", provider: "openai", model: "gpt-4o", status: "stopped", created: "2 weeks ago" },
  { id: "6", name: "Security Scanner", provider: "anthropic", model: "claude-sonnet-4-6", status: "failed", created: "3 days ago" },
]

function StatusBadge({ status }: { status: string }) {
  const variant =
    status === "running"
      ? "default"
      : status === "pending"
        ? "secondary"
        : status === "failed"
          ? "destructive"
          : "outline"

  return <Badge variant={variant}>{status}</Badge>
}

export default function AgentsPage() {
  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Agents"
        action={
          <Button size="sm">
            <PlusIcon />
            New agent
          </Button>
        }
      />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="rounded-xl border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-left text-muted-foreground">
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Provider</th>
                <th className="px-4 py-3 font-medium">Model</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Created</th>
              </tr>
            </thead>
            <tbody>
              {agents.map((agent) => (
                <tr
                  key={agent.id}
                  className="border-b last:border-0 hover:bg-muted/50 cursor-pointer transition-colors"
                >
                  <td className="px-4 py-3 font-medium">{agent.name}</td>
                  <td className="px-4 py-3 text-muted-foreground">{agent.provider}</td>
                  <td className="px-4 py-3 text-muted-foreground font-mono text-xs">{agent.model}</td>
                  <td className="px-4 py-3">
                    <StatusBadge status={agent.status} />
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">{agent.created}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  )
}
