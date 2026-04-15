import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { pods } from "./cli/pods.ts";
import { deployments } from "./cli/deployments.ts";
import { services } from "./cli/services.ts";
import { configmaps } from "./cli/configmaps.ts";
import { secrets } from "./cli/secrets.ts";
import { namespaces } from "./cli/namespaces.ts";
import { nodes } from "./cli/nodes.ts";
import { jobs } from "./cli/jobs.ts";
import { apply } from "./cli/apply.ts";
import { context } from "./cli/context.ts";

export default defineSkill({
  name: "kubernetes",
  title: "Kubernetes",
  emoji: "⚙️",
  description:
    "Manage Kubernetes clusters, workloads, services, config, nodes, and jobs via the Kubernetes REST API (kubectl parity).",
  doc,
  credentials: {
    api_url: {
      label: "API Server URL",
      kind: "text",
      placeholder: "https://kubernetes.default.svc",
      help: "Kubernetes API server URL. Usually https://<cluster-ip>:6443 or the URL from kubeconfig.",
    },
    token: {
      label: "Bearer Token",
      kind: "password",
      placeholder: "eyJhbGciOi...",
      help: "Service account or user bearer token for API authentication.",
    },
    namespace: {
      label: "Default Namespace",
      kind: "text",
      placeholder: "default",
      help: "Default namespace used when --namespace is not specified.",
    },
  },
  actions: {
    ...pods,
    ...deployments,
    ...services,
    ...configmaps,
    ...secrets,
    ...namespaces,
    ...nodes,
    ...jobs,
    ...apply,
    ...context,
  },
});
