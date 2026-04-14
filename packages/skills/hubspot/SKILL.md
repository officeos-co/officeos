# HubSpot

Manage CRM contacts, companies, deals, tickets, pipelines, engagement activities, lists, and marketing emails via the HubSpot API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Contacts

### List contacts

```
hubspot list_contacts --limit 20 --properties '["email","firstname","lastname","phone","company"]'
```

| Argument     | Type     | Required | Default | Description               |
| ------------ | -------- | -------- | ------- | ------------------------- |
| `limit`      | int      | no       | 10      | Results to return (1-100) |
| `after`      | string   | no       |         | Pagination cursor         |
| `properties` | string[] | no       |         | Properties to include     |

Returns: list of `id`, `properties` (email, firstname, lastname, etc.), `createdAt`, `updatedAt`.

### Get contact

```
hubspot get_contact --contact_id "123" --properties '["email","firstname","lastname","phone","company","lifecyclestage"]'
```

| Argument     | Type     | Required | Description           |
| ------------ | -------- | -------- | --------------------- |
| `contact_id` | string   | yes      | Contact ID            |
| `properties` | string[] | no       | Properties to include |

Returns: `id`, `properties`, `createdAt`, `updatedAt`, `associations`.

### Create contact

```
hubspot create_contact --email "jane@example.com" --firstname "Jane" --lastname "Doe" --phone "+1234567890" --company "Acme Corp" --properties '{"lifecyclestage":"lead"}'
```

| Argument     | Type   | Required | Description                        |
| ------------ | ------ | -------- | ---------------------------------- |
| `email`      | string | yes      | Contact email                      |
| `firstname`  | string | no       | First name                         |
| `lastname`   | string | no       | Last name                          |
| `phone`      | string | no       | Phone number                       |
| `company`    | string | no       | Company name                       |
| `properties` | object | no       | Additional properties as key-value |

Returns: `id`, `properties`, `createdAt`.

### Update contact

```
hubspot update_contact --contact_id "123" --properties '{"lifecyclestage":"customer","phone":"+9876543210"}'
```

| Argument     | Type   | Required | Description                     |
| ------------ | ------ | -------- | ------------------------------- |
| `contact_id` | string | yes      | Contact ID                      |
| `email`      | string | no       | Updated email                   |
| `firstname`  | string | no       | Updated first name              |
| `lastname`   | string | no       | Updated last name               |
| `properties` | object | no       | Additional properties to update |

Returns: `id`, `properties`, `updatedAt`.

### Delete contact

```
hubspot delete_contact --contact_id "123"
```

| Argument     | Type   | Required | Description |
| ------------ | ------ | -------- | ----------- |
| `contact_id` | string | yes      | Contact ID  |

Returns: `success` boolean.

### Search contacts

```
hubspot search_contacts --query "jane@example.com" --filter_groups '[{"filters":[{"propertyName":"lifecyclestage","operator":"EQ","value":"customer"}]}]' --limit 10
```

| Argument        | Type     | Required | Default | Description                |
| --------------- | -------- | -------- | ------- | -------------------------- |
| `query`         | string   | no       |         | Free-text search query     |
| `filter_groups` | object[] | no       |         | HubSpot filter groups JSON |
| `sorts`         | object[] | no       |         | Sort criteria JSON         |
| `properties`    | string[] | no       |         | Properties to return       |
| `limit`         | int      | no       | 10      | Results to return (1-100)  |

Returns: list of matching contact objects.

## Companies

### List companies

```
hubspot list_companies --limit 20 --properties '["name","domain","industry","numberofemployees"]'
```

| Argument     | Type     | Required | Default | Description               |
| ------------ | -------- | -------- | ------- | ------------------------- |
| `limit`      | int      | no       | 10      | Results to return (1-100) |
| `after`      | string   | no       |         | Pagination cursor         |
| `properties` | string[] | no       |         | Properties to include     |

Returns: list of `id`, `properties` (name, domain, industry, etc.), `createdAt`, `updatedAt`.

### Get company

```
hubspot get_company --company_id "456" --properties '["name","domain","industry","annualrevenue"]'
```

| Argument     | Type     | Required | Description           |
| ------------ | -------- | -------- | --------------------- |
| `company_id` | string   | yes      | Company ID            |
| `properties` | string[] | no       | Properties to include |

Returns: `id`, `properties`, `createdAt`, `updatedAt`, `associations`.

### Create company

```
hubspot create_company --properties '{"name":"Acme Corp","domain":"acme.com","industry":"Technology","numberofemployees":"500"}'
```

| Argument     | Type   | Required | Description                        |
| ------------ | ------ | -------- | ---------------------------------- |
| `properties` | object | yes      | Company properties (name required) |

Returns: `id`, `properties`, `createdAt`.

### Update company

```
hubspot update_company --company_id "456" --properties '{"annualrevenue":"5000000","industry":"SaaS"}'
```

| Argument     | Type   | Required | Description          |
| ------------ | ------ | -------- | -------------------- |
| `company_id` | string | yes      | Company ID           |
| `properties` | object | yes      | Properties to update |

Returns: `id`, `properties`, `updatedAt`.

### Delete company

```
hubspot delete_company --company_id "456"
```

| Argument     | Type   | Required | Description |
| ------------ | ------ | -------- | ----------- |
| `company_id` | string | yes      | Company ID  |

Returns: `success` boolean.

### Search companies

```
hubspot search_companies --filter_groups '[{"filters":[{"propertyName":"industry","operator":"EQ","value":"Technology"}]}]' --limit 10
```

| Argument        | Type     | Required | Default | Description                |
| --------------- | -------- | -------- | ------- | -------------------------- |
| `query`         | string   | no       |         | Free-text search query     |
| `filter_groups` | object[] | no       |         | HubSpot filter groups JSON |
| `sorts`         | object[] | no       |         | Sort criteria JSON         |
| `properties`    | string[] | no       |         | Properties to return       |
| `limit`         | int      | no       | 10      | Results to return (1-100)  |

Returns: list of matching company objects.

## Deals

### List deals

```
hubspot list_deals --limit 20 --properties '["dealname","dealstage","amount","pipeline","closedate"]'
```

| Argument     | Type     | Required | Default | Description               |
| ------------ | -------- | -------- | ------- | ------------------------- |
| `limit`      | int      | no       | 10      | Results to return (1-100) |
| `after`      | string   | no       |         | Pagination cursor         |
| `properties` | string[] | no       |         | Properties to include     |

Returns: list of `id`, `properties` (dealname, dealstage, amount, etc.), `createdAt`, `updatedAt`.

### Get deal

```
hubspot get_deal --deal_id "789" --properties '["dealname","dealstage","amount","pipeline","closedate","hubspot_owner_id"]'
```

| Argument     | Type     | Required | Description           |
| ------------ | -------- | -------- | --------------------- |
| `deal_id`    | string   | yes      | Deal ID               |
| `properties` | string[] | no       | Properties to include |

Returns: `id`, `properties`, `createdAt`, `updatedAt`, `associations`.

### Create deal

```
hubspot create_deal --dealname "Enterprise License" --pipeline "default" --dealstage "appointmentscheduled" --amount "50000" --closedate "2026-06-30"
```

| Argument     | Type   | Required | Description                      |
| ------------ | ------ | -------- | -------------------------------- |
| `dealname`   | string | yes      | Deal name                        |
| `pipeline`   | string | no       | Pipeline ID (default: `default`) |
| `dealstage`  | string | yes      | Stage ID within the pipeline     |
| `amount`     | string | no       | Deal amount                      |
| `closedate`  | string | no       | Expected close date (YYYY-MM-DD) |
| `properties` | object | no       | Additional properties            |

Returns: `id`, `properties`, `createdAt`.

### Update deal

```
hubspot update_deal --deal_id "789" --properties '{"dealstage":"closedwon","amount":"55000"}'
```

| Argument     | Type   | Required | Description          |
| ------------ | ------ | -------- | -------------------- |
| `deal_id`    | string | yes      | Deal ID              |
| `properties` | object | yes      | Properties to update |

Returns: `id`, `properties`, `updatedAt`.

### Delete deal

```
hubspot delete_deal --deal_id "789"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `deal_id` | string | yes      | Deal ID     |

Returns: `success` boolean.

### Search deals

```
hubspot search_deals --filter_groups '[{"filters":[{"propertyName":"dealstage","operator":"EQ","value":"closedwon"}]}]' --limit 20
```

| Argument        | Type     | Required | Default | Description                |
| --------------- | -------- | -------- | ------- | -------------------------- |
| `query`         | string   | no       |         | Free-text search query     |
| `filter_groups` | object[] | no       |         | HubSpot filter groups JSON |
| `sorts`         | object[] | no       |         | Sort criteria JSON         |
| `properties`    | string[] | no       |         | Properties to return       |
| `limit`         | int      | no       | 10      | Results to return (1-100)  |

Returns: list of matching deal objects.

## Pipelines

### List pipelines

```
hubspot list_pipelines --object_type deals
```

| Argument      | Type   | Required | Default | Description          |
| ------------- | ------ | -------- | ------- | -------------------- |
| `object_type` | string | no       | `deals` | `deals` or `tickets` |

Returns: list of `id`, `label`, `displayOrder`, `stages`.

### Get pipeline

```
hubspot get_pipeline --pipeline_id "default" --object_type deals
```

| Argument      | Type   | Required | Default | Description          |
| ------------- | ------ | -------- | ------- | -------------------- |
| `pipeline_id` | string | yes      |         | Pipeline ID          |
| `object_type` | string | no       | `deals` | `deals` or `tickets` |

Returns: `id`, `label`, `displayOrder`, `stages` (list of `id`, `label`, `displayOrder`).

### List pipeline stages

```
hubspot list_pipeline_stages --pipeline_id "default" --object_type deals
```

| Argument      | Type   | Required | Default | Description          |
| ------------- | ------ | -------- | ------- | -------------------- |
| `pipeline_id` | string | yes      |         | Pipeline ID          |
| `object_type` | string | no       | `deals` | `deals` or `tickets` |

Returns: list of `id`, `label`, `displayOrder`, `metadata`.

## Tickets

### List tickets

```
hubspot list_tickets --limit 20 --properties '["subject","content","hs_pipeline_stage","hs_ticket_priority"]'
```

| Argument     | Type     | Required | Default | Description               |
| ------------ | -------- | -------- | ------- | ------------------------- |
| `limit`      | int      | no       | 10      | Results to return (1-100) |
| `after`      | string   | no       |         | Pagination cursor         |
| `properties` | string[] | no       |         | Properties to include     |

Returns: list of `id`, `properties`, `createdAt`, `updatedAt`.

### Get ticket

```
hubspot get_ticket --ticket_id "101" --properties '["subject","content","hs_pipeline_stage","hs_ticket_priority"]'
```

| Argument     | Type     | Required | Description           |
| ------------ | -------- | -------- | --------------------- |
| `ticket_id`  | string   | yes      | Ticket ID             |
| `properties` | string[] | no       | Properties to include |

Returns: `id`, `properties`, `createdAt`, `updatedAt`, `associations`.

### Create ticket

```
hubspot create_ticket --properties '{"subject":"Login issue","content":"User cannot log in","hs_pipeline":"0","hs_pipeline_stage":"1","hs_ticket_priority":"HIGH"}'
```

| Argument     | Type   | Required | Description                          |
| ------------ | ------ | -------- | ------------------------------------ |
| `properties` | object | yes      | Ticket properties (subject required) |

Returns: `id`, `properties`, `createdAt`.

### Update ticket

```
hubspot update_ticket --ticket_id "101" --properties '{"hs_pipeline_stage":"3","hs_ticket_priority":"LOW"}'
```

| Argument     | Type   | Required | Description          |
| ------------ | ------ | -------- | -------------------- |
| `ticket_id`  | string | yes      | Ticket ID            |
| `properties` | object | yes      | Properties to update |

Returns: `id`, `properties`, `updatedAt`.

### Delete ticket

```
hubspot delete_ticket --ticket_id "101"
```

| Argument    | Type   | Required | Description |
| ----------- | ------ | -------- | ----------- |
| `ticket_id` | string | yes      | Ticket ID   |

Returns: `success` boolean.

## Associations

### Create association

```
hubspot create_association --from_object_type contacts --from_id "123" --to_object_type companies --to_id "456" --association_type "contact_to_company"
```

| Argument           | Type   | Required | Description                                                      |
| ------------------ | ------ | -------- | ---------------------------------------------------------------- |
| `from_object_type` | string | yes      | Source object type (`contacts`, `companies`, `deals`, `tickets`) |
| `from_id`          | string | yes      | Source object ID                                                 |
| `to_object_type`   | string | yes      | Target object type                                               |
| `to_id`            | string | yes      | Target object ID                                                 |
| `association_type` | string | yes      | Association type label (e.g. `contact_to_company`)               |

Returns: `from_id`, `to_id`, `association_type`.

### List associations

```
hubspot list_associations --object_type contacts --object_id "123" --to_object_type companies
```

| Argument         | Type   | Required | Description        |
| ---------------- | ------ | -------- | ------------------ |
| `object_type`    | string | yes      | Source object type |
| `object_id`      | string | yes      | Source object ID   |
| `to_object_type` | string | yes      | Target object type |

Returns: list of `id`, `type` (association type label).

### Remove association

```
hubspot remove_association --from_object_type contacts --from_id "123" --to_object_type companies --to_id "456" --association_type "contact_to_company"
```

| Argument           | Type   | Required | Description            |
| ------------------ | ------ | -------- | ---------------------- |
| `from_object_type` | string | yes      | Source object type     |
| `from_id`          | string | yes      | Source object ID       |
| `to_object_type`   | string | yes      | Target object type     |
| `to_id`            | string | yes      | Target object ID       |
| `association_type` | string | yes      | Association type label |

Returns: `success` boolean.

## Engagement

### Create note

```
hubspot create_note --body "Spoke with Jane about renewal" --contact_ids '["123"]' --deal_ids '["789"]'
```

| Argument      | Type     | Required | Description                  |
| ------------- | -------- | -------- | ---------------------------- |
| `body`        | string   | yes      | Note content (supports HTML) |
| `contact_ids` | string[] | no       | Contact IDs to associate     |
| `company_ids` | string[] | no       | Company IDs to associate     |
| `deal_ids`    | string[] | no       | Deal IDs to associate        |

Returns: `id`, `createdAt`.

### Create task

```
hubspot create_task --subject "Follow up with Acme" --body "Discuss pricing" --due_date "2026-05-01" --priority HIGH --contact_ids '["123"]'
```

| Argument      | Type     | Required | Description                               |
| ------------- | -------- | -------- | ----------------------------------------- |
| `subject`     | string   | yes      | Task subject                              |
| `body`        | string   | no       | Task body                                 |
| `due_date`    | string   | no       | Due date (YYYY-MM-DD)                     |
| `priority`    | string   | no       | `LOW`, `MEDIUM`, `HIGH`                   |
| `status`      | string   | no       | `NOT_STARTED`, `IN_PROGRESS`, `COMPLETED` |
| `contact_ids` | string[] | no       | Contact IDs to associate                  |
| `company_ids` | string[] | no       | Company IDs to associate                  |
| `deal_ids`    | string[] | no       | Deal IDs to associate                     |

Returns: `id`, `createdAt`.

### Create email activity

```
hubspot create_email_activity --subject "Re: Proposal" --body "Thanks for your interest" --from_email "sales@officeos.co" --to_email "jane@example.com" --contact_ids '["123"]'
```

| Argument      | Type     | Required | Description                |
| ------------- | -------- | -------- | -------------------------- |
| `subject`     | string   | yes      | Email subject              |
| `body`        | string   | yes      | Email body (supports HTML) |
| `from_email`  | string   | yes      | Sender email               |
| `to_email`    | string   | yes      | Recipient email            |
| `contact_ids` | string[] | no       | Contact IDs to associate   |
| `deal_ids`    | string[] | no       | Deal IDs to associate      |

Returns: `id`, `createdAt`.

### List engagements

```
hubspot list_engagements --object_type contacts --object_id "123" --limit 20
```

| Argument      | Type   | Required | Default | Description                             |
| ------------- | ------ | -------- | ------- | --------------------------------------- |
| `object_type` | string | yes      |         | Object type (`contacts`, `deals`, etc.) |
| `object_id`   | string | yes      |         | Object ID                               |
| `limit`       | int    | no       | 10      | Results to return                       |

Returns: list of `id`, `type` (NOTE, TASK, EMAIL), `body`, `createdAt`, `updatedAt`.

## Lists

### List contact lists

```
hubspot list_contact_lists --limit 20
```

| Argument | Type | Required | Default | Description       |
| -------- | ---- | -------- | ------- | ----------------- |
| `limit`  | int  | no       | 20      | Results to return |
| `offset` | int  | no       | 0       | Pagination offset |

Returns: list of `listId`, `name`, `listType` (STATIC or DYNAMIC), `metaData`, `createdAt`, `updatedAt`.

### Get list

```
hubspot get_list --list_id "1"
```

| Argument  | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| `list_id` | string | yes      | List ID     |

Returns: `listId`, `name`, `listType`, `filters`, `metaData`, `createdAt`, `updatedAt`.

### Create list

```
hubspot create_list --name "Enterprise Leads" --list_type STATIC
```

| Argument    | Type   | Required | Default  | Description                        |
| ----------- | ------ | -------- | -------- | ---------------------------------- |
| `name`      | string | yes      |          | List name                          |
| `list_type` | string | no       | `STATIC` | `STATIC` or `DYNAMIC`              |
| `filters`   | object | no       |          | Filter JSON (required for DYNAMIC) |

Returns: `listId`, `name`, `listType`.

### Add to list

```
hubspot add_to_list --list_id "1" --contact_ids '["123","456","789"]'
```

| Argument      | Type     | Required | Description              |
| ------------- | -------- | -------- | ------------------------ |
| `list_id`     | string   | yes      | List ID (must be STATIC) |
| `contact_ids` | string[] | yes      | Contact IDs to add       |

Returns: `updated` count, `discarded` count, `invalidVids`.

### Remove from list

```
hubspot remove_from_list --list_id "1" --contact_ids '["123"]'
```

| Argument      | Type     | Required | Description              |
| ------------- | -------- | -------- | ------------------------ |
| `list_id`     | string   | yes      | List ID (must be STATIC) |
| `contact_ids` | string[] | yes      | Contact IDs to remove    |

Returns: `updated` count.

## Email

### List marketing emails

```
hubspot list_marketing_emails --limit 20
```

| Argument | Type | Required | Default | Description       |
| -------- | ---- | -------- | ------- | ----------------- |
| `limit`  | int  | no       | 20      | Results to return |
| `offset` | int  | no       | 0       | Pagination offset |

Returns: list of `id`, `name`, `subject`, `state` (DRAFT, PUBLISHED, SENT), `publishDate`, `stats`.

### Get email stats

```
hubspot get_email_stats --email_id "12345"
```

| Argument   | Type   | Required | Description        |
| ---------- | ------ | -------- | ------------------ |
| `email_id` | string | yes      | Marketing email ID |

Returns: `sent`, `delivered`, `opens`, `clicks`, `unsubscribes`, `bounces`, `open_rate`, `click_rate`, `click_through_rate`.

## Properties

### List properties

```
hubspot list_properties --object_type contacts
```

| Argument      | Type   | Required | Description                                 |
| ------------- | ------ | -------- | ------------------------------------------- |
| `object_type` | string | yes      | `contacts`, `companies`, `deals`, `tickets` |

Returns: list of `name`, `label`, `type`, `fieldType`, `groupName`, `description`.

### Create property

```
hubspot create_property --object_type contacts --name "preferred_language" --label "Preferred Language" --type string --field_type text --group_name "contactinformation"
```

| Argument      | Type     | Required | Description                                                |
| ------------- | -------- | -------- | ---------------------------------------------------------- |
| `object_type` | string   | yes      | `contacts`, `companies`, `deals`, `tickets`                |
| `name`        | string   | yes      | Internal property name (no spaces, lowercase)              |
| `label`       | string   | yes      | Display label                                              |
| `type`        | string   | yes      | `string`, `number`, `date`, `datetime`, `enumeration`      |
| `field_type`  | string   | yes      | `text`, `textarea`, `number`, `select`, `checkbox`, `date` |
| `group_name`  | string   | yes      | Property group name                                        |
| `description` | string   | no       | Property description                                       |
| `options`     | object[] | no       | Options for `enumeration` type                             |

Returns: `name`, `label`, `type`, `fieldType`, `groupName`.

## Workflow

1. **Start with `search_contacts`** or `list_contacts` to find existing records. Never guess IDs.
2. Use `list_pipelines` and `list_pipeline_stages` to understand deal/ticket stages before creating or updating.
3. Create associations to link contacts, companies, deals, and tickets together.
4. Log engagement activities (notes, tasks, emails) against CRM records for a complete timeline.
5. Use `list_properties` to discover available fields before creating or updating records.
6. Use `search_*` actions with filter groups for precise queries, and `list_*` for browsing.
7. Build static lists for manual segmentation, dynamic lists for automatic filtering.
8. Check `get_email_stats` to review campaign performance.

## Safety notes

- Object IDs are numeric strings. **Never fabricate them** -- always discover via search or list actions.
- HubSpot API rate limits: 100 requests per 10 seconds for OAuth apps, 200 for private apps.
- `delete_contact`, `delete_company`, `delete_deal`, and `delete_ticket` are **permanent**. HubSpot moves deleted records to a recycling bin for 90 days, but rely on confirmation before deleting.
- Associations must use valid association type labels. Use `list_associations` to inspect existing relationships.
- Property names must be lowercase with no spaces. Use underscores for multi-word names.
- `add_to_list` and `remove_from_list` only work on STATIC lists. DYNAMIC lists are filter-based.
- Search results are limited to 10,000 total matches. Use filters to narrow results.
- Only data accessible to the configured API key or OAuth token is visible.
