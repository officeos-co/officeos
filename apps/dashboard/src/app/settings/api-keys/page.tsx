import { TopBar } from "@/components/shared/TopBar";
import { Key } from "lucide-react";
import { Button } from "@/components/ui/button";

export default function ApiKeysPage() {
  return (
    <div>
      <TopBar
        title="API Keys"
        subtitle="Manage programmatic access keys for your workspace."
      />
      <div className="p-6 max-w-2xl">
        {/* Empty state */}
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed border-border py-20 gap-4 text-center">
          <div className="grid h-12 w-12 place-items-center rounded-xl bg-muted">
            <Key className="h-5 w-5 text-muted-foreground" />
          </div>
          <div>
            <p className="font-medium">No API keys yet</p>
            <p className="text-sm text-muted-foreground mt-1">
              API keys let external tools authenticate with your workspace.
            </p>
          </div>
          <div className="relative group">
            <Button disabled className="cursor-not-allowed opacity-50">
              Create API key
            </Button>
            <div className="absolute -top-9 left-1/2 -translate-x-1/2 whitespace-nowrap rounded bg-black/80 px-2 py-1 text-xs text-white opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none">
              Coming soon
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
