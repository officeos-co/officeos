import { GeistSans } from "geist/font/sans";
import { GeistMono } from "geist/font/mono";
import type { Viewport } from "next";
import "./globals.css";

export { metadata } from "./metadata";

export const dynamic = "force-dynamic";

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
      <body
        className={`${GeistSans.variable} ${GeistMono.variable} bg-background font-sans antialiased`}
      >
        {children}
      </body>
    </html>
  );
}
