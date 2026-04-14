export type ServiceStatus = "operational" | "degraded" | "down";

export interface ServiceResult {
  name: string;
  url: string;
  description: string;
  status: ServiceStatus;
  responseTime: number | null;
  statusCode: number | null;
}

export interface StatusResponse {
  services: ServiceResult[];
  overall: ServiceStatus;
  checkedAt: string;
}

const services = [
  {
    name: "Website",
    url: "https://www.harrokrog.com",
    description: "Public landing page",
  },
  {
    name: "Dashboard",
    url: "https://dashboard.harrokrog.com",
    description: "Mission Control dashboard",
  },
  {
    name: "API",
    url: "https://api.harrokrog.com/health",
    description: "Backend API",
  },
];

async function checkService(service: {
  name: string;
  url: string;
  description: string;
}): Promise<ServiceResult> {
  const start = Date.now();
  try {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 5000);

    const response = await fetch(service.url, {
      method: "HEAD",
      signal: controller.signal,
      redirect: "follow",
    });

    clearTimeout(timeout);
    const responseTime = Date.now() - start;

    if (response.ok) {
      return {
        ...service,
        status: "operational",
        responseTime,
        statusCode: response.status,
      };
    }

    return {
      ...service,
      status: "degraded",
      responseTime,
      statusCode: response.status,
    };
  } catch {
    const responseTime = Date.now() - start;
    return {
      ...service,
      status: "down",
      responseTime: responseTime >= 5000 ? null : responseTime,
      statusCode: null,
    };
  }
}

function deriveOverall(results: ServiceResult[]): ServiceStatus {
  if (results.every((r) => r.status === "operational")) return "operational";
  if (results.some((r) => r.status === "down")) return "down";
  return "degraded";
}

export async function checkAllServices(): Promise<StatusResponse> {
  const results = await Promise.all(services.map(checkService));
  return {
    services: results,
    overall: deriveOverall(results),
    checkedAt: new Date().toISOString(),
  };
}
