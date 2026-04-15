# Stock Analysis Skill

Fetch real-time and historical stock market data, financials, analyst ratings, and market trends via Yahoo Finance. No authentication required; an optional proxy URL can be configured for high-volume use.

## Credentials

| Key | Description |
|---|---|
| `proxy_url` | (Optional) HTTP/HTTPS proxy URL to route Yahoo Finance requests through. |

## Actions

### Quotes

#### `quote`
Get a real-time quote for a single ticker.

**Params**
| Name | Type | Description |
|---|---|---|
| `symbol` | `string` | Ticker symbol e.g. `AAPL`, `MSFT` |

**Returns** Quote object with price, change, volume, market cap.

---

#### `quotes`
Get quotes for multiple tickers in one call.

**Params**
| Name | Type | Description |
|---|---|---|
| `symbols` | `string[]` | Array of ticker symbols (max 20) |

**Returns** Array of quote objects.

---

#### `search`
Search for tickers and companies by name or symbol.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `query` | `string` | — | Company name or ticker fragment |
| `limit` | `number` | `10` | Max results |

**Returns** Array of search results with symbol, name, exchange, type.

---

### Historical Data

#### `historical`
Get historical OHLCV (open/high/low/close/volume) data.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `symbol` | `string` | — | Ticker symbol |
| `start` | `string` | — | Start date `YYYY-MM-DD` |
| `end` | `string` | today | End date `YYYY-MM-DD` |
| `interval` | `1d \| 1wk \| 1mo` | `1d` | Data interval |

**Returns** Array of OHLCV bars with date, open, high, low, close, volume, adjClose.

---

### Fundamentals

#### `summary`
Get a comprehensive summary for a ticker: valuation, key stats, and dividend info.

**Params**
| Name | Type | Description |
|---|---|---|
| `symbol` | `string` | Ticker symbol |

**Returns** Object with priceToBook, trailingPE, forwardPE, dividendYield, beta, 52-week range, etc.

---

#### `financials`
Get income statement, balance sheet, and cash flow data.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `symbol` | `string` | — | Ticker symbol |
| `quarterly` | `boolean` | `false` | Return quarterly instead of annual |

**Returns** Object with revenue, grossProfit, netIncome, totalCash, totalDebt, freeCashflow.

---

### Analyst Data

#### `recommendations`
Get analyst buy/sell/hold recommendations.

**Params**
| Name | Type | Description |
|---|---|---|
| `symbol` | `string` | Ticker symbol |

**Returns** Array of recommendation objects with firm, toGrade, fromGrade, action, date.

---

#### `analyst_ratings`
Get recent analyst upgrades and downgrades.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `symbol` | `string` | — | Ticker symbol |
| `limit` | `number` | `10` | Max results |

**Returns** Array of rating changes with firm, toGrade, fromGrade, action, epochDate.

---

### Market Data

#### `trending`
Get trending tickers on Yahoo Finance.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `region` | `string` | `US` | Market region (US, GB, IN, etc.) |
| `limit` | `number` | `10` | Max results |

**Returns** Array of trending symbols with quote data.

---

### Options

#### `options`
Get options chain for a ticker.

**Params**
| Name | Type | Description |
|---|---|---|
| `symbol` | `string` | Ticker symbol |
| `expiration_date` | `string` | Options expiration date `YYYY-MM-DD` (optional, defaults to nearest) |

**Returns** Object with calls and puts arrays, each with strike, lastPrice, bid, ask, impliedVolatility, openInterest.

---

### Earnings

#### `earnings_calendar`
Get upcoming earnings dates for a ticker.

**Params**
| Name | Type | Description |
|---|---|---|
| `symbol` | `string` | Ticker symbol |

**Returns** Object with earningsDate, revenueEstimate, epsEstimate.

---

#### `institutional_holders`
Get top institutional holders of a stock.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `symbol` | `string` | — | Ticker symbol |
| `limit` | `number` | `10` | Max holders to return |

**Returns** Array of holder objects with organization, pctHeld, shares, value, reportDate.
