import { RootProvider } from "fumadocs-ui/provider/next";
import { DocsShell } from "@/components/docs-shell";
import "fumadocs-ui/style.css";
import "./globals.css";
import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: {
    default: "OfficeOS Docs",
    template: "%s | OfficeOS Docs",
  },
  description:
    "Documentation for OfficeOS — Kubernetes-native platform for autonomous AI agents.",
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en" className="light" suppressHydrationWarning>
      <body>
        <RootProvider theme={{ defaultTheme: "light", enableSystem: false }}>
          <DocsShell>{children}</DocsShell>
        </RootProvider>
      </body>
    </html>
  );
}
