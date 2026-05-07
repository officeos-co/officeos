import { NextRequest } from "next/server";
import { getEnvConfig } from "@/lib/env";

type RouteContext = {
  params: Promise<{ path?: string[] }>;
};

const HOP_BY_HOP_HEADERS = new Set([
  "connection",
  "content-encoding",
  "content-length",
  "host",
  "keep-alive",
  "proxy-authenticate",
  "proxy-authorization",
  "te",
  "trailer",
  "transfer-encoding",
  "upgrade",
]);

export async function GET(request: NextRequest, context: RouteContext) {
  return proxyAuthRequest(request, context);
}

export async function POST(request: NextRequest, context: RouteContext) {
  return proxyAuthRequest(request, context);
}

async function proxyAuthRequest(request: NextRequest, context: RouteContext) {
  const { path = [] } = await context.params;
  const sourceUrl = new URL(request.url);
  const targetUrl = new URL(
    `/api/auth/${path.map(encodeURIComponent).join("/")}${sourceUrl.search}`,
    getEnvConfig().apiUrl,
  );

  const headers = new Headers(request.headers);
  for (const header of HOP_BY_HOP_HEADERS) headers.delete(header);

  headers.set("x-forwarded-host", sourceUrl.host);
  headers.set("x-forwarded-proto", sourceUrl.protocol.replace(":", ""));
  headers.set("x-forwarded-prefix", "/api/auth");

  const backendResponse = await fetch(targetUrl, {
    method: request.method,
    headers,
    body: request.method === "GET" || request.method === "HEAD"
      ? undefined
      : await request.arrayBuffer(),
    redirect: "manual",
  });

  const responseHeaders = new Headers();
  backendResponse.headers.forEach((value, key) => {
    if (!HOP_BY_HOP_HEADERS.has(key.toLowerCase()) && key.toLowerCase() !== "set-cookie") {
      responseHeaders.set(key, value);
    }
  });

  const setCookies = (backendResponse.headers as Headers & {
    getSetCookie?: () => string[];
  }).getSetCookie?.();

  if (setCookies?.length) {
    for (const cookie of setCookies) responseHeaders.append("set-cookie", cookie);
  } else {
    const cookie = backendResponse.headers.get("set-cookie");
    if (cookie) responseHeaders.append("set-cookie", cookie);
  }

  return new Response(
    backendResponse.status === 204 ? null : await backendResponse.arrayBuffer(),
    {
      status: backendResponse.status,
      statusText: backendResponse.statusText,
      headers: responseHeaders,
    },
  );
}
