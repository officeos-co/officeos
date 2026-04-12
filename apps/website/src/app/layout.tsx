import { GeistMono } from "geist/font/mono";
import type { Viewport } from "next";
import { ThemeProvider } from "@/components/theme-provider";
import "./globals.css";

export const viewport: Viewport = {
	themeColor: "white",
};

export const metadata = {
	title: "Office OS — Deploy, Connect, and Control AI Agents",
	description:
		"Give every team their own AI agent with the right skills, permissions, and access to your organization's knowledge.",
	keywords: [
		"Office OS",
		"AI agent platform",
		"agent deployment",
		"Kubernetes agents",
		"enterprise AI",
	],
};

export default function RootLayout({
	children,
}: Readonly<{
	children: React.ReactNode;
}>) {
	return (
		<html lang="en" className="light" suppressHydrationWarning>
			<body
				className={`${GeistMono.className} bg-background font-sans antialiased`}
			>
				<ThemeProvider
					attribute="class"
					defaultTheme="light"
					enableSystem={false}
					disableTransitionOnChange
				>
					{children}
				</ThemeProvider>
			</body>
		</html>
	);
}
