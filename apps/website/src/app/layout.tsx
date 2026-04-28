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
        className={`${GeistMono.className} bg-background font-sans antialiased`}
      >
        {children}
      </body>
    </html>
  );
}
