import { defineSkill, z } from "@harro/skill-sdk";

import manifest from "./skill.json" with { type: "json" };
const BASE = "https://api.coingecko.com/api/v3";

async function cgFetch(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  path: string,
  queryParams?: Record<string, string | number | boolean | undefined>,
): Promise<any> {
  const qs = queryParams
    ? "?" +
      new URLSearchParams(
        Object.fromEntries(
          Object.entries(queryParams)
            .filter(([, v]) => v !== undefined)
            .map(([k, v]) => [k, String(v)]),
        ),
      ).toString()
    : "";
  const headers: Record<string, string> = {
    Accept: "application/json",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
  const res = await ctx.fetch(`${BASE}${path}${qs}`, { headers });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`CoinGecko API ${res.status}: ${body.slice(0, 300)}`);
  }
  return res.json();
}

import doc from "./SKILL.md";

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Coins ──────────────────────────────────────────────────────────

    list_coins: {
      description:
        "List all supported coins with their CoinGecko IDs and symbols. Use the id field in other actions.",
      params: z.object({
        include_platform: z
          .boolean()
          .default(false)
          .describe("Include contract address per blockchain platform"),
      }),
      returns: z.array(z.object({ id: z.string(), symbol: z.string(), name: z.string() })),
      execute: async (params, ctx) => {
        const data = await cgFetch(ctx, "/coins/list", {
          include_platform: params.include_platform,
        });
        return data.map((c: any) => ({ id: c.id, symbol: c.symbol, name: c.name }));
      },
    },

    get_coin: {
      description: "Get detailed data for a single coin by CoinGecko ID.",
      params: z.object({
        id: z.string().describe("CoinGecko coin ID e.g. 'bitcoin', 'ethereum'"),
        localization: z.boolean().default(false).describe("Include localized names"),
        tickers: z.boolean().default(false).describe("Include exchange tickers"),
        market_data: z.boolean().default(true).describe("Include current market data"),
        community_data: z.boolean().default(false).describe("Include social stats"),
        developer_data: z.boolean().default(false).describe("Include GitHub stats"),
      }),
      returns: z.object({
        id: z.string(),
        symbol: z.string(),
        name: z.string(),
        description: z.string().nullable(),
        homepage: z.string().nullable(),
        current_price_usd: z.number().nullable(),
        market_cap_usd: z.number().nullable(),
        price_change_24h: z.number().nullable(),
        ath_usd: z.number().nullable(),
        atl_usd: z.number().nullable(),
        last_updated: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const { id, ...opts } = params;
        const c = await cgFetch(ctx, `/coins/${id}`, opts as any);
        return {
          id: c.id,
          symbol: c.symbol,
          name: c.name,
          description: c.description?.en ?? null,
          homepage: c.links?.homepage?.[0] ?? null,
          current_price_usd: c.market_data?.current_price?.usd ?? null,
          market_cap_usd: c.market_data?.market_cap?.usd ?? null,
          price_change_24h: c.market_data?.price_change_percentage_24h ?? null,
          ath_usd: c.market_data?.ath?.usd ?? null,
          atl_usd: c.market_data?.atl?.usd ?? null,
          last_updated: c.last_updated ?? null,
        };
      },
    },

    price: {
      description: "Get current price for one or more coins in one or more currencies.",
      params: z.object({
        ids: z.array(z.string()).min(1).describe("CoinGecko coin IDs e.g. ['bitcoin','ethereum']"),
        vs_currencies: z
          .array(z.string())
          .default(["usd"])
          .describe("Target currencies e.g. ['usd','eur','btc']"),
        include_market_cap: z.boolean().default(false).describe("Include market cap"),
        include_24hr_vol: z.boolean().default(false).describe("Include 24h volume"),
        include_24hr_change: z.boolean().default(false).describe("Include 24h price change %"),
      }),
      returns: z.record(z.record(z.number())),
      execute: async (params, ctx) => {
        const data = await cgFetch(ctx, "/simple/price", {
          ids: params.ids.join(","),
          vs_currencies: params.vs_currencies.join(","),
          include_market_cap: params.include_market_cap,
          include_24hr_vol: params.include_24hr_vol,
          include_24hr_change: params.include_24hr_change,
        });
        return data;
      },
    },

    market_chart: {
      description:
        "Get historical price, market cap, and volume data for a coin over time.",
      params: z.object({
        id: z.string().describe("CoinGecko coin ID"),
        vs_currency: z.string().default("usd").describe("Target currency"),
        days: z
          .union([z.number().min(1), z.literal("max")])
          .describe("Number of days back or 'max' for full history"),
        interval: z
          .enum(["daily", "hourly"])
          .optional()
          .describe("Data granularity (auto-selected when omitted)"),
      }),
      returns: z.object({
        prices: z.array(z.tuple([z.number(), z.number()])),
        market_caps: z.array(z.tuple([z.number(), z.number()])),
        total_volumes: z.array(z.tuple([z.number(), z.number()])),
      }),
      execute: async (params, ctx) => {
        const data = await cgFetch(ctx, `/coins/${params.id}/market_chart`, {
          vs_currency: params.vs_currency,
          days: String(params.days),
          interval: params.interval,
        });
        return {
          prices: data.prices ?? [],
          market_caps: data.market_caps ?? [],
          total_volumes: data.total_volumes ?? [],
        };
      },
    },

    markets: {
      description: "List coins with full market data, sorted and paginated.",
      params: z.object({
        vs_currency: z.string().default("usd").describe("Target currency"),
        ids: z
          .array(z.string())
          .optional()
          .describe("Filter to specific CoinGecko coin IDs"),
        category: z.string().optional().describe("Filter by category slug"),
        order: z.string().default("market_cap_desc").describe("Sort order"),
        per_page: z
          .number()
          .min(1)
          .max(250)
          .default(50)
          .describe("Results per page (max 250)"),
        page: z.number().min(1).default(1).describe("Page number"),
        price_change_percentage: z
          .string()
          .optional()
          .describe("Include change periods e.g. '1h,24h,7d'"),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          symbol: z.string(),
          name: z.string(),
          market_cap_rank: z.number().nullable(),
          current_price: z.number().nullable(),
          price_change_percentage_24h: z.number().nullable(),
          market_cap: z.number().nullable(),
          total_volume: z.number().nullable(),
          circulating_supply: z.number().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await cgFetch(ctx, "/coins/markets", {
          vs_currency: params.vs_currency,
          ids: params.ids?.join(","),
          category: params.category,
          order: params.order,
          per_page: params.per_page,
          page: params.page,
          price_change_percentage: params.price_change_percentage,
          sparkline: false,
        });
        return (data ?? []).map((c: any) => ({
          id: c.id,
          symbol: c.symbol,
          name: c.name,
          market_cap_rank: c.market_cap_rank ?? null,
          current_price: c.current_price ?? null,
          price_change_percentage_24h: c.price_change_percentage_24h ?? null,
          market_cap: c.market_cap ?? null,
          total_volume: c.total_volume ?? null,
          circulating_supply: c.circulating_supply ?? null,
        }));
      },
    },

    // ── Search & Discovery ─────────────────────────────────────────────

    search: {
      description: "Search CoinGecko for coins, exchanges, and categories.",
      params: z.object({
        query: z.string().describe("Search term e.g. coin name or ticker"),
      }),
      returns: z.object({
        coins: z.array(
          z.object({ id: z.string(), name: z.string(), symbol: z.string(), market_cap_rank: z.number().nullable() }),
        ),
        exchanges: z.array(z.object({ id: z.string(), name: z.string() })),
        categories: z.array(z.object({ id: z.number(), name: z.string() })),
      }),
      execute: async (params, ctx) => {
        const data = await cgFetch(ctx, "/search", { query: params.query });
        return {
          coins: (data.coins ?? []).map((c: any) => ({
            id: c.id,
            name: c.name,
            symbol: c.symbol,
            market_cap_rank: c.market_cap_rank ?? null,
          })),
          exchanges: (data.exchanges ?? []).map((e: any) => ({ id: e.id, name: e.name })),
          categories: (data.categories ?? []).map((c: any) => ({ id: c.id, name: c.name })),
        };
      },
    },

    trending: {
      description:
        "Get trending coins on CoinGecko (top-7 searched in last 24h) with current BTC price.",
      params: z.object({}),
      returns: z.array(
        z.object({
          rank: z.number(),
          id: z.string(),
          name: z.string(),
          symbol: z.string(),
          price_btc: z.number().nullable(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await cgFetch(ctx, "/search/trending");
        return (data.coins ?? []).map((entry: any) => ({
          rank: entry.item?.score ?? 0,
          id: entry.item?.id ?? "",
          name: entry.item?.name ?? "",
          symbol: entry.item?.symbol ?? "",
          price_btc: entry.item?.price_btc ?? null,
        }));
      },
    },

    // ── Exchanges ──────────────────────────────────────────────────────

    exchanges: {
      description: "List crypto exchanges with trust score and 24h BTC volume.",
      params: z.object({
        per_page: z.number().min(1).max(250).default(20).describe("Results per page"),
        page: z.number().min(1).default(1).describe("Page number"),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          name: z.string(),
          trust_score: z.number().nullable(),
          trust_score_rank: z.number().nullable(),
          trade_volume_24h_btc: z.number().nullable(),
          country: z.string().nullable(),
          url: z.string().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await cgFetch(ctx, "/exchanges", {
          per_page: params.per_page,
          page: params.page,
        });
        return (data ?? []).map((e: any) => ({
          id: e.id,
          name: e.name,
          trust_score: e.trust_score ?? null,
          trust_score_rank: e.trust_score_rank ?? null,
          trade_volume_24h_btc: e.trade_volume_24h_btc ?? null,
          country: e.country ?? null,
          url: e.url ?? null,
        }));
      },
    },

    exchange_rates: {
      description: "Get BTC exchange rates against fiat currencies and crypto.",
      params: z.object({}),
      returns: z.record(
        z.object({
          name: z.string(),
          unit: z.string(),
          value: z.number(),
          type: z.string(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await cgFetch(ctx, "/exchange_rates");
        return data.rates ?? {};
      },
    },

    // ── Market Overview ────────────────────────────────────────────────

    categories: {
      description: "List coin categories with market cap data.",
      params: z.object({
        order: z.string().default("market_cap_desc").describe("Sort order"),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          name: z.string(),
          market_cap: z.number().nullable(),
          market_cap_change_24h: z.number().nullable(),
          volume_24h: z.number().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await cgFetch(ctx, "/coins/categories", { order: params.order });
        return (data ?? []).map((c: any) => ({
          id: c.id,
          name: c.name,
          market_cap: c.market_cap ?? null,
          market_cap_change_24h: c.market_cap_change_24h ?? null,
          volume_24h: c.volume_24h ?? null,
        }));
      },
    },

    global: {
      description:
        "Get global cryptocurrency market overview: total market cap, dominance, and active coins.",
      params: z.object({}),
      returns: z.object({
        active_cryptocurrencies: z.number(),
        markets: z.number(),
        total_market_cap_usd: z.number().nullable(),
        total_volume_usd: z.number().nullable(),
        market_cap_percentage: z.record(z.number()),
        market_cap_change_percentage_24h_usd: z.number().nullable(),
        updated_at: z.number(),
      }),
      execute: async (_params, ctx) => {
        const json = await cgFetch(ctx, "/global");
        const d = json.data ?? {};
        return {
          active_cryptocurrencies: d.active_cryptocurrencies ?? 0,
          markets: d.markets ?? 0,
          total_market_cap_usd: d.total_market_cap?.usd ?? null,
          total_volume_usd: d.total_volume?.usd ?? null,
          market_cap_percentage: d.market_cap_percentage ?? {},
          market_cap_change_percentage_24h_usd:
            d.market_cap_change_percentage_24h_usd ?? null,
          updated_at: d.updated_at ?? 0,
        };
      },
    },

    // ── Technical ──────────────────────────────────────────────────────

    ohlc: {
      description: "Get OHLC candlestick data for a coin.",
      params: z.object({
        id: z.string().describe("CoinGecko coin ID"),
        vs_currency: z.string().default("usd").describe("Target currency"),
        days: z
          .union([
            z.literal(1),
            z.literal(7),
            z.literal(14),
            z.literal(30),
            z.literal(90),
            z.literal(180),
            z.literal(365),
          ])
          .default(30)
          .describe("Days of OHLC data: 1, 7, 14, 30, 90, 180, or 365"),
      }),
      returns: z.array(z.tuple([z.number(), z.number(), z.number(), z.number(), z.number()])),
      execute: async (params, ctx) => {
        const data = await cgFetch(ctx, `/coins/${params.id}/ohlc`, {
          vs_currency: params.vs_currency,
          days: params.days,
        });
        // Each element: [timestamp, open, high, low, close]
        return data ?? [];
      },
    },
  },
});
