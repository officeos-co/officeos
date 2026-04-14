# Kubernetes

Manage Kubernetes clusters, workloads, services, config, and nodes via the Kubernetes API (kubectl parity).

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

All operations accept `--namespace` (default: `default`) and `--context` (optional) to target a specific cluster.

## Pods

### List pods

```
kubernetes list_pods --namespace default --label_selector app=backend
```

| Argument         | Type   | Required | Default   | Description                       |
|------------------|--------|----------|-----------|-----------------------------------|
| `namespace`      | string | no       | `default` | Kubernetes namespace              |
| `label_selector` | string | no       |           | Label filter (e.g. `app=backend`) |
| `field_selector` | string | no       |           | Field filter (e.g. `status.phase=Running`) |
| `context`        | string | no       |           | Kubeconfig context                |

Returns: list of pods with `name`, `namespace`, `status`, `ready`, `restarts`, `age`, `node`, `ip`.

### Get pod

```
kubernetes get_pod --name backend-abc123 --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `name`      | string | yes      |           | Pod name             |
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: full pod spec including `containers`, `volumes`, `status`, `conditions`, `events`.

### Delete pod

```
kubernetes delete_pod --name backend-abc123 --namespace default --grace_period 30
```

| Argument       | Type   | Required | Default   | Description                        |
|----------------|--------|----------|-----------|------------------------------------|
| `name`         | string | yes      |           | Pod name                           |
| `namespace`    | string | no       | `default` | Kubernetes namespace               |
| `grace_period` | int    | no       | 30        | Seconds to wait for graceful stop  |
| `context`      | string | no       |           | Kubeconfig context                 |

Returns: confirmation with `name` and `status`.

### Pod logs

```
kubernetes logs --name backend-abc123 --namespace default --container api --tail 100 --previous false
```

| Argument    | Type    | Required | Default   | Description                        |
|-------------|---------|----------|-----------|------------------------------------|
| `name`      | string  | yes      |           | Pod name                           |
| `namespace` | string  | no       | `default` | Kubernetes namespace               |
| `container` | string  | no       |           | Container name (required if multi-container) |
| `tail`      | int     | no       | 200       | Number of lines from the end       |
| `previous`  | boolean | no       | false     | Logs from previous container instance |
| `since`     | string  | no       |           | Duration (e.g. `1h`, `30m`)        |
| `context`   | string  | no       |           | Kubeconfig context                 |

Returns: log output as text.

### Exec in pod

```
kubernetes exec --name backend-abc123 --namespace default --command "ls -la /app" --container api
```

| Argument    | Type   | Required | Default   | Description                          |
|-------------|--------|----------|-----------|--------------------------------------|
| `name`      | string | yes      |           | Pod name                             |
| `namespace` | string | no       | `default` | Kubernetes namespace                 |
| `command`   | string | yes      |           | Command to execute                   |
| `container` | string | no       |           | Container name (required if multi-container) |
| `context`   | string | no       |           | Kubeconfig context                   |

Returns: `exit_code`, `stdout`, `stderr`.

### Describe pod

```
kubernetes describe_pod --name backend-abc123 --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `name`      | string | yes      |           | Pod name             |
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: human-readable description including `events`, `conditions`, `resource_requests`, `resource_limits`, `volumes`.

## Deployments

### List deployments

```
kubernetes list_deployments --namespace default --label_selector app=backend
```

| Argument         | Type   | Required | Default   | Description                       |
|------------------|--------|----------|-----------|-----------------------------------|
| `namespace`      | string | no       | `default` | Kubernetes namespace              |
| `label_selector` | string | no       |           | Label filter (e.g. `app=backend`) |
| `context`        | string | no       |           | Kubeconfig context                |

Returns: list with `name`, `ready`, `up_to_date`, `available`, `age`, `images`.

### Get deployment

```
kubernetes get_deployment --name backend --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `name`      | string | yes      |           | Deployment name      |
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: full deployment spec including `replicas`, `strategy`, `template`, `conditions`, `revision_history`.

### Create deployment

```
kubernetes create_deployment --name my-app --image nginx:latest --replicas 3 --namespace default --port 80
```

| Argument    | Type   | Required | Default   | Description                  |
|-------------|--------|----------|-----------|------------------------------|
| `name`      | string | yes      |           | Deployment name              |
| `image`     | string | yes      |           | Container image              |
| `replicas`  | int    | no       | 1         | Number of replicas           |
| `namespace` | string | no       | `default` | Kubernetes namespace         |
| `port`      | int    | no       |           | Container port               |
| `env`       | string | no       |           | JSON object of env vars      |
| `labels`    | string | no       |           | JSON object of labels        |
| `context`   | string | no       |           | Kubeconfig context           |

Returns: `name`, `namespace`, `replicas`, `image`.

### Scale deployment

```
kubernetes scale --name backend --replicas 5 --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `name`      | string | yes      |           | Deployment name      |
| `replicas`  | int    | yes      |           | Desired replica count|
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: `name`, `previous_replicas`, `current_replicas`.

### Rollout status

```
kubernetes rollout_status --name backend --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `name`      | string | yes      |           | Deployment name      |
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: `status`, `message`, `ready_replicas`, `updated_replicas`, `available_replicas`.

### Rollout restart

```
kubernetes rollout_restart --name backend --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `name`      | string | yes      |           | Deployment name      |
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: confirmation with `name` and restart timestamp.

### Rollout undo

```
kubernetes rollout_undo --name backend --namespace default --revision 3
```

| Argument    | Type   | Required | Default   | Description                          |
|-------------|--------|----------|-----------|--------------------------------------|
| `name`      | string | yes      |           | Deployment name                      |
| `namespace` | string | no       | `default` | Kubernetes namespace                 |
| `revision`  | int    | no       |           | Revision to roll back to (omit for previous) |
| `context`   | string | no       |           | Kubeconfig context                   |

Returns: confirmation with `name` and `rolled_back_to` revision.

### Delete deployment

```
kubernetes delete_deployment --name my-app --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `name`      | string | yes      |           | Deployment name      |
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: confirmation with `name`.

## Services

### List services

```
kubernetes list_services --namespace default
```

| Argument         | Type   | Required | Default   | Description                       |
|------------------|--------|----------|-----------|-----------------------------------|
| `namespace`      | string | no       | `default` | Kubernetes namespace              |
| `label_selector` | string | no       |           | Label filter                      |
| `context`        | string | no       |           | Kubeconfig context                |

Returns: list with `name`, `type`, `cluster_ip`, `external_ip`, `ports`, `age`.

### Get service

```
kubernetes get_service --name backend-svc --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `name`      | string | yes      |           | Service name         |
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: full service spec including `type`, `selector`, `ports`, `endpoints`.

### Create service

```
kubernetes create_service --name backend-svc --type ClusterIP --port 80 --target_port 8080 --selector '{"app":"backend"}' --namespace default
```

| Argument      | Type   | Required | Default     | Description                     |
|---------------|--------|----------|-------------|---------------------------------|
| `name`        | string | yes      |             | Service name                    |
| `type`        | string | no       | `ClusterIP` | `ClusterIP`, `NodePort`, `LoadBalancer` |
| `port`        | int    | yes      |             | Service port                    |
| `target_port` | int    | no       |             | Target port on pods             |
| `selector`    | string | yes      |             | JSON object of pod selectors    |
| `namespace`   | string | no       | `default`   | Kubernetes namespace            |
| `context`     | string | no       |             | Kubeconfig context              |

Returns: `name`, `type`, `cluster_ip`, `ports`.

### Delete service

```
kubernetes delete_service --name backend-svc --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `name`      | string | yes      |           | Service name         |
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: confirmation with `name`.

## ConfigMaps and Secrets

### List ConfigMaps

```
kubernetes list_configmaps --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: list with `name`, `data_keys`, `age`.

### Get ConfigMap

```
kubernetes get_configmap --name app-config --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `name`      | string | yes      |           | ConfigMap name       |
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: `name`, `namespace`, `data` (key-value pairs).

### Create ConfigMap

```
kubernetes create_configmap --name app-config --namespace default --data '{"DATABASE_URL":"postgres://localhost/db","LOG_LEVEL":"info"}'
```

| Argument    | Type   | Required | Default   | Description                  |
|-------------|--------|----------|-----------|------------------------------|
| `name`      | string | yes      |           | ConfigMap name               |
| `namespace` | string | no       | `default` | Kubernetes namespace         |
| `data`      | string | yes      |           | JSON object of key-value pairs |
| `context`   | string | no       |           | Kubeconfig context           |

Returns: `name`, `namespace`, `data_keys`.

### List Secrets

```
kubernetes list_secrets --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: list with `name`, `type`, `data_keys`, `age`.

### Get Secret

```
kubernetes get_secret --name db-credentials --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `name`      | string | yes      |           | Secret name          |
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: `name`, `namespace`, `type`, `data_keys`. Values are base64-encoded.

### Create Secret

```
kubernetes create_secret --name db-credentials --namespace default --type Opaque --data '{"username":"admin","password":"s3cret"}'
```

| Argument    | Type   | Required | Default   | Description                         |
|-------------|--------|----------|-----------|-------------------------------------|
| `name`      | string | yes      |           | Secret name                         |
| `namespace` | string | no       | `default` | Kubernetes namespace                |
| `type`      | string | no       | `Opaque`  | Secret type                         |
| `data`      | string | yes      |           | JSON object of key-value pairs (values will be base64-encoded) |
| `context`   | string | no       |           | Kubeconfig context                  |

Returns: `name`, `namespace`, `type`, `data_keys`.

## Namespaces

### List namespaces

```
kubernetes list_namespaces
```

| Argument  | Type   | Required | Description        |
|-----------|--------|----------|--------------------|
| `context` | string | no       | Kubeconfig context |

Returns: list with `name`, `status`, `age`, `labels`.

### Create namespace

```
kubernetes create_namespace --name staging --labels '{"team":"platform"}'
```

| Argument  | Type   | Required | Description                |
|-----------|--------|----------|----------------------------|
| `name`    | string | yes      | Namespace name             |
| `labels`  | string | no       | JSON object of labels      |
| `context` | string | no       | Kubeconfig context         |

Returns: `name`, `status`.

### Delete namespace

```
kubernetes delete_namespace --name staging
```

| Argument  | Type   | Required | Description        |
|-----------|--------|----------|--------------------|
| `name`    | string | yes      | Namespace name     |
| `context` | string | no       | Kubeconfig context |

Returns: confirmation with `name`. All resources in the namespace will be deleted.

## Nodes

### List nodes

```
kubernetes list_nodes
```

| Argument         | Type   | Required | Description                    |
|------------------|--------|----------|--------------------------------|
| `label_selector` | string | no       | Label filter                   |
| `context`        | string | no       | Kubeconfig context             |

Returns: list with `name`, `status`, `roles`, `version`, `os`, `arch`, `cpu`, `memory`, `pods`.

### Describe node

```
kubernetes describe_node --name worker-01
```

| Argument  | Type   | Required | Description        |
|-----------|--------|----------|--------------------|
| `name`    | string | yes      | Node name          |
| `context` | string | no       | Kubeconfig context |

Returns: full node info including `conditions`, `capacity`, `allocatable`, `system_info`, `pods`, `events`.

### Cordon node

```
kubernetes cordon --name worker-01
```

| Argument  | Type   | Required | Description        |
|-----------|--------|----------|--------------------|
| `name`    | string | yes      | Node name          |
| `context` | string | no       | Kubeconfig context |

Returns: confirmation. Node marked as unschedulable.

### Uncordon node

```
kubernetes uncordon --name worker-01
```

| Argument  | Type   | Required | Description        |
|-----------|--------|----------|--------------------|
| `name`    | string | yes      | Node name          |
| `context` | string | no       | Kubeconfig context |

Returns: confirmation. Node marked as schedulable.

### Drain node

```
kubernetes drain --name worker-01 --ignore_daemonsets true --delete_emptydir_data true --grace_period 60
```

| Argument               | Type    | Required | Default | Description                          |
|------------------------|---------|----------|---------|--------------------------------------|
| `name`                 | string  | yes      |         | Node name                            |
| `ignore_daemonsets`    | boolean | no       | true    | Ignore DaemonSet pods                |
| `delete_emptydir_data` | boolean | no       | false   | Delete emptyDir data                 |
| `grace_period`         | int     | no       | 30      | Seconds for graceful termination     |
| `context`              | string  | no       |         | Kubeconfig context                   |

Returns: list of evicted pods and confirmation.

## Jobs and CronJobs

### List jobs

```
kubernetes list_jobs --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: list with `name`, `completions`, `succeeded`, `failed`, `age`, `duration`.

### Create job

```
kubernetes create_job --name db-migration --image myapp:latest --command "python manage.py migrate" --namespace default
```

| Argument          | Type   | Required | Default   | Description                    |
|-------------------|--------|----------|-----------|--------------------------------|
| `name`            | string | yes      |           | Job name                       |
| `image`           | string | yes      |           | Container image                |
| `command`         | string | yes      |           | Command to execute             |
| `namespace`       | string | no       | `default` | Kubernetes namespace           |
| `backoff_limit`   | int    | no       | 6         | Number of retries              |
| `restart_policy`  | string | no       | `Never`   | `Never` or `OnFailure`        |
| `context`         | string | no       |           | Kubeconfig context             |

Returns: `name`, `namespace`, `status`.

### List CronJobs

```
kubernetes list_cronjobs --namespace default
```

| Argument    | Type   | Required | Default   | Description          |
|-------------|--------|----------|-----------|----------------------|
| `namespace` | string | no       | `default` | Kubernetes namespace |
| `context`   | string | no       |           | Kubeconfig context   |

Returns: list with `name`, `schedule`, `suspend`, `active`, `last_schedule`, `age`.

### Create CronJob

```
kubernetes create_cronjob --name nightly-backup --image backup:latest --command "/backup.sh" --schedule "0 2 * * *" --namespace default
```

| Argument                         | Type   | Required | Default   | Description                   |
|----------------------------------|--------|----------|-----------|-------------------------------|
| `name`                           | string | yes      |           | CronJob name                  |
| `image`                          | string | yes      |           | Container image               |
| `command`                        | string | yes      |           | Command to execute            |
| `schedule`                       | string | yes      |           | Cron expression               |
| `namespace`                      | string | no       | `default` | Kubernetes namespace          |
| `successful_jobs_history_limit`  | int    | no       | 3         | Completed jobs to keep        |
| `failed_jobs_history_limit`      | int    | no       | 1         | Failed jobs to keep           |
| `context`                        | string | no       |           | Kubeconfig context            |

Returns: `name`, `namespace`, `schedule`.

## Apply and Delete

### Apply manifest

```
kubernetes apply --manifest "apiVersion: v1\nkind: ConfigMap\nmetadata:\n  name: test" --namespace default
```

| Argument    | Type   | Required | Default   | Description                          |
|-------------|--------|----------|-----------|--------------------------------------|
| `manifest`  | string | yes      |           | YAML or JSON manifest content        |
| `namespace` | string | no       | `default` | Kubernetes namespace                 |
| `context`   | string | no       |           | Kubeconfig context                   |

Returns: `kind`, `name`, `namespace`, `action` (`created`, `configured`, or `unchanged`).

### Delete resource

```
kubernetes delete_resource --kind Deployment --name my-app --namespace default
```

| Argument       | Type   | Required | Default   | Description                        |
|----------------|--------|----------|-----------|------------------------------------|
| `kind`         | string | yes      |           | Resource kind (e.g. `Deployment`, `Service`, `Pod`) |
| `name`         | string | yes      |           | Resource name                      |
| `namespace`    | string | no       | `default` | Kubernetes namespace               |
| `grace_period` | int    | no       |           | Seconds for graceful termination   |
| `context`      | string | no       |           | Kubeconfig context                 |

Returns: confirmation with `kind`, `name`.

## Context

### List contexts

```
kubernetes list_contexts
```

Returns: list of available contexts with `name`, `cluster`, `user`, `namespace`, `current` (boolean).

### Use context

```
kubernetes use_context --name production
```

| Argument | Type   | Required | Description       |
|----------|--------|----------|-------------------|
| `name`   | string | yes      | Context name      |

Returns: confirmation with active context `name`.

### Current context

```
kubernetes current_context
```

Returns: `name`, `cluster`, `user`, `namespace` of the active context.

## Workflow

1. **Check cluster connectivity** with `kubernetes current_context` and `kubernetes list_nodes`.
2. **Inspect workloads** with `kubernetes list_deployments` and `kubernetes list_pods` to see running state.
3. **Debug failing pods** with `kubernetes describe_pod` for events, then `kubernetes logs` for application output.
4. **Deploy changes** with `kubernetes apply` using a YAML manifest, or `kubernetes create_deployment` for simple cases.
5. **Scale workloads** with `kubernetes scale` to adjust replica count.
6. **Roll back** with `kubernetes rollout_undo` if a deployment is unhealthy.
7. **Manage config** with ConfigMaps and Secrets, then restart deployments with `kubernetes rollout_restart`.
8. **Node maintenance** with `kubernetes cordon`, `kubernetes drain`, then `kubernetes uncordon` after work is done.

## Safety notes

- `delete_namespace` deletes all resources within it. This is irreversible. Always confirm with the user.
- `drain` evicts all pods from a node. Ensure sufficient capacity on remaining nodes before draining.
- Secret values are base64-encoded, not encrypted. Do not log or display secret values.
- `apply` with a manifest can create or modify any resource. Review the manifest content before applying.
- `delete_pod` on a pod managed by a Deployment will cause a replacement pod to be created. To remove permanently, delete the Deployment.
- `rollout_undo` without `--revision` rolls back to the immediately previous revision. Verify the target revision first.
- Exec commands run with the container's service account privileges. Avoid running destructive commands without confirmation.
