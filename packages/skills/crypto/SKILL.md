# Crypto Skill

Access cryptocurrency market data via CoinGecko: prices, market charts, trending coins, exchange data, and global market overview. No authentication required.

## Credentials

None required. The CoinGecko public API is freely accessible.

## Actions

### Coins

#### `list_coins`
List all supported coins with their IDs and symbols.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `include_platform` | `boolean` | `false` | Include contract address per platform |

**Returns** Array of `{ id, symbol, name }` objects. Use `id` in other actions.

---

#### `get_coin`
Get detailed data for a single coin by CoinGecko ID.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `id` | `string` | — | CoinGecko coin ID e.g. `bitcoin`, `ethereum` |
| `localization` | `boolean` | `false` | Include localized languages |
| `tickers` | `boolean` | `false` | Include exchange tickers |
| `market_data` | `boolean` | `true` | Include current market data |
| `community_data` | `boolean` | `false` | Include social/community stats |
| `developer_data` | `boolean` | `false` | Include GitHub stats |

**Returns** Coin detail object with name, symbol, description, market data, links.

---

#### `price`
Get current price for one or more coins in one or more currencies.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `ids` | `string[]` | — | CoinGecko coin IDs |
| `vs_currencies` | `string[]` | `["usd"]` | Target currencies e.g. `["usd","eur","btc"]` |
| `include_market_cap` | `boolean` | `false` | Include market cap |
| `include_24hr_vol` | `boolean` | `false` | Include 24h volume |
| `include_24hr_change` | `boolean` | `false` | Include 24h price change % |

**Returns** Object keyed by coin ID, each with currency prices and optional stats.

---

#### `market_chart`
Get historical price, market cap, and volume data for a coin.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `id` | `string` | — | CoinGecko coin ID |
| `vs_currency` | `string` | `usd` | Target currency |
| `days` | `number \| string` | — | Number of days or `max` for full history |
| `interval` | `daily \| hourly` | — | Data interval (auto-selected when omitted) |

**Returns** Object with `prices`, `market_caps`, `total_volumes` arrays of `[timestamp_ms, value]`.

---

#### `markets`
List coins with market data, sorted and filtered.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `vs_currency` | `string` | `usd` | Target currency |
| `ids` | `string[]` | — | Filter to specific coin IDs |
| `category` | `string` | — | Filter by category slug |
| `order` | `string` | `market_cap_desc` | Sort order |
| `per_page` | `number` | `50` | Results per page (max 250) |
| `page` | `number` | `1` | Page number |
| `price_change_percentage` | `string` | — | Include price change periods e.g. `1h,24h,7d` |

**Returns** Array of coin market objects with rank, price, 24h change, market cap, volume.

---

### Search & Discovery

#### `search`
Search CoinGecko for coins, exchanges, and categories.

**Params**
| Name | Type | Description |
|---|---|---|
| `query` | `string` | Search term |

**Returns** Object with `coins`, `exchanges`, `categories` arrays.

---

#### `trending`
Get trending coins (top-7 on CoinGecko in 24h based on searches).

**Params** None.

**Returns** Array of trending coin objects with rank, id, symbol, price_btc.

---

### Exchanges

#### `exchanges`
List crypto exchanges with volume data.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `per_page` | `number` | `20` | Results per page (max 250) |
| `page` | `number` | `1` | Page number |

**Returns** Array of exchange objects with id, name, trust_score, trade_volume_24h_btc.

---

#### `exchange_rates`
Get BTC exchange rates against major currencies and crypto.

**Params** None.

**Returns** Object of rates keyed by currency code with name, unit, value, type.

---

### Market Overview

#### `categories`
List coin categories with market data.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `order` | `string` | `market_cap_desc` | Sort order |

**Returns** Array of category objects with id, name, market_cap, market_cap_change_24h.

---

#### `global`
Get global crypto market stats.

**Params** None.

**Returns** Object with total_market_cap, total_volume, market_cap_percentage (dominance), active_cryptocurrencies, markets, market_cap_change_percentage_24h.

---

### Technical

#### `ohlc`
Get OHLC candlestick data for a coin.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `id` | `string` | — | CoinGecko coin ID |
| `vs_currency` | `string` | `usd` | Target currency |
| `days` | `1 \| 7 \| 14 \| 30 \| 90 \| 180 \| 365` | `30` | Number of days |

**Returns** Array of `[timestamp_ms, open, high, low, close]` arrays.
