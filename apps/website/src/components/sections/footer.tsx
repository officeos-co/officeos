import { siteConfig, footerConfig } from "@/lib/config";
import { Separator } from "@/components/ui/separator";

export function Footer() {
  return (
    <footer className="border-t border-border/40 py-16">
      <div className="mx-auto max-w-6xl px-6">
        <div className="grid gap-12 sm:grid-cols-2 lg:grid-cols-5">
          {/* Logo + tagline */}
          <div className="lg:col-span-2 flex flex-col gap-3">
            <span className="font-mono text-lg font-bold tracking-tight">
              {siteConfig.name}
            </span>
            <p className="text-sm text-muted-foreground max-w-xs">
              {footerConfig.tagline}
            </p>
          </div>

          {/* Link columns */}
          {footerConfig.columns.map((col) => (
            <div key={col.title} className="flex flex-col gap-3">
              <span className="text-sm font-semibold">{col.title}</span>
              <ul className="flex flex-col gap-2">
                {col.links.map((link) => (
                  <li key={link}>
                    <a
                      href="#"
                      className="text-sm text-muted-foreground hover:text-foreground transition-colors"
                    >
                      {link}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <Separator className="my-8 bg-border/40" />

        <p className="text-xs text-muted-foreground text-center">
          {footerConfig.copyright}
        </p>
      </div>
    </footer>
  );
}
