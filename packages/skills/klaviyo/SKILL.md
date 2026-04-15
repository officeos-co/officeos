# Klaviyo Skill

Manage Klaviyo email marketing: profiles, lists, segments, campaigns, flows, templates, and events via the Klaviyo REST API.

## Credentials

| Key | Description |
|---|---|
| `api_key` | Private API key from Klaviyo Settings → API Keys. Starts with `pk_`. |

## Actions

### Profiles

#### `list_profiles`
List profiles (subscribers/contacts) with pagination.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `page_size` | `number` | `20` | Results per page (max 100) |
| `page_cursor` | `string` | — | Cursor for next page (from previous response) |
| `sort` | `string` | — | Sort field e.g. `created` or `-created` (prefix `-` for desc) |

**Returns** Array of profile objects + `next_cursor` for pagination.

---

#### `get_profile`
Get a single profile by ID.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `string` | Profile ID |

**Returns** Full profile object.

---

#### `create_profile`
Create a new profile (subscribe a contact).

**Params**
| Name | Type | Required | Description |
|---|---|---|---|
| `email` | `string` | yes | Email address |
| `first_name` | `string` | no | First name |
| `last_name` | `string` | no | Last name |
| `phone_number` | `string` | no | E.164 phone number |
| `properties` | `object` | no | Custom properties |

**Returns** Created profile object.

---

#### `update_profile`
Update a profile's attributes.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `string` | Profile ID |
| All create params (partial) | — | Fields to update |

**Returns** Updated profile object.

---

#### `search_profiles`
Search profiles by email.

**Params**
| Name | Type | Description |
|---|---|---|
| `email` | `string` | Exact email to search for |

**Returns** Array of matching profiles.

---

### Lists

#### `list_lists`
List all lists.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `page_size` | `number` | `20` | Results per page |
| `page_cursor` | `string` | — | Pagination cursor |

**Returns** Array of list objects.

---

#### `get_list`
Get a list by ID.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `string` | List ID |

**Returns** List object with name, created, updated.

---

#### `create_list`
Create a new list.

**Params**
| Name | Type | Description |
|---|---|---|
| `name` | `string` | List name |

**Returns** Created list object.

---

#### `add_profiles_to_list`
Add profiles to a list by email addresses.

**Params**
| Name | Type | Description |
|---|---|---|
| `list_id` | `string` | List ID |
| `emails` | `string[]` | Email addresses to subscribe |

**Returns** `{ subscribed: number }` count.

---

### Segments

#### `list_segments`
List all segments.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `page_size` | `number` | `20` | Results per page |
| `page_cursor` | `string` | — | Pagination cursor |

**Returns** Array of segment objects.

---

#### `get_segment`
Get a segment by ID.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `string` | Segment ID |

**Returns** Segment object.

---

### Campaigns

#### `list_campaigns`
List campaigns.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `channel` | `email \| sms` | `email` | Channel filter |
| `page_size` | `number` | `20` | Results per page |
| `page_cursor` | `string` | — | Pagination cursor |

**Returns** Array of campaign objects.

---

#### `get_campaign`
Get a campaign by ID.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `string` | Campaign ID |

**Returns** Campaign object with status, send_time, message.

---

#### `create_campaign`
Create a new campaign.

**Params**
| Name | Type | Required | Description |
|---|---|---|---|
| `name` | `string` | yes | Campaign name |
| `subject` | `string` | yes | Email subject line |
| `from_email` | `string` | yes | Sender email |
| `from_label` | `string` | yes | Sender name |
| `list_ids` | `string[]` | yes | List IDs to send to |
| `template_id` | `string` | no | Template ID |

**Returns** Created campaign object.

---

### Flows

#### `list_flows`
List automation flows.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `page_size` | `number` | `20` | Results per page |
| `page_cursor` | `string` | — | Pagination cursor |

**Returns** Array of flow objects.

---

#### `get_flow`
Get a flow by ID.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `string` | Flow ID |

**Returns** Flow object with status and trigger.

---

### Templates

#### `list_templates`
List email templates.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `page_size` | `number` | `20` | Results per page |
| `page_cursor` | `string` | — | Pagination cursor |

**Returns** Array of template objects.

---

#### `get_template`
Get a template by ID.

**Params**
| Name | Type | Description |
|---|---|---|
| `id` | `string` | Template ID |

**Returns** Template object with html and text content.

---

### Events

#### `track_event`
Track a custom event for a profile.

**Params**
| Name | Type | Required | Description |
|---|---|---|---|
| `event` | `string` | yes | Event name (metric) |
| `email` | `string` | yes | Profile email |
| `properties` | `object` | no | Event properties |
| `value` | `number` | no | Event value (e.g. purchase amount) |
| `time` | `string` | no | ISO 8601 timestamp (defaults to now) |

**Returns** `{ success: true }`.

---

#### `list_events`
List recent events.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `page_size` | `number` | `20` | Results per page |
| `page_cursor` | `string` | — | Pagination cursor |
| `sort` | `string` | `-datetime` | Sort field |

**Returns** Array of event objects.

---

### Metrics

#### `list_metrics`
List available metrics (event types).

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `page_size` | `number` | `50` | Results per page |

**Returns** Array of metric objects with id, name, integration.
