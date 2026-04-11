import { NextRequest, NextResponse } from "next/server";
import { BACKEND_URL } from "@/lib/backend";

export async function GET(req: NextRequest) {
  // Forward the full query string (code, state, scope, etc.) to the backend
  const url = new URL(req.url);
  const backendUrl = `${BACKEND_URL}/api/auth/callback/google${url.search}`;

  const res = await fetch(backendUrl, {
    redirect: "manual",
    headers: { cookie: req.headers.get("cookie") ?? "" },
  });

  // Backend responds with 302 + Set-Cookie (eaos-session)
  const location = res.headers.get("location");
  if (!location) {
    // Non-redirect response — might be an error
    const body = await res.text();
    return new NextResponse(body, { status: res.status });
  }

  // Resolve relative redirects using the incoming Host header (not url.origin which may be 0.0.0.0 in containers)
  let redirectUrl = location;
  if (location.startsWith("/")) {
    const proto = req.headers.get("x-forwarded-proto") ?? "https";
    const host = req.headers.get("host") ?? url.host;
    redirectUrl = `${proto}://${host}${location}`;
  }

  const response = NextResponse.redirect(redirectUrl, 302);
  // Forward all Set-Cookie headers (the session cookie)
  for (const cookie of res.headers.getSetCookie()) {
    response.headers.append("set-cookie", cookie);
  }
  return response;
}
