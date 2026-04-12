import { RootProvider } from "fumadocs-ui/provider/next";
import "fumadocs-ui/style.css";
import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: {
    default: "EnterpriseAgentOS Docs",
    template: "%s | EAOS Docs",
  },
  description:
    "Documentation for EnterpriseAgentOS — Kubernetes-native platform for autonomous AI agents.",
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body>
        <RootProvider>{children}</RootProvider>
      </body>
    </html>
  );
}
