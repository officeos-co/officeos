import { NextRequest, NextResponse } from "next/server";
import { BACKEND_URL } from "@/lib/backend";

export async function GET(req: NextRequest) {
  const res = await fetch(`${BACKEND_URL}/api/auth/google`, {
    redirect: "manual",
    headers: { cookie: req.headers.get("cookie") ?? "" },
  });

  // Forward the redirect + Set-Cookie headers
  const location = res.headers.get("location");
  if (!location) {
    return NextResponse.json({ error: "No redirect from backend" }, { status: 502 });
  }

  const response = NextResponse.redirect(location, 302);
  // Forward all Set-Cookie headers (oauth-state cookie)
  for (const cookie of res.headers.getSetCookie()) {
    response.headers.append("set-cookie", cookie);
  }
  return response;
}
