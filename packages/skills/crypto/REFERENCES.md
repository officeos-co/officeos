# Crypto Skill — References

## Source library
- **Repo**: https://github.com/ccxt/ccxt
- **License**: MIT
- **npm**: `ccxt`

## API reference
- **Provider**: CoinGecko Public API
- **Base URL**: `https://api.coingecko.com/api/v3/`
- **Auth**: None required for free tier (rate limit: ~10-30 req/min)
- **Docs**: https://www.coingecko.com/en/api/documentation
- **Pro API**: `https://pro-api.coingecko.com/api/v3/` (requires `x-cg-pro-api-key` header)

## Key endpoints used
| Endpoint | Action |
|---|---|
| `/coins/list` | `list_coins` |
| `/coins/{id}` | `get_coin` |
| `/simple/price` | `price` |
| `/coins/{id}/market_chart` | `market_chart` |
| `/coins/markets` | `markets` |
| `/search` | `search` |
| `/search/trending` | `trending` |
| `/exchanges` | `exchanges` |
| `/exchange_rates` | `exchange_rates` |
| `/coins/categories` | `categories` |
| `/global` | `global` |
| `/coins/{id}/ohlc` | `ohlc` |

## Notes
- CoinGecko IDs differ from ticker symbols: use `search` or `list_coins` to find the ID
- Free tier has ~10-30 req/min; avoid hammering historical endpoints
- Market cap and volume denominated in `vs_currency` (default USD)
