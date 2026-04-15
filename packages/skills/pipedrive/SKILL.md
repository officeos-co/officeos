# Pipedrive Skill

Manage your Pipedrive CRM: deals, persons, organizations, activities, pipelines, and stages via the Pipedrive REST API v1.

## Credentials

| Key | Description |
|---|---|
| `api_token` | Pipedrive API token. Found under Settings → Personal Preferences → API. |

## Actions

### Deals

#### `list_deals`
List deals with optional filtering.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `status` | `open \| won \| lost \| all` | `open` | Filter by deal status |
| `start` | `number` | `0` | Pagination start |
| `limit` | `number` | `50` | Max results (1–500) |
| `sort` | `string` | — | Field to sort by (e.g. `add_time DESC`) |

**Returns** Array of deal objects with id, title, status, value, currency, owner.

---

#### `get_deal`
Get a single deal by ID.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `number` | Deal ID |

**Returns** Full deal object.

---

#### `create_deal`
Create a new deal.

**Params**
| Name | Type | Required | Description |
|---|---|---|---|
| `title` | `string` | yes | Deal title |
| `value` | `number` | no | Deal value |
| `currency` | `string` | no | Currency code (e.g. `USD`) |
| `person_id` | `number` | no | Associated person |
| `org_id` | `number` | no | Associated organization |
| `pipeline_id` | `number` | no | Pipeline ID |
| `stage_id` | `number` | no | Stage ID |
| `status` | `string` | no | `open`, `won`, `lost` |
| `expected_close_date` | `string` | no | Date string `YYYY-MM-DD` |

**Returns** Created deal object.

---

#### `update_deal`
Update an existing deal.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `number` | Deal ID |
| All create params (partial) | — | Fields to update |

**Returns** Updated deal object.

---

#### `delete_deal`
Delete a deal by ID.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `number` | Deal ID |

**Returns** `{ id, success }`.

---

#### `search_deals`
Search deals by term.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `term` | `string` | — | Search term |
| `exact_match` | `boolean` | `false` | Exact match only |
| `limit` | `number` | `50` | Max results |

**Returns** Array of matching deals.

---

### Persons

#### `list_persons`
List persons (contacts).

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `start` | `number` | `0` | Pagination start |
| `limit` | `number` | `50` | Max results |

**Returns** Array of person objects.

---

#### `get_person`
Get a person by ID.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `number` | Person ID |

**Returns** Full person object.

---

#### `create_person`
Create a new person.

**Params**
| Name | Type | Required | Description |
|---|---|---|---|
| `name` | `string` | yes | Full name |
| `email` | `string` | no | Email address |
| `phone` | `string` | no | Phone number |
| `org_id` | `number` | no | Associated organization ID |

**Returns** Created person object.

---

#### `update_person`
Update a person.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `number` | Person ID |
| All create params (partial) | — | Fields to update |

**Returns** Updated person object.

---

### Organizations

#### `list_organizations`
List organizations.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `start` | `number` | `0` | Pagination start |
| `limit` | `number` | `50` | Max results |

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

#### `create_organization`
Create a new organization.

**Params**
| Name | Type | Required | Description |
|---|---|---|---|
| `name` | `string` | yes | Organization name |
| `address` | `string` | no | Address |

**Returns** Created organization object.

---

### Activities

#### `list_activities`
List activities.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `start` | `number` | `0` | Pagination start |
| `limit` | `number` | `50` | Max results |
| `done` | `boolean` | — | Filter by done status |
| `type` | `string` | — | Activity type (call, meeting, email, etc.) |

**Returns** Array of activity objects.

---

#### `create_activity`
Create a new activity.

**Params**
| Name | Type | Required | Description |
|---|---|---|---|
| `subject` | `string` | yes | Activity subject |
| `type` | `string` | yes | Type: `call`, `meeting`, `task`, `deadline`, `email`, `lunch` |
| `due_date` | `string` | no | Date `YYYY-MM-DD` |
| `due_time` | `string` | no | Time `HH:MM` |
| `duration` | `string` | no | Duration `HH:MM` |
| `deal_id` | `number` | no | Associated deal |
| `person_id` | `number` | no | Associated person |
| `note` | `string` | no | Note text |

**Returns** Created activity object.

---

#### `update_activity`
Update an activity (e.g. mark done).

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `number` | Activity ID |
| `done` | `boolean` | Mark as done |
| All create params (partial) | — | Fields to update |

**Returns** Updated activity object.

---

### Pipelines & Stages

#### `list_pipelines`
List all pipelines.

**Params** None.

**Returns** Array of pipeline objects with id, name, active.

---

#### `list_stages`
List stages, optionally filtered by pipeline.

**Params**
| Name | Type | Description |
|---|---|---|
| `pipeline_id` | `number` | Filter by pipeline |

**Returns** Array of stage objects.

---

### Search

#### `search`
Search across all Pipedrive item types.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `term` | `string` | — | Search term (min 2 chars) |
| `item_type` | `string` | — | Filter: `deal`, `person`, `organization`, `activity` |
| `exact_match` | `boolean` | `false` | Exact match only |
| `limit` | `number` | `50` | Max results |

**Returns** Array of matching items with type and data.
