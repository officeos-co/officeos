import Link from "next/link";

export function CTASection() {
  return (
    <section
      id="cta"
      className="flex w-full flex-col items-center justify-center"
    >
      <div className="w-full">
        <div className="relative z-20 flex h-[400px] w-full flex-col items-center justify-center gap-6 overflow-hidden rounded-xl border border-border bg-accent shadow-xl md:h-[400px]">
          <h2 className="max-w-xs text-center font-medium text-4xl text-primary tracking-tighter md:max-w-xl md:text-5xl">
            Ready to deploy your first agent?
          </h2>
          <p className="max-w-md text-center text-muted-foreground">
            Start free with your own API keys. No credit card required.
          </p>
          <div className="flex flex-col items-center justify-center gap-3">
            <Link
              href="https://dashboard.officeos.co"
              className="btn-glow flex h-10 w-fit items-center justify-center rounded-full bg-secondary px-6 font-normal text-sm tracking-wide transition-all ease-out hover:bg-secondary/80 active:scale-95"
            >
              Start Free
            </Link>
            <span className="text-sm text-muted-foreground">
              Free tier includes 3 agents. Bring your own API keys. Cancel
              anytime.
            </span>
          </div>
        </div>
      </div>
    </section>
  );
}
