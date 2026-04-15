# Terraform

Full Terraform CLI parity: manage infrastructure-as-code with init, plan, apply, destroy, state management, workspaces, imports, validation, and more via a command proxy.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Init

### Initialize a working directory

```
terraform init --directory . --upgrade true --reconfigure true
```

| Argument         | Type   | Required | Default | Description                                      |
|------------------|--------|----------|---------|--------------------------------------------------|
| `directory`      | string | no       | `.`     | Path to the Terraform configuration directory     |
| `backend_config` | string | no       |         | Backend config key=value (e.g. `bucket=my-bucket`)|
| `upgrade`        | bool   | no       | false   | Upgrade modules and plugins to latest             |
| `reconfigure`    | bool   | no       | false   | Reconfigure backend, ignoring saved config        |

## Plan

### Create an execution plan

```
terraform plan --directory . --var 'region=us-east-1' --var_file prod.tfvars --target aws_instance.web --out plan.tfplan --destroy true
```

| Argument    | Type   | Required | Default | Description                                 |
|-------------|--------|----------|---------|---------------------------------------------|
| `directory` | string | no       | `.`     | Path to the Terraform configuration         |
| `var`       | string | no       |         | Variable in key=value format                |
| `var_file`  | string | no       |         | Path to a .tfvars file                      |
| `target`    | string | no       |         | Resource address to target                  |
| `out`       | string | no       |         | Save plan to file                           |
| `destroy`   | bool   | no       | false   | Plan a destroy operation                    |

## Apply

### Apply changes to infrastructure

```
terraform apply --directory . --auto_approve true --var 'region=us-east-1' --plan_file plan.tfplan
```

| Argument       | Type   | Required | Default | Description                                |
|----------------|--------|----------|---------|--------------------------------------------|
| `directory`    | string | no       | `.`     | Path to the Terraform configuration        |
| `auto_approve` | bool   | no       | false   | Skip interactive approval                  |
| `var`          | string | no       |         | Variable in key=value format               |
| `var_file`     | string | no       |         | Path to a .tfvars file                     |
| `target`       | string | no       |         | Resource address to target                 |
| `plan_file`    | string | no       |         | Path to a saved plan file                  |

## Destroy

### Destroy managed infrastructure

```
terraform destroy --directory . --auto_approve true --target aws_instance.web
```

| Argument       | Type   | Required | Default | Description                                |
|----------------|--------|----------|---------|--------------------------------------------|
| `directory`    | string | no       | `.`     | Path to the Terraform configuration        |
| `auto_approve` | bool   | no       | false   | Skip interactive approval                  |
| `var`          | string | no       |         | Variable in key=value format               |
| `var_file`     | string | no       |         | Path to a .tfvars file                     |
| `target`       | string | no       |         | Resource address to target                 |

## State

### List resources in state

```
terraform list_resources
```

Returns all resource addresses tracked in state.

### Show a single resource in state

```
terraform show_resource --address aws_instance.web
```

| Argument  | Type   | Required | Description                   |
|-----------|--------|----------|-------------------------------|
| `address` | string | yes      | Resource address to inspect   |

### Move a resource in state

```
terraform mv_resource --source aws_instance.old --destination aws_instance.new
```

| Argument      | Type   | Required | Description             |
|---------------|--------|----------|-------------------------|
| `source`      | string | yes      | Current resource address|
| `destination` | string | yes      | New resource address    |

### Remove a resource from state

```
terraform rm_resource --address aws_instance.web
```

| Argument  | Type   | Required | Description                       |
|-----------|--------|----------|-----------------------------------|
| `address` | string | yes      | Resource address to remove        |

### Pull remote state

```
terraform pull_state
```

Returns the raw state JSON from the configured backend.

### Push state to backend

```
terraform push_state --state_file terraform.tfstate
```

| Argument     | Type   | Required | Description                     |
|--------------|--------|----------|---------------------------------|
| `state_file` | string | yes      | Path to local state file        |

## Output

### List all outputs

```
terraform list_outputs
```

Returns all output names and values.

### Get a single output

```
terraform get_output --name vpc_id
```

| Argument | Type   | Required | Description      |
|----------|--------|----------|------------------|
| `name`   | string | yes      | Output name      |

## Workspace

### List workspaces

```
terraform list_workspaces
```

### Create a new workspace

```
terraform new_workspace --name staging
```

| Argument | Type   | Required | Description         |
|----------|--------|----------|---------------------|
| `name`   | string | yes      | Workspace name      |

### Select a workspace

```
terraform select_workspace --name production
```

| Argument | Type   | Required | Description         |
|----------|--------|----------|---------------------|
| `name`   | string | yes      | Workspace name      |

### Delete a workspace

```
terraform delete_workspace --name staging
```

| Argument | Type   | Required | Description         |
|----------|--------|----------|---------------------|
| `name`   | string | yes      | Workspace name      |

### Show current workspace

```
terraform show_workspace
```

Returns the name of the currently selected workspace.

## Import

### Import existing infrastructure

```
terraform import_resource --address aws_instance.web --id i-1234567890abcdef0
```

| Argument  | Type   | Required | Description                        |
|-----------|--------|----------|------------------------------------|
| `address` | string | yes      | Resource address in configuration  |
| `id`      | string | yes      | Provider-specific resource ID      |

## Validate & Format

### Validate configuration

```
terraform validate --directory .
```

| Argument    | Type   | Required | Default | Description                         |
|-------------|--------|----------|---------|-------------------------------------|
| `directory` | string | no       | `.`     | Path to the Terraform configuration |

### Format configuration files

```
terraform fmt --check true --diff true --recursive true
```

| Argument    | Type | Required | Default | Description                              |
|-------------|------|----------|---------|------------------------------------------|
| `check`     | bool | no       | false   | Check if files are formatted (exit code) |
| `diff`      | bool | no       | false   | Display diffs of formatting changes      |
| `recursive` | bool | no       | false   | Also process subdirectories              |

## Providers

### List providers used in configuration

```
terraform list_providers
```

### Lock provider versions

```
terraform lock_providers
```

Writes provider hashes to `.terraform.lock.hcl`.

## Graph

### Generate a dependency graph

```
terraform graph --type plan
```

| Argument | Type   | Required | Default | Description                       |
|----------|--------|----------|---------|-----------------------------------|
| `type`   | string | no       | `plan`  | Graph type: `plan` or `apply`     |

## Taint & Untaint

### Taint a resource

```
terraform taint --address aws_instance.web
```

| Argument  | Type   | Required | Description                        |
|-----------|--------|----------|------------------------------------|
| `address` | string | yes      | Resource address to taint          |

### Untaint a resource

```
terraform untaint --address aws_instance.web
```

| Argument  | Type   | Required | Description                        |
|-----------|--------|----------|------------------------------------|
| `address` | string | yes      | Resource address to untaint        |

## Workflow

1. Run `terraform init` to initialize the working directory and download providers/modules.
2. Use `terraform validate` and `terraform fmt` to check configuration correctness and style.
3. Run `terraform plan` to preview changes before applying.
4. Use `terraform apply` to provision or update infrastructure.
5. Manage state with `list_resources`, `show_resource`, `mv_resource`, `rm_resource`.
6. Use workspaces to manage multiple environments (dev, staging, production).
7. Import existing resources with `terraform import_resource`.
8. Use `terraform destroy` to tear down infrastructure when no longer needed.

## Safety notes

- `apply` and `destroy` without `--auto_approve` require interactive confirmation which is not possible through the proxy. Set `--auto_approve true` when you intend to apply.
- `taint` and `untaint` are deprecated in Terraform 1.5+ in favor of `-replace` flag on `plan`/`apply`. They are included for backward compatibility.
- State operations (`mv_resource`, `rm_resource`, `push_state`) modify state directly. Use with caution.
- Always run `plan` before `apply` to review changes.
- Sensitive outputs are redacted by default. Use `get_output` to retrieve specific values.
