export type ChannelPermissions = {
  receive: "allow" | "ask" | "deny"
  send: "allow" | "ask" | "deny"
  initiate: "allow" | "ask" | "deny"
}

export type OnboardingStep = {
  title: string
  description: string
  action: "url" | "qr" | "input" | "copy"
  value?: string
  inputKey?: string
  inputLabel?: string
  inputPlaceholder?: string
}

export type Channel = {
  name: string
  slug: string
  logo: string
  description: string
  protocol: string
  capabilities: string[]
  defaultPermissions: ChannelPermissions
  added: boolean
  onboarding: OnboardingStep[]
}
