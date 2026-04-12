import { defineSkill, z } from "@harro/skill-sdk";
import type { Page } from "playwright-core";
import doc from "./SKILL.md";

export default defineSkill({
  name: "browser",
  title: "Browser",
  emoji: "\ud83c\udf10",
  description:
    "Control a headless browser \u2014 navigate, click, fill forms, take screenshots, and extract page content.",
  doc,

  credentials: {},

  actions: {
    open: {
      description: "Open a URL in the browser.",
      params: z.object({
        url: z.string().describe("URL to open"),
      }),
      returns: z.object({
        title: z.string(),
        content: z.string(),
        url: z.string(),
      }),
      execute: async (params, ctx) => {
        const page = ctx.page as Page;
        await page.goto(params.url);
        const title = await page.title();
        const content = await page.evaluate(() => document.body.innerText);
        return {
          title,
          content: content.slice(0, 20000),
          url: page.url(),
        };
      },
    },

    navigate: {
      description: "Navigate to a URL.",
      params: z.object({
        url: z.string().describe("URL to navigate to"),
      }),
      returns: z.object({
        title: z.string(),
        content: z.string(),
        url: z.string(),
      }),
      execute: async (params, ctx) => {
        const page = ctx.page as Page;
        await page.goto(params.url);
        const title = await page.title();
        const content = await page.evaluate(() => document.body.innerText);
        return {
          title,
          content: content.slice(0, 20000),
          url: page.url(),
        };
      },
    },

    click: {
      description: "Click an element matching a CSS selector.",
      params: z.object({
        selector: z.string().describe("CSS selector"),
      }),
      returns: z.object({
        clicked: z.string(),
      }),
      execute: async (params, ctx) => {
        const page = ctx.page as Page;
        await page.click(params.selector);
        return { clicked: params.selector };
      },
    },

    fill: {
      description: "Fill a form field with a value.",
      params: z.object({
        selector: z.string().describe("CSS selector"),
        value: z.string().describe("Value to fill"),
      }),
      returns: z.object({
        filled: z.string(),
      }),
      execute: async (params, ctx) => {
        const page = ctx.page as Page;
        await page.fill(params.selector, params.value);
        return { filled: params.selector };
      },
    },

    screenshot: {
      description: "Take a screenshot of the current page.",
      params: z.object({
        full_page: z
          .boolean()
          .optional()
          .default(false)
          .describe("Capture the entire page"),
      }),
      returns: z.object({
        image_base64: z.string(),
        width: z.number(),
        height: z.number(),
      }),
      execute: async (params, ctx) => {
        const page = ctx.page as Page;
        const buffer = await page.screenshot({
          fullPage: params.full_page,
          type: "png",
        });
        const viewport = page.viewportSize() ?? { width: 1280, height: 720 };
        return {
          image_base64: buffer.toString("base64"),
          width: viewport.width,
          height: viewport.height,
        };
      },
    },

    snapshot: {
      description:
        "Get an accessibility snapshot of the page as plain text.",
      params: z.object({}),
      returns: z.object({
        content: z.string(),
      }),
      execute: async (_params, ctx) => {
        const page = ctx.page as Page;
        const content = await page.evaluate(() => document.body.innerText);
        return { content: content.slice(0, 30000) };
      },
    },

    scroll: {
      description: "Scroll the page up or down.",
      params: z.object({
        direction: z.enum(["up", "down"]).describe("Scroll direction"),
        amount: z
          .number()
          .optional()
          .default(500)
          .describe("Pixels to scroll"),
      }),
      returns: z.object({
        scrolled: z.string(),
      }),
      execute: async (params, ctx) => {
        const page = ctx.page as Page;
        const delta = params.direction === "down" ? params.amount : -params.amount;
        await page.evaluate((d) => window.scrollBy(0, d), delta);
        return { scrolled: `${params.direction} ${params.amount}px` };
      },
    },

    press: {
      description: "Press a keyboard key.",
      params: z.object({
        key: z
          .string()
          .describe("Key to press, e.g. Enter, Tab, Escape"),
      }),
      returns: z.object({
        pressed: z.string(),
      }),
      execute: async (params, ctx) => {
        const page = ctx.page as Page;
        await page.keyboard.press(params.key);
        return { pressed: params.key };
      },
    },

    get_text: {
      description: "Extract text content from the page or a specific element.",
      params: z.object({
        selector: z
          .string()
          .optional()
          .describe("CSS selector, or omit for full page"),
      }),
      returns: z.object({
        text: z.string(),
      }),
      execute: async (params, ctx) => {
        const page = ctx.page as Page;
        let text: string;
        if (params.selector) {
          text = (await page.textContent(params.selector)) ?? "";
        } else {
          text = await page.evaluate(() => document.body.innerText);
        }
        return { text: text.slice(0, 30000) };
      },
    },

    close: {
      description: "Close the browser session.",
      params: z.object({}),
      returns: z.object({
        closed: z.boolean(),
      }),
      execute: async (_params, _ctx) => {
        return { closed: true };
      },
    },
  },
});
