import type { MetadataRoute } from "next";
import { getSiteConfig } from "@/lib/site";

const baseUrl = getSiteConfig().url;

export default function sitemap(): MetadataRoute.Sitemap {
  const now = new Date();

  const staticPages = [
    { path: "/", priority: 1.0, changeFrequency: "weekly" as const },
    { path: "/about", priority: 0.7, changeFrequency: "monthly" as const },
    { path: "/pricing", priority: 0.8, changeFrequency: "monthly" as const },
    { path: "/privacy", priority: 0.3, changeFrequency: "yearly" as const },
    { path: "/terms", priority: 0.3, changeFrequency: "yearly" as const },
  ];

  const productPages = [
    "/product/platform",
    "/product/knowledge-graph",
    "/product/skills",
    "/product/integrations",
    "/product/security",
  ].map((path) => ({
    path,
    priority: 0.8 as const,
    changeFrequency: "monthly" as const,
  }));

  const solutionPages = [
    "/solutions/sales-intelligence",
    "/solutions/customer-success",
    "/solutions/competitive-intelligence",
    "/solutions/contract-review",
    "/solutions/content-strategy",
  ].map((path) => ({
    path,
    priority: 0.7 as const,
    changeFrequency: "monthly" as const,
  }));

  return [...staticPages, ...productPages, ...solutionPages].map((page) => ({
    url: `${baseUrl}${page.path}`,
    lastModified: now,
    changeFrequency: page.changeFrequency,
    priority: page.priority,
  }));
}
