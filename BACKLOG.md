# MAKE SDK WORK IN ANY LANGUAGE

Extism (extism.org) — WASM-based plugin system. Developers write skills in any
language (Rust, Go, Python, JS, C), compile to .wasm, and your runtime loads
them into a single process. No containers per skill, no build pipelines. SDKs
exist for most languages. You'd get the "upload a zip" DX you have now but
language-agnostic. The host (your skill-runtime) provides host functions for
HTTP, credentials, logging — which maps directly to your SkillContext.

OpenFaaS — if you want the container-per-skill model. Handles build, deploy,
scaling on K8s. But it's a whole platform to operate and overkill for your
scale.

No. You wouldn't sacrifice anything. Extism gives you:

- Same single-process model (no containers per skill)
- Same upload-a-file DX (.wasm instead of .zip)
- Same sandboxing (WASM is more isolated than Node.js eval, actually)
- Same host-controlled I/O (you provide fetch/credentials as host functions,
  just like SkillContext)
- Any language that compiles to WASM

The only cost is time:

1. Rewrite the skill-runtime executor to load .wasm modules instead of esbuild
   bundles
2. Write the host functions (fetch, log, credentials) as WASM imports
3. Build a compile step per language (TS→WASM, Python→WASM, Go→WASM)
4. Rewrite or adapt the three existing skills to target WASM
5. The manifest/defineSkill contract stays the same — it's just how execution
   happens underneath

No architectural trade-off, no new services to operate, no K8s complexity
increase. It's a pure upgrade path. The reason I said "don't do it yet" is only
time — it's weeks of work to replace something that works fine today for a
capability nobody is asking for yet.

---

# Add loading placeholder skeleton

Just add meaningful skeleton placeholders. Especially an anti pattern is that we sometimes show wrong default values instead of waiting for the correct values. Add meaningful error popups if something goes wrong.
Especially in enterprise rather show something got wrong than showing something wrongly

---

# Agent visualization

This is not refined right now but it would be cool to see like dependencys between agents and skills.
Like for paperclip which shows the org structure.

---

Make agent panel less tab heavy. Claude also does have managed agents it also has the same functional sidebar layout and it shows that you can put all the menaingful information on one page.

Claude only has one prompt, mcp tools ![alt text](image.png)![alt text](image-1.png) but it shows how to abstract tools well. Basically the abstraction is that you dont have to separarte tools and skills. Also skills i the industry has been established as just knoweldge.
Our abstraction for a skill hub is thus incorrect we should reframe them as tools. Thus The agent detaiils should follow claudes layout. I would probably propose one agent tab which includes a chat -> system prompt -> tools

The second tab sohuld be sessions.

Then we would want a logs tab

And a memory tab which also isnt perfect currently.The problem with system prompt in claude agents is that it is really just a single prompt. In our implementation its made up of several files. We would need to brainstorm about that. But id say openclaw established
that a system prompt made out of those files is good. WE shouldnt reduce t hat. So id probably propose just put Prompt into a separate tab since its complex.
And separate memory from prompt although both should be stored in obsidian

\*Conclusion after talking:

---

# Obsidian

We want to use obsidian as the only source for knoweldge. I know that partially it has already been established that at least the system prompt is pulled from couchdb.
But we need to provide the agent with a meaningful way of interacting with the organizations knowledge graph.

We have built the skill for it /Users/harrokrog/Desktop/EnterpriseAgentOs/packages/skills/obsidian which is the skill to interact with the knowledge graph trough the cli. But maybe we would need to make it actually native tools. But the cli is so komplex and we need that complexity to all persist. So Id suggest we should talk about that again

This org wide knoweldge graph also needs to be a new tab in the sidebar.

\*Solution after talking:
