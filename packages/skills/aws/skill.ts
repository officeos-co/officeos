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
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M6 19a5 5 0 0 1-1-9.9A6 6 0 0 1 17 8a4.5 4.5 0 0 1 3 8.5 1 1 0 0 1-.3.2A5 5 0 0 1 18 19H6Z\"/></svg>",
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
