"use client";

import Link from "next/link";
import { Marquee } from "@/components/ui/marquee";

const posts = [
  {
    quote:
      "Innovation for inclusion truly came to life today — Team CUE took home the win",
    author: "Disability Tech Denmark",
    title: "Official organiser · Disability Tech Hackathon 2026",
    url: "https://www.linkedin.com/feed/update/urn:li:activity:7437633585332940800",
  },
  {
    quote:
      "If you make products that can be used by all people, you also get a wider reach",
    author: "Niels Hygum Nielsen",
    title: "Junior Programme Coordinator · DTU Skylab",
    url: "https://www.linkedin.com/feed/update/urn:li:activity:7437823415782273024",
  },
  {
    quote:
      "Thoughtful, creative solutions delivered in such a short time",
    author: "Tobias Nyhuus Jensen",
    title: "EU National Accessibility Expert · Jury member",
    url: "https://www.linkedin.com/feed/update/urn:li:activity:7437778126568488962",
  },
  {
    quote:
      "Disability tech is more than a niche — it is a frontier for innovation that makes the world better for everyone",
    author: "Danske Iværksættere",
    title: "Danish Entrepreneurs · 6 000 members",
    url: "https://www.linkedin.com/feed/update/urn:li:activity:7437857738036191233",
  },
  {
    quote:
      "A field full of possibilities — nearly 100 students, experts, and people with disabilities came together",
    author: "Elsass Fonden",
    title: "Danish foundation · Hackathon partner",
    url: "https://www.linkedin.com/feed/update/urn:li:activity:7437879616360353794",
  },
  {
    quote:
      "Our product guides you on your travels and takes your hand when you need it the most",
    author: "Harro Krog",
    title: "Founder · OfficeOS",
    url: "https://www.linkedin.com/feed/update/urn:li:activity:7437594727799136256",
  },
];

function LinkedInIcon() {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="currentColor"
      className="h-3.5 w-3.5"
      aria-hidden="true"
    >
      <path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433a2.062 2.062 0 0 1-2.063-2.065 2.064 2.064 0 1 1 2.063 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z" />
    </svg>
  );
}

function QuoteCard({ quote, author, title, url }: (typeof posts)[number]) {
  return (
    <div className="flex w-[300px] flex-col gap-4 rounded-lg border border-border bg-accent/50 p-5">
      <span className="text-2xl font-serif leading-none text-secondary select-none">
        &ldquo;
      </span>
      <p className="line-clamp-2 text-sm leading-relaxed text-muted-foreground -mt-2">
        {quote}
      </p>
      <div className="flex items-center justify-between pt-1 border-t border-border">
        <div className="min-w-0">
          <p className="text-sm font-semibold text-primary truncate">{author}</p>
          <p className="text-xs text-muted-foreground truncate">{title}</p>
        </div>
        <Link
          href={url}
          target="_blank"
          rel="noopener noreferrer"
          aria-label={`View ${author}'s post on LinkedIn`}
          className="ml-3 shrink-0 text-muted-foreground transition-colors hover:text-[#0A66C2]"
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
        <p className="mt-3 text-sm text-muted-foreground">
          Judges, organisers and partners at the Disability Tech Hackathon 2026
        </p>
      </div>

      <div className="py-10">
        <Marquee
          pauseOnHover
          repeat={2}
          className="[--duration:50s] [--gap:1rem]"
        >
          {posts.map((post) => (
            <QuoteCard key={post.url} {...post} />
          ))}
        </Marquee>
      </div>
    </section>
  );
}
