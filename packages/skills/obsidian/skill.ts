import { defineSkill } from "@harro/skill-sdk";
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
  name: "obsidian",
  title: "Obsidian",
  emoji: "\ud83d\udcce",
  description:
    "Manage an Obsidian vault backed by CouchDB -- read, write, search, graph, tags, templates.",
  doc,
  credentials: {
    couchdb_url: {
      label: "CouchDB URL",
      kind: "text",
      placeholder: "https://couch.example.com",
      help: "Full URL to your CouchDB instance (Self-hosted LiveSync backend).",
    },
    couchdb_user: { label: "CouchDB Username", kind: "text", placeholder: "admin" },
    couchdb_password: { label: "CouchDB Password", kind: "password" },
    vault_name: {
      label: "Vault Name",
      kind: "text",
      placeholder: "my-vault",
      help: "CouchDB database name for this vault.",
    },
  },
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
