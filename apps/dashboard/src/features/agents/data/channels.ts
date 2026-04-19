export type ChannelPermissions = {
  receive: "allow" | "ask" | "deny"
  send: "allow" | "ask" | "deny"
  initiate: "allow" | "ask" | "deny"
}

export type OnboardingStep = {
  type: "url" | "qr" | "input" | "copy"
  title: string
  description: string
  value?: string
  inputKey?: string
  inputLabel?: string
  inputPlaceholder?: string
  inputHelp?: string
  inputKind?: "text" | "password" | "textarea"
  inputRequired?: boolean
}

export type Channel = {
  name: string
  slug: string
  logo: string
  description: string
  defaultPermissions: ChannelPermissions
  added: boolean
  onboarding: OnboardingStep[]
}
