import { SectionHeader } from "@/components/section-header";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";

const faqItems = [
  {
    question: "What is OfficeOS?",
    answer:
      "OfficeOS is an AI agent platform that deploys autonomous agents across your company. Each agent has persistent memory, a knowledge graph, custom skills, and responds in the channels your team already uses — Slack, Teams, WhatsApp, Telegram, Discord, and email. Think of it as hiring AI employees that work 24/7 and never need onboarding.",
  },
  {
    question: "Why not just use OpenClaw / open-source agent frameworks?",
    answer:
      "Open-source agent frameworks are great for prototyping a single agent, but they don't scale to production. They lack multi-tenant credential management, persistent memory across conversations, scheduled automation, multi-channel routing, and centralized observability. OfficeOS gives you the full infrastructure layer — from skill sandboxing to knowledge graphs to team-wide billing — so you're not rebuilding it yourself every time you deploy a new agent.",
  },
  {
    question: "Why not use managed AI agents (ChatGPT, Gemini, etc.)?",
    answer:
      "Managed agents like ChatGPT or Gemini are chatbots with MCP server access — not deeply integrated agents. They can't run scheduled tasks, persist memory across sessions, execute custom code on your infrastructure, or respond autonomously in your team's Slack channels. OfficeOS agents are actual autonomous workers with their own container runtime, cron schedules, and full access to your internal systems — not a chat window with plugins.",
  },
  {
    question: "How do skills work?",
    answer:
      "Skills are TypeScript modules executed in sandboxed runtimes. You define them with the Skill SDK using Zod schemas, and they run on your infrastructure with access to internal APIs, databases, and third-party services. Skills can be attached to any agent and shared across your organization.",
  },
  {
    question: "Can I self-host OfficeOS?",
    answer:
      "Yes. Run the full stack with docker compose up — Postgres, Redis, backend, dashboard, skill runtime, and channel gateway all start automatically. For production, OfficeOS is Kubernetes-native. Your data never leaves your network.",
  },
  {
    question: "What channels do agents support?",
    answer:
      "Slack, Microsoft Teams, Discord, Telegram, WhatsApp, and email. Agents respond in the channels your team already uses — no new tools to adopt.",
  },
  {
    question: "What LLM providers are supported?",
    answer:
      "Anthropic, OpenAI, Google, xAI, Groq, DeepSeek, OpenRouter, and Ollama. Bring your own API keys or use platform-managed keys on OfficeOS Cloud. You can configure providers per-agent and switch at any time.",
  },
  {
    question: "How are credentials managed?",
    answer:
      "All credentials are centrally encrypted in the backend. Agent containers never see raw API keys — the backend injects them per-request through a secure proxy. OAuth integrations (Google, GitHub, etc.) are handled through the dashboard with scoped permissions.",
  },
  {
    question: "Why should we trust you with our data?",
    answer:
      "OfficeOS is fully open-source and self-hostable — you can audit every line of code and run the entire stack on your own infrastructure. Your data never touches our servers unless you choose OfficeOS Cloud. Credentials are encrypted at rest, agents run in isolated containers, and all integrations use scoped OAuth permissions. OfficeOS Cloud is hosted in Europe. We don't train on your data, we don't sell your data, and we never will.",
  },
];

export function FAQSection() {
  return (
    <section
      id="faq"
      className="relative flex w-full flex-col items-center justify-center gap-10 pb-10"
    >
      <SectionHeader>
        <h2 className="text-balance text-center font-medium text-3xl tracking-tighter md:text-4xl">
          Frequently Asked Questions
        </h2>
        <p className="text-balance text-center font-medium text-muted-foreground">
          Common questions about OfficeOS and how it works.
        </p>
      </SectionHeader>

      <div className="mx-auto w-full max-w-3xl px-10">
        <Accordion
          type="single"
          collapsible
          className="grid w-full gap-2 border-b-0"
        >
          {faqItems.map((faq, index) => (
            <AccordionItem
              key={index}
              value={index.toString()}
              className="grid gap-2 border-0"
            >
              <AccordionTrigger className="cursor-pointer rounded-lg border border-border bg-accent px-4 py-3.5 no-underline hover:no-underline data-[state=open]:ring data-[state=open]:ring-primary/20">
                {faq.question}
              </AccordionTrigger>
              <AccordionContent className="rounded-lg border bg-accent p-3 text-primary">
                <p className="font-medium text-primary leading-relaxed">
                  {faq.answer}
                </p>
              </AccordionContent>
            </AccordionItem>
          ))}
        </Accordion>
      </div>
    </section>
  );
}
