export const SHEETS_API = "https://sheets.googleapis.com/v4/spreadsheets";

export type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

export function authHeaders(token: string): Record<string, string> {
  return {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
  };
}

export async function shtFetch(ctx: Ctx, url: string, init?: RequestInit) {
  const res = await ctx.fetch(url, {
    ...init,
    headers: { ...authHeaders(ctx.credentials.access_token), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Google Sheets API ${res.status}: ${body}`);
  }
  if (res.status === 204) return {};
  return res.json();
}

export async function shtPost(ctx: Ctx, url: string, body: unknown, method = "POST") {
  return shtFetch(ctx, url, { method, body: JSON.stringify(body) });
}

export function enc(s: string) {
  return encodeURIComponent(s);
}

export function parseRange(sheetId: number, a1: string): any {
  const match = a1.match(/^(?:.*!)?([A-Z]+)(\d+):([A-Z]+)(\d+)$/);
  if (!match) return { sheetId };
  const colToIndex = (col: string) => {
    let idx = 0;
    for (let i = 0; i < col.length; i++) {
      idx = idx * 26 + (col.charCodeAt(i) - 64);
    }
    return idx - 1;
  };
  return {
    sheetId,
    startRowIndex: parseInt(match[2]) - 1,
    endRowIndex: parseInt(match[4]),
    startColumnIndex: colToIndex(match[1]),
    endColumnIndex: colToIndex(match[3]) + 1,
  };
}

export async function getSheetId(ctx: Ctx, spreadsheetId: string, range: string): Promise<number> {
  const rangeSheet = range.split("!")[0];
  const spreadsheet = await shtFetch(ctx, `${SHEETS_API}/${spreadsheetId}?fields=sheets.properties`);
  const sheet = (spreadsheet.sheets ?? []).find((s: any) => s.properties?.title === rangeSheet);
  return sheet?.properties?.sheetId ?? 0;
}
