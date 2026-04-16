import {
  ApolloClient,
  HttpLink,
  InMemoryCache,
  from,
  split,
} from "@apollo/client"
import { onError } from "@apollo/client/link/error"
import { GraphQLWsLink } from "@apollo/client/link/subscriptions"
import { getMainDefinition } from "@apollo/client/utilities"
import { createClient } from "graphql-ws"

/**
 * Singleton Apollo client for the dashboard.
 *
 * - HTTP queries/mutations hit POST {API}/api/dashboard/graphql.
 * - Subscriptions use graphql-ws over WS at the same path.
 * - `credentials: "include"` sends the dashboard session cookie.
 *
 * The API base URL comes from NEXT_PUBLIC_API_URL (defaults to
 * http://localhost:5000 in dev). WS scheme is derived from it.
 */

const API_URL =
  (typeof process !== "undefined" &&
    process.env.NEXT_PUBLIC_API_URL) ||
  "http://localhost:5000"

const HTTP_ENDPOINT = `${API_URL}/api/dashboard/graphql`

function toWsUrl(httpUrl: string): string {
  if (httpUrl.startsWith("https://")) return "wss://" + httpUrl.slice("https://".length)
  if (httpUrl.startsWith("http://")) return "ws://" + httpUrl.slice("http://".length)
  return httpUrl
}

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

// Only instantiate the WS link in the browser — graphql-ws needs a real WebSocket.
const wsLink =
  typeof window !== "undefined"
    ? new GraphQLWsLink(
        createClient({
          url: toWsUrl(HTTP_ENDPOINT),
          // Browser WS inherits cookies from the origin automatically when
          // the backend is same-site; for cross-site we rely on the auth
          // token being accepted via the HTTP session handshake.
          lazy: true,
          retryAttempts: 5,
        }),
      )
    : null

const terminatingLink =
  wsLink != null
    ? split(
        ({ query }) => {
          const def = getMainDefinition(query)
          return (
            def.kind === "OperationDefinition" &&
            def.operation === "subscription"
          )
        },
        wsLink,
        httpLink,
      )
    : httpLink

export const apolloClient = new ApolloClient({
  link: from([errorLink, terminatingLink]),
  cache: new InMemoryCache(),
  connectToDevTools: process.env.NODE_ENV !== "production",
  defaultOptions: {
    watchQuery: { fetchPolicy: "cache-and-network" },
  },
})
