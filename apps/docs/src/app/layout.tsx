import { RootProvider } from "fumadocs-ui/provider/next";
import { DocsLayout } from "fumadocs-ui/layouts/docs";
import { source } from "@/lib/source";
import "fumadocs-ui/style.css";
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
    <html lang="en" className="light">
      <body>
        <RootProvider theme={{ defaultTheme: "light", enableSystem: false }}>
          <DocsLayout tree={source.pageTree}>{children}</DocsLayout>
        </RootProvider>
      </body>
    </html>
  );
}
