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

export type SkillAuthor = {
  name: string;
  url?: string | null;
};

export type SkillContributor = {
  name: string;
  url?: string | null;
};

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
  version: string
  license: string | null
  repository: string | null
  categories: string[]
  keywords: string[]
  readme: string | null
  changelog: string | null
  author: SkillAuthor | null
  contributors: SkillContributor[]
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
