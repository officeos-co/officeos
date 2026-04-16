/** Set NEXT_PUBLIC_USE_MOCKS=1 to force hooks to return data/*.ts mock fixtures
 *  instead of hitting GraphQL. Enables developing new UI without running
 *  the backend. In production and when the var is unset, hooks hit GraphQL. */
export const USE_MOCKS =
  typeof process !== "undefined" &&
  process.env.NEXT_PUBLIC_USE_MOCKS === "1"
