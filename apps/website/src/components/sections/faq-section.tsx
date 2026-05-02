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
      "OfficeOS is an open-source AI agent platform that deploys autonomous agents across your company. Each agent has persistent memory, a knowledge graph, MCP server access, and responds in the channels your team already uses — Slack, Teams, WhatsApp, Telegram, Discord, and email. Think of it as hiring AI employees that work 24/7 and never need onboarding.",
  },
  {
    question: "Why not just use OpenClaw?",
    answer:
      "OpenClaw is useful for running a single coding agent, but OfficeOS is built for company-wide scale. You get multi-agent orchestration, central credentials, persistent memory, scheduled automation, team channels, observability, and admin controls so hundreds of agents can run across teams without each one becoming a separate project to operate.",
  },
  {
    question: "Why not use managed AI agents (ChatGPT, Gemini, etc.)?",
    answer:
      "Managed agents are closed products you cannot fully audit, self-host, or adapt to your infrastructure. OfficeOS is open source: your team can inspect the code, run it in your own environment, connect your own MCP servers, keep credentials under your control, and avoid locking critical workflows inside someone else's chat product.",
  },
  {
    question: "How do integrations work?",
    answer:
      "OfficeOS uses MCP servers for integrations. Agents connect to approved MCP servers for tools like GitHub, Slack, databases, browsers, internal APIs, and SaaS apps. You can use community MCP servers, run your own, or publish new ones using the same open protocol.",
  },
  {
    question: "Can I self-host OfficeOS?",
    answer:
      "Yes. Run the stack yourself with the backend, dashboard, database, queue, channel gateway, and the MCP servers your agents need. Your data stays in your environment unless you choose OfficeOS Cloud.",
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
      "All credentials are centrally encrypted in the backend. Agents can use approved credentials through controlled tool calls, but they never see raw API keys, OAuth tokens, or bot tokens. OAuth integrations are handled through the dashboard with scoped permissions.",
  },
  {
    question: "Why should we trust you with our data?",
    answer:
      "OfficeOS is fully open-source and self-hostable — you can audit every line of code and run the entire stack on your own infrastructure. Your data never touches our servers unless you choose OfficeOS Cloud. Credentials are encrypted at rest, agents only access approved tools and MCP servers, and integrations use scoped OAuth permissions. OfficeOS Cloud is hosted in Europe. We don't train on your data, we don't sell your data, and we never will.",
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
