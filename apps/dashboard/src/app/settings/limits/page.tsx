import { TopBar } from "@/components/shared/TopBar";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

const FREE_LIMITS = [
  { resource: "Concurrent agents", limit: "1" },
  { resource: "Tokens / month", limit: "2,000,000" },
  { resource: "Skill executions / month", limit: "Unlimited" },
  { resource: "LLM providers", limit: "All" },
];

export default function LimitsPage() {
  return (
    <div>
      <TopBar title="Limits" />
      <div className="max-w-lg px-6 py-6">
        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent">
              <TableHead className="text-[12px] font-normal text-muted-foreground">Resource</TableHead>
              <TableHead className="text-[12px] font-normal text-muted-foreground">Limit (Free)</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {FREE_LIMITS.map((row) => (
              <TableRow key={row.resource}>
                <TableCell className="text-[13px] font-medium">{row.resource}</TableCell>
                <TableCell className="text-[13px] text-muted-foreground">{row.limit}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        <p className="mt-4 text-[12px] text-muted-foreground">
          Need higher limits?{" "}
          <a href="mailto:harro@officeos.co" className="underline underline-offset-2 hover:text-foreground">
            Contact us
          </a>{" "}
          or{" "}
          <a href="/pricing" className="underline underline-offset-2 hover:text-foreground">
            upgrade
          </a>.
        </p>
      </div>
    </div>
  );
}
