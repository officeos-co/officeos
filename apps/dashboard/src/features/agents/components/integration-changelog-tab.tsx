import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { PROSE_CLASSES } from "./integration-prose";

interface IntegrationChangelogTabProps {
  changelog: string | null;
}

export function IntegrationChangelogTab({
  changelog,
}: IntegrationChangelogTabProps) {
  return (
    <div className={PROSE_CLASSES}>
      {changelog ? (
        <ReactMarkdown remarkPlugins={[remarkGfm]}>{changelog}</ReactMarkdown>
      ) : (
        <p className="text-sm text-muted-foreground">
          No changelog available.
        </p>
      )}
    </div>
  );
}
