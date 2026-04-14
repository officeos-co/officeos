import { GeistMono } from "geist/font/mono";
import type { Metadata, Viewport } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Office OS Status",
  description: "Real-time system status for Office OS services.",
  robots: { index: true, follow: true },
};

export const viewport: Viewport = {
  themeColor: "white",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className="light">
      <body className={`${GeistMono.className} bg-background antialiased`}>
        {children}
      </body>
    </html>
  );
}
