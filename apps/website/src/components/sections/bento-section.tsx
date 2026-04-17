"use client";

import { SectionHeader } from "@/components/section-header";
import { ChannelOrbitAnimation } from "@/components/channel-orbit-animation";
import { KnowledgeGraphAnimation } from "@/components/knowledge-graph-animation";
import { CronJobsAnimation } from "@/components/cron-jobs-animation";
import { ToolStackAnimation } from "@/components/tool-stack-animation";
import { DeploymentSwimLane } from "@/components/deployment-swim-lane";
import { CredentialsAnimation } from "@/components/credentials-animation";

const items = [
  {
    id: 1,
    content: <ChannelOrbitAnimation />,
    title: "Meet Your Team Where They Work",
    description:
      "Agents respond in the channels your team already uses — Slack, Teams, WhatsApp, email, or any webhook.",
    wide: true,
    className: "md:col-span-2 lg:col-span-2",
  },
  {
    id: 2,
    content: <KnowledgeGraphAnimation />,
    title: "Enterprise Knowledge Graph",
    description:
      "Every agent taps into your company's knowledge. Contracts, docs, past decisions — always in context.",
    wide: false,
    className: "lg:col-span-1",
  },
  {
    id: 3,
    content: <CronJobsAnimation once={false} />,
    title: "Automated Schedules",
    description:
      "Set it and forget it. Agents scan, report, sync, and brief — autonomously, on your timeline.",
    wide: false,
    className: "lg:col-span-1",
  },
  {
    id: 4,
    content: <ToolStackAnimation />,
    title: "Works With Your Stack",
    description:
      "Agents plug into the tools your team already uses. One-click setup, no code required.",
    wide: true,
    className: "md:col-span-2 lg:col-span-2",
  },
  {
    id: 5,
    content: <DeploymentSwimLane />,
    title: "Scale to Hundreds of Agents",
    description:
      "Launch an entire workforce in seconds. Scale from one agent to hundreds with zero additional infrastructure cost — every agent runs at peak performance, no matter the size of your fleet.",
    wide: true,
    className: "md:col-span-2 lg:col-span-2",
  },
  {
    id: 6,
    content: <CredentialsAnimation />,
    title: "Central Credentials",
    description:
      "API keys and secrets managed once, used everywhere. Your agents stay secure without any manual setup per tool.",
    wide: false,
    className: "lg:col-span-1",
  },
];

export function BentoSection() {
  return (
    <section
      id="bento"
      className="relative flex w-full flex-col items-center justify-center px-5 md:px-10"
    >
      <div className="relative mx-5 border-x md:mx-10">
        <div className="absolute top-0 -left-4 h-full w-4 bg-[size:10px_10px] text-primary/5 [background-image:repeating-linear-gradient(315deg,currentColor_0_1px,#0000_0_50%)] md:-left-14 md:w-14"></div>
        <div className="absolute top-0 -right-4 h-full w-4 bg-[size:10px_10px] text-primary/5 [background-image:repeating-linear-gradient(315deg,currentColor_0_1px,#0000_0_50%)] md:-right-14 md:w-14"></div>

        <SectionHeader>
          <h2 className="text-balance pb-1 text-center font-medium text-3xl tracking-tighter md:text-4xl">
            Everything your agents need to operate.
          </h2>
          <p className="text-balance text-center font-medium text-muted-foreground">
            Knowledge, tools, channels, schedules, credentials — managed from
            one dashboard.
          </p>
        </SectionHeader>

        <div className="grid grid-cols-1 overflow-hidden md:grid-cols-2 lg:grid-cols-3">
          {items.map((item) => (
            <div
              key={item.id}
              className={`group relative flex min-h-[420px] cursor-pointer flex-col items-start justify-end p-0.5 before:absolute before:top-0 before:-left-0.5 before:z-10 before:h-full before:w-px before:bg-border before:content-[''] after:absolute after:-top-0.5 after:left-0 after:z-10 after:h-px after:w-full after:bg-border after:content-[''] ${item.className}`}
            >
              <div className="relative flex size-full h-full items-center justify-center overflow-hidden">
                {item.content}
              </div>
              <div
                className={`flex-1 flex-col gap-2 p-6 ${item.wide ? "lg:max-w-2xl" : ""}`}
              >
                <h3 className="font-semibold text-lg tracking-tighter">
                  {item.title}
                </h3>
                <p className="text-sm text-muted-foreground">
                  {item.description}
                </p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
