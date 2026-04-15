import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { containers } from "./cli/containers.ts";
import { images } from "./cli/images.ts";
import { volumes } from "./cli/volumes.ts";
import { networks } from "./cli/networks.ts";
import { compose } from "./cli/compose.ts";
import { system } from "./cli/system.ts";

export default defineSkill({
  name: "docker",
  title: "Docker",
  emoji: "🐳",
  description:
    "Manage Docker containers, images, volumes, networks, Compose stacks, and system resources via the Docker Engine REST API.",
  doc,
  credentials: {
    host: {
      label: "Docker Host URL",
      kind: "text",
      placeholder: "http://localhost:2375",
      help: "Docker Engine API endpoint. Ensure the API is exposed (e.g. via dockerd -H tcp://0.0.0.0:2375).",
    },
  },
  actions: {
    ...containers,
    ...images,
    ...volumes,
    ...networks,
    ...compose,
    ...system,
  },
});
