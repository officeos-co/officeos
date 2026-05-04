import {
  ApolloClient,
  HttpLink,
  InMemoryCache,
  from,
  split,
} from "@apollo/client"
import { GraphQLWsLink } from "@apollo/client/link/subscriptions"
import { onError } from "@apollo/client/link/error"
import { getMainDefinition } from "@apollo/client/utilities"
import { createClient } from "graphql-ws"
import { getEnvConfig } from "@/lib/env"

/**
 * Singleton Apollo client for the dashboard.
 *
 * Queries/mutations go through POST /api/dashboard/graphql which Next.js
 * rewrites to the backend. Subscriptions connect directly to the backend
 * because Next.js rewrites do not proxy WebSocket upgrades.
 */

const HTTP_ENDPOINT = "/api/dashboard/graphql"
const GRAPHQL_PATH = "/api/dashboard/graphql"

function toWebSocketUrl(httpUrl: string) {
  const url = new URL(GRAPHQL_PATH, httpUrl)
  url.protocol = url.protocol === "https:" ? "wss:" : "ws:"
  return url.toString()
}

const httpLink = new HttpLink({
  uri: HTTP_ENDPOINT,
  credentials: "include",
})

const wsLink =
  typeof window === "undefined"
    ? null
    : new GraphQLWsLink(
        createClient({
          url: toWebSocketUrl(getEnvConfig().apiUrl),
          shouldRetry: () => true,
        }),
      )

const errorLink = onError(({ graphQLErrors, networkError, operation }) => {
  if (graphQLErrors) {
    for (const err of graphQLErrors) {
      console.error(
        `[GraphQL error] op=${operation.operationName} message=${err.message}`,
        err,
      )
    }
  }
  if (networkError) {
    console.error(
      `[Network error] op=${operation.operationName}`,
      networkError,
    )
  }
})

export const apolloClient = new ApolloClient({
  link: from([
    errorLink,
    wsLink
      ? split(
          ({ query }) => {
            const definition = getMainDefinition(query)
            return (
              definition.kind === "OperationDefinition" &&
              definition.operation === "subscription"
            )
          },
          wsLink,
          httpLink,
        )
      : httpLink,
  ]),
  cache: new InMemoryCache(),
  devtools: { enabled: process.env.NODE_ENV !== "production" },
  defaultOptions: {
    watchQuery: { fetchPolicy: "cache-first" },
  },
})
