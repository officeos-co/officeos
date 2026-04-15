import { Metadata } from "next";
import { siteConfig } from "@/lib/site";

export const metadata: Metadata = {
  metadataBase: new URL(siteConfig.url),
  title: "OfficeOS — AI Agent Platform for Enterprise Teams",
  description:
    "Deploy autonomous AI agents across your company. Enterprise knowledge graph, custom skills SDK, self-hosted Kubernetes runtime, and full credential isolation. Not an office suite — an intelligence layer for AI agents.",
  keywords: [
    "OfficeOS",
    "AI agent platform",
    "enterprise AI agents",
    "autonomous agents",
    "Kubernetes AI",
    "agent deployment",
    "AI workforce",
    "custom AI skills",
    "self-hosted AI",
    "knowledge graph",
  ],
  alternates: {
    canonical: "/",
  },
  openGraph: {
    type: "website",
    locale: "en_US",
    url: siteConfig.url,
    title: "OfficeOS — AI Agent Platform for Enterprise Teams",
    description:
      "Deploy autonomous AI agents across your company. Enterprise knowledge graph, custom skills, self-hosted runtime, and full credential isolation.",
    siteName: "OfficeOS",
  },
  twitter: {
    card: "summary_large_image",
    title: "OfficeOS — AI Agent Platform for Enterprise Teams",
    description:
      "Deploy autonomous AI agents across your company. Enterprise knowledge graph, custom skills, self-hosted runtime, and full credential isolation.",
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      "max-video-preview": -1,
      "max-image-preview": "large",
      "max-snippet": -1,
    },
  },
};
