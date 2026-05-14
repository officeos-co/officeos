export const defaultApiUrl = "https://api.officeos.co";

export function resolveApiUrl(value?: string): string {
  return (value ?? process.env.EAOS_API_URL ?? defaultApiUrl).replace(
    /\/$/,
    "",
  );
}
