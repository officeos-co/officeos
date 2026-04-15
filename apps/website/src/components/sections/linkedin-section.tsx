"use client";

import { Marquee } from "@/components/ui/marquee";

const posts = [
  {
    activityId: "7437594727799136256",
    title: "Harro Krog — winning the Disability Tech Hackathon",
  },
  {
    activityId: "7437633585332940800",
    title: "Disability Tech Denmark — hackathon winners",
  },
  {
    activityId: "7437823415782273024",
    title: "DTU Skylab — CUE at the hackathon",
  },
  {
    activityId: "7437879616360353794",
    title: "Elsass Fonden — hackathon winner",
  },
  {
    activityId: "7437857738036191233",
    title: "Danske Iværksættere — hackathon winners",
  },
  {
    activityId: "7437778126568488962",
    title: "Philippa's post about CUE at the hackathon",
  },
];

export function LinkedInSection() {
  return (
    <section className="w-full py-20 overflow-hidden">
      <div className="max-w-7xl mx-auto px-6 mb-12 text-center">
        <p className="text-sm font-medium uppercase tracking-[0.15em] text-muted-foreground mb-3">
          Social proof
        </p>
        <h2 className="text-3xl font-bold tracking-tight md:text-4xl">
          What people are saying
        </h2>
      </div>

      <Marquee pauseOnHover repeat={2} className="[--duration:60s] [--gap:1.5rem]">
        {posts.map((post) => (
          <div
            key={post.activityId}
            className="flex-shrink-0 w-[340px] rounded-xl border border-border overflow-hidden shadow-sm"
          >
            <iframe
              src={`https://www.linkedin.com/embed/feed/update/urn:li:activity:${post.activityId}`}
              title={post.title}
              width="340"
              height="560"
              frameBorder="0"
              loading="lazy"
            />
          </div>
        ))}
      </Marquee>
    </section>
  );
}
