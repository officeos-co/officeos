import { describe, test, expect, beforeAll } from "bun:test"
import { buildClientSchema, getIntrospectionQuery, parse, validate } from "graphql"
import { readFileSync, readdirSync } from "fs"
import { join } from "path"

const SCHEMA_URL =
  process.env.GRAPHQL_SCHEMA_URL ?? "https://api.officeos.co/api/dashboard/graphql"

const HOOKS_DIR = join(import.meta.dir, "..", "hooks")

/** Extract all gql`` template bodies from a TypeScript source file. */
function extractGqlBodies(source: string): string[] {
  const bodies: string[] = []
  const re = /gql\s*`([\s\S]*?)`/g
  let m: RegExpExecArray | null
  while ((m = re.exec(source)) !== null) {
    bodies.push(m[1])
  }
  return bodies
}

/** Collect all operations from hook files. */
function collectOperations(): Array<{ file: string; name: string; body: string }> {
  const hookFiles = readdirSync(HOOKS_DIR).filter((f) => f.endsWith(".ts"))
  const ops: Array<{ file: string; name: string; body: string }> = []
  for (const file of hookFiles) {
    const source = readFileSync(join(HOOKS_DIR, file), "utf-8")
    const bodies = extractGqlBodies(source)
    for (const body of bodies) {
      const nameMatch = body.match(/(?:query|mutation|subscription)\s+(\w+)/)
      const name = nameMatch?.[1] ?? "anonymous"
      ops.push({ file, name, body })
    }
  }
  return ops
}

const operations = collectOperations()

describe("GraphQL contract tests", () => {
  let schema: ReturnType<typeof buildClientSchema>

  beforeAll(async () => {
    const res = await fetch(SCHEMA_URL, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ query: getIntrospectionQuery() }),
    })
    if (!res.ok) throw new Error(`Schema fetch failed: ${res.status} ${res.statusText}`)
    const json = (await res.json()) as { data: Parameters<typeof buildClientSchema>[0] }
    schema = buildClientSchema(json.data)
  })

  for (const op of operations) {
    test(`${op.name} (${op.file})`, () => {
      const doc = parse(op.body)
      const errors = validate(schema, doc)
      if (errors.length > 0) {
        const msgs = errors.map((e) => e.message).join("\n  ")
        throw new Error(`${op.name} failed validation:\n  ${msgs}`)
      }
    })
  }
})
