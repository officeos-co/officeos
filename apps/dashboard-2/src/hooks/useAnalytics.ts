"use client"

import { useMutation, gql } from "@apollo/client"
import { USE_MOCKS } from "@/lib/graphql/mock-mode"

const CAPTURE = gql`
  mutation CaptureEvent($input: CaptureEventInput!) {
    captureEvent(input: $input)
  }
`

export function useAnalytics() {
  const [captureMutation] = useMutation(CAPTURE)
  const capture = async (name: string, properties?: Record<string, unknown>) => {
    if (USE_MOCKS) {
      // eslint-disable-next-line no-console
      console.debug("[analytics:mock]", name, properties)
      return
    }
    try {
      await captureMutation({ variables: { input: { name, properties: properties ?? {} } } })
    } catch {
      // Analytics must never surface to the user.
    }
  }
  return { capture }
}
