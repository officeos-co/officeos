import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { crud } from "./cli/crud.ts";
import { files } from "./cli/files.ts";
import { graph } from "./cli/graph.ts";
import { search } from "./cli/search.ts";
import { tags } from "./cli/tags.ts";
import { properties } from "./cli/properties.ts";
import { templates } from "./cli/templates.ts";
import { relocate } from "./cli/relocate.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: {
    ...crud,
    ...files,
    ...graph,
    ...search,
    ...tags,
    ...properties,
    ...templates,
    ...relocate,
  },
});
