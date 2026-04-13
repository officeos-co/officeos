import { TopBar } from "@/components/shared/TopBar";

const FREE_LIMITS = [
  { resource: "Concurrent agents", limit: "1" },
  { resource: "Tokens / month", limit: "2,000,000" },
  { resource: "Skill executions / month", limit: "Unlimited" },
  { resource: "LLM providers", limit: "All (OpenAI, Anthropic, Gemini, …)" },
];

export default function LimitsPage() {
  return (
    <div>
      <TopBar
        title="Limits"
        subtitle="Current plan limits for your workspace."
      />
      <div className="p-6 max-w-2xl">
        <div className="rounded-xl border border-border overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-muted/40">
                <th className="px-4 py-3 text-left font-medium text-muted-foreground">
                  Resource
                </th>
                <th className="px-4 py-3 text-left font-medium text-muted-foreground">
                  Limit (Free tier)
                </th>
              </tr>
            </thead>
            <tbody>
              {FREE_LIMITS.map((row, i) => (
                <tr
                  key={row.resource}
                  className={i < FREE_LIMITS.length - 1 ? "border-b border-border" : ""}
                >
                  <td className="px-4 py-3 font-medium">{row.resource}</td>
                  <td className="px-4 py-3 text-muted-foreground">{row.limit}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <p className="mt-4 text-[13px] text-muted-foreground">
          Need higher limits?{" "}
          <a
            href="mailto:harro@harrokrog.com"
            className="text-foreground underline underline-offset-2 hover:text-primary"
          >
            Contact us to increase limits
          </a>{" "}
          or{" "}
          <a
            href="/pricing"
            className="text-foreground underline underline-offset-2 hover:text-primary"
          >
            upgrade your plan
          </a>
          .
        </p>
      </div>
    </div>
  );
}
