/**
 * Environment-derived configuration — mirrors the backend's appsettings.json pattern.
 *
 * A single env var (NEXT_PUBLIC_ENV) controls the environment.
 * All URLs and config are derived from it. No other env vars needed.
 */

type Environment = "development" | "staging" | "production";

interface EnvConfig {
  env: Environment;
  apiUrl: string;
  dashboardUrl: string;
  websiteUrl: string;
}

const configs: Record<Environment, EnvConfig> = {
  development: {
    env: "development",
    apiUrl: "http://localhost:5000",
    dashboardUrl: "http://localhost:3000",
    websiteUrl: "http://localhost:3001",
  },
  staging: {
    env: "staging",
    apiUrl: "https://staging-api.officeos.co",
    dashboardUrl: "https://staging-dashboard.officeos.co",
    websiteUrl: "https://staging.officeos.co",
  },
  production: {
    env: "production",
    apiUrl: "https://api.officeos.co",
    dashboardUrl: "https://dashboard.officeos.co",
    websiteUrl: "https://officeos.co",
  },
};

function resolveEnv(): Environment {
  const env = process.env.APP_ENV;
  if (env === "staging" || env === "production" || env === "development") {
    return env;
  }
  // Fall back to NODE_ENV mapping
  return process.env.NODE_ENV === "production" ? "production" : "development";
}

export function getEnvConfig(): EnvConfig {
  return configs[resolveEnv()];
}

export function isDevelopment(): boolean {
  return getEnvConfig().env === "development";
}
