import { defineSkill, z } from "@harro/skill-sdk";

import manifest from "./skill.json" with { type: "json" };
// Yahoo Finance v8 / crumb-based endpoints
const YF_BASE = "https://query1.finance.yahoo.com";
const YF_BASE2 = "https://query2.finance.yahoo.com";

function enc(s: string) {
  return encodeURIComponent(s);
}

function yfHeaders() {
  return {
    "User-Agent":
      "Mozilla/5.0 (compatible; eaos-skill-runtime/1.0; +https://officeos.co)",
    Accept: "application/json",
  };
}

async function yfFetch(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  url: string,
): Promise<any> {
  const res = await ctx.fetch(url, { headers: yfHeaders() });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Yahoo Finance ${res.status} for ${url}: ${body.slice(0, 200)}`);
  }
  return res.json();
}

async function quoteSummary(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  symbol: string,
  modules: string[],
): Promise<any> {
  const url = `${YF_BASE}/v10/finance/quoteSummary/${enc(symbol)}?modules=${enc(modules.join(","))}&corsDomain=finance.yahoo.com`;
  const json = await yfFetch(ctx, url);
  if (json?.quoteSummary?.error) {
    throw new Error(`Yahoo Finance error: ${JSON.stringify(json.quoteSummary.error)}`);
  }
  return json?.quoteSummary?.result?.[0] ?? {};
}

const quoteShape = z.object({
  symbol: z.string(),
  longName: z.string().nullable(),
  regularMarketPrice: z.number().nullable(),
  regularMarketChange: z.number().nullable(),
  regularMarketChangePercent: z.number().nullable(),
  regularMarketVolume: z.number().nullable(),
  marketCap: z.number().nullable(),
  fiftyTwoWeekHigh: z.number().nullable(),
  fiftyTwoWeekLow: z.number().nullable(),
  currency: z.string().nullable(),
});

function mapQuote(q: any): z.infer<typeof quoteShape> {
  return {
    symbol: q.symbol,
    longName: q.longName ?? q.shortName ?? null,
    regularMarketPrice: q.regularMarketPrice ?? null,
    regularMarketChange: q.regularMarketChange ?? null,
    regularMarketChangePercent: q.regularMarketChangePercent ?? null,
    regularMarketVolume: q.regularMarketVolume ?? null,
    marketCap: q.marketCap ?? null,
    fiftyTwoWeekHigh: q.fiftyTwoWeekHigh ?? null,
    fiftyTwoWeekLow: q.fiftyTwoWeekLow ?? null,
    currency: q.currency ?? null,
  };
}

import doc from "./SKILL.md";

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Quotes ─────────────────────────────────────────────────────────

    quote: {
      description: "Get a real-time quote for a single ticker symbol.",
      params: z.object({
        symbol: z.string().describe("Ticker symbol e.g. AAPL, MSFT, TSLA"),
      }),
      returns: quoteShape,
      execute: async (params, ctx) => {
        const url = `${YF_BASE2}/v8/finance/chart/${enc(params.symbol)}?interval=1d&range=1d`;
        const json = await yfFetch(ctx, url);
        const meta = json?.chart?.result?.[0]?.meta;
        if (!meta) throw new Error(`No data returned for symbol ${params.symbol}`);
        return {
          symbol: meta.symbol,
          longName: meta.longName ?? null,
          regularMarketPrice: meta.regularMarketPrice ?? null,
          regularMarketChange: meta.regularMarketChange ?? null,
          regularMarketChangePercent: meta.regularMarketChangePercent ?? null,
          regularMarketVolume: meta.regularMarketVolume ?? null,
          marketCap: meta.marketCap ?? null,
          fiftyTwoWeekHigh: meta.fiftyTwoWeekHigh ?? null,
          fiftyTwoWeekLow: meta.fiftyTwoWeekLow ?? null,
          currency: meta.currency ?? null,
        };
      },
    },

    quotes: {
      description: "Get real-time quotes for multiple ticker symbols in one call.",
      params: z.object({
        symbols: z.array(z.string()).min(1).max(20).describe("Array of ticker symbols (max 20)"),
      }),
      returns: z.array(quoteShape),
      execute: async (params, ctx) => {
        const syms = params.symbols.map(enc).join(",");
        const url = `${YF_BASE}/v7/finance/quote?symbols=${syms}`;
        const json = await yfFetch(ctx, url);
        const results = json?.quoteResponse?.result ?? [];
        return results.map(mapQuote);
      },
    },

    search: {
      description: "Search for tickers and companies by name or symbol fragment.",
      params: z.object({
        query: z.string().describe("Company name or ticker fragment to search for"),
        limit: z.number().min(1).max(50).default(10).describe("Max results"),
      }),
      returns: z.array(
        z.object({
          symbol: z.string(),
          shortname: z.string().nullable(),
          longname: z.string().nullable(),
          exchange: z.string().nullable(),
          quoteType: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const url = `${YF_BASE}/v1/finance/search?q=${enc(params.query)}&quotesCount=${params.limit}&newsCount=0`;
        const json = await yfFetch(ctx, url);
        return (json?.finance?.result?.[0]?.quotes ?? json?.quotes ?? []).map((r: any) => ({
          symbol: r.symbol,
          shortname: r.shortname ?? null,
          longname: r.longname ?? null,
          exchange: r.exchange ?? null,
          quoteType: r.quoteType ?? "EQUITY",
        }));
      },
    },

    // ── Historical ─────────────────────────────────────────────────────

    historical: {
      description: "Get historical OHLCV price data for a ticker.",
      params: z.object({
        symbol: z.string().describe("Ticker symbol"),
        start: z.string().describe("Start date YYYY-MM-DD"),
        end: z.string().optional().describe("End date YYYY-MM-DD (defaults to today)"),
        interval: z
          .enum(["1d", "1wk", "1mo"])
          .default("1d")
          .describe("Data interval: daily, weekly, monthly"),
      }),
      returns: z.array(
        z.object({
          date: z.string(),
          open: z.number().nullable(),
          high: z.number().nullable(),
          low: z.number().nullable(),
          close: z.number().nullable(),
          adjClose: z.number().nullable(),
          volume: z.number().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const period1 = Math.floor(new Date(params.start).getTime() / 1000);
        const period2 = params.end
          ? Math.floor(new Date(params.end).getTime() / 1000)
          : Math.floor(Date.now() / 1000);
        const url = `${YF_BASE2}/v8/finance/chart/${enc(params.symbol)}?period1=${period1}&period2=${period2}&interval=${params.interval}&events=adjsplits`;
        const json = await yfFetch(ctx, url);
        const result = json?.chart?.result?.[0];
        if (!result) throw new Error(`No historical data for ${params.symbol}`);
        const timestamps: number[] = result.timestamp ?? [];
        const quotes = result.indicators?.quote?.[0] ?? {};
        const adjClose: number[] = result.indicators?.adjclose?.[0]?.adjclose ?? [];
        return timestamps.map((ts, i) => ({
          date: new Date(ts * 1000).toISOString().slice(0, 10),
          open: quotes.open?.[i] ?? null,
          high: quotes.high?.[i] ?? null,
          low: quotes.low?.[i] ?? null,
          close: quotes.close?.[i] ?? null,
          adjClose: adjClose[i] ?? null,
          volume: quotes.volume?.[i] ?? null,
        }));
      },
    },

    // ── Fundamentals ───────────────────────────────────────────────────

    summary: {
      description: "Get valuation summary, key stats, and dividend info for a ticker.",
      params: z.object({ symbol: z.string().describe("Ticker symbol") }),
      returns: z.object({
        symbol: z.string(),
        trailingPE: z.number().nullable(),
        forwardPE: z.number().nullable(),
        priceToBook: z.number().nullable(),
        dividendYield: z.number().nullable(),
        beta: z.number().nullable(),
        fiftyTwoWeekHigh: z.number().nullable(),
        fiftyTwoWeekLow: z.number().nullable(),
        averageVolume: z.number().nullable(),
        marketCap: z.number().nullable(),
        enterpriseValue: z.number().nullable(),
        profitMargins: z.number().nullable(),
      }),
      execute: async (params, ctx) => {
        const data = await quoteSummary(ctx, params.symbol, [
          "summaryDetail",
          "defaultKeyStatistics",
        ]);
        const sd = data.summaryDetail ?? {};
        const ks = data.defaultKeyStatistics ?? {};
        return {
          symbol: params.symbol,
          trailingPE: sd.trailingPE?.raw ?? null,
          forwardPE: sd.forwardPE?.raw ?? null,
          priceToBook: ks.priceToBook?.raw ?? null,
          dividendYield: sd.dividendYield?.raw ?? null,
          beta: sd.beta?.raw ?? null,
          fiftyTwoWeekHigh: sd.fiftyTwoWeekHigh?.raw ?? null,
          fiftyTwoWeekLow: sd.fiftyTwoWeekLow?.raw ?? null,
          averageVolume: sd.averageVolume?.raw ?? null,
          marketCap: sd.marketCap?.raw ?? null,
          enterpriseValue: ks.enterpriseValue?.raw ?? null,
          profitMargins: ks.profitMargins?.raw ?? null,
        };
      },
    },

    financials: {
      description: "Get income statement, cash flow, and balance sheet data for a ticker.",
      params: z.object({
        symbol: z.string().describe("Ticker symbol"),
        quarterly: z
          .boolean()
          .default(false)
          .describe("Return quarterly data instead of annual"),
      }),
      returns: z.object({
        symbol: z.string(),
        totalRevenue: z.number().nullable(),
        grossProfit: z.number().nullable(),
        netIncome: z.number().nullable(),
        ebitda: z.number().nullable(),
        totalCash: z.number().nullable(),
        totalDebt: z.number().nullable(),
        freeCashflow: z.number().nullable(),
        operatingCashflow: z.number().nullable(),
      }),
      execute: async (params, ctx) => {
        const data = await quoteSummary(ctx, params.symbol, ["financialData", "cashflowStatementHistory"]);
        const fd = data.financialData ?? {};
        return {
          symbol: params.symbol,
          totalRevenue: fd.totalRevenue?.raw ?? null,
          grossProfit: fd.grossProfits?.raw ?? null,
          netIncome: fd.netIncomeToCommon?.raw ?? null,
          ebitda: fd.ebitda?.raw ?? null,
          totalCash: fd.totalCash?.raw ?? null,
          totalDebt: fd.totalDebt?.raw ?? null,
          freeCashflow: fd.freeCashflow?.raw ?? null,
          operatingCashflow: fd.operatingCashflow?.raw ?? null,
        };
      },
    },

    // ── Analyst Data ───────────────────────────────────────────────────

    recommendations: {
      description: "Get analyst buy/sell/hold consensus recommendations for a ticker.",
      params: z.object({ symbol: z.string().describe("Ticker symbol") }),
      returns: z.object({
        symbol: z.string(),
        strongBuy: z.number(),
        buy: z.number(),
        hold: z.number(),
        sell: z.number(),
        strongSell: z.number(),
        mean: z.number().nullable(),
      }),
      execute: async (params, ctx) => {
        const url = `${YF_BASE}/v6/finance/recommendationsBySymbol/${enc(params.symbol)}`;
        const json = await yfFetch(ctx, url);
        const recs = json?.finance?.result?.[0]?.recommendedSymbols ?? [];
        // Also get consensus trend
        const data = await quoteSummary(ctx, params.symbol, ["recommendationTrend"]);
        const trend = data.recommendationTrend?.trend?.[0] ?? {};
        return {
          symbol: params.symbol,
          strongBuy: trend.strongBuy ?? 0,
          buy: trend.buy ?? 0,
          hold: trend.hold ?? 0,
          sell: trend.sell ?? 0,
          strongSell: trend.strongSell ?? 0,
          mean: trend.period ? null : null, // mean rating not always available
        };
      },
    },

    analyst_ratings: {
      description: "Get recent analyst upgrades and downgrades for a ticker.",
      params: z.object({
        symbol: z.string().describe("Ticker symbol"),
        limit: z.number().min(1).max(50).default(10).describe("Max results"),
      }),
      returns: z.array(
        z.object({
          firm: z.string(),
          toGrade: z.string(),
          fromGrade: z.string().nullable(),
          action: z.string(),
          date: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await quoteSummary(ctx, params.symbol, ["upgradeDowngradeHistory"]);
        const history = data.upgradeDowngradeHistory?.history ?? [];
        return history.slice(0, params.limit).map((r: any) => ({
          firm: r.firm,
          toGrade: r.toGrade ?? "",
          fromGrade: r.fromGrade ?? null,
          action: r.action ?? "",
          date: r.epochGradeDate
            ? new Date(r.epochGradeDate * 1000).toISOString().slice(0, 10)
            : "",
        }));
      },
    },

    // ── Market Data ────────────────────────────────────────────────────

    trending: {
      description: "Get currently trending tickers on Yahoo Finance.",
      params: z.object({
        region: z.string().default("US").describe("Market region code e.g. US, GB, IN"),
        limit: z.number().min(1).max(25).default(10).describe("Max results"),
      }),
      returns: z.array(z.object({ symbol: z.string(), rank: z.number() })),
      execute: async (params, ctx) => {
        const url = `${YF_BASE}/v1/finance/trending/${enc(params.region)}?count=${params.limit}`;
        const json = await yfFetch(ctx, url);
        const quotes = json?.finance?.result?.[0]?.quotes ?? [];
        return quotes.slice(0, params.limit).map((q: any, i: number) => ({
          symbol: q.symbol,
          rank: i + 1,
        }));
      },
    },

    // ── Options ────────────────────────────────────────────────────────

    options: {
      description: "Get options chain (calls and puts) for a ticker.",
      params: z.object({
        symbol: z.string().describe("Ticker symbol"),
        expiration_date: z
          .string()
          .optional()
          .describe("Options expiration date YYYY-MM-DD (defaults to nearest expiry)"),
      }),
      returns: z.object({
        expirationDate: z.string().nullable(),
        calls: z.array(
          z.object({
            strike: z.number(),
            lastPrice: z.number().nullable(),
            bid: z.number().nullable(),
            ask: z.number().nullable(),
            impliedVolatility: z.number().nullable(),
            openInterest: z.number().nullable(),
            inTheMoney: z.boolean(),
          }),
        ),
        puts: z.array(
          z.object({
            strike: z.number(),
            lastPrice: z.number().nullable(),
            bid: z.number().nullable(),
            ask: z.number().nullable(),
            impliedVolatility: z.number().nullable(),
            openInterest: z.number().nullable(),
            inTheMoney: z.boolean(),
          }),
        ),
      }),
      execute: async (params, ctx) => {
        let url = `${YF_BASE}/v7/finance/options/${enc(params.symbol)}`;
        if (params.expiration_date) {
          const ts = Math.floor(new Date(params.expiration_date).getTime() / 1000);
          url += `?date=${ts}`;
        }
        const json = await yfFetch(ctx, url);
        const result = json?.optionChain?.result?.[0];
        if (!result) throw new Error(`No options data for ${params.symbol}`);
        const chain = result.options?.[0] ?? {};
        const expTs = result.expirationDates?.[0];
        const mapOption = (o: any) => ({
          strike: o.strike,
          lastPrice: o.lastPrice ?? null,
          bid: o.bid ?? null,
          ask: o.ask ?? null,
          impliedVolatility: o.impliedVolatility ?? null,
          openInterest: o.openInterest ?? null,
          inTheMoney: o.inTheMoney ?? false,
        });
        return {
          expirationDate: expTs
            ? new Date(expTs * 1000).toISOString().slice(0, 10)
            : null,
          calls: (chain.calls ?? []).map(mapOption),
          puts: (chain.puts ?? []).map(mapOption),
        };
      },
    },

    // ── Earnings ───────────────────────────────────────────────────────

    earnings_calendar: {
      description: "Get upcoming earnings dates and estimates for a ticker.",
      params: z.object({ symbol: z.string().describe("Ticker symbol") }),
      returns: z.object({
        symbol: z.string(),
        earningsDate: z.array(z.string()),
        epsEstimate: z.number().nullable(),
        revenueEstimate: z.number().nullable(),
      }),
      execute: async (params, ctx) => {
        const data = await quoteSummary(ctx, params.symbol, ["calendarEvents"]);
        const cal = data.calendarEvents ?? {};
        const earningsDates = (cal.earnings?.earningsDate ?? []).map((d: any) =>
          new Date((d.raw ?? d) * 1000).toISOString().slice(0, 10),
        );
        return {
          symbol: params.symbol,
          earningsDate: earningsDates,
          epsEstimate: cal.earnings?.epsEstimate?.raw ?? null,
          revenueEstimate: cal.earnings?.revenueEstimate?.raw ?? null,
        };
      },
    },

    institutional_holders: {
      description: "Get top institutional shareholders of a stock.",
      params: z.object({
        symbol: z.string().describe("Ticker symbol"),
        limit: z.number().min(1).max(50).default(10).describe("Max holders to return"),
      }),
      returns: z.array(
        z.object({
          organization: z.string(),
          pctHeld: z.number().nullable(),
          shares: z.number().nullable(),
          value: z.number().nullable(),
          reportDate: z.string().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await quoteSummary(ctx, params.symbol, ["institutionOwnership"]);
        const holders = data.institutionOwnership?.ownershipList ?? [];
        return holders.slice(0, params.limit).map((h: any) => ({
          organization: h.organization,
          pctHeld: h.pctHeld?.raw ?? null,
          shares: h.position?.raw ?? null,
          value: h.value?.raw ?? null,
          reportDate: h.reportDate?.fmt ?? null,
        }));
      },
    },
  },
});
