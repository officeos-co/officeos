export type Tool = {
  name: string
  description: string
}

export type CredentialField = {
  key: string
  label: string
  type: "password" | "text"
  placeholder: string
}

export type Integration = {
  id: string
  name: string
  slug: string
  logo: string
  description: string
  likes: number
  likedByMe: boolean
  commentsCount: number
  tools: Tool[]
  installed: boolean
  doc: string
  sourceCodeUrl: string
}

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
