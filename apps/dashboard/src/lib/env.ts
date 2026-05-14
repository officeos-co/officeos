type Environment = "development" | "staging" | "production";

export interface EnvConfig {
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

  return process.env.NODE_ENV === "production" ? "production" : "development";
}

export function getEnvConfig(): EnvConfig {
  const config = configs[resolveEnv()];

  return {
    ...config,
    apiUrl: process.env.EAOS_API_URL ?? process.env.NEXT_PUBLIC_API_BASE_URL ?? config.apiUrl,
    dashboardUrl: process.env.EAOS_DASHBOARD_URL ?? process.env.NEXT_PUBLIC_DASHBOARD_URL ?? config.dashboardUrl,
    websiteUrl: process.env.EAOS_WEBSITE_URL ?? process.env.NEXT_PUBLIC_WEBSITE_URL ?? config.websiteUrl,
  };
}
