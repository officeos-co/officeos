export const siteConfig = {
  name: "Office OS",
  tagline: "The intelligence layer for AI agents",
  url: "https://officeos.dev",
};

export const navConfig = {
  cta: "Start Free",
};

export const heroConfig = {
  headline:
    "Stop managing agents manually. Deploy, connect, and control them from one place.",
  subtitle:
    "Give every team their own AI agent with the right skills, permissions, and access to your organization's knowledge.",
  primaryCta: "Start Free",
  secondaryCta: "Book a Demo",
  visualLabel: "Agent Deployment Animation — Coming Soon",
};

export const socialProofConfig = {
  headline: "Built by engineers from",
  logos: [
    "Company A",
    "Company B",
    "Company C",
    "Company D",
    "Company E",
  ],
};

export const productConfig = {
  headline: "One dashboard for every agent in your organization.",
  sections: [
    {
      title: "Agent Deployment",
      description:
        "Deploy a production-ready agent in under a minute. Kubernetes-native, minimal resources, zero configuration beyond a single environment variable.",
      visualLabel: "Agent Deployment Flow — Coming Soon",
    },
    {
      title: "Custom Skills",
      description:
        "Define skills in TypeScript with full type safety. V8 isolates provide sandboxed execution for your business logic — no shared state, no side effects.",
      visualLabel: "Skill Editor — Coming Soon",
    },
    {
      title: "Knowledge Graph",
      description:
        "Per-team and per-organization knowledge graphs. Agents access the context they need without leaking data across boundaries.",
      visualLabel: "Knowledge Graph Visualization — Coming Soon",
    },
    {
      title: "Central Credentials",
      description:
        "Manage API keys and secrets once. Agents never see raw credentials — the backend injects them per-request through a secure proxy.",
      visualLabel: "Credential Vault — Coming Soon",
    },
  ],
};

export const capabilityConfig = {
  knowledgeGraph: {
    title: "Organization Knowledge Graph",
    description:
      "Every agent draws from a shared understanding of your organization. Teams maintain their own graphs while inheriting from the org-wide knowledge base.",
    visualLabel: "Interactive Knowledge Graph — Coming Soon",
  },
  footprint: {
    title: "Micro Footprint, Massive Scale",
    metrics: [
      { value: "100x", label: "less resources than VM-based agents" },
      { value: "<5ms", label: "cold start time" },
      { value: "MB", label: "not gigabytes — per agent" },
    ],
  },
  skills: {
    title: "Your logic. Your network. Not generic MCP.",
    comparison: {
      generic: {
        label: "Generic MCP",
        points: [
          "Cloud-only execution",
          "No access to internal systems",
          "Shared tool marketplace",
          "One-size-fits-all integrations",
        ],
      },
      custom: {
        label: "Custom Skill",
        points: [
          "Runs in your infrastructure",
          "Full access to internal APIs",
          "Skills tailored to your workflows",
          "Type-safe, sandboxed execution",
        ],
      },
    },
  },
};

export const integrationConfig = {
  headline: "Connect your existing tools in minutes.",
  tools: ["GitHub", "Notion", "Google Workspace", "Slack", "Linear", "Jira"],
};

export const trustConfig = {
  headline: "Infrastructure your IT team will trust.",
  points: [
    {
      title: "Kubernetes-native",
      description: "Deploys as standard K8s workloads. No custom runtimes, no vendor lock-in.",
    },
    {
      title: "Sandboxed execution",
      description: "Every skill runs in an isolated V8 context. No shared memory, no side effects.",
    },
    {
      title: "Role-based permissions",
      description: "Control which agents access which skills and data sources at the org level.",
    },
    {
      title: "Audit logs",
      description: "Every tool call, LLM request, and credential access is logged and traceable.",
    },
  ],
};

export const ctaConfig = {
  headline: "Ready to deploy your first agent?",
  primaryCta: "Start Free",
  secondaryCta: "Book a Demo",
  reassurance:
    "No credit card required. Deploy your first agent in under 5 minutes.",
};

export const footerConfig = {
  tagline: "The intelligence layer for AI agents.",
  columns: [
    {
      title: "Product",
      links: ["Dashboard", "Skills SDK", "Documentation", "Changelog"],
    },
    {
      title: "Company",
      links: ["About", "Blog", "Careers", "Contact"],
    },
    {
      title: "Legal",
      links: ["Privacy", "Terms", "Security"],
    },
  ],
  copyright: "Made in Hamburg — \u00a9 2026 Office OS GmbH",
};
