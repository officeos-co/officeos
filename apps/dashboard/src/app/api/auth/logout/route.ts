import { NextRequest, NextResponse } from "next/server";
import { BACKEND_URL } from "@/lib/backend";

export async function POST(req: NextRequest) {
  const res = await fetch(`${BACKEND_URL}/api/auth/logout`, {
    method: "POST",
    headers: { cookie: req.headers.get("cookie") ?? "" },
  });

  const body = await res.text();
  const response = new NextResponse(body, {
    status: res.status,
    headers: { "content-type": "application/json" },
  });

  // Forward Set-Cookie (cookie deletion)
  for (const cookie of res.headers.getSetCookie()) {
    response.headers.append("set-cookie", cookie);
  }
  return response;
}
