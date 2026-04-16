import type { CodegenConfig } from "@graphql-codegen/cli"

const config: CodegenConfig = {
  overwrite: true,
  schema: process.env.GRAPHQL_SCHEMA_URL ?? "http://localhost:5000/api/dashboard/graphql",
  documents: ["src/**/*.{ts,tsx}", "src/lib/graphql/operations/**/*.graphql"],
  generates: {
    "src/lib/graphql/generated/": {
      preset: "client",
      presetConfig: { gqlTagName: "gql" },
    },
  },
  ignoreNoDocuments: true,
}
export default config
