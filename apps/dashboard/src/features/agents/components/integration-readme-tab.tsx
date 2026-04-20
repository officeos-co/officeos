import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { PROSE_CLASSES } from "./integration-prose";

interface IntegrationReadmeTabProps {
  readme: string | null;
}

export function IntegrationReadmeTab({ readme }: IntegrationReadmeTabProps) {
  return (
    <div className={PROSE_CLASSES}>
      {readme ? (
        <ReactMarkdown remarkPlugins={[remarkGfm]}>{readme}</ReactMarkdown>
      ) : (
        <p className="text-sm text-muted-foreground">No README available.</p>
      )}
    </div>
  );
}
