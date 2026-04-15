export const LINEAR_API = "https://api.linear.app/graphql";

export type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

export async function gql(
  ctx: Ctx,
  query: string,
  variables: Record<string, unknown> = {},
) {
  const res = await ctx.fetch(LINEAR_API, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: ctx.credentials.api_key,
    },
    body: JSON.stringify({ query, variables }),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Linear API ${res.status}: ${body}`);
  }
  const json = await res.json();
  if (json.errors?.length) {
    throw new Error(`Linear GraphQL: ${json.errors.map((e: any) => e.message).join(", ")}`);
  }
  return json.data;
}
