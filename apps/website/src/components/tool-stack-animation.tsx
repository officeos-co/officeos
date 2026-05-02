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
    { name: "Zapier", src: "/logos/zapier.svg" },
    { name: "Tableau", src: "/logos/tableau.svg" },
    { name: "MongoDB", src: "/logos/mongodb.svg" },
  ],
  [
    { name: "Jira", src: "/logos/jira.svg" },
    { name: "Google Drive", src: "/logos/google-drive.svg" },
    { name: "HubSpot", src: "/logos/hubspot.svg" },
    { name: "Figma", src: "/logos/figma.svg" },
    { name: "Snowflake", src: "/logos/snowflake.svg" },
    { name: "Power BI", src: "/logos/powerbi.svg" },
    { name: "Supabase", src: "/logos/supabase.svg" },
  ],
  [
    { name: "Gmail", src: "/logos/gmail.svg" },
    { name: "Google Calendar", src: "/logos/google-calendar.svg" },
    { name: "Stripe", src: "/logos/stripe.svg" },
    { name: "Google Sheets", src: "/logos/google-sheets.svg" },
    { name: "Google Meet", src: "/logos/google-meet.svg" },
    { name: "Google Analytics", src: "/logos/google-analytics.svg" },
    { name: "Google Docs", src: "/logos/google-docs.svg" },
  ],
  [
    { name: "Confluence", src: "/logos/confluence.svg" },
    { name: "Airtable", src: "/logos/airtable.svg" },
    { name: "Intercom", src: "/logos/intercom.svg" },
    { name: "Zendesk", src: "/logos/zendesk.svg" },
    { name: "Sentry", src: "/logos/sentry.svg" },
    { name: "Datadog", src: "/logos/datadog.svg" },
    { name: "PagerDuty", src: "/logos/pagerduty.svg" },
  ],
  [
    { name: "Trello", src: "/logos/trello.svg" },
    { name: "Shopify", src: "/logos/shopify.svg" },
    { name: "Dropbox", src: "/logos/dropbox.svg" },
    { name: "Asana", src: "/logos/asana.svg" },
    { name: "Monday", src: "/logos/monday.svg" },
    { name: "Twilio", src: "/logos/twilio.svg" },
    { name: "SendGrid", src: "/logos/sendgrid.svg" },
  ],
  [
    { name: "Outlook", src: "/logos/outlook.svg" },
    { name: "Excel", src: "/logos/excel.svg" },
    { name: "OneDrive", src: "/logos/onedrive.svg" },
    { name: "SharePoint", src: "/logos/sharepoint.svg" },
    { name: "Word", src: "/logos/word.svg" },
    { name: "PowerPoint", src: "/logos/powerpoint.svg" },
    { name: "Teams", src: "/logos/teams.svg" },
  ],
  [
    { name: "AWS", src: "/logos/aws.svg" },
    { name: "Azure", src: "/logos/azure.svg" },
    { name: "Google Cloud", src: "/logos/google-cloud.svg" },
    { name: "Stripe", src: "/logos/stripe.svg" },
    { name: "Zapier", src: "/logos/zapier.svg" },
    { name: "Terraform", src: "/logos/terraform.svg" },
    { name: "Redis", src: "/logos/redis.svg" },
  ],
];

const mobileExtraColumns = [
  [
    { name: "Outlook", src: "/logos/outlook.svg" },
    { name: "Excel", src: "/logos/excel.svg" },
    { name: "SharePoint", src: "/logos/sharepoint.svg" },
    { name: "Google Docs", src: "/logos/google-docs.svg" },
    { name: "AWS", src: "/logos/aws.svg" },
    { name: "Sentry", src: "/logos/sentry.svg" },
  ],
  [
    { name: "Google Drive", src: "/logos/google-drive.svg" },
    { name: "Stripe", src: "/logos/stripe.svg" },
    { name: "OneDrive", src: "/logos/onedrive.svg" },
    { name: "Gmail", src: "/logos/gmail.svg" },
    { name: "Azure", src: "/logos/azure.svg" },
    { name: "Datadog", src: "/logos/datadog.svg" },
  ],
];

export function ToolStackAnimation() {
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
      <div className="pointer-events-none absolute inset-y-0 left-0 z-10 w-16 bg-gradient-to-r from-background to-transparent" />
      <div className="pointer-events-none absolute inset-y-0 right-0 z-10 w-16 bg-gradient-to-l from-background to-transparent" />

      <div className="flex h-full w-full justify-center gap-4 px-8 sm:px-4">
        {desktopColumns.map((tools, colIdx) => (
          <Marquee
            key={colIdx}
            vertical
            reverse={colIdx % 2 === 1}
            className={`h-full [--duration:55s] [--gap:0.75rem] ${colIdx >= 3 ? "hidden md:flex" : ""}`}
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
            className="h-full sm:hidden [--duration:55s] [--gap:0.75rem]"
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
