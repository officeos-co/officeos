import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { containers } from "./cli/containers.ts";
import { images } from "./cli/images.ts";
import { volumes } from "./cli/volumes.ts";
import { networks } from "./cli/networks.ts";
import { compose } from "./cli/compose.ts";
import { system } from "./cli/system.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: {
    ...containers,
    ...images,
    ...volumes,
    ...networks,
    ...compose,
    ...system,
  },
});
