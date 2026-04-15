# Stock Analysis Skill — References

## Source library
- **Repo**: https://github.com/gadicc/node-yahoo-finance2
- **License**: MIT
- **npm**: `yahoo-finance2`
- **Docs**: https://github.com/gadicc/node-yahoo-finance2/blob/main/docs/

## Data source
- **Provider**: Yahoo Finance (unofficial public API)
- **No auth required** for most endpoints
- **Proxy support**: optional `proxy_url` credential to route requests

## Key Yahoo Finance endpoints proxied
| Yahoo Finance module | Action |
|---|---|
| `quoteSummary` (price) | `quote` |
| `quote` (batch) | `quotes` |
| `search` | `search` |
| `historical` | `historical` |
| `quoteSummary` (summaryDetail + defaultKeyStatistics) | `summary` |
| `quoteSummary` (financialData + incomeStatementHistory) | `financials` |
| `recommendationsBySymbol` | `recommendations` |
| `trendingSymbols` | `trending` |
| `quoteSummary` (upgradeDowngradeHistory) | `analyst_ratings` |
| `quoteSummary` (calendarEvents) | `earnings_calendar` |
| `quoteSummary` (institutionOwnership) | `institutional_holders` |
| `options` | `options` |

## Notes
- Yahoo Finance rate limits are informal; use proxy_url to rotate IPs for high-volume use
- Historical data uses `period1`/`period2` Unix timestamps
- All prices in USD unless otherwise noted
