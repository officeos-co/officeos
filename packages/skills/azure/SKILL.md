# Azure Skill

Manage Azure resources via the `az` CLI proxy. All actions send `{ command: "az", args: [...] }` to the proxy and return parsed JSON output.

## Credentials
- `proxy_url` — URL of the az CLI proxy endpoint (e.g. `https://az-proxy.internal/exec`)

---

## Resource Groups

### `resource_groups_list`
List all resource groups in the subscription.
```
az group list --output json
```
Params: `subscription` (string, optional)

### `resource_groups_create`
Create a resource group.
```
az group create --name NAME --location LOCATION --output json
```
Params: `name`, `location`

### `resource_groups_delete`
Delete a resource group and all its resources.
```
az group delete --name NAME --yes --output json
```
Params: `name`

---

## Virtual Machines

### `vms_list`
List VMs in a resource group or subscription.
```
az vm list --resource-group RG --output json
```
Params: `resource_group` (string, optional)

### `vms_describe`
Show details of a VM.
```
az vm show --name NAME --resource-group RG --output json
```
Params: `name`, `resource_group`

### `vms_create`
Create a VM.
```
az vm create --name NAME --resource-group RG --image IMAGE --size SIZE --output json
```
Params: `name`, `resource_group`, `image` (default: "Ubuntu2204"), `size` (default: "Standard_B1s"), `location` (optional)

### `vms_start` / `vms_stop` / `vms_deallocate`
Start, stop, or deallocate a VM.
```
az vm start|stop|deallocate --name NAME --resource-group RG --output json
```
Params: `name`, `resource_group`

---

## Web Apps (App Service)

### `webapps_list`
List web apps in a resource group.
```
az webapp list --resource-group RG --output json
```
Params: `resource_group` (string, optional)

### `webapps_describe`
Show details of a web app.
```
az webapp show --name NAME --resource-group RG --output json
```
Params: `name`, `resource_group`

### `webapps_create`
Create a web app.
```
az webapp create --name NAME --resource-group RG --plan PLAN --runtime RUNTIME --output json
```
Params: `name`, `resource_group`, `plan`, `runtime` (e.g. "NODE:20-lts", "PYTHON:3.12")

### `webapps_deploy`
Deploy from a local zip or Docker image.
```
az webapp deploy --name NAME --resource-group RG --src-path PATH --type zip --output json
```
Params: `name`, `resource_group`, `src_path`, `deploy_type` (default: "zip")

---

## Storage

### `storage_accounts_list`
List storage accounts.
```
az storage account list --resource-group RG --output json
```
Params: `resource_group` (string, optional)

### `storage_containers_list`
List blob containers in a storage account.
```
az storage container list --account-name ACCOUNT --output json
```
Params: `account_name`

### `storage_blobs_list`
List blobs in a container.
```
az storage blob list --account-name ACCOUNT --container-name CONTAINER --output json
```
Params: `account_name`, `container_name`, `prefix` (optional)

---

## Azure SQL

### `sql_servers_list`
List SQL servers.
```
az sql server list --resource-group RG --output json
```
Params: `resource_group` (string, optional)

### `sql_databases_list`
List databases on a SQL server.
```
az sql db list --server SERVER --resource-group RG --output json
```
Params: `server`, `resource_group`

---

## AKS

### `aks_list`
List AKS clusters.
```
az aks list --resource-group RG --output json
```
Params: `resource_group` (string, optional)

### `aks_describe`
Show details of an AKS cluster.
```
az aks show --name NAME --resource-group RG --output json
```
Params: `name`, `resource_group`

### `aks_get_credentials`
Get kubeconfig credentials for an AKS cluster.
```
az aks get-credentials --name NAME --resource-group RG
```
Params: `name`, `resource_group`

---

## Azure Functions

### `functionapp_list`
List function apps.
```
az functionapp list --resource-group RG --output json
```
Params: `resource_group` (string, optional)

### `functionapp_create`
Create a function app.
```
az functionapp create --name NAME --resource-group RG --storage-account STORAGE --consumption-plan-location LOCATION --runtime RUNTIME --output json
```
Params: `name`, `resource_group`, `storage_account`, `location`, `runtime` (e.g. "node", "python")

---

## Exit Codes & Errors
All actions throw on non-zero exit codes with stderr as the error message.
JSON output is parsed and returned directly.
