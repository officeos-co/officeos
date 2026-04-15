"use client";

import Image from "next/image";
import { motion, useInView } from "motion/react";
import { useRef } from "react";
import { Marquee } from "@/components/ui/marquee";

const desktopColumns = [
  [
    { name: "Notion", src: "/logos/notion.svg" },
    { name: "GitHub", src: "/logos/github.svg" },
    { name: "Salesforce", src: "/logos/salesforce.svg" },
    { name: "Linear", src: "/logos/linear.svg" },
  ],
  [
    { name: "Slack", src: "/logos/slack.svg" },
    { name: "Jira", src: "/logos/jira.svg" },
    { name: "Google Drive", src: "/logos/google-drive.svg" },
    { name: "HubSpot", src: "/logos/hubspot.svg" },
  ],
  [
    { name: "Gmail", src: "/logos/gmail.svg" },
    { name: "Google Calendar", src: "/logos/google-calendar.svg" },
    { name: "Discord", src: "/logos/discord.svg" },
    { name: "Teams", src: "/logos/teams.svg" },
  ],
];

const mobileExtraColumns = [
  [
    { name: "WhatsApp", src: "/logos/whatsapp.svg" },
    { name: "Telegram", src: "/logos/telegram.svg" },
    { name: "Browser", src: "/logos/browser.svg" },
    { name: "Email", src: "/logos/email.svg" },
  ],
  [
    { name: "Google Drive", src: "/logos/google-drive.svg" },
    { name: "Notion", src: "/logos/notion.svg" },
    { name: "Jira", src: "/logos/jira.svg" },
    { name: "Gmail", src: "/logos/gmail.svg" },
  ],
];

export function FifthBentoAnimation() {
  const ref = useRef(null);
  const isInView = useInView(ref, { once: false });

  return (
    <motion.div
      ref={ref}
      initial={{ opacity: 0, y: 16 }}
      animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 16 }}
      transition={{ duration: 0.5, ease: [0.25, 0.1, 0.25, 1] }}
      className="relative flex h-full max-h-[280px] w-full items-center justify-center overflow-hidden"
    >
      <div className="pointer-events-none absolute inset-x-0 top-0 z-10 h-24 bg-gradient-to-b from-background to-transparent" />
      <div className="pointer-events-none absolute inset-x-0 bottom-0 z-10 h-24 bg-gradient-to-t from-background to-transparent" />

      <div className="flex h-full justify-center gap-3 px-8 sm:px-0">
        {desktopColumns.map((tools, colIdx) => (
          <Marquee
            key={colIdx}
            vertical
            reverse={colIdx % 2 === 1}
            className="h-full [--duration:25s] [--gap:0.75rem]"
            repeat={3}
          >
            {tools.map((tool) => (
              <div
                key={tool.name}
                className="flex items-center justify-center p-1"
              >
                <Image
                  src={tool.src}
                  alt={tool.name}
                  width={36}
                  height={36}
                  className="h-8 w-8 sm:h-9 sm:w-9"
                />
              </div>
            ))}
          </Marquee>
        ))}
        {mobileExtraColumns.map((tools, colIdx) => (
          <Marquee
            key={`mobile-${colIdx}`}
            vertical
            reverse={colIdx % 2 === 0}
            className="h-full sm:hidden [--duration:25s] [--gap:0.75rem]"
            repeat={3}
          >
            {tools.map((tool) => (
              <div
                key={tool.name}
                className="flex items-center justify-center p-1"
              >
                <Image
                  src={tool.src}
                  alt={tool.name}
                  width={36}
                  height={36}
                  className="h-8 w-8"
                />
              </div>
            ))}
          </Marquee>
        ))}
      </div>
    </motion.div>
  );
}
