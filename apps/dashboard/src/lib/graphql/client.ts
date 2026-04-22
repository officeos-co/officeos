import {
  ApolloClient,
  HttpLink,
  InMemoryCache,
  from,
} from "@apollo/client"
import { onError } from "@apollo/client/link/error"

/**
 * Singleton Apollo client for the dashboard.
 *
 * All queries/mutations go through POST /api/dashboard/graphql which Next.js
 * rewrites to the backend. No WebSocket subscriptions — real-time data uses
 * polling queries instead (Next.js rewrites don't support WS upgrades).
 */

const HTTP_ENDPOINT = "/api/dashboard/graphql"

const httpLink = new HttpLink({
  uri: HTTP_ENDPOINT,
  credentials: "include",
})

const errorLink = onError(({ graphQLErrors, networkError, operation }) => {
  if (graphQLErrors) {
    for (const err of graphQLErrors) {
      // eslint-disable-next-line no-console
      console.error(
        `[GraphQL error] op=${operation.operationName} message=${err.message}`,
        err,
      )
    }
  }
  if (networkError) {
    // eslint-disable-next-line no-console
    console.error(
      `[Network error] op=${operation.operationName}`,
      networkError,
    )
  }
})

export const apolloClient = new ApolloClient({
  link: from([errorLink, httpLink]),
  cache: new InMemoryCache(),
  devtools: { enabled: process.env.NODE_ENV !== "production" },
  defaultOptions: {
    watchQuery: { fetchPolicy: "cache-first" },
  },
})
