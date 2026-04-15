# Railway Skill

Deploy and manage Railway projects via the Railway GraphQL API v2.

## Credentials
- `api_token` — Railway API token (create at https://railway.app/account/tokens)

---

## Projects

### `projects_list`
List all projects accessible to the authenticated user.
Returns: `[{ id, name, description, createdAt, services: [{ id, name }] }]`

### `projects_get`
Get a specific project by ID.
Params: `project_id` (string)
Returns: `{ id, name, description, createdAt, environments: [{ id, name }] }`

### `projects_create`
Create a new project.
Params: `name` (string), `description` (string, optional)
Returns: `{ id, name }`

### `projects_delete`
Delete a project by ID.
Params: `project_id` (string)
Returns: `{ success: boolean }`

---

## Services

### `services_list`
List all services in a project.
Params: `project_id` (string)
Returns: `[{ id, name, createdAt }]`

### `services_get`
Get a specific service.
Params: `service_id` (string)
Returns: `{ id, name, createdAt, deployments: [{ id, status }] }`

### `services_create`
Create a service in a project.
Params: `project_id` (string), `name` (string)
Returns: `{ id, name }`

### `services_delete`
Delete a service.
Params: `service_id` (string)
Returns: `{ success: boolean }`

---

## Deployments

### `deployments_list`
List deployments for a service.
Params: `service_id` (string), `environment_id` (string), `limit` (number, default: 20)
Returns: `[{ id, status, createdAt, url }]`

### `deployments_get`
Get details of a specific deployment.
Params: `deployment_id` (string)
Returns: `{ id, status, createdAt, url, meta: { ... } }`

### `deployments_trigger`
Trigger a new deployment (redeploy latest).
Params: `service_id` (string), `environment_id` (string)
Returns: `{ id, status }`

### `deployments_cancel`
Cancel a running deployment.
Params: `deployment_id` (string)
Returns: `{ success: boolean }`

### `deployments_rollback`
Rollback to a previous deployment.
Params: `deployment_id` (string)
Returns: `{ id, status }`

---

## Variables

### `variables_list`
List all variables for a service in an environment.
Params: `project_id` (string), `service_id` (string), `environment_id` (string)
Returns: `Record<string, string>` (key-value pairs)

### `variables_upsert`
Create or update one or more variables.
Params: `project_id` (string), `service_id` (string), `environment_id` (string), `variables` (Record<string, string>)
Returns: `{ success: boolean }`

### `variables_delete`
Delete a variable.
Params: `project_id` (string), `service_id` (string), `environment_id` (string), `name` (string)
Returns: `{ success: boolean }`

---

## Domains

### `domains_list`
List custom and railway.app domains for a service.
Params: `service_id` (string), `environment_id` (string)
Returns: `[{ id, domain, createdAt }]`

### `domains_create`
Add a custom domain to a service.
Params: `service_id` (string), `environment_id` (string), `domain` (string)
Returns: `{ id, domain, status }`

### `domains_delete`
Remove a domain from a service.
Params: `domain_id` (string)
Returns: `{ success: boolean }`

---

## Logs

### `logs_deployment`
Fetch logs for a deployment.
Params: `deployment_id` (string), `limit` (number, default: 100)
Returns: `[{ timestamp, message, severity }]`

### `logs_build`
Fetch build logs for a deployment.
Params: `deployment_id` (string)
Returns: `[{ timestamp, message }]`

---

## Environments

### `environments_list`
List environments in a project.
Params: `project_id` (string)
Returns: `[{ id, name, createdAt }]`
