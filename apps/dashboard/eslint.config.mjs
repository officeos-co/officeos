import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";
import dashboardArchitecturePlugin from "./eslint-rules/dashboard-architecture.mjs";

const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTs,
  {
    files: ["src/**/*.{ts,tsx}"],
    plugins: {
      "dashboard-architecture": dashboardArchitecturePlugin,
    },
    rules: {
      "dashboard-architecture/path-naming": "error",
      "dashboard-architecture/boundaries": "error",
      "dashboard-architecture/no-page-types": "error",
    },
  },
  // Override default ignores of eslint-config-next.
  globalIgnores([
    // Default ignores of eslint-config-next:
    ".next/**",
    "out/**",
    "build/**",
    "next-env.d.ts",
  ]),
]);

export default eslintConfig;
