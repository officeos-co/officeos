# Salesforce

Query, create, update, and manage Salesforce records, leads, contacts, opportunities, accounts, cases, reports, and bulk operations via the Salesforce REST API and SOQL.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Records (SOQL)

### Query records

```
salesforce query --soql "SELECT Id, Name, Email FROM Contact WHERE CreatedDate = THIS_YEAR LIMIT 100"
```

| Argument | Type   | Required | Description                        |
|----------|--------|----------|------------------------------------|
| `soql`   | string | yes      | SOQL query string                  |

Returns: `totalSize`, `done`, `records` (list of matching records with requested fields).

### Query all records

```
salesforce query_all --soql "SELECT Id, Name, IsDeleted FROM Account WHERE IsDeleted = true"
```

| Argument | Type   | Required | Description                                    |
|----------|--------|----------|------------------------------------------------|
| `soql`   | string | yes      | SOQL query (includes deleted/archived records) |

Returns: `totalSize`, `done`, `records` (includes soft-deleted and archived records).

## Objects

### List objects

```
salesforce list_objects
```

No arguments required.

Returns: list of `name`, `label`, `keyPrefix`, `custom`, `queryable`, `createable`, `updateable`, `deletable`.

### Describe object

```
salesforce describe_object --object_type Contact
```

| Argument      | Type   | Required | Description                          |
|---------------|--------|----------|--------------------------------------|
| `object_type` | string | yes      | API name (e.g. `Contact`, `Account`) |

Returns: `name`, `label`, `fields` (list of `name`, `label`, `type`, `length`, `nillable`, `createable`, `updateable`, `picklistValues`), `recordTypeInfos`, `childRelationships`.

## Records CRUD

### Get record

```
salesforce get_record --object_type Contact --record_id "003ABC123" --fields '["Id","Name","Email","Phone","AccountId"]'
```

| Argument      | Type     | Required | Description                          |
|---------------|----------|----------|--------------------------------------|
| `object_type` | string   | yes      | Salesforce object API name           |
| `record_id`   | string   | yes      | Record ID (15 or 18 character)       |
| `fields`      | string[] | no       | Fields to retrieve (default: all)    |

Returns: record object with requested fields.

### Create record

```
salesforce create_record --object_type Contact --fields '{"FirstName":"Jane","LastName":"Doe","Email":"jane@example.com","AccountId":"001ABC123"}'
```

| Argument      | Type   | Required | Description                 |
|---------------|--------|----------|-----------------------------|
| `object_type` | string | yes      | Salesforce object API name  |
| `fields`      | object | yes      | Field-value pairs to set    |

Returns: `id`, `success`, `errors`.

### Update record

```
salesforce update_record --object_type Contact --record_id "003ABC123" --fields '{"Phone":"+1234567890","Title":"VP Sales"}'
```

| Argument      | Type   | Required | Description                  |
|---------------|--------|----------|------------------------------|
| `object_type` | string | yes      | Salesforce object API name   |
| `record_id`   | string | yes      | Record ID                    |
| `fields`      | object | yes      | Field-value pairs to update  |

Returns: `id`, `success`, `errors`.

### Delete record

```
salesforce delete_record --object_type Contact --record_id "003ABC123"
```

| Argument      | Type   | Required | Description                |
|---------------|--------|----------|----------------------------|
| `object_type` | string | yes      | Salesforce object API name |
| `record_id`   | string | yes      | Record ID                  |

Returns: `id`, `success`.

### Upsert record

```
salesforce upsert_record --object_type Contact --external_id_field "Email" --external_id_value "jane@example.com" --fields '{"FirstName":"Jane","LastName":"Doe","Phone":"+1234567890"}'
```

| Argument             | Type   | Required | Description                          |
|----------------------|--------|----------|--------------------------------------|
| `object_type`        | string | yes      | Salesforce object API name           |
| `external_id_field`  | string | yes      | External ID field name               |
| `external_id_value`  | string | yes      | External ID value to match           |
| `fields`             | object | yes      | Field-value pairs to set             |

Returns: `id`, `success`, `created` (boolean indicating insert vs update).

## Leads

### List leads

```
salesforce list_leads --status Open --limit 50
```

| Argument | Type   | Required | Default | Description                                      |
|----------|--------|----------|---------|--------------------------------------------------|
| `status` | string | no       |         | Filter: `Open`, `Working`, `Closed`, `Converted` |
| `owner`  | string | no       |         | Filter by owner user ID                          |
| `limit`  | int    | no       | 100     | Max records to return                            |

Returns: list of `Id`, `Name`, `Email`, `Company`, `Status`, `Phone`, `LeadSource`, `CreatedDate`.

### Create lead

```
salesforce create_lead --first_name "John" --last_name "Smith" --company "Acme Corp" --email "john@acme.com" --phone "+1234567890" --lead_source "Web"
```

| Argument      | Type   | Required | Description                          |
|---------------|--------|----------|--------------------------------------|
| `first_name`  | string | no       | First name                           |
| `last_name`   | string | yes      | Last name                            |
| `company`     | string | yes      | Company name                         |
| `email`       | string | no       | Email address                        |
| `phone`       | string | no       | Phone number                         |
| `lead_source` | string | no       | Lead source (e.g. `Web`, `Referral`) |
| `status`      | string | no       | Lead status                          |
| `fields`      | object | no       | Additional field-value pairs         |

Returns: `id`, `success`.

### Convert lead

```
salesforce convert_lead --lead_id "00QABC123" --account_id "001ABC123" --opportunity_name "Acme Deal" --converted_status "Closed - Converted"
```

| Argument           | Type    | Required | Description                                    |
|--------------------|---------|----------|------------------------------------------------|
| `lead_id`          | string  | yes      | Lead ID to convert                             |
| `account_id`       | string  | no       | Existing account to merge into                 |
| `contact_id`       | string  | no       | Existing contact to merge into                 |
| `opportunity_name` | string  | no       | Name for the new opportunity                   |
| `converted_status` | string  | yes      | Status value for converted leads               |
| `do_not_create_opportunity` | boolean | no | Skip opportunity creation              |

Returns: `accountId`, `contactId`, `opportunityId`, `success`.

## Contacts

### List contacts

```
salesforce list_contacts --account_id "001ABC123" --limit 50
```

| Argument     | Type   | Required | Default | Description              |
|--------------|--------|----------|---------|--------------------------|
| `account_id` | string | no       |         | Filter by account        |
| `limit`      | int    | no       | 100     | Max records to return    |

Returns: list of `Id`, `Name`, `Email`, `Phone`, `AccountId`, `Title`, `CreatedDate`.

### Create contact

```
salesforce create_contact --first_name "Jane" --last_name "Doe" --email "jane@example.com" --account_id "001ABC123" --phone "+1234567890"
```

| Argument     | Type   | Required | Description            |
|--------------|--------|----------|------------------------|
| `first_name` | string | no       | First name             |
| `last_name`  | string | yes      | Last name              |
| `email`      | string | no       | Email address          |
| `phone`      | string | no       | Phone number           |
| `account_id` | string | no       | Associated account ID  |
| `title`      | string | no       | Job title              |
| `fields`     | object | no       | Additional fields      |

Returns: `id`, `success`.

### Update contact

```
salesforce update_contact --contact_id "003ABC123" --fields '{"Phone":"+9876543210","Title":"CTO"}'
```

| Argument     | Type   | Required | Description          |
|--------------|--------|----------|----------------------|
| `contact_id` | string | yes      | Contact ID           |
| `fields`     | object | yes      | Fields to update     |

Returns: `id`, `success`.

## Opportunities

### List opportunities

```
salesforce list_opportunities --stage "Prospecting" --close_date_range '{"start":"2026-01-01","end":"2026-12-31"}' --limit 50
```

| Argument           | Type   | Required | Default | Description                              |
|--------------------|--------|----------|---------|------------------------------------------|
| `stage`            | string | no       |         | Filter by stage name                     |
| `account_id`       | string | no       |         | Filter by account                        |
| `close_date_range` | object | no       |         | JSON with `start` and `end` (YYYY-MM-DD) |
| `limit`            | int    | no       | 100     | Max records to return                    |

Returns: list of `Id`, `Name`, `StageName`, `Amount`, `CloseDate`, `AccountId`, `Probability`, `CreatedDate`.

### Create opportunity

```
salesforce create_opportunity --name "Enterprise License" --stage "Prospecting" --close_date "2026-06-30" --amount 50000 --account_id "001ABC123"
```

| Argument     | Type   | Required | Description                       |
|--------------|--------|----------|-----------------------------------|
| `name`       | string | yes      | Opportunity name                  |
| `stage`      | string | yes      | Stage name                        |
| `close_date` | string | yes      | Close date (YYYY-MM-DD)           |
| `amount`     | number | no       | Deal amount                       |
| `account_id` | string | no       | Associated account ID             |
| `fields`     | object | no       | Additional fields                 |

Returns: `id`, `success`.

### Update opportunity

```
salesforce update_opportunity --opportunity_id "006ABC123" --fields '{"StageName":"Closed Won","Amount":55000}'
```

| Argument         | Type   | Required | Description          |
|------------------|--------|----------|----------------------|
| `opportunity_id` | string | yes      | Opportunity ID       |
| `fields`         | object | yes      | Fields to update     |

Returns: `id`, `success`.

## Accounts

### List accounts

```
salesforce list_accounts --type "Customer" --limit 50
```

| Argument | Type   | Required | Default | Description                    |
|----------|--------|----------|---------|--------------------------------|
| `type`   | string | no       |         | Filter by account type         |
| `owner`  | string | no       |         | Filter by owner user ID        |
| `limit`  | int    | no       | 100     | Max records to return          |

Returns: list of `Id`, `Name`, `Type`, `Industry`, `AnnualRevenue`, `NumberOfEmployees`, `Website`, `CreatedDate`.

### Create account

```
salesforce create_account --name "Acme Corp" --type "Customer" --industry "Technology" --website "https://acme.com"
```

| Argument   | Type   | Required | Description           |
|------------|--------|----------|-----------------------|
| `name`     | string | yes      | Account name          |
| `type`     | string | no       | Account type          |
| `industry` | string | no       | Industry              |
| `website`  | string | no       | Website URL           |
| `phone`    | string | no       | Phone number          |
| `fields`   | object | no       | Additional fields     |

Returns: `id`, `success`.

### Update account

```
salesforce update_account --account_id "001ABC123" --fields '{"AnnualRevenue":5000000,"NumberOfEmployees":500}'
```

| Argument     | Type   | Required | Description          |
|--------------|--------|----------|----------------------|
| `account_id` | string | yes      | Account ID           |
| `fields`     | object | yes      | Fields to update     |

Returns: `id`, `success`.

## Cases

### List cases

```
salesforce list_cases --status "New" --priority "High" --limit 50
```

| Argument   | Type   | Required | Default | Description                                   |
|------------|--------|----------|---------|-----------------------------------------------|
| `status`   | string | no       |         | Filter: `New`, `Working`, `Escalated`, `Closed` |
| `priority` | string | no       |         | Filter: `Low`, `Medium`, `High`, `Critical`   |
| `account_id`| string | no      |         | Filter by account                              |
| `limit`    | int    | no       | 100     | Max records to return                          |

Returns: list of `Id`, `CaseNumber`, `Subject`, `Status`, `Priority`, `AccountId`, `ContactId`, `CreatedDate`.

### Create case

```
salesforce create_case --subject "Login issue" --description "User unable to access dashboard" --priority "High" --status "New" --account_id "001ABC123" --contact_id "003ABC123"
```

| Argument      | Type   | Required | Description              |
|---------------|--------|----------|--------------------------|
| `subject`     | string | yes      | Case subject             |
| `description` | string | no       | Detailed description     |
| `priority`    | string | no       | `Low`, `Medium`, `High`, `Critical` |
| `status`      | string | no       | Case status              |
| `account_id`  | string | no       | Associated account ID    |
| `contact_id`  | string | no       | Associated contact ID    |
| `fields`      | object | no       | Additional fields        |

Returns: `id`, `success`.

### Update case

```
salesforce update_case --case_id "500ABC123" --fields '{"Status":"Working","Priority":"Critical"}'
```

| Argument  | Type   | Required | Description        |
|-----------|--------|----------|--------------------|
| `case_id` | string | yes      | Case ID            |
| `fields`  | object | yes      | Fields to update   |

Returns: `id`, `success`.

### Close case

```
salesforce close_case --case_id "500ABC123" --resolution "Issue resolved by resetting password"
```

| Argument     | Type   | Required | Description               |
|--------------|--------|----------|---------------------------|
| `case_id`    | string | yes      | Case ID                   |
| `resolution` | string | no       | Resolution description    |

Returns: `id`, `success`, `status` (will be `Closed`).

## Reports

### List reports

```
salesforce list_reports --limit 20
```

| Argument | Type | Required | Default | Description           |
|----------|------|----------|---------|-----------------------|
| `limit`  | int  | no       | 100     | Max results to return |

Returns: list of `Id`, `Name`, `ReportFormat`, `FolderName`, `LastRunDate`.

### Run report

```
salesforce run_report --report_id "00OABC123" --filters '{"dateFilter":{"column":"CREATED_DATE","startDate":"2026-01-01","endDate":"2026-03-31"}}'
```

| Argument    | Type   | Required | Description                                  |
|-------------|--------|----------|----------------------------------------------|
| `report_id` | string | yes      | Report ID                                    |
| `filters`   | object | no       | Runtime filter overrides (JSON)              |

Returns: `reportMetadata` (name, columns, filters), `factMap` (aggregated data), `hasDetailRows`, `reportExtendedMetadata`.

### Get report metadata

```
salesforce get_report_metadata --report_id "00OABC123"
```

| Argument    | Type   | Required | Description |
|-------------|--------|----------|-------------|
| `report_id` | string | yes      | Report ID   |

Returns: `id`, `name`, `reportFormat`, `reportType`, `detailColumns`, `aggregates`, `groupingsDown`, `groupingsAcross`, `reportFilters`.

## Users

### List users

```
salesforce list_users --active true --limit 50
```

| Argument | Type    | Required | Default | Description              |
|----------|---------|----------|---------|--------------------------|
| `active` | boolean | no       |         | Filter by active status  |
| `role`   | string  | no       |         | Filter by role name      |
| `limit`  | int     | no       | 100     | Max records to return    |

Returns: list of `Id`, `Name`, `Email`, `Username`, `IsActive`, `Profile`, `UserRole`, `LastLoginDate`.

### Get user

```
salesforce get_user --user_id "005ABC123"
```

| Argument  | Type   | Required | Description |
|-----------|--------|----------|-------------|
| `user_id` | string | yes      | User ID     |

Returns: `Id`, `Name`, `Email`, `Username`, `IsActive`, `Profile`, `UserRole`, `Department`, `Title`, `Phone`, `LastLoginDate`, `CreatedDate`.

## Bulk

### Bulk insert

```
salesforce bulk_insert --object_type Contact --records '[{"FirstName":"Jane","LastName":"Doe","Email":"jane@example.com"},{"FirstName":"John","LastName":"Smith","Email":"john@example.com"}]'
```

| Argument      | Type     | Required | Description                         |
|---------------|----------|----------|-------------------------------------|
| `object_type` | string   | yes      | Salesforce object API name          |
| `records`     | object[] | yes      | List of record objects to insert    |

Returns: `job_id`, `state`, `numberRecordsProcessed`, `numberRecordsFailed`, `results` (per-record `id`, `success`, `errors`).

### Bulk update

```
salesforce bulk_update --object_type Contact --records '[{"Id":"003ABC123","Phone":"+111"},{"Id":"003DEF456","Phone":"+222"}]'
```

| Argument      | Type     | Required | Description                                    |
|---------------|----------|----------|------------------------------------------------|
| `object_type` | string   | yes      | Salesforce object API name                     |
| `records`     | object[] | yes      | List of records with `Id` and fields to update |

Returns: `job_id`, `state`, `numberRecordsProcessed`, `numberRecordsFailed`, `results`.

### Bulk delete

```
salesforce bulk_delete --object_type Contact --record_ids '["003ABC123","003DEF456","003GHI789"]'
```

| Argument      | Type     | Required | Description                |
|---------------|----------|----------|----------------------------|
| `object_type` | string   | yes      | Salesforce object API name |
| `record_ids`  | string[] | yes      | List of record IDs         |

Returns: `job_id`, `state`, `numberRecordsProcessed`, `numberRecordsFailed`, `results`.

## Metadata

### List metadata types

```
salesforce list_metadata_types
```

No arguments required.

Returns: list of `xmlName`, `directoryName`, `suffix`, `inFolder`, `metaFile`.

### Deploy metadata

```
salesforce deploy_metadata --zip_path "/tmp/metadata.zip" --check_only true
```

| Argument       | Type    | Required | Default | Description                                |
|----------------|---------|----------|---------|--------------------------------------------|
| `zip_path`     | string  | yes      |         | Path to metadata zip package               |
| `check_only`   | boolean | no       | false   | Validate only without deploying            |
| `test_level`   | string  | no       |         | `NoTestRun`, `RunSpecifiedTests`, `RunLocalTests`, `RunAllTestsInOrg` |
| `run_tests`    | string[]| no       |         | Test class names (for RunSpecifiedTests)   |

Returns: `id`, `status`, `numberComponentsDeployed`, `numberComponentErrors`, `numberTestsCompleted`, `numberTestErrors`, `success`.

## Workflow

1. **Start with `list_objects`** or `describe_object` to understand available objects and their fields.
2. Use `query` with SOQL for flexible record retrieval. Use `describe_object` to discover field names first.
3. Use the typed actions (`list_leads`, `create_contact`, etc.) for common objects to simplify syntax.
4. For generic or custom objects, use `create_record`, `update_record`, `get_record`, and `delete_record`.
5. Use `convert_lead` to move leads through the sales pipeline into contacts, accounts, and opportunities.
6. Use `run_report` to leverage existing Salesforce reports rather than building complex SOQL queries.
7. For large data operations (100+ records), use `bulk_insert`, `bulk_update`, or `bulk_delete` instead of individual record operations.
8. Always use `describe_object` to verify required fields before creating records.

## Safety notes

- Record IDs are 15 or 18 character alphanumeric strings. **Never fabricate them** -- always discover via query or list actions.
- `delete_record` moves records to the Recycle Bin (recoverable for 15 days). Bulk deletes are harder to recover.
- SOQL queries have a **50,000 row limit** per query. Use `LIMIT` and `OFFSET` for pagination.
- Bulk operations are **asynchronous**. The `job_id` can be used to poll for completion status.
- `deploy_metadata` with `check_only: false` modifies the Salesforce org schema. Always validate with `check_only: true` first.
- API call limits depend on the Salesforce edition (typically 15,000-100,000 per 24 hours). Monitor usage.
- `convert_lead` is irreversible. The lead record is permanently converted.
- SOQL injection is possible if user input is concatenated into queries. Use bind variables where available.
- Only records and metadata accessible to the configured user's profile and permissions are visible.
