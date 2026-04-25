"use client";

import Image from "next/image";
import Link from "next/link";
import { ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";
import { Marquee } from "@/components/ui/marquee";

const row1 = [
  {
    name: "Sarah",
    username: "@sarah",
    body: "Deployed 5 agents in one afternoon. Each saves us hours daily.",
    img: "https://avatar.vercel.sh/sarah",
  },
  {
    name: "Marcus",
    username: "@marcus",
    body: "We automated 80% of our workflows in a single week.",
    img: "https://avatar.vercel.sh/marcus",
  },
  {
    name: "Priya",
    username: "@priya",
    body: "The agents just work. No babysitting needed.",
    img: "https://avatar.vercel.sh/priya",
  },
  {
    name: "David",
    username: "@david",
    body: "I've never seen anything like this. It's incredible.",
    img: "https://avatar.vercel.sh/david",
  },
  {
    name: "Lisa",
    username: "@lisa",
    body: "Our support response time dropped from hours to seconds.",
    img: "https://avatar.vercel.sh/lisa",
  },
];

const row2 = [
  {
    name: "Tom",
    username: "@tom",
    body: "The knowledge graph connects everything beautifully.",
    img: "https://avatar.vercel.sh/tom",
  },
  {
    name: "Emma",
    username: "@emma",
    body: "Customer support runs 24/7 now. Total game changer.",
    img: "https://avatar.vercel.sh/emma",
  },
  {
    name: "Alex",
    username: "@alex",
    body: "Best infrastructure decision we've made this year.",
    img: "https://avatar.vercel.sh/alex",
  },
  {
    name: "Nina",
    username: "@nina",
    body: "Our agents handle contract review faster than our legal team.",
    img: "https://avatar.vercel.sh/nina",
  },
  {
    name: "Ryan",
    username: "@ryan",
    body: "Set up competitive intelligence in under an hour.",
    img: "https://avatar.vercel.sh/ryan",
  },
];

const row3 = [
  {
    name: "Mia",
    username: "@mia",
    body: "I'm speechless. This has transformed our entire operation.",
    img: "https://avatar.vercel.sh/mia",
  },
  {
    name: "Jake",
    username: "@jake",
    body: "The SDK is so clean. Built custom skills in minutes.",
    img: "https://avatar.vercel.sh/jake",
  },
  {
    name: "Zara",
    username: "@zara",
    body: "Replaced three SaaS tools with a single OfficeOS agent.",
    img: "https://avatar.vercel.sh/zara",
  },
  {
    name: "Leo",
    username: "@leo",
    body: "Self-hosted means we actually own our data. Finally.",
    img: "https://avatar.vercel.sh/leo",
  },
  {
    name: "Chloe",
    username: "@chloe",
    body: "Our sales team loves the lead research agent.",
    img: "https://avatar.vercel.sh/chloe",
  },
];

const row4 = [
  {
    name: "Omar",
    username: "@omar",
    body: "Compliance checking that used to take days now takes minutes.",
    img: "https://avatar.vercel.sh/omar",
  },
  {
    name: "Ivy",
    username: "@ivy",
    body: "The integrations are seamless. Slack, email, everything.",
    img: "https://avatar.vercel.sh/ivy",
  },
  {
    name: "Ben",
    username: "@ben",
    body: "We went from idea to deployed agent in 30 minutes flat.",
    img: "https://avatar.vercel.sh/ben",
  },
  {
    name: "Aria",
    username: "@aria",
    body: "Enterprise-grade but actually easy to use. Rare combo.",
    img: "https://avatar.vercel.sh/aria",
  },
  {
    name: "Max",
    username: "@max",
    body: "The credential isolation gives our security team peace of mind.",
    img: "https://avatar.vercel.sh/max",
  },
];

const row5 = [
  {
    name: "Lena",
    username: "@lena",
    body: "Content strategy agent generates better briefs than our agency.",
    img: "https://avatar.vercel.sh/lena",
  },
  {
    name: "Finn",
    username: "@finn",
    body: "Kubernetes-native means it scales with us. No limits.",
    img: "https://avatar.vercel.sh/finn",
  },
  {
    name: "Sophie",
    username: "@sophie",
    body: "I don't know what to say. This is truly amazing.",
    img: "https://avatar.vercel.sh/sophie",
  },
  {
    name: "Noah",
    username: "@noah",
    body: "Cron jobs + agents = automated everything. We love it.",
    img: "https://avatar.vercel.sh/noah",
  },
  {
    name: "Ella",
    username: "@ella",
    body: "Our team productivity jumped 40% in the first month.",
    img: "https://avatar.vercel.sh/ella",
  },
];

const row6 = [
  {
    name: "Kai",
    username: "@kai",
    body: "Finally an AI platform that doesn't feel like a toy.",
    img: "https://avatar.vercel.sh/kai",
  },
  {
    name: "Ruby",
    username: "@ruby",
    body: "The audit trail makes compliance reporting effortless.",
    img: "https://avatar.vercel.sh/ruby",
  },
  {
    name: "Sam",
    username: "@sam",
    body: "Onboarding was instant. Agents running same day.",
    img: "https://avatar.vercel.sh/sam",
  },
  {
    name: "Vera",
    username: "@vera",
    body: "We cancelled three subscriptions after switching to OfficeOS.",
    img: "https://avatar.vercel.sh/vera",
  },
  {
    name: "Theo",
    username: "@theo",
    body: "I'm at a loss for words. This is amazing. I love it.",
    img: "https://avatar.vercel.sh/theo",
  },
];

const row7 = [
  {
    name: "Iris",
    username: "@iris",
    body: "The agents learn from our knowledge base automatically.",
    img: "https://avatar.vercel.sh/iris",
  },
  {
    name: "Caleb",
    username: "@caleb",
    body: "Security-first design. Our CISO actually approved this.",
    img: "https://avatar.vercel.sh/caleb",
  },
  {
    name: "Hana",
    username: "@hana",
    body: "We run 12 agents now. Each one pays for itself.",
    img: "https://avatar.vercel.sh/hana",
  },
  {
    name: "Ravi",
    username: "@ravi",
    body: "TypeScript SDK made building custom skills trivial.",
    img: "https://avatar.vercel.sh/ravi",
  },
  {
    name: "Yuki",
    username: "@yuki",
    body: "Best developer experience of any AI platform we've tried.",
    img: "https://avatar.vercel.sh/yuki",
  },
];

function ReviewCard({
  img,
  name,
  username,
  body,
}: {
  img: string;
  name: string;
  username: string;
  body: string;
}) {
  return (
    <figure
      className={cn(
        "relative h-full w-44 cursor-pointer overflow-hidden rounded-lg border p-2.5",
        "border-gray-950/[.1] bg-gray-950/[.01] hover:bg-gray-950/[.05]",
      )}
    >
      <div className="flex flex-row items-center gap-1.5">
        <img className="rounded-full" width="18" height="18" alt="" src={img} />
        <div className="flex flex-col">
          <figcaption className="text-[11px] font-medium">{name}</figcaption>
          <p className="text-[9px] font-medium text-muted-foreground">
            {username}
          </p>
        </div>
      </div>
      <blockquote className="mt-1 text-[10px] leading-snug">{body}</blockquote>
    </figure>
  );
}

export function CTASection() {
  return (
    <section
      id="cta"
      className="flex w-full flex-col items-center justify-center"
    >
      <div className="w-full">
        <div className="relative z-20 h-[500px] w-full overflow-hidden rounded-xl border border-border bg-muted/30 shadow-xl md:h-[480px]">
          {/* Marquee quote cards - 7 rows, steep angle */}
          <div className="-mx-[50%] -ml-10 rotate-[35deg] scale-[1.1] origin-center">
            <Marquee className="[--duration:30s]">
              {row1.map((r) => (
                <ReviewCard key={r.username} {...r} />
              ))}
            </Marquee>
            <Marquee reverse className="[--duration:35s]">
              {row2.map((r) => (
                <ReviewCard key={r.username} {...r} />
              ))}
            </Marquee>
            <Marquee className="[--duration:28s]">
              {row3.map((r) => (
                <ReviewCard key={r.username} {...r} />
              ))}
            </Marquee>
            <Marquee reverse className="[--duration:32s]">
              {row4.map((r) => (
                <ReviewCard key={r.username} {...r} />
              ))}
            </Marquee>
            <Marquee className="[--duration:26s]">
              {row5.map((r) => (
                <ReviewCard key={r.username} {...r} />
              ))}
            </Marquee>
            <Marquee reverse className="[--duration:33s]">
              {row6.map((r) => (
                <ReviewCard key={r.username} {...r} />
              ))}
            </Marquee>
            <Marquee className="[--duration:29s]">
              {row7.map((r) => (
                <ReviewCard key={r.username} {...r} />
              ))}
            </Marquee>
          </div>

          {/* White gradient overlay from bottom */}
          <div className="pointer-events-none absolute inset-0 bg-gradient-to-t from-white via-white/95 via-40% to-transparent" />
          {/* Side fades */}
          <div className="pointer-events-none absolute inset-y-0 left-0 w-1/3 bg-gradient-to-r from-white/90" />
          <div className="pointer-events-none absolute inset-y-0 right-0 w-1/3 bg-gradient-to-l from-white/90" />
          {/* Top fade */}
          <div className="pointer-events-none absolute inset-x-0 top-0 h-16 bg-gradient-to-b from-white/60" />

          {/* Centered content */}
          <div className="absolute inset-0 flex flex-col items-center justify-center">
            {/* Glass app icon — matching navbar glass style */}
            <div className="mb-5 flex h-32 w-32 items-center justify-center rounded-3xl border border-border bg-background/75 shadow-2xl backdrop-blur-lg">
              <Image src="/logo.svg" alt="OfficeOS" width={100} height={100} />
            </div>

            <h2 className="text-center font-semibold text-2xl tracking-tight text-primary md:text-3xl">
              Ready to deploy your first agent?
            </h2>
            <p className="mt-2 text-center text-sm text-muted-foreground">
              Start free with your own API keys. No credit card required.
            </p>

            <Link
              href="https://dashboard.officeos.co"
              className="btn-glow mt-5 flex h-10 items-center gap-1.5 rounded-full bg-secondary px-5 text-sm font-medium text-white tracking-wide transition-all hover:bg-secondary/80 active:scale-95"
            >
              Get Started
              <ChevronRight className="h-4 w-4" />
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
