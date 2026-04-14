import { checkAllServices } from "@/lib/check-status";

export const dynamic = "force-dynamic";

export async function GET() {
  const status = await checkAllServices();
  return Response.json(status, {
    headers: {
      "Cache-Control": "no-store, max-age=0",
    },
  });
}
