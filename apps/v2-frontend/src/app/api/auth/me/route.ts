import { NextRequest, NextResponse } from "next/server";
import { BACKEND_URL } from "@/lib/backend";

export async function GET(req: NextRequest) {
  const res = await fetch(`${BACKEND_URL}/api/auth/me`, {
    headers: { cookie: req.headers.get("cookie") ?? "" },
  });

  const body = await res.text();
  return new NextResponse(body, {
    status: res.status,
    headers: { "content-type": "application/json" },
  });
}
