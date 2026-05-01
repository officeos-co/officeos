export type Tool = {
  name: string
  description: string
}

export type CredentialField = {
  name: string
  label: string
  type: string    // "password" | "text"
  required: boolean
}

export type McpServer = {
  id: string
  name: string        // slug
  title: string
  subtitle: string
  description: string
  transportType: string
  logo: string
  category: string
  credentialFields: CredentialField[]
  configured: boolean  // derived: has credentials saved
  isBuiltin: boolean
  authorName: string
  authorUrl: string
  documentationUrl: string
  repositoryUrl: string
  tools: Tool[]
}

/** @deprecated Use McpServer instead */
export type Integration = McpServer
