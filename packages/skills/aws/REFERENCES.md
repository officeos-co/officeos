# References

## Source SDK/CLI
- **Repository**: [aws/aws-cli](https://github.com/aws/aws-cli)
- **JS SDK**: [aws/aws-sdk-js-v3](https://github.com/aws/aws-sdk-js-v3) (`@aws-sdk/client-*`)
- **License**: Apache-2.0
- **npm packages**: `@aws-sdk/client-ec2`, `@aws-sdk/client-s3`, `@aws-sdk/client-lambda`, `@aws-sdk/client-iam`, `@aws-sdk/client-cloudwatch`, `@aws-sdk/client-cloudwatch-logs`, `@aws-sdk/client-ecs`, `@aws-sdk/client-route-53`, `@aws-sdk/client-sqs`, `@aws-sdk/client-sns`, `@aws-sdk/client-rds`, `@aws-sdk/s3-request-presigner`
- **Documentation**: [https://docs.aws.amazon.com](https://docs.aws.amazon.com)

## API Coverage
- **EC2**: list/get/start/stop/terminate/create instances
- **S3**: list buckets, list/get/put/delete objects, create/delete buckets, presign URLs
- **Lambda**: list/get/invoke/create/update/delete functions, list function logs
- **IAM**: list users/roles/policies, create user, attach policy
- **CloudWatch**: get metrics, put metric alarm, list alarms, get log events
- **ECS**: list clusters/services/tasks, describe service, update service
- **Route53**: list hosted zones, list/create/delete records
- **SQS**: list/create queues, send/receive/delete messages
- **SNS**: list topics, publish, subscribe, create topic
- **RDS**: list/describe DB instances, create DB snapshot
