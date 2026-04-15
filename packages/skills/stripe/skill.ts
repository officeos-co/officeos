import { defineSkill } from "@harro/skill-sdk";
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
  name: "stripe",
  title: "Stripe",
  emoji: "💳",
  description:
    "Manage payments, customers, subscriptions, products, invoices, refunds, and payouts via the Stripe API.",
  doc,

  credentials: {
    secret_key: {
      label: "Secret Key",
      kind: "password",
      placeholder: "sk_live_… or sk_test_…",
      help: "Stripe secret API key. Use a test key (sk_test_) for development. Find yours at https://dashboard.stripe.com/apikeys.",
    },
  },

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
