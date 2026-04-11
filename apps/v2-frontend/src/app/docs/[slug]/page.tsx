import { notFound } from "next/navigation";
import { getDoc } from "@/lib/docs";
import { DocContent } from "@/components/DocContent";

type Props = {
  params: Promise<{ slug: string }>;
};

export default async function DocPage({ params }: Props) {
  const { slug } = await params;
  const doc = getDoc(slug);
  if (!doc) notFound();

  return (
    <>
      <div className="sticky top-0 z-10 border-b border-[var(--eaos-border)] bg-[var(--eaos-bg)]/90 px-8 py-6 backdrop-blur">
        <h1 className="text-2xl font-semibold">{doc.title}</h1>
        {doc.subtitle && (
          <p className="mt-1 text-sm text-[var(--eaos-text-muted)]">
            {doc.subtitle}
          </p>
        )}
      </div>
      <div className="px-8 py-6">
        <DocContent content={doc.content} />
      </div>
    </>
  );
}
