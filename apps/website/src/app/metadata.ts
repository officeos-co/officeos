import { Metadata } from "next";
import { siteConfig } from "@/lib/site";

export const metadata: Metadata = {
	title: "Office OS — Deploy, Connect, and Control AI Agents",
	description:
		"Give every team their own AI agent with the right skills, permissions, and access to your organization's knowledge.",
	keywords: [
		"Office OS",
		"AI agent platform",
		"agent deployment",
		"Kubernetes agents",
		"enterprise AI",
		"custom skills",
	],
	openGraph: {
		type: "website",
		locale: "en_US",
		url: siteConfig.url,
		title: "Office OS — Deploy, Connect, and Control AI Agents",
		description:
			"Give every team their own AI agent with the right skills, permissions, and access to your organization's knowledge.",
		siteName: "Office OS",
	},
	twitter: {
		card: "summary_large_image",
		title: "Office OS — Deploy, Connect, and Control AI Agents",
		description:
			"Give every team their own AI agent with the right skills, permissions, and access to your organization's knowledge.",
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
