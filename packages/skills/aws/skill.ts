import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
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
  ...manifest,
  doc,

  actions: { ...ec2, ...s3, ...lambda, ...iam, ...cloudwatch, ...ecs, ...route53, ...sqs, ...sns, ...rds },
});
