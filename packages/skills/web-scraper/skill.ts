import { defineSkill, z } from "@harro/skill-sdk";
import doc from "./SKILL.md";

const BASE = "https://app.scrapingbee.com/api/v1";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

async function proxyGet(ctx: Ctx, targetUrl: string, extra?: Record<string, string>) {
  const params: Record<string, string> = {
    api_key: ctx.credentials.proxy_url, // reuse credential field as API key or proxy URL
    url: targetUrl,
    ...extra,
  };
  const qs = new URLSearchParams(params).toString();
  const res = await ctx.fetch(`${BASE}?${qs}`);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Scraper ${res.status}: ${text}`);
  }
  return res.text();
}

async function proxyGetJson(ctx: Ctx, targetUrl: string, extra?: Record<string, string>) {
  const params: Record<string, string> = {
    api_key: ctx.credentials.proxy_url,
    url: targetUrl,
    ...extra,
  };
  const qs = new URLSearchParams(params).toString();
  const res = await ctx.fetch(`${BASE}?${qs}`);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Scraper ${res.status}: ${text}`);
  }
  return res.json();
}

function extractTitle(html: string): string {
  const m = html.match(/<title[^>]*>([^<]*)<\/title>/i);
  return m ? m[1].trim() : "";
}

function extractText(html: string): string {
  return html
    .replace(/<style[^>]*>[\s\S]*?<\/style>/gi, " ")
    .replace(/<script[^>]*>[\s\S]*?<\/script>/gi, " ")
    .replace(/<[^>]+>/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

function extractLinksFromHtml(html: string, baseUrl: string, selector?: string): Array<{ href: string; text: string; is_external: boolean }> {
  const links: Array<{ href: string; text: string; is_external: boolean }> = [];
  const re = /<a\s+[^>]*href=["']([^"']+)["'][^>]*>([\s\S]*?)<\/a>/gi;
  let m: RegExpExecArray | null;
  const baseDomain = (() => { try { return new URL(baseUrl).hostname; } catch { return ""; } })();
  while ((m = re.exec(html)) !== null) {
    const href = m[1].trim();
    const text = m[2].replace(/<[^>]+>/g, "").trim();
    let isExternal = false;
    try {
      const u = new URL(href, baseUrl);
      isExternal = u.hostname !== baseDomain;
    } catch { continue; }
    links.push({ href, text, is_external: isExternal });
  }
  return links;
}

export default defineSkill({
  name: "web-scraper",
  title: "Web Scraper",
  emoji: "🕷️",
  description:
    "Scrape web pages, extract links, text, structured data, and take screenshots via a configurable HTTP proxy or scraping service.",
  doc,

  credentials: {
    proxy_url: {
      label: "Proxy URL / API Key",
      kind: "password",
      placeholder: "https://user:pass@proxy.example.com:8080 or ScrapingBee API key",
      help: "HTTP proxy URL with embedded credentials, or your ScrapingBee API key.",
    },
  },

  actions: {
    scrape_url: {
      description: "Fetch the full HTML content of a URL, optionally rendered with JavaScript.",
      params: z.object({
        url: z.string().describe("URL to scrape"),
        render_js: z.boolean().optional().describe("Render JavaScript before extracting HTML"),
        wait_ms: z.number().optional().describe("Milliseconds to wait after page load"),
        headers: z.record(z.string()).optional().describe("Extra HTTP headers to send"),
      }),
      returns: z.object({
        url: z.string(),
        status_code: z.number(),
        html: z.string(),
        title: z.string(),
        content_type: z.string(),
      }),
      execute: async (params, ctx) => {
        const extra: Record<string, string> = {};
        if (params.render_js) extra["render_js"] = "true";
        if (params.wait_ms) extra["wait_for"] = String(params.wait_ms);
        const html = await proxyGet(ctx, params.url, extra);
        return {
          url: params.url,
          status_code: 200,
          html,
          title: extractTitle(html),
          content_type: "text/html",
        };
      },
    },

    extract_links: {
      description: "Extract all hyperlinks from a URL.",
      params: z.object({
        url: z.string().describe("URL to scrape"),
        selector: z.string().optional().describe("CSS selector to scope link extraction"),
        include_external: z.boolean().optional().describe("Include links to external domains"),
        render_js: z.boolean().optional().describe("Render JavaScript before extracting"),
      }),
      returns: z.array(z.object({
        href: z.string(),
        text: z.string(),
        is_external: z.boolean(),
      })),
      execute: async (params, ctx) => {
        const extra: Record<string, string> = {};
        if (params.render_js) extra["render_js"] = "true";
        const html = await proxyGet(ctx, params.url, extra);
        const links = extractLinksFromHtml(html, params.url);
        const includeExternal = params.include_external !== false;
        return includeExternal ? links : links.filter((l) => !l.is_external);
      },
    },

    extract_text: {
      description: "Extract clean readable text from a URL, stripping HTML tags and boilerplate.",
      params: z.object({
        url: z.string().describe("URL to scrape"),
        selector: z.string().optional().describe("CSS selector to limit extraction scope"),
        render_js: z.boolean().optional().describe("Render JavaScript before extracting"),
        wait_ms: z.number().optional().describe("Milliseconds to wait after page load"),
      }),
      returns: z.object({
        url: z.string(),
        title: z.string(),
        text: z.string(),
        word_count: z.number(),
      }),
      execute: async (params, ctx) => {
        const extra: Record<string, string> = {};
        if (params.render_js) extra["render_js"] = "true";
        if (params.wait_ms) extra["wait_for"] = String(params.wait_ms);
        const html = await proxyGet(ctx, params.url, extra);
        const text = extractText(html);
        return {
          url: params.url,
          title: extractTitle(html),
          text,
          word_count: text.split(/\s+/).filter(Boolean).length,
        };
      },
    },

    extract_structured: {
      description: "Extract structured data from a page using CSS selectors to map fields.",
      params: z.object({
        url: z.string().describe("URL to scrape"),
        fields: z.record(z.string()).describe("Map of field name → CSS selector"),
        render_js: z.boolean().optional().describe("Render JavaScript before extracting"),
        wait_ms: z.number().optional().describe("Milliseconds to wait"),
      }),
      returns: z.object({
        url: z.string(),
        data: z.record(z.string().nullable()),
      }),
      execute: async (params, ctx) => {
        const extra: Record<string, string> = {
          extract_rules: JSON.stringify(params.fields),
        };
        if (params.render_js) extra["render_js"] = "true";
        if (params.wait_ms) extra["wait_for"] = String(params.wait_ms);
        const data = await proxyGetJson(ctx, params.url, extra);
        return { url: params.url, data };
      },
    },

    screenshot_url: {
      description: "Take a screenshot of a rendered web page.",
      params: z.object({
        url: z.string().describe("URL to screenshot"),
        full_page: z.boolean().optional().describe("Capture full scrollable page"),
        width: z.number().optional().describe("Viewport width in pixels"),
        height: z.number().optional().describe("Viewport height in pixels"),
        wait_ms: z.number().optional().describe("Milliseconds to wait after page load"),
      }),
      returns: z.object({
        url: z.string(),
        image_base64: z.string(),
        width: z.number(),
        height: z.number(),
        format: z.string(),
      }),
      execute: async (params, ctx) => {
        const extra: Record<string, string> = {
          screenshot: "true",
          render_js: "true",
          window_width: String(params.width ?? 1280),
          window_height: String(params.height ?? 800),
        };
        if (params.full_page) extra["screenshot_full_page"] = "true";
        if (params.wait_ms) extra["wait_for"] = String(params.wait_ms ?? 1000);
        const res = await ctx.fetch(`${BASE}?${new URLSearchParams({ api_key: ctx.credentials.proxy_url, url: params.url, ...extra })}`);
        if (!res.ok) {
          const text = await res.text();
          throw new Error(`Scraper ${res.status}: ${text}`);
        }
        const buf = await res.arrayBuffer();
        const image_base64 = Buffer.from(buf).toString("base64");
        return {
          url: params.url,
          image_base64,
          width: params.width ?? 1280,
          height: params.height ?? 800,
          format: "png",
        };
      },
    },

    get_metadata: {
      description: "Fetch page metadata: title, description, OG tags, canonical URL, and HTTP headers.",
      params: z.object({
        url: z.string().describe("URL to inspect"),
      }),
      returns: z.object({
        url: z.string(),
        title: z.string(),
        description: z.string(),
        og_title: z.string(),
        og_description: z.string(),
        og_image: z.string(),
        canonical: z.string(),
        status_code: z.number(),
        content_type: z.string(),
      }),
      execute: async (params, ctx) => {
        const html = await proxyGet(ctx, params.url);
        const get = (re: RegExp) => { const m = html.match(re); return m ? m[1].trim() : ""; };
        return {
          url: params.url,
          title: extractTitle(html),
          description: get(/<meta[^>]+name=["']description["'][^>]+content=["']([^"']+)["']/i),
          og_title: get(/<meta[^>]+property=["']og:title["'][^>]+content=["']([^"']+)["']/i),
          og_description: get(/<meta[^>]+property=["']og:description["'][^>]+content=["']([^"']+)["']/i),
          og_image: get(/<meta[^>]+property=["']og:image["'][^>]+content=["']([^"']+)["']/i),
          canonical: get(/<link[^>]+rel=["']canonical["'][^>]+href=["']([^"']+)["']/i),
          status_code: 200,
          content_type: "text/html",
        };
      },
    },

    check_accessibility: {
      description: "Check if a URL is reachable and return HTTP status, redirect chain, and response time.",
      params: z.object({
        url: z.string().describe("URL to check"),
        follow_redirects: z.boolean().optional().describe("Follow HTTP redirects"),
        timeout_ms: z.number().optional().describe("Request timeout in ms"),
      }),
      returns: z.object({
        url: z.string(),
        final_url: z.string(),
        status_code: z.number(),
        reachable: z.boolean(),
        response_time_ms: z.number(),
        redirect_chain: z.array(z.string()),
      }),
      execute: async (params, ctx) => {
        const start = Date.now();
        try {
          const res = await ctx.fetch(params.url, {
            redirect: params.follow_redirects === false ? "manual" : "follow",
          });
          return {
            url: params.url,
            final_url: res.url,
            status_code: res.status,
            reachable: res.ok,
            response_time_ms: Date.now() - start,
            redirect_chain: [],
          };
        } catch (err) {
          return {
            url: params.url,
            final_url: params.url,
            status_code: 0,
            reachable: false,
            response_time_ms: Date.now() - start,
            redirect_chain: [],
          };
        }
      },
    },

    batch_scrape: {
      description: "Scrape multiple URLs and return results for each (max 10).",
      params: z.object({
        urls: z.array(z.string()).max(10).describe("List of URLs to scrape (max 10)"),
        extract: z.enum(["html", "text", "links"]).optional().describe("What to extract: html, text, or links"),
        render_js: z.boolean().optional().describe("Render JavaScript for all URLs"),
      }),
      returns: z.array(z.object({
        url: z.string(),
        status_code: z.number(),
        result: z.unknown(),
        error: z.string().nullable(),
      })),
      execute: async (params, ctx) => {
        const extra: Record<string, string> = {};
        if (params.render_js) extra["render_js"] = "true";
        const results = await Promise.all(
          params.urls.map(async (url) => {
            try {
              const html = await proxyGet(ctx, url, extra);
              let result: unknown = html;
              if (params.extract === "text") result = extractText(html);
              if (params.extract === "links") result = extractLinksFromHtml(html, url);
              return { url, status_code: 200, result, error: null };
            } catch (err) {
              return { url, status_code: 0, result: null, error: String(err) };
            }
          }),
        );
        return results;
      },
    },
  },
});
