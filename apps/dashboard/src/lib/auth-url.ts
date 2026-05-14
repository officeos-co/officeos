export function buildOAuthUrl(
  apiBaseUrl: string,
  provider: "google" | "github",
  returnTo?: string,
): string {
  const url = new URL(`/api/auth/${provider}`, apiBaseUrl);
  if (returnTo) url.searchParams.set("returnTo", returnTo);
  return url.toString();
}
