# @eaos/skill-sdk

TypeScript SDK for defining EnterpriseAgentOS skills.

## Install

```bash
npm install @eaos/skill-sdk
```

## Usage

```typescript
import { defineSkill, z } from '@eaos/skill-sdk'

export default defineSkill({
  name: 'my-skill',
  description: 'Does something useful.',

  credentials: {
    api_key: z.string().describe('API key for the service'),
  },

  actions: {
    hello: {
      description: 'Say hello',
      params: z.object({
        name: z.string().describe('Who to greet'),
      }),
      execute: async (params, ctx) => {
        return { message: `Hello, ${params.name}!` }
      },
    },
  },
})
```

Skills are packaged as single-file TypeScript modules that export a `defineSkill()` call. The skill runtime bundles and executes them in a sandboxed environment with injected credentials.
