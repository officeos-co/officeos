import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
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
  ...manifest,
  doc,
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
