import Image from "next/image";

export function QuoteSection() {
  return (
    <section className="relative z-20 flex w-full flex-col items-center justify-center">
      <blockquote className="w-full rounded-xl border border-border bg-white px-10 py-10 shadow-xl md:px-[25%] md:py-14">
        <p className="text-xl font-medium leading-relaxed tracking-tight text-primary md:text-xl">
          OfficeOS has transformed how we think about operational efficiency.
          Tasks that once consumed hours now complete in moments, freeing our
          team to focus on creativity and strategic growth.
        </p>
        <div className="mt-8 flex items-center gap-3">
          <Image
            src="/HarroProfile.jpg"
            alt="Harro Krog"
            width={44}
            height={44}
            className="rounded-full object-cover"
          />
          <div>
            <p className="text-sm font-semibold text-primary">Harro Krog</p>
            <p className="text-sm text-muted-foreground">Founder, OfficeOS</p>
          </div>
        </div>
      </blockquote>
    </section>
  );
}
