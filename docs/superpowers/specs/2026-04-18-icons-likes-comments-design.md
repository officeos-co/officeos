# Icons, Likes, Comments & Install Flow — Design Spec

## Summary

Replace mock data in the dashboard with real backend GraphQL queries. Surface skill logos and channel icons from the backend. Wire up likes, comments, and install/uninstall flows that already exist in the backend. Remove emoji from the entire stack. Auto-generate sourceCodeUrl by convention.

---

## 1. Emoji Removal & Logo Surfacing

### Skill SDK (`packages/skill-sdk/src/types.ts`)
- Remove `emoji?: string` from `SkillDefinition`
- `logo: string` remains required (inline SVG)

### All skills (`packages/skills/*/skill.ts`)
- Regex-based bulk removal of `emoji: "..."` lines across all skill files
- Any skills missing a `logo` field get one added (SVGs sourced from thesvg.org)

### Backend
- Remove `Emoji` column from `SkillRecord` model + EF Core migration
- Remove `Emoji` from `RuntimeManifest` class
- Add `Logo` field to `SkillDashboardDto`, resolved by parsing `ManifestJson` to extract the inline SVG logo
- Remove `Emoji` from all DTOs, GraphQL types, and seed logic

### Skill Runtime (`packages/skill-runtime`)
- Remove emoji from manifest serialization

### Channel Types
- Add `Logo` (inline SVG string) to `ChannelTypeDefinition` record
- Add inline SVGs for all 7 channel types: slack, telegram, discord, whatsapp, teams, google-chat, webchat (generic chat icon)
- Expose `Logo` on `ChannelTypeGqlDto` GraphQL type

---

## 2. SourceCodeUrl Convention & Install Flow

### SourceCodeUrl
- Auto-generate `SourceCodeUrl` on `SkillDashboardDto` as `https://github.com/officeos/integrations/tree/main/packages/{skill.Name}`
- Remove `setSkillSourceCodeUrl` mutation (convention replaces manual config)
- Remove `SourceCodeUrl` column from `SkillRecord` if present

### Install Flow
- Skills seeded from skill-runtime appear in catalog with `Installed = false` by default
- `installSkill` / `uninstallSkill` mutations toggle `SkillCredentialRecord.Enabled`
- `SkillDashboardDto` exposes `Installed: bool` derived from `SkillCredentialRecord.Enabled`
- Dashboard uses this to distinguish "available" vs "installed"

---

## 3. Dashboard — Delete Mock Data, Wire to Backend

### Delete
- `apps/dashboard/src/features/agents/data/integrations.ts` (mock integrations array)
- `apps/dashboard/src/features/agents/data/channels.ts` (mock channels array)
- Static `/public/logos/*.svg` files used only by mock data

### Integrations List Page (`integrations/page.tsx`)
- Query `skills` from GraphQL: `id, name, title, description, logo, installed, likes, likedByMe, commentsCount, tools`
- Render `logo` as raw SVG via `dangerouslySetInnerHTML` (trusted — our own backend)
- "Add" / "Added" button calls `installSkill` / `uninstallSkill` mutations
- Filter tabs: "All" (full catalog), "Installed" (`installed = true`), "Explore" (`installed = false`)

### Integration Detail Page (`integrations/[slug]/page.tsx`)
- Query `skill(name)` + `skillComments(skillId)` from GraphQL
- **Header:** logo, title, description, like button (toggles `likeSkill`/`unlikeSkill`), tool count
- **Body:** tools section, SKILL.md rendered as markdown, source code link (auto-generated)
- **Bottom:** full comments section — list with author/date, input to post via `commentOnSkill`, delete own via `deleteSkillComment`

### Channels Page (`channels/page.tsx`)
- Query `channelTypes` from GraphQL — now includes `logo` SVG
- Render logos same way as integrations (raw SVG)
- Query `channelConnections` for connected vs available status

---

## 4. Testing Strategy

- **Backend:** Unit tests for logo resolution from ManifestJson, sourceCodeUrl generation, install status derivation
- **Dashboard:** Component tests for integration cards, detail page likes/comments, channel icon rendering
- **TDD:** Tests written before implementation in each area
