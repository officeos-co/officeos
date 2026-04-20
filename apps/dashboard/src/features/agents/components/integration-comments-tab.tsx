import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { SendIcon, Trash2Icon } from "lucide-react";

interface Comment {
  id: string;
  author: { name: string | null };
  createdAt: string;
  body: string;
}

interface IntegrationCommentsTabProps {
  comments: Comment[];
  onComment: (body: string) => Promise<void>;
  onDeleteComment: (commentId: string) => Promise<void>;
}

export function IntegrationCommentsTab({
  comments,
  onComment,
  onDeleteComment,
}: IntegrationCommentsTabProps) {
  const [commentBody, setCommentBody] = useState("");

  async function handleComment() {
    if (!commentBody.trim()) return;
    await onComment(commentBody.trim());
    setCommentBody("");
  }

  return (
    <div className="space-y-4">
      <div className="flex gap-2">
        <Textarea
          placeholder="Write a comment..."
          value={commentBody}
          onChange={(e) => setCommentBody(e.target.value)}
          className="min-h-[60px] text-sm"
        />
        <Button
          size="sm"
          onClick={handleComment}
          disabled={!commentBody.trim()}
          className="self-end"
        >
          <SendIcon className="size-4" />
        </Button>
      </div>

      {comments.map((comment) => (
        <div
          key={comment.id}
          className="flex gap-3 rounded-xl border border-border bg-card p-4"
        >
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <span className="text-sm font-medium">{comment.author.name}</span>
              <span className="text-xs text-muted-foreground">
                {new Date(comment.createdAt).toLocaleDateString()}
              </span>
            </div>
            <p className="text-sm text-muted-foreground whitespace-pre-wrap">
              {comment.body}
            </p>
          </div>
          <button
            type="button"
            onClick={() => onDeleteComment(comment.id)}
            className="text-muted-foreground hover:text-foreground transition-colors self-start"
          >
            <Trash2Icon className="size-3" />
          </button>
        </div>
      ))}

      {comments.length === 0 && (
        <p className="text-sm text-muted-foreground text-center py-8">
          No comments yet. Be the first to share your thoughts.
        </p>
      )}
    </div>
  );
}
