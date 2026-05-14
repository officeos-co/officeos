export interface ApiClientOptions {
  apiUrl: string;
  token?: string;
}

export class ApiClient {
  private readonly apiUrl: string;
  private readonly token?: string;

  constructor(options: ApiClientOptions) {
    this.apiUrl = options.apiUrl.replace(/\/$/, "");
    this.token = options.token;
  }

  async get<T>(path: string): Promise<T> {
    const response = await fetch(`${this.apiUrl}${path}`, {
      headers: this.headers(),
    });
    return await readResponse<T>(response);
  }

  async getText(path: string): Promise<string> {
    const response = await fetch(`${this.apiUrl}${path}`, {
      headers: this.headers(),
    });
    if (!response.ok) throw new Error(await responseError(response));
    return await response.text();
  }

  async post<T>(path: string, body: unknown): Promise<T> {
    const response = await fetch(`${this.apiUrl}${path}`, {
      method: "POST",
      headers: this.headers({ "content-type": "application/json" }),
      body: JSON.stringify(body),
    });
    return await readResponse<T>(response);
  }

  async delete<T>(path: string): Promise<T> {
    const response = await fetch(`${this.apiUrl}${path}`, {
      method: "DELETE",
      headers: this.headers(),
    });
    return await readResponse<T>(response);
  }

  private headers(extra: Record<string, string> = {}): Record<string, string> {
    return {
      ...extra,
      ...(this.token ? { authorization: `Bearer ${this.token}` } : {}),
    };
  }
}

async function readResponse<T>(response: Response): Promise<T> {
  if (!response.ok) throw new Error(await responseError(response));
  return (await response.json()) as T;
}

async function responseError(response: Response): Promise<string> {
  const text = await response.text();
  if (!text) return `${response.status} ${response.statusText}`;
  const contentType = response.headers.get("content-type") ?? "";
  if (contentType.includes("text/html") || looksLikeHtml(text)) {
    return `${response.status} ${response.statusText}${htmlTitle(text) ? ` (${htmlTitle(text)})` : ""}`;
  }
  try {
    const parsed = JSON.parse(text) as { error?: string; message?: string };
    return parsed.error ?? parsed.message ?? text;
  } catch {
    return text;
  }
}

function looksLikeHtml(value: string): boolean {
  const trimmed = value.trimStart().toLowerCase();
  return trimmed.startsWith("<!doctype html") || trimmed.startsWith("<html");
}

function htmlTitle(value: string): string | null {
  const match = /<title>\s*([^<]+?)\s*<\/title>/i.exec(value);
  return match?.[1]?.trim() ?? null;
}
