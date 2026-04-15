import { TopBar } from "@/components/shared/TopBar";
import { EmptyState } from "@/components/shared/EmptyState";
import { Button } from "@/components/ui/button";

export default function ApiKeysPage() {
  return (
    <div>
      <TopBar title="API Keys" />
      <div className="px-6 py-6">
        <EmptyState
          title="No API keys yet"
          description="API keys let external tools authenticate with your workspace."
          action={
            <Button size="sm" disabled className="opacity-50 h-7 text-[12px]">
              Coming soon
            </Button>
          }
        />
      </div>
    </div>
  );
}
