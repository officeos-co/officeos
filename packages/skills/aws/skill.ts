import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { ec2 } from "./cli/ec2.ts";
import { s3 } from "./cli/s3.ts";
import { lambda } from "./cli/lambda.ts";
import { iam } from "./cli/iam.ts";
import { cloudwatch } from "./cli/cloudwatch.ts";
import { ecs } from "./cli/ecs.ts";
import { route53 } from "./cli/route53.ts";
import { sqs } from "./cli/sqs.ts";
import { sns } from "./cli/sns.ts";
import { rds } from "./cli/rds.ts";

export default defineSkill({
  name: "aws",
  title: "AWS",
  emoji: "☁️",
  description:
    "Manage AWS cloud infrastructure: EC2, S3, Lambda, IAM, CloudWatch, ECS, Route53, SQS, SNS, and RDS via the AWS REST API.",
  doc,

  credentials: {
    access_key_id: {
      label: "Access Key ID",
      kind: "text",
      placeholder: "AKIA...",
      help: "AWS Access Key ID from IAM. Create one at https://console.aws.amazon.com/iam/.",
    },
    secret_access_key: {
      label: "Secret Access Key",
      kind: "password",
      placeholder: "wJalr...",
      help: "AWS Secret Access Key paired with the Access Key ID.",
    },
    region: {
      label: "Default Region",
      kind: "text",
      placeholder: "us-east-1",
      help: "Default AWS region. Can be overridden per action with --region.",
    },
  },

  actions: { ...ec2, ...s3, ...lambda, ...iam, ...cloudwatch, ...ecs, ...route53, ...sqs, ...sns, ...rds },
});
