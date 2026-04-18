import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { customers } from "./cli/customers.ts";
import { payments } from "./cli/payments.ts";
import { subscriptions } from "./cli/subscriptions.ts";
import { products } from "./cli/products.ts";
import { invoices } from "./cli/invoices.ts";
import { refunds } from "./cli/refunds.ts";
import { balance } from "./cli/balance.ts";
import { webhooks } from "./cli/webhooks.ts";

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    ...customers,
    ...payments,
    ...subscriptions,
    ...products,
    ...invoices,
    ...refunds,
    ...balance,
    ...webhooks,
  },
});
