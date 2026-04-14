# AWS

Manage AWS cloud infrastructure: EC2 instances, S3 storage, Lambda functions, IAM, CloudWatch, ECS, Route53, SQS, SNS, and RDS via the AWS API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## EC2

### List instances

```
aws list_instances --region us-east-1 --state running --per_page 50
```

| Argument   | Type   | Required | Default     | Description                                          |
| ---------- | ------ | -------- | ----------- | ---------------------------------------------------- |
| `region`   | string | no       | from config | AWS region                                           |
| `state`    | string | no       | `all`       | `running`, `stopped`, `pending`, `terminated`, `all` |
| `per_page` | int    | no       | 50          | Results per page (1-1000)                            |
| `filters`  | string | no       |             | JSON array of Name/Values filters                    |

Returns: `instance_id`, `instance_type`, `state`, `public_ip`, `private_ip`, `launch_time`, `tags`, `vpc_id`, `subnet_id`, `security_groups`.

### Get instance

```
aws get_instance --instance_id i-0abc123def456 --region us-east-1
```

| Argument      | Type   | Required | Default     | Description     |
| ------------- | ------ | -------- | ----------- | --------------- |
| `instance_id` | string | yes      |             | EC2 instance ID |
| `region`      | string | no       | from config | AWS region      |

Returns: full instance details including `instance_id`, `instance_type`, `state`, `public_ip`, `private_ip`, `launch_time`, `tags`, `vpc_id`, `subnet_id`, `security_groups`, `iam_instance_profile`, `block_device_mappings`, `network_interfaces`.

### Start instance

```
aws start_instance --instance_id i-0abc123def456 --region us-east-1
```

| Argument      | Type   | Required | Default     | Description     |
| ------------- | ------ | -------- | ----------- | --------------- |
| `instance_id` | string | yes      |             | EC2 instance ID |
| `region`      | string | no       | from config | AWS region      |

Returns: `instance_id`, `previous_state`, `current_state`.

### Stop instance

```
aws stop_instance --instance_id i-0abc123def456 --region us-east-1
```

| Argument      | Type   | Required | Default     | Description     |
| ------------- | ------ | -------- | ----------- | --------------- |
| `instance_id` | string | yes      |             | EC2 instance ID |
| `region`      | string | no       | from config | AWS region      |

Returns: `instance_id`, `previous_state`, `current_state`.

### Terminate instance

```
aws terminate_instance --instance_id i-0abc123def456 --region us-east-1
```

| Argument      | Type   | Required | Default     | Description     |
| ------------- | ------ | -------- | ----------- | --------------- |
| `instance_id` | string | yes      |             | EC2 instance ID |
| `region`      | string | no       | from config | AWS region      |

Returns: `instance_id`, `previous_state`, `current_state`.

### Create instance

```
aws create_instance --ami ami-0abcdef1234567890 --instance_type t3.micro --key_name my-key --security_groups '["sg-12345"]' --subnet subnet-abc123 --region us-east-1
```

| Argument          | Type     | Required | Default     | Description                     |
| ----------------- | -------- | -------- | ----------- | ------------------------------- |
| `ami`             | string   | yes      |             | AMI ID                          |
| `instance_type`   | string   | yes      |             | Instance type (e.g. `t3.micro`) |
| `key_name`        | string   | no       |             | SSH key pair name               |
| `security_groups` | string[] | no       |             | Security group IDs              |
| `subnet`          | string   | no       |             | Subnet ID                       |
| `tags`            | string   | no       |             | JSON object of key-value tags   |
| `user_data`       | string   | no       |             | Base64-encoded user data script |
| `count`           | int      | no       | 1           | Number of instances to launch   |
| `region`          | string   | no       | from config | AWS region                      |

Returns: `instance_id`, `instance_type`, `state`, `public_ip`, `private_ip`, `launch_time`.

## S3

### List buckets

```
aws list_buckets --region us-east-1
```

| Argument | Type   | Required | Default     | Description |
| -------- | ------ | -------- | ----------- | ----------- |
| `region` | string | no       | from config | AWS region  |

Returns: array of `name`, `creation_date`, `region`.

### List objects

```
aws list_objects --bucket my-bucket --prefix logs/ --max_keys 100
```

| Argument   | Type   | Required | Default     | Description               |
| ---------- | ------ | -------- | ----------- | ------------------------- |
| `bucket`   | string | yes      |             | Bucket name               |
| `prefix`   | string | no       |             | Key prefix filter         |
| `max_keys` | int    | no       | 1000        | Maximum objects to return |
| `region`   | string | no       | from config | AWS region                |

Returns: array of `key`, `size`, `last_modified`, `storage_class`, `etag`.

### Get object

```
aws get_object --bucket my-bucket --key config.json
```

| Argument | Type   | Required | Default     | Description |
| -------- | ------ | -------- | ----------- | ----------- |
| `bucket` | string | yes      |             | Bucket name |
| `key`    | string | yes      |             | Object key  |
| `region` | string | no       | from config | AWS region  |

Returns: `content_type`, `content_length`, `last_modified`, `body` (text content or base64 for binary).

### Put object

```
aws put_object --bucket my-bucket --key data/report.json --body '{"status":"ok"}' --content_type application/json
```

| Argument       | Type   | Required | Default                    | Description    |
| -------------- | ------ | -------- | -------------------------- | -------------- |
| `bucket`       | string | yes      |                            | Bucket name    |
| `key`          | string | yes      |                            | Object key     |
| `body`         | string | yes      |                            | Object content |
| `content_type` | string | no       | `application/octet-stream` | MIME type      |
| `region`       | string | no       | from config                | AWS region     |

Returns: `etag`, `version_id`.

### Delete object

```
aws delete_object --bucket my-bucket --key old-file.txt
```

| Argument | Type   | Required | Default     | Description |
| -------- | ------ | -------- | ----------- | ----------- |
| `bucket` | string | yes      |             | Bucket name |
| `key`    | string | yes      |             | Object key  |
| `region` | string | no       | from config | AWS region  |

Returns: `deleted` (boolean), `version_id`.

### Create bucket

```
aws create_bucket --bucket my-new-bucket --region us-west-2
```

| Argument | Type   | Required | Default     | Description |
| -------- | ------ | -------- | ----------- | ----------- |
| `bucket` | string | yes      |             | Bucket name |
| `region` | string | no       | from config | AWS region  |

Returns: `bucket`, `location`.

### Delete bucket

```
aws delete_bucket --bucket my-old-bucket --region us-east-1
```

| Argument | Type   | Required | Default     | Description |
| -------- | ------ | -------- | ----------- | ----------- |
| `bucket` | string | yes      |             | Bucket name |
| `region` | string | no       | from config | AWS region  |

Returns: `deleted` (boolean).

### Presign URL

```
aws presign_url --bucket my-bucket --key report.pdf --expires_in 3600
```

| Argument     | Type   | Required | Default     | Description                          |
| ------------ | ------ | -------- | ----------- | ------------------------------------ |
| `bucket`     | string | yes      |             | Bucket name                          |
| `key`        | string | yes      |             | Object key                           |
| `expires_in` | int    | no       | 3600        | URL validity in seconds (max 604800) |
| `method`     | string | no       | `GET`       | HTTP method (`GET` or `PUT`)         |
| `region`     | string | no       | from config | AWS region                           |

Returns: `url`, `expires_at`.

## Lambda

### List functions

```
aws list_functions --region us-east-1 --per_page 50
```

| Argument   | Type   | Required | Default     | Description             |
| ---------- | ------ | -------- | ----------- | ----------------------- |
| `region`   | string | no       | from config | AWS region              |
| `per_page` | int    | no       | 50          | Results per page (1-50) |

Returns: array of `function_name`, `runtime`, `handler`, `code_size`, `last_modified`, `memory_size`, `timeout`, `description`.

### Get function

```
aws get_function --function_name my-function --region us-east-1
```

| Argument        | Type   | Required | Default     | Description          |
| --------------- | ------ | -------- | ----------- | -------------------- |
| `function_name` | string | yes      |             | Function name or ARN |
| `region`        | string | no       | from config | AWS region           |

Returns: `function_name`, `function_arn`, `runtime`, `handler`, `code_size`, `description`, `timeout`, `memory_size`, `last_modified`, `environment`, `layers`, `vpc_config`, `role`.

### Invoke function

```
aws invoke_function --function_name my-function --payload '{"key":"value"}' --region us-east-1
```

| Argument          | Type   | Required | Default           | Description                          |
| ----------------- | ------ | -------- | ----------------- | ------------------------------------ |
| `function_name`   | string | yes      |                   | Function name or ARN                 |
| `payload`         | string | no       |                   | JSON input payload                   |
| `invocation_type` | string | no       | `RequestResponse` | `RequestResponse`, `Event`, `DryRun` |
| `region`          | string | no       | from config       | AWS region                           |

Returns: `status_code`, `payload` (response body), `function_error`, `executed_version`.

### Create function

```
aws create_function --function_name my-function --runtime nodejs20.x --handler index.handler --role arn:aws:iam::123456789:role/lambda-role --zip_file base64data --region us-east-1
```

| Argument        | Type   | Required | Default     | Description                               |
| --------------- | ------ | -------- | ----------- | ----------------------------------------- |
| `function_name` | string | yes      |             | Function name                             |
| `runtime`       | string | yes      |             | Runtime (e.g. `nodejs20.x`, `python3.12`) |
| `handler`       | string | yes      |             | Handler function (e.g. `index.handler`)   |
| `role`          | string | yes      |             | IAM role ARN for execution                |
| `zip_file`      | string | no       |             | Base64-encoded deployment package         |
| `s3_bucket`     | string | no       |             | S3 bucket containing code package         |
| `s3_key`        | string | no       |             | S3 key of code package                    |
| `memory_size`   | int    | no       | 128         | Memory in MB (128-10240)                  |
| `timeout`       | int    | no       | 3           | Timeout in seconds (1-900)                |
| `environment`   | string | no       |             | JSON object of environment variables      |
| `description`   | string | no       |             | Function description                      |
| `region`        | string | no       | from config | AWS region                                |

Returns: `function_name`, `function_arn`, `runtime`, `state`.

### Update function code

```
aws update_function_code --function_name my-function --zip_file base64data --region us-east-1
```

| Argument        | Type   | Required | Default     | Description                       |
| --------------- | ------ | -------- | ----------- | --------------------------------- |
| `function_name` | string | yes      |             | Function name or ARN              |
| `zip_file`      | string | no       |             | Base64-encoded deployment package |
| `s3_bucket`     | string | no       |             | S3 bucket containing code package |
| `s3_key`        | string | no       |             | S3 key of code package            |
| `region`        | string | no       | from config | AWS region                        |

Returns: `function_name`, `function_arn`, `code_sha256`, `last_modified`.

### Delete function

```
aws delete_function --function_name my-function --region us-east-1
```

| Argument        | Type   | Required | Default     | Description          |
| --------------- | ------ | -------- | ----------- | -------------------- |
| `function_name` | string | yes      |             | Function name or ARN |
| `region`        | string | no       | from config | AWS region           |

Returns: `deleted` (boolean).

### List function logs

```
aws list_function_logs --function_name my-function --start_time 1700000000000 --limit 50 --region us-east-1
```

| Argument        | Type   | Required | Default     | Description                  |
| --------------- | ------ | -------- | ----------- | ---------------------------- |
| `function_name` | string | yes      |             | Function name                |
| `start_time`    | long   | no       |             | Start time (epoch ms)        |
| `end_time`      | long   | no       |             | End time (epoch ms)          |
| `limit`         | int    | no       | 50          | Maximum log events to return |
| `region`        | string | no       | from config | AWS region                   |

Returns: array of `timestamp`, `message`, `ingestion_time`.

## IAM

### List users

```
aws list_users --per_page 100
```

| Argument   | Type | Required | Default | Description               |
| ---------- | ---- | -------- | ------- | ------------------------- |
| `per_page` | int  | no       | 100     | Results per page (1-1000) |

Returns: array of `user_name`, `user_id`, `arn`, `create_date`, `path`, `password_last_used`.

### List roles

```
aws list_roles --per_page 100
```

| Argument   | Type | Required | Default | Description               |
| ---------- | ---- | -------- | ------- | ------------------------- |
| `per_page` | int  | no       | 100     | Results per page (1-1000) |

Returns: array of `role_name`, `role_id`, `arn`, `create_date`, `description`, `max_session_duration`.

### List policies

```
aws list_policies --scope Local --per_page 100
```

| Argument   | Type   | Required | Default | Description               |
| ---------- | ------ | -------- | ------- | ------------------------- |
| `scope`    | string | no       | `All`   | `All`, `AWS`, or `Local`  |
| `per_page` | int    | no       | 100     | Results per page (1-1000) |

Returns: array of `policy_name`, `policy_id`, `arn`, `attachment_count`, `create_date`, `update_date`, `description`.

### Create user

```
aws create_user --user_name deploy-bot --tags '{"team":"platform"}'
```

| Argument    | Type   | Required | Default | Description                   |
| ----------- | ------ | -------- | ------- | ----------------------------- |
| `user_name` | string | yes      |         | IAM user name                 |
| `path`      | string | no       | `/`     | Path prefix                   |
| `tags`      | string | no       |         | JSON object of key-value tags |

Returns: `user_name`, `user_id`, `arn`, `create_date`.

### Attach policy

```
aws attach_policy --user_name deploy-bot --policy_arn arn:aws:iam::aws:policy/AmazonS3ReadOnlyAccess
```

| Argument     | Type   | Required | Description                   |
| ------------ | ------ | -------- | ----------------------------- |
| `user_name`  | string | no       | IAM user name (one required)  |
| `role_name`  | string | no       | IAM role name (one required)  |
| `group_name` | string | no       | IAM group name (one required) |
| `policy_arn` | string | yes      | Policy ARN to attach          |

Returns: `attached` (boolean), `entity`, `policy_arn`.

## CloudWatch

### Get metrics

```
aws get_metrics --namespace AWS/EC2 --metric_name CPUUtilization --dimensions '{"InstanceId":"i-0abc123"}' --period 300 --start_time 2024-01-01T00:00:00Z --end_time 2024-01-02T00:00:00Z --region us-east-1
```

| Argument      | Type   | Required | Default     | Description                                           |
| ------------- | ------ | -------- | ----------- | ----------------------------------------------------- |
| `namespace`   | string | yes      |             | CloudWatch namespace (e.g. `AWS/EC2`, `AWS/Lambda`)   |
| `metric_name` | string | yes      |             | Metric name (e.g. `CPUUtilization`)                   |
| `dimensions`  | string | no       |             | JSON object of dimension name-value pairs             |
| `period`      | int    | no       | 300         | Period in seconds (min 60)                            |
| `start_time`  | string | yes      |             | ISO 8601 start time                                   |
| `end_time`    | string | yes      |             | ISO 8601 end time                                     |
| `statistics`  | string | no       | `Average`   | `Average`, `Sum`, `Minimum`, `Maximum`, `SampleCount` |
| `region`      | string | no       | from config | AWS region                                            |

Returns: array of `timestamp`, `value`, `unit`.

### Put metric alarm

```
aws put_metric_alarm --alarm_name high-cpu --namespace AWS/EC2 --metric_name CPUUtilization --threshold 80 --comparison GreaterThanThreshold --evaluation_periods 2 --period 300 --statistic Average --actions '["arn:aws:sns:us-east-1:123456789:alerts"]' --region us-east-1
```

| Argument             | Type     | Required | Default     | Description                                                                                                |
| -------------------- | -------- | -------- | ----------- | ---------------------------------------------------------------------------------------------------------- |
| `alarm_name`         | string   | yes      |             | Alarm name                                                                                                 |
| `namespace`          | string   | yes      |             | CloudWatch namespace                                                                                       |
| `metric_name`        | string   | yes      |             | Metric name                                                                                                |
| `threshold`          | float    | yes      |             | Threshold value                                                                                            |
| `comparison`         | string   | yes      |             | `GreaterThanThreshold`, `LessThanThreshold`, `GreaterThanOrEqualToThreshold`, `LessThanOrEqualToThreshold` |
| `evaluation_periods` | int      | yes      |             | Number of periods to evaluate                                                                              |
| `period`             | int      | no       | 300         | Period in seconds                                                                                          |
| `statistic`          | string   | no       | `Average`   | `Average`, `Sum`, `Minimum`, `Maximum`, `SampleCount`                                                      |
| `actions`            | string[] | no       |             | SNS topic ARNs to notify                                                                                   |
| `dimensions`         | string   | no       |             | JSON object of dimension name-value pairs                                                                  |
| `description`        | string   | no       |             | Alarm description                                                                                          |
| `region`             | string   | no       | from config | AWS region                                                                                                 |

Returns: `alarm_name`, `alarm_arn`.

### List alarms

```
aws list_alarms --state ALARM --region us-east-1
```

| Argument | Type   | Required | Default     | Description                               |
| -------- | ------ | -------- | ----------- | ----------------------------------------- |
| `state`  | string | no       | `all`       | `OK`, `ALARM`, `INSUFFICIENT_DATA`, `all` |
| `prefix` | string | no       |             | Alarm name prefix filter                  |
| `region` | string | no       | from config | AWS region                                |

Returns: array of `alarm_name`, `alarm_arn`, `state`, `metric_name`, `namespace`, `threshold`, `comparison`, `evaluation_periods`, `updated_at`.

### Get log events

```
aws get_log_events --log_group /aws/lambda/my-function --log_stream 2024/01/01/[$LATEST]abc123 --limit 100 --region us-east-1
```

| Argument     | Type   | Required | Default     | Description               |
| ------------ | ------ | -------- | ----------- | ------------------------- |
| `log_group`  | string | yes      |             | CloudWatch log group name |
| `log_stream` | string | no       |             | Specific log stream name  |
| `start_time` | long   | no       |             | Start time (epoch ms)     |
| `end_time`   | long   | no       |             | End time (epoch ms)       |
| `limit`      | int    | no       | 100         | Maximum events to return  |
| `region`     | string | no       | from config | AWS region                |

Returns: array of `timestamp`, `message`, `ingestion_time`.

## ECS

### List clusters

```
aws list_clusters --region us-east-1
```

| Argument | Type   | Required | Default     | Description |
| -------- | ------ | -------- | ----------- | ----------- |
| `region` | string | no       | from config | AWS region  |

Returns: array of `cluster_arn`, `cluster_name`, `status`, `running_tasks_count`, `pending_tasks_count`, `active_services_count`.

### List services

```
aws list_services --cluster my-cluster --region us-east-1
```

| Argument  | Type   | Required | Default     | Description         |
| --------- | ------ | -------- | ----------- | ------------------- |
| `cluster` | string | yes      |             | Cluster name or ARN |
| `region`  | string | no       | from config | AWS region          |

Returns: array of `service_name`, `service_arn`, `status`, `desired_count`, `running_count`, `pending_count`, `task_definition`, `launch_type`.

### List tasks

```
aws list_tasks --cluster my-cluster --service my-service --status RUNNING --region us-east-1
```

| Argument  | Type   | Required | Default     | Description                     |
| --------- | ------ | -------- | ----------- | ------------------------------- |
| `cluster` | string | yes      |             | Cluster name or ARN             |
| `service` | string | no       |             | Filter by service name          |
| `status`  | string | no       | `RUNNING`   | `RUNNING`, `STOPPED`, `PENDING` |
| `region`  | string | no       | from config | AWS region                      |

Returns: array of `task_arn`, `task_definition_arn`, `last_status`, `desired_status`, `started_at`, `container_instance_arn`.

### Describe service

```
aws describe_service --cluster my-cluster --service my-service --region us-east-1
```

| Argument  | Type   | Required | Default     | Description         |
| --------- | ------ | -------- | ----------- | ------------------- |
| `cluster` | string | yes      |             | Cluster name or ARN |
| `service` | string | yes      |             | Service name or ARN |
| `region`  | string | no       | from config | AWS region          |

Returns: `service_name`, `service_arn`, `status`, `task_definition`, `desired_count`, `running_count`, `pending_count`, `launch_type`, `load_balancers`, `deployments`, `events`.

### Update service

```
aws update_service --cluster my-cluster --service my-service --desired_count 3 --task_definition my-task:5 --region us-east-1
```

| Argument           | Type    | Required | Default     | Description                    |
| ------------------ | ------- | -------- | ----------- | ------------------------------ |
| `cluster`          | string  | yes      |             | Cluster name or ARN            |
| `service`          | string  | yes      |             | Service name or ARN            |
| `desired_count`    | int     | no       |             | Desired number of tasks        |
| `task_definition`  | string  | no       |             | New task definition family:rev |
| `force_deployment` | boolean | no       | false       | Force new deployment           |
| `region`           | string  | no       | from config | AWS region                     |

Returns: `service_name`, `service_arn`, `status`, `desired_count`, `running_count`, `task_definition`.

## Route53

### List hosted zones

```
aws list_hosted_zones
```

| Argument   | Type | Required | Default | Description              |
| ---------- | ---- | -------- | ------- | ------------------------ |
| `per_page` | int  | no       | 100     | Results per page (1-100) |

Returns: array of `zone_id`, `name`, `record_count`, `private_zone`, `comment`.

### List records

```
aws list_records --zone_id Z1234567890 --type A
```

| Argument  | Type   | Required | Default | Description                                                           |
| --------- | ------ | -------- | ------- | --------------------------------------------------------------------- |
| `zone_id` | string | yes      |         | Hosted zone ID                                                        |
| `type`    | string | no       |         | Filter by type: `A`, `AAAA`, `CNAME`, `MX`, `TXT`, `NS`, `SOA`, `SRV` |

Returns: array of `name`, `type`, `ttl`, `values`, `alias_target`.

### Create record

```
aws create_record --zone_id Z1234567890 --name api.example.com --type A --value 1.2.3.4 --ttl 300
```

| Argument       | Type   | Required | Default | Description                                        |
| -------------- | ------ | -------- | ------- | -------------------------------------------------- |
| `zone_id`      | string | yes      |         | Hosted zone ID                                     |
| `name`         | string | yes      |         | Record name (FQDN)                                 |
| `type`         | string | yes      |         | Record type (`A`, `CNAME`, `TXT`, etc.)            |
| `value`        | string | no       |         | Record value (required unless alias)               |
| `ttl`          | int    | no       | 300     | TTL in seconds                                     |
| `alias_target` | string | no       |         | JSON with `dns_name`, `zone_id`, `evaluate_health` |

Returns: `change_id`, `status`, `submitted_at`.

### Delete record

```
aws delete_record --zone_id Z1234567890 --name api.example.com --type A --value 1.2.3.4
```

| Argument  | Type   | Required | Description          |
| --------- | ------ | -------- | -------------------- |
| `zone_id` | string | yes      | Hosted zone ID       |
| `name`    | string | yes      | Record name (FQDN)   |
| `type`    | string | yes      | Record type          |
| `value`   | string | yes      | Current record value |
| `ttl`     | int    | no       | Current TTL          |

Returns: `change_id`, `status`.

## SQS

### List queues

```
aws list_queues --prefix my-app --region us-east-1
```

| Argument | Type   | Required | Default     | Description       |
| -------- | ------ | -------- | ----------- | ----------------- |
| `prefix` | string | no       |             | Queue name prefix |
| `region` | string | no       | from config | AWS region        |

Returns: array of `queue_url`, `queue_name`, `approximate_message_count`, `created_at`.

### Send message

```
aws send_message --queue_url https://sqs.us-east-1.amazonaws.com/123456789/my-queue --body '{"task":"process"}' --delay_seconds 0
```

| Argument        | Type   | Required | Default     | Description                       |
| --------------- | ------ | -------- | ----------- | --------------------------------- |
| `queue_url`     | string | yes      |             | SQS queue URL                     |
| `body`          | string | yes      |             | Message body                      |
| `delay_seconds` | int    | no       | 0           | Delay before visible (0-900)      |
| `attributes`    | string | no       |             | JSON object of message attributes |
| `group_id`      | string | no       |             | Message group ID (FIFO queues)    |
| `region`        | string | no       | from config | AWS region                        |

Returns: `message_id`, `md5_of_body`, `sequence_number`.

### Receive messages

```
aws receive_messages --queue_url https://sqs.us-east-1.amazonaws.com/123456789/my-queue --max_messages 10 --wait_time 5
```

| Argument             | Type   | Required | Default     | Description                    |
| -------------------- | ------ | -------- | ----------- | ------------------------------ |
| `queue_url`          | string | yes      |             | SQS queue URL                  |
| `max_messages`       | int    | no       | 1           | Max messages to receive (1-10) |
| `wait_time`          | int    | no       | 0           | Long poll wait seconds (0-20)  |
| `visibility_timeout` | int    | no       |             | Override visibility timeout    |
| `region`             | string | no       | from config | AWS region                     |

Returns: array of `message_id`, `receipt_handle`, `body`, `attributes`, `md5_of_body`.

### Delete message

```
aws delete_message --queue_url https://sqs.us-east-1.amazonaws.com/123456789/my-queue --receipt_handle AQEBwJ...
```

| Argument         | Type   | Required | Default     | Description                 |
| ---------------- | ------ | -------- | ----------- | --------------------------- |
| `queue_url`      | string | yes      |             | SQS queue URL               |
| `receipt_handle` | string | yes      |             | Receipt handle from receive |
| `region`         | string | no       | from config | AWS region                  |

Returns: `deleted` (boolean).

### Create queue

```
aws create_queue --queue_name my-queue --fifo false --region us-east-1
```

| Argument             | Type    | Required | Default     | Description                    |
| -------------------- | ------- | -------- | ----------- | ------------------------------ |
| `queue_name`         | string  | yes      |             | Queue name                     |
| `fifo`               | boolean | no       | false       | Create as FIFO queue           |
| `visibility_timeout` | int     | no       | 30          | Default visibility timeout (s) |
| `message_retention`  | int     | no       | 345600      | Message retention period (s)   |
| `delay_seconds`      | int     | no       | 0           | Default delivery delay (s)     |
| `region`             | string  | no       | from config | AWS region                     |

Returns: `queue_url`, `queue_arn`.

## SNS

### List topics

```
aws list_topics --region us-east-1
```

| Argument | Type   | Required | Default     | Description |
| -------- | ------ | -------- | ----------- | ----------- |
| `region` | string | no       | from config | AWS region  |

Returns: array of `topic_arn`, `topic_name`.

### Publish

```
aws publish --topic_arn arn:aws:sns:us-east-1:123456789:alerts --message "Server is down" --subject "Alert"
```

| Argument    | Type   | Required | Default     | Description                       |
| ----------- | ------ | -------- | ----------- | --------------------------------- |
| `topic_arn` | string | yes      |             | SNS topic ARN                     |
| `message`   | string | yes      |             | Message body                      |
| `subject`   | string | no       |             | Message subject (email endpoints) |
| `region`    | string | no       | from config | AWS region                        |

Returns: `message_id`.

### Subscribe

```
aws subscribe --topic_arn arn:aws:sns:us-east-1:123456789:alerts --protocol email --endpoint user@example.com
```

| Argument    | Type   | Required | Default     | Description                              |
| ----------- | ------ | -------- | ----------- | ---------------------------------------- |
| `topic_arn` | string | yes      |             | SNS topic ARN                            |
| `protocol`  | string | yes      |             | `email`, `sms`, `sqs`, `lambda`, `https` |
| `endpoint`  | string | yes      |             | Endpoint (email, phone, ARN, or URL)     |
| `region`    | string | no       | from config | AWS region                               |

Returns: `subscription_arn` (pending confirmation for email/sms).

### Create topic

```
aws create_topic --name alerts --region us-east-1
```

| Argument | Type    | Required | Default     | Description          |
| -------- | ------- | -------- | ----------- | -------------------- |
| `name`   | string  | yes      |             | Topic name           |
| `fifo`   | boolean | no       | false       | Create as FIFO topic |
| `tags`   | string  | no       |             | JSON object of tags  |
| `region` | string  | no       | from config | AWS region           |

Returns: `topic_arn`.

## RDS

### List instances

```
aws list_db_instances --engine postgres --region us-east-1
```

| Argument | Type   | Required | Default     | Description                                                |
| -------- | ------ | -------- | ----------- | ---------------------------------------------------------- |
| `engine` | string | no       |             | Filter by engine: `postgres`, `mysql`, `aurora`, `mariadb` |
| `region` | string | no       | from config | AWS region                                                 |

Returns: array of `db_instance_id`, `engine`, `engine_version`, `instance_class`, `status`, `endpoint`, `port`, `storage_gb`, `multi_az`, `vpc_id`.

### Describe instance

```
aws describe_db_instance --db_instance_id my-database --region us-east-1
```

| Argument         | Type   | Required | Default     | Description    |
| ---------------- | ------ | -------- | ----------- | -------------- |
| `db_instance_id` | string | yes      |             | DB instance ID |
| `region`         | string | no       | from config | AWS region     |

Returns: `db_instance_id`, `engine`, `engine_version`, `instance_class`, `status`, `endpoint`, `port`, `storage_gb`, `multi_az`, `vpc_id`, `security_groups`, `parameter_group`, `backup_retention`, `latest_restorable_time`, `ca_certificate`.

### Create snapshot

```
aws create_db_snapshot --db_instance_id my-database --snapshot_id my-db-snapshot-2024 --region us-east-1
```

| Argument         | Type   | Required | Default     | Description                   |
| ---------------- | ------ | -------- | ----------- | ----------------------------- |
| `db_instance_id` | string | yes      |             | DB instance ID                |
| `snapshot_id`    | string | yes      |             | Identifier for the snapshot   |
| `tags`           | string | no       |             | JSON object of key-value tags |
| `region`         | string | no       | from config | AWS region                    |

Returns: `snapshot_id`, `db_instance_id`, `status`, `engine`, `allocated_storage`, `snapshot_create_time`.

## Workflow

1. **Always specify `--region`** or ensure the agent's AWS config has a default region set.
2. Start with list operations (`list_instances`, `list_buckets`, `list_functions`) to discover resources.
3. Use get/describe operations for detailed information before making changes.
4. For EC2: check instance state before start/stop operations. Use tags to identify instances.
5. For S3: use `list_objects` with `--prefix` to navigate bucket contents. Use `presign_url` for temporary access.
6. For Lambda: use `invoke_function` for testing. Check `list_function_logs` for debugging.
7. For ECS: use `describe_service` to understand current state before `update_service`.
8. For Route53: always verify `zone_id` with `list_hosted_zones` before modifying records.
9. For IAM: follow least-privilege principle when attaching policies.

## Safety notes

- **Destructive operations** (`terminate_instance`, `delete_bucket`, `delete_function`, `delete_record`) cannot be undone. Confirm with the user before executing.
- S3 `delete_bucket` requires the bucket to be empty first. Delete all objects before deleting the bucket.
- EC2 `terminate_instance` permanently destroys the instance and its root volume (unless `DeleteOnTermination` is false).
- IAM changes are global and not region-scoped. Policy changes take effect immediately.
- Lambda `invoke_function` executes real code. Use `--invocation_type DryRun` to validate permissions without execution.
- SQS messages are consumed on receive. Use `visibility_timeout` to prevent premature deletion.
- Route53 record changes propagate globally and may take up to 60 seconds.
- All operations are subject to AWS IAM permissions. Insufficient permissions will return an access denied error.
- Results are paginated. Use `per_page` / `max_keys` to control page size.
- CloudWatch metrics have a retention period. Data older than 15 months may not be available.
