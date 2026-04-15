# Zendesk Skill

Manage Zendesk Support: tickets, users, organizations, comments, views, and macros via the Zendesk REST API v2.

## Credentials

| Key | Description |
|---|---|
| `subdomain` | Your Zendesk subdomain (e.g. `mycompany` for `mycompany.zendesk.com`) |
| `email` | Agent email address |
| `api_token` | API token from Admin → Apps and Integrations → Zendesk API |

## Actions

### Tickets

#### `list_tickets`
List tickets with optional status filter.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `status` | `new \| open \| pending \| hold \| solved \| closed` | — | Filter by status |
| `page` | `number` | `1` | Page number |
| `per_page` | `number` | `25` | Results per page (max 100) |
| `sort_by` | `string` | `created_at` | Sort field |
| `sort_order` | `asc \| desc` | `desc` | Sort direction |

**Returns** Array of ticket objects.

---

#### `get_ticket`
Get a single ticket by ID.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `number` | Ticket ID |

**Returns** Full ticket object.

---

#### `create_ticket`
Create a new support ticket.

**Params**
| Name | Type | Required | Description |
|---|---|---|---|
| `subject` | `string` | yes | Ticket subject |
| `body` | `string` | yes | Initial comment body |
| `requester_email` | `string` | no | Requester email |
| `requester_name` | `string` | no | Requester name |
| `priority` | `urgent \| high \| normal \| low` | no | Priority |
| `type` | `problem \| incident \| question \| task` | no | Ticket type |
| `tags` | `string[]` | no | Tags |
| `assignee_id` | `number` | no | Assignee user ID |
| `group_id` | `number` | no | Group ID |

**Returns** Created ticket object.

---

#### `update_ticket`
Update a ticket's fields or status.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `number` | Ticket ID |
| `status` | `string` | New status |
| `priority` | `string` | Priority |
| `assignee_id` | `number` | Assignee |
| `tags` | `string[]` | Tags (replaces existing) |
| `subject` | `string` | Subject |
| `comment` | `string` | Add a public comment |

**Returns** Updated ticket object.

---

#### `delete_ticket`
Delete (soft-delete) a ticket.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `number` | Ticket ID |

**Returns** `{ success: true }`.

---

### Comments

#### `list_comments`
List all comments on a ticket.

**Params**
| Name | Type | Description |
|---|---|---|
| `ticket_id` | `number` | Ticket ID |

**Returns** Array of comment objects with author and body.

---

#### `add_comment`
Add a comment to a ticket.

**Params**
| Name | Type | Required | Description |
|---|---|---|---|
| `ticket_id` | `number` | yes | Ticket ID |
| `body` | `string` | yes | Comment body |
| `public` | `boolean` | no | Public (true) or internal note (false). Default `true`. |

**Returns** Updated ticket with the new comment.

---

### Users

#### `list_users`
List users with optional role filter.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `role` | `end-user \| agent \| admin` | — | Filter by role |
| `page` | `number` | `1` | Page |
| `per_page` | `number` | `25` | Per page (max 100) |

**Returns** Array of user objects.

---

#### `get_user`
Get a user by ID.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `number` | User ID |

**Returns** Full user object.

---

#### `create_user`
Create or update a user (upsert by email).

**Params**
| Name | Type | Required | Description |
|---|---|---|---|
| `name` | `string` | yes | Display name |
| `email` | `string` | yes | Email address |
| `role` | `end-user \| agent` | no | Role (default `end-user`) |
| `phone` | `string` | no | Phone number |
| `organization_id` | `number` | no | Organization ID |

**Returns** Created or updated user object.

---

### Organizations

#### `list_organizations`
List organizations.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `page` | `number` | `1` | Page |
| `per_page` | `number` | `25` | Per page (max 100) |

**Returns** Array of organization objects.

---

#### `get_organization`
Get an organization by ID.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `number` | Organization ID |

**Returns** Full organization object.

---

### Search

#### `search`
Search across tickets, users, and organizations.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `query` | `string` | — | Zendesk search query (supports `type:ticket`, field filters, etc.) |
| `page` | `number` | `1` | Page |
| `per_page` | `number` | `25` | Per page (max 100) |
| `sort_by` | `string` | — | Sort field |
| `sort_order` | `asc \| desc` | — | Sort direction |

**Returns** Array of matching objects with type field.

---

### Views

#### `list_views`
List all views (saved ticket filters).

**Params** None.

**Returns** Array of view objects with id, title, active.

---

#### `execute_view`
Get tickets matching a view.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `view_id` | `number` | — | View ID |
| `page` | `number` | `1` | Page |
| `per_page` | `number` | `25` | Per page |

**Returns** Array of ticket objects.

---

### Macros

#### `list_macros`
List available macros.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `active` | `boolean` | `true` | Filter by active status |

**Returns** Array of macro objects with id, title, actions.

---

#### `apply_macro`
Apply a macro to a ticket.

**Params**
| Name | Type | Description |
|---|---|---|
| `ticket_id` | `number` | Ticket ID |
| `macro_id` | `number` | Macro ID |

**Returns** Resulting ticket object after macro application.
