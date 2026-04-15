"use client";

import Link from "next/link";
import { Marquee } from "@/components/ui/marquee";

const posts = [
  {
    quote:
      "Innovation for inclusion truly came to life today. 16 strong teams pitched solutions across invisible disabilities, communication, and caregiving — and Team CUE took home the win.",
    author: "Disability Tech Denmark",
    title: "Official organiser · Disability Tech Hackathon 2026",
    initials: "DT",
    url: "https://www.linkedin.com/feed/update/urn:li:activity:7437633585332940800",
  },
  {
    quote:
      "If you make products that can be used by all people, you also get a wider reach. ~30% of Denmark's population experiences some form of disability.",
    author: "Niels Hygum Nielsen",
    title: "Junior Programme Coordinator · DTU Skylab",
    initials: "NH",
    url: "https://www.linkedin.com/feed/update/urn:li:activity:7437823415782273024",
  },
  {
    quote:
      "Thoughtful, creative solutions delivered in such a short time. The teams showed what is possible when you build with real users in mind.",
    author: "Tobias Nyhuus Jensen",
    title: "EU National Accessibility Expert · Jury member",
    initials: "TJ",
    url: "https://www.linkedin.com/feed/update/urn:li:activity:7437778126568488962",
  },
  {
    quote:
      "Disability tech is more than a niche — it is a frontier for innovation that makes the world better for everyone.",
    author: "Danske Iværksættere",
    title: "Danish Entrepreneurs · 6 000 members",
    initials: "DI",
    url: "https://www.linkedin.com/feed/update/urn:li:activity:7437857738036191233",
  },
  {
    quote:
      "A field full of possibilities. Nearly 100 students, people with disabilities, and domain experts came together to prove what accessible innovation looks like.",
    author: "Elsass Fonden",
    title: "Danish foundation · Hackathon partner",
    initials: "EF",
    url: "https://www.linkedin.com/feed/update/urn:li:activity:7437879616360353794",
  },
  {
    quote:
      "Our product guides you on your travels and takes your hand when you need it the most. Proud to have won the Disability Tech Hackathon at Microsoft Denmark.",
    author: "Harro Krog",
    title: "Founder · OfficeOS",
    initials: "HK",
    url: "https://www.linkedin.com/feed/update/urn:li:activity:7437594727799136256",
  },
];

function LinkedInIcon() {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="currentColor"
      className="h-4 w-4"
      aria-hidden="true"
    >
      <path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433a2.062 2.062 0 0 1-2.063-2.065 2.064 2.064 0 1 1 2.063 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z" />
    </svg>
  );
}

function QuoteCard({
  quote,
  author,
  title,
  initials,
  url,
}: (typeof posts)[number]) {
  return (
    <div className="flex h-full w-[320px] flex-col justify-between rounded-xl border border-border bg-background p-6 shadow-sm">
      <p className="text-sm leading-relaxed text-muted-foreground">
        &ldquo;{quote}&rdquo;
      </p>
      <div className="mt-6 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-semibold text-primary">
            {initials}
          </div>
          <div>
            <p className="text-sm font-semibold text-primary">{author}</p>
            <p className="text-xs text-muted-foreground">{title}</p>
          </div>
        </div>
        <Link
          href={url}
          target="_blank"
          rel="noopener noreferrer"
          aria-label={`View ${author}'s post on LinkedIn`}
          className="shrink-0 text-muted-foreground transition-colors hover:text-[#0A66C2]"
        >
          <LinkedInIcon />
        </Link>
      </div>
    </div>
  );
}

export function LinkedInSection() {
  return (
    <section className="w-full divide-y divide-border">
      <div className="px-6 py-16 text-center">
        <h2 className="text-2xl font-bold tracking-tight md:text-3xl">
          What people are saying
        </h2>
        <p className="mt-3 text-muted-foreground">
          From the judges, organisers, and partners at the Disability Tech
          Hackathon 2026
        </p>
      </div>

      <div className="py-10">
        <Marquee
          pauseOnHover
          repeat={2}
          className="[--duration:50s] [--gap:1.25rem]"
        >
          {posts.map((post) => (
            <QuoteCard key={post.url} {...post} />
          ))}
        </Marquee>
      </div>
    </section>
  );
}
