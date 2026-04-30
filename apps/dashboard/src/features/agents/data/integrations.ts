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
  description: string
  transportType: string
  logo: string
  category: string
  credentialFields: CredentialField[]
  configured: boolean  // derived: has credentials saved
  isBuiltin: boolean
}

/** @deprecated Use McpServer instead */
export type Integration = McpServer

export const builtInTools: Tool[] = [
  { name: "bash", description: "Execute bash commands" },
  { name: "read", description: "Read files" },
  { name: "write", description: "Write files" },
  { name: "edit", description: "String replacement in files" },
  { name: "glob", description: "File pattern matching" },
  { name: "grep", description: "Text search with regex" },
  { name: "web_fetch", description: "Fetch URL content" },
  { name: "web_search", description: "Search the web" },
]
