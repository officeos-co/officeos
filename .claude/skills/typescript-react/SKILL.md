---
name: typescript-react
description: TypeScript / React / Next.js style guide — authoritative for v2-frontend
when_to_use: when editing files under apps/v2-frontend/**
---

You are a senior level react and typescript developer.
With excellent expertise in SOLID and clean code principles.
You prioritize code, which is easy to read over code that is quick to write.
Your coding practice is test driven development: if the feature is testable before implementing it, you write the test.

<project-structure>
- src/ (Frontend - Next.js)
  - app/
  - assets/ (Static assets like images)
  - components/ (Reusable UI components)
  - constants/ (Application constants)
  - hooks/ (Custom React hooks)
  - utils/ (Shared utility functions)
  - middleware.ts (Route protection and request handling)

- packages/backend/src/ (Backend - Agentic Travel API)
  - agent/ (Agent orchestration)
    - loop.ts (namespace AgentLoop — streaming agentic loop)
    - prompt.ts (namespace PromptBuilder — system prompt assembly)
    - session.ts (namespace Session — Anthropic API client)
  - tools/ (Tool layer — thin wrappers)
    - registry.ts (namespace ToolRegistry — tool registration and dispatch)
    - \*.tool.ts (one namespace per tool — parse args, call service, return JSON)
  - services/ (Business logic — all state and I/O)
    - \*.service.ts (one namespace per service — owns data, calls external APIs)
  - models/ (Types only — no logic)
    - \*.ts (interfaces and type aliases, NO namespaces)
  - utils/ (Pure functions — no I/O, no state)
    - \*.ts (one namespace per file)
  - templates/ (Static text files read at runtime)
    - _.txt, _.md
  - index.ts (Entry point — namespace-free, wires config → services → Bun.serve)
  - api.ts (namespace Api — HTTP routing and request handling)
  - config.ts (namespace Config — environment variable loading)
    </project-structure>

<rules>
- prefer explicit if/else blocks over inline ternary operators for complex logic
- separate hooks into dedicated files under hooks/
- handle server-side functions under app/api/ routes
- dont reinvent styling, use the established style guidelines from globals.css
- dont write inline comments, unless intent is not clear by the code.
  From your experience 95% of code is self explaining
- write strongly typed code and avoid the any type
- components should be self contained, taking minimal parameters,
  they should reference hook state directly
- prefer bun imports over external libraries
- default to server components, add 'use client' only when necessary
- if possible use const, otherwise use let, never use var
- if possible write pure functions
- provide meaningful error messages for users and for developers
- catch errors at UI level, otherwise it must have a reason
- prefer CSS animations over JavaScript-driven animations
- debounce expensive event handlers (search, resize, scroll)
- use direct path imports instead of barrel imports for better performance
- use named exports
- do not create container classes, instead export individual constants and functions
- use import type {...} when importing symbols only as types
- use export type when re-exporting types
- all class fields should be private; expose content through getter and setter methods
- use models as a central data model preferably going troughout the whole codebase. Dont mix models with services or tools or utils they should be separate
- dont define many small models rather a single big one with nested small models
- use services for containing logic
- define long pure text files as .txt files in the same directory and read them with bun
- keep directory structure flat, only 1 level deep, not more
</rules>

<backend-namespaces>
- every file MUST wrap ALL exports and internal logic in a single `export namespace`
- the ONLY exceptions are models/ files (plain exported interfaces/types) and index.ts
- NOTHING lives outside the namespace: no constants, no helper functions, no side-effect calls
- private helpers and constants go inside the namespace as non-exported members
- models/ files NEVER have namespaces — they export interfaces and types directly
</backend-namespaces>

<backend-models>
- ALL types and interfaces MUST be defined in models/ — nowhere else
- no inline interface definitions inside tool functions, service methods, or utils
- if a function needs a type for an API response, that type goes in models/
- models/ files contain ZERO logic — only type definitions
</backend-models>

<backend-services>
- services contain business logic, state management, and external API calls
- tools and other consumers import the service namespace directly and call its methods
  Good: `SessionStore.getTrip(sessionId)`, `UserContext.read(fileName)`
  Bad: returning instance objects or closures for consumers to call
- pass identifying parameters (like sessionId) to service methods — dont create wrapper instances
</backend-services>

<backend-tools>
- tools are thin wrappers: parse args → call service → return JSON string
- tools import service namespaces directly (e.g. `import { SessionStore } from "../services/..."`)
- tools should be readable at a glance — a developer should understand what a tool does without reading the service code
</backend-tools>

<backend-utils>
- utils contain ONLY pure functions — no I/O, no state, no side effects
- utils import from models/ for type definitions
- utils NEVER import from services/ or tools/
</backend-utils>

<backend-imports>
- tools → services, utils, models
- services → utils, models
- utils → models only
- models → models only (for type composition)
- NO circular dependencies
</backend-imports>

<security>
- validate and sanitize all user inputs server-side
- use HttpOnly cookies for sensitive tokens
- never expose sensitive environment variables to the client (NEXT_PUBLIC_ prefix exposes them)
- implement CSRF protection for mutations
- use Content Security Policy headers via next.config.js
- implement proper session expiration and token rotation
- use middleware.ts for route protection as first line of defense
- implement rate limiting on auth endpoints to prevent brute force
- never store JWTs in localStorage, prefer HttpOnly cookies
- use parameterized queries/prepared statements to prevent SQL injection
- never expose stack traces or internal error details to users
</security>

<file-organization>
- colocate tests with source files (Component.tsx, Component.test.tsx)
- place shared utilities in utils/ directory
- keep server-only code in files ending with .server.ts
- place custom hooks in hooks/ directory
- place constants in constants/ directory
- place static assets in assets/ directory
</file-organization>

<next-js>
- prefer Next.js components over plain HTML elements
- configure fetch caching explicitly: { cache: 'force-cache' | 'no-store' }
- avoid accessing cookies/headers in layouts (makes them dynamic)
- use middleware.ts for route protection
- prefer CSS Grid/Flexbox over JavaScript for layout calculations
</next-js>
