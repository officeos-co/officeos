interface PlaceholderProps {
  label: string;
  className?: string;
}

export function Placeholder({ label, className = "" }: PlaceholderProps) {
  return (
    <div
      className={`rounded-xl bg-muted/50 border border-dashed border-muted-foreground/20 flex items-center justify-center ${className}`}
    >
      <p className="text-muted-foreground text-sm text-center px-4">
        {label}
      </p>
    </div>
  );
}
