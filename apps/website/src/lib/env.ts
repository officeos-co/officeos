/**
 * Environment-derived configuration — mirrors the backend's appsettings.json pattern.
 *
 * A single env var (APP_ENV) controls the environment.
 * All URLs are derived from it.
 */

type Environment = "development" | "staging" | "production"

interface EnvConfig {
  env: Environment
  websiteUrl: string
  dashboardUrl: string
  docsUrl: string
}

const configs: Record<Environment, EnvConfig> = {
  development: {
    env: "development",
    websiteUrl: "http://localhost:3001",
    dashboardUrl: "http://localhost:3000",
    docsUrl: "http://localhost:3002",
  },
  staging: {
    env: "staging",
    websiteUrl: "https://staging.officeos.co",
    dashboardUrl: "https://staging-dashboard.officeos.co",
    docsUrl: "https://docs.officeos.co",
  },
  production: {
    env: "production",
    websiteUrl: "https://www.officeos.co",
    dashboardUrl: "https://dashboard.officeos.co",
    docsUrl: "https://docs.officeos.co",
  },
}

function resolveEnv(): Environment {
  const env = process.env.APP_ENV
  if (env === "staging" || env === "production" || env === "development") {
    return env
  }
  return process.env.NODE_ENV === "production" ? "production" : "development"
}

export const envConfig = configs[resolveEnv()]
