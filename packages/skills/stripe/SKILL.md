# Stripe

Manage payments, customers, subscriptions, products, invoices, refunds, and payouts via the Stripe API.

All commands go through `skill_exec` using CLI-style syntax.
Use `--help` at any level to discover actions and arguments.

## Customers

### List customers

```
stripe list_customers --limit 20 --email "user@example.com"
```

| Argument  | Type   | Required | Default | Description                                                    |
| --------- | ------ | -------- | ------- | -------------------------------------------------------------- |
| `limit`   | int    | no       | 10      | Results to return (1-100)                                      |
| `email`   | string | no       |         | Filter by exact email                                          |
| `created` | object | no       |         | JSON date filter (`gt`, `gte`, `lt`, `lte` as Unix timestamps) |

Returns: list of `id`, `email`, `name`, `phone`, `created`, `metadata`, `default_source`, `currency`.

### Get customer

```
stripe get_customer --customer_id "cus_ABC123"
```

| Argument      | Type   | Required | Description |
| ------------- | ------ | -------- | ----------- |
| `customer_id` | string | yes      | Customer ID |

Returns: `id`, `email`, `name`, `phone`, `description`, `created`, `metadata`, `subscriptions`, `default_source`, `balance`, `currency`, `delinquent`.

### Create customer

```
stripe create_customer --email "jane@example.com" --name "Jane Doe" --metadata '{"plan":"enterprise","source":"website"}'
```

| Argument      | Type   | Required | Description                   |
| ------------- | ------ | -------- | ----------------------------- |
| `email`       | string | no       | Customer email                |
| `name`        | string | no       | Customer full name            |
| `phone`       | string | no       | Customer phone number         |
| `description` | string | no       | Internal description          |
| `metadata`    | object | no       | Key-value pairs (max 50 keys) |

Returns: `id`, `email`, `name`, `created`.

### Update customer

```
stripe update_customer --customer_id "cus_ABC123" --name "Jane Smith" --metadata '{"plan":"premium"}'
```

| Argument      | Type   | Required | Description             |
| ------------- | ------ | -------- | ----------------------- |
| `customer_id` | string | yes      | Customer ID             |
| `email`       | string | no       | Updated email           |
| `name`        | string | no       | Updated name            |
| `metadata`    | object | no       | Updated key-value pairs |

Returns: `id`, `email`, `name`.

### Delete customer

```
stripe delete_customer --customer_id "cus_ABC123"
```

| Argument      | Type   | Required | Description |
| ------------- | ------ | -------- | ----------- |
| `customer_id` | string | yes      | Customer ID |

Returns: `id`, `deleted` boolean.

### Search customers

```
stripe search_customers --query "email:'jane@example.com' AND metadata['plan']:'enterprise'"
```

| Argument | Type   | Required | Description                |
| -------- | ------ | -------- | -------------------------- |
| `query`  | string | yes      | Stripe search query syntax |
| `limit`  | int    | no       | Results to return (1-100)  |

Returns: list of matching customer objects.

## Payments

### List payment intents

```
stripe list_payment_intents --customer "cus_ABC123" --limit 10
```

| Argument   | Type   | Required | Default | Description                        |
| ---------- | ------ | -------- | ------- | ---------------------------------- |
| `customer` | string | no       |         | Filter by customer ID              |
| `limit`    | int    | no       | 10      | Results to return (1-100)          |
| `created`  | object | no       |         | JSON date filter (Unix timestamps) |

Returns: list of `id`, `amount`, `currency`, `status`, `customer`, `payment_method`, `created`, `description`.

### Get payment intent

```
stripe get_payment_intent --payment_intent_id "pi_ABC123"
```

| Argument            | Type   | Required | Description       |
| ------------------- | ------ | -------- | ----------------- |
| `payment_intent_id` | string | yes      | Payment intent ID |

Returns: `id`, `amount`, `currency`, `status`, `customer`, `payment_method`, `payment_method_types`, `description`, `metadata`, `charges`, `created`, `canceled_at`, `cancellation_reason`.

### Create payment intent

```
stripe create_payment_intent --amount 5000 --currency usd --customer "cus_ABC123" --payment_method_types '["card"]' --description "Order #1234"
```

| Argument               | Type     | Required | Default    | Description                                      |
| ---------------------- | -------- | -------- | ---------- | ------------------------------------------------ |
| `amount`               | int      | yes      |            | Amount in smallest currency unit (cents for USD) |
| `currency`             | string   | yes      |            | Three-letter ISO currency code                   |
| `customer`             | string   | no       |            | Customer ID to attach to                         |
| `payment_method_types` | string[] | no       | `["card"]` | Accepted payment methods                         |
| `description`          | string   | no       |            | Internal description                             |
| `metadata`             | object   | no       |            | Key-value pairs                                  |

Returns: `id`, `client_secret`, `amount`, `currency`, `status`.

### Confirm payment

```
stripe confirm_payment --payment_intent_id "pi_ABC123" --payment_method "pm_XYZ789"
```

| Argument            | Type   | Required | Description       |
| ------------------- | ------ | -------- | ----------------- |
| `payment_intent_id` | string | yes      | Payment intent ID |
| `payment_method`    | string | no       | Payment method ID |

Returns: `id`, `status`, `amount`, `currency`.

### Cancel payment

```
stripe cancel_payment --payment_intent_id "pi_ABC123" --cancellation_reason "requested_by_customer"
```

| Argument              | Type   | Required | Description                                                     |
| --------------------- | ------ | -------- | --------------------------------------------------------------- |
| `payment_intent_id`   | string | yes      | Payment intent ID                                               |
| `cancellation_reason` | string | no       | `duplicate`, `fraudulent`, `requested_by_customer`, `abandoned` |

Returns: `id`, `status`, `cancellation_reason`.

## Charges

### List charges

```
stripe list_charges --customer "cus_ABC123" --limit 20
```

| Argument   | Type   | Required | Default | Description               |
| ---------- | ------ | -------- | ------- | ------------------------- |
| `customer` | string | no       |         | Filter by customer ID     |
| `limit`    | int    | no       | 10      | Results to return (1-100) |
| `created`  | object | no       |         | JSON date filter          |

Returns: list of `id`, `amount`, `currency`, `status`, `customer`, `description`, `payment_method`, `created`, `refunded`.

### Get charge

```
stripe get_charge --charge_id "ch_ABC123"
```

| Argument    | Type   | Required | Description |
| ----------- | ------ | -------- | ----------- |
| `charge_id` | string | yes      | Charge ID   |

Returns: `id`, `amount`, `currency`, `status`, `customer`, `description`, `payment_method_details`, `receipt_url`, `refunded`, `refunds`, `metadata`.

### Create charge

```
stripe create_charge --amount 2500 --currency usd --customer "cus_ABC123" --description "One-time charge"
```

| Argument      | Type   | Required | Description                                    |
| ------------- | ------ | -------- | ---------------------------------------------- |
| `amount`      | int    | yes      | Amount in smallest currency unit               |
| `currency`    | string | yes      | Three-letter ISO currency code                 |
| `customer`    | string | no       | Customer ID (required if no source)            |
| `source`      | string | no       | Payment source token (required if no customer) |
| `description` | string | no       | Charge description                             |
| `metadata`    | object | no       | Key-value pairs                                |

Returns: `id`, `amount`, `currency`, `status`, `receipt_url`.

## Subscriptions

### List subscriptions

```
stripe list_subscriptions --customer "cus_ABC123" --status active --limit 10
```

| Argument   | Type   | Required | Default | Description                                                   |
| ---------- | ------ | -------- | ------- | ------------------------------------------------------------- |
| `customer` | string | no       |         | Filter by customer ID                                         |
| `status`   | string | no       |         | `active`, `past_due`, `canceled`, `unpaid`, `trialing`, `all` |
| `limit`    | int    | no       | 10      | Results to return (1-100)                                     |

Returns: list of `id`, `customer`, `status`, `current_period_start`, `current_period_end`, `items`, `cancel_at_period_end`, `created`.

### Get subscription

```
stripe get_subscription --subscription_id "sub_ABC123"
```

| Argument          | Type   | Required | Description     |
| ----------------- | ------ | -------- | --------------- |
| `subscription_id` | string | yes      | Subscription ID |

Returns: `id`, `customer`, `status`, `items`, `current_period_start`, `current_period_end`, `cancel_at_period_end`, `trial_start`, `trial_end`, `default_payment_method`, `latest_invoice`, `metadata`.

### Create subscription

```
stripe create_subscription --customer "cus_ABC123" --price_ids '["price_ABC123"]' --trial_period_days 14
```

| Argument                 | Type     | Required | Description                              |
| ------------------------ | -------- | -------- | ---------------------------------------- |
| `customer`               | string   | yes      | Customer ID                              |
| `price_ids`              | string[] | yes      | List of price IDs for subscription items |
| `trial_period_days`      | int      | no       | Number of trial days                     |
| `default_payment_method` | string   | no       | Payment method ID                        |
| `metadata`               | object   | no       | Key-value pairs                          |

Returns: `id`, `customer`, `status`, `current_period_start`, `current_period_end`, `items`.

### Update subscription

```
stripe update_subscription --subscription_id "sub_ABC123" --price_ids '["price_NEW456"]' --proration_behavior create_prorations
```

| Argument             | Type     | Required | Description                                   |
| -------------------- | -------- | -------- | --------------------------------------------- |
| `subscription_id`    | string   | yes      | Subscription ID                               |
| `price_ids`          | string[] | no       | Updated price IDs (replaces existing items)   |
| `proration_behavior` | string   | no       | `create_prorations`, `none`, `always_invoice` |
| `metadata`           | object   | no       | Updated key-value pairs                       |

Returns: `id`, `status`, `items`.

### Cancel subscription

```
stripe cancel_subscription --subscription_id "sub_ABC123" --at_period_end true
```

| Argument          | Type    | Required | Default | Description                     |
| ----------------- | ------- | -------- | ------- | ------------------------------- |
| `subscription_id` | string  | yes      |         | Subscription ID                 |
| `at_period_end`   | boolean | no       | false   | Cancel at end of current period |

Returns: `id`, `status`, `cancel_at_period_end`, `canceled_at`.

### Pause subscription

```
stripe pause_subscription --subscription_id "sub_ABC123" --behavior mark_uncollectible
```

| Argument          | Type   | Required | Default              | Description                                   |
| ----------------- | ------ | -------- | -------------------- | --------------------------------------------- |
| `subscription_id` | string | yes      |                      | Subscription ID                               |
| `behavior`        | string | no       | `mark_uncollectible` | `mark_uncollectible`, `keep_as_draft`, `void` |

Returns: `id`, `status`, `pause_collection`.

## Products

### List products

```
stripe list_products --active true --limit 20
```

| Argument | Type    | Required | Default | Description               |
| -------- | ------- | -------- | ------- | ------------------------- |
| `active` | boolean | no       |         | Filter by active status   |
| `limit`  | int     | no       | 10      | Results to return (1-100) |

Returns: list of `id`, `name`, `description`, `active`, `default_price`, `images`, `metadata`, `created`.

### Get product

```
stripe get_product --product_id "prod_ABC123"
```

| Argument     | Type   | Required | Description |
| ------------ | ------ | -------- | ----------- |
| `product_id` | string | yes      | Product ID  |

Returns: `id`, `name`, `description`, `active`, `default_price`, `images`, `metadata`, `created`, `updated`.

### Create product

```
stripe create_product --name "Pro Plan" --description "Full-featured plan" --metadata '{"tier":"pro"}'
```

| Argument      | Type     | Required | Description                |
| ------------- | -------- | -------- | -------------------------- |
| `name`        | string   | yes      | Product name               |
| `description` | string   | no       | Product description        |
| `images`      | string[] | no       | List of image URLs (max 8) |
| `metadata`    | object   | no       | Key-value pairs            |

Returns: `id`, `name`, `active`.

### Update product

```
stripe update_product --product_id "prod_ABC123" --name "Pro Plan v2" --description "Updated features"
```

| Argument      | Type   | Required | Description             |
| ------------- | ------ | -------- | ----------------------- |
| `product_id`  | string | yes      | Product ID              |
| `name`        | string | no       | Updated name            |
| `description` | string | no       | Updated description     |
| `metadata`    | object | no       | Updated key-value pairs |

Returns: `id`, `name`, `active`.

### Archive product

```
stripe archive_product --product_id "prod_ABC123"
```

| Argument     | Type   | Required | Description |
| ------------ | ------ | -------- | ----------- |
| `product_id` | string | yes      | Product ID  |

Returns: `id`, `active` (will be `false`).

## Prices

### List prices

```
stripe list_prices --product "prod_ABC123" --active true --limit 10
```

| Argument  | Type    | Required | Default | Description               |
| --------- | ------- | -------- | ------- | ------------------------- |
| `product` | string  | no       |         | Filter by product ID      |
| `active`  | boolean | no       |         | Filter by active status   |
| `limit`   | int     | no       | 10      | Results to return (1-100) |

Returns: list of `id`, `product`, `unit_amount`, `currency`, `recurring`, `active`, `type`, `created`.

### Get price

```
stripe get_price --price_id "price_ABC123"
```

| Argument   | Type   | Required | Description |
| ---------- | ------ | -------- | ----------- |
| `price_id` | string | yes      | Price ID    |

Returns: `id`, `product`, `unit_amount`, `currency`, `recurring`, `active`, `type`, `billing_scheme`, `metadata`.

### Create price

```
stripe create_price --product "prod_ABC123" --unit_amount 2999 --currency usd --recurring_interval month
```

| Argument                   | Type   | Required | Description                                        |
| -------------------------- | ------ | -------- | -------------------------------------------------- |
| `product`                  | string | yes      | Product ID                                         |
| `unit_amount`              | int    | yes      | Price in smallest currency unit                    |
| `currency`                 | string | yes      | Three-letter ISO currency code                     |
| `recurring_interval`       | string | no       | `day`, `week`, `month`, `year` (omit for one-time) |
| `recurring_interval_count` | int    | no       | Number of intervals between billings (default 1)   |
| `metadata`                 | object | no       | Key-value pairs                                    |

Returns: `id`, `product`, `unit_amount`, `currency`, `recurring`.

## Invoices

### List invoices

```
stripe list_invoices --customer "cus_ABC123" --status paid --limit 20
```

| Argument   | Type   | Required | Default | Description                                      |
| ---------- | ------ | -------- | ------- | ------------------------------------------------ |
| `customer` | string | no       |         | Filter by customer ID                            |
| `status`   | string | no       |         | `draft`, `open`, `paid`, `void`, `uncollectible` |
| `limit`    | int    | no       | 10      | Results to return (1-100)                        |

Returns: list of `id`, `customer`, `status`, `amount_due`, `amount_paid`, `currency`, `due_date`, `created`, `hosted_invoice_url`.

### Get invoice

```
stripe get_invoice --invoice_id "in_ABC123"
```

| Argument     | Type   | Required | Description |
| ------------ | ------ | -------- | ----------- |
| `invoice_id` | string | yes      | Invoice ID  |

Returns: `id`, `customer`, `status`, `amount_due`, `amount_paid`, `amount_remaining`, `currency`, `lines`, `due_date`, `hosted_invoice_url`, `invoice_pdf`, `metadata`.

### Create invoice

```
stripe create_invoice --customer "cus_ABC123" --collection_method charge_automatically --description "April 2026 services"
```

| Argument            | Type   | Required | Default                | Description                               |
| ------------------- | ------ | -------- | ---------------------- | ----------------------------------------- |
| `customer`          | string | yes      |                        | Customer ID                               |
| `collection_method` | string | no       | `charge_automatically` | `charge_automatically` or `send_invoice`  |
| `description`       | string | no       |                        | Invoice description                       |
| `due_date`          | int    | no       |                        | Unix timestamp (required if send_invoice) |
| `metadata`          | object | no       |                        | Key-value pairs                           |

Returns: `id`, `customer`, `status`.

### Send invoice

```
stripe send_invoice --invoice_id "in_ABC123"
```

| Argument     | Type   | Required | Description |
| ------------ | ------ | -------- | ----------- |
| `invoice_id` | string | yes      | Invoice ID  |

Returns: `id`, `status`, `hosted_invoice_url`.

### Void invoice

```
stripe void_invoice --invoice_id "in_ABC123"
```

| Argument     | Type   | Required | Description |
| ------------ | ------ | -------- | ----------- |
| `invoice_id` | string | yes      | Invoice ID  |

Returns: `id`, `status` (will be `void`).

### Finalize invoice

```
stripe finalize_invoice --invoice_id "in_ABC123"
```

| Argument     | Type   | Required | Description |
| ------------ | ------ | -------- | ----------- |
| `invoice_id` | string | yes      | Invoice ID  |

Returns: `id`, `status`, `hosted_invoice_url`, `invoice_pdf`.

## Refunds

### List refunds

```
stripe list_refunds --charge "ch_ABC123" --limit 10
```

| Argument         | Type   | Required | Default | Description               |
| ---------------- | ------ | -------- | ------- | ------------------------- |
| `charge`         | string | no       |         | Filter by charge ID       |
| `payment_intent` | string | no       |         | Filter by payment intent  |
| `limit`          | int    | no       | 10      | Results to return (1-100) |

Returns: list of `id`, `amount`, `currency`, `status`, `charge`, `payment_intent`, `reason`, `created`.

### Create refund

```
stripe create_refund --payment_intent "pi_ABC123" --amount 1500 --reason requested_by_customer
```

| Argument         | Type   | Required | Description                                           |
| ---------------- | ------ | -------- | ----------------------------------------------------- |
| `charge`         | string | no       | Charge ID (required if no payment_intent)             |
| `payment_intent` | string | no       | Payment intent ID (required if no charge)             |
| `amount`         | int    | no       | Partial refund amount in cents (omit for full refund) |
| `reason`         | string | no       | `duplicate`, `fraudulent`, `requested_by_customer`    |
| `metadata`       | object | no       | Key-value pairs                                       |

Returns: `id`, `amount`, `currency`, `status`, `charge`.

## Payouts

### List payouts

```
stripe list_payouts --status paid --limit 10
```

| Argument | Type   | Required | Default | Description                                           |
| -------- | ------ | -------- | ------- | ----------------------------------------------------- |
| `status` | string | no       |         | `paid`, `pending`, `in_transit`, `canceled`, `failed` |
| `limit`  | int    | no       | 10      | Results to return (1-100)                             |

Returns: list of `id`, `amount`, `currency`, `status`, `arrival_date`, `method`, `created`.

### Get payout

```
stripe get_payout --payout_id "po_ABC123"
```

| Argument    | Type   | Required | Description |
| ----------- | ------ | -------- | ----------- |
| `payout_id` | string | yes      | Payout ID   |

Returns: `id`, `amount`, `currency`, `status`, `arrival_date`, `method`, `description`, `metadata`.

### Create payout

```
stripe create_payout --amount 50000 --currency usd --description "Weekly payout"
```

| Argument      | Type   | Required | Description                      |
| ------------- | ------ | -------- | -------------------------------- |
| `amount`      | int    | yes      | Amount in smallest currency unit |
| `currency`    | string | yes      | Three-letter ISO currency code   |
| `description` | string | no       | Payout description               |
| `method`      | string | no       | `standard` or `instant`          |
| `metadata`    | object | no       | Key-value pairs                  |

Returns: `id`, `amount`, `currency`, `status`, `arrival_date`.

## Balance

### Get balance

```
stripe get_balance
```

No arguments required.

Returns: `available` (list of amounts by currency), `pending` (list of amounts by currency), `connect_reserved`.

### List balance transactions

```
stripe list_balance_transactions --limit 20 --type charge
```

| Argument  | Type   | Required | Default | Description                                            |
| --------- | ------ | -------- | ------- | ------------------------------------------------------ |
| `limit`   | int    | no       | 10      | Results to return (1-100)                              |
| `type`    | string | no       |         | `charge`, `refund`, `adjustment`, `payout`, `transfer` |
| `created` | object | no       |         | JSON date filter (Unix timestamps)                     |

Returns: list of `id`, `amount`, `currency`, `type`, `description`, `fee`, `net`, `source`, `created`, `available_on`.

## Webhooks

### List webhook endpoints

```
stripe list_webhook_endpoints --limit 10
```

| Argument | Type | Required | Default | Description               |
| -------- | ---- | -------- | ------- | ------------------------- |
| `limit`  | int  | no       | 10      | Results to return (1-100) |

Returns: list of `id`, `url`, `status`, `enabled_events`, `created`.

### Create webhook endpoint

```
stripe create_webhook_endpoint --url "https://api.officeos.co/webhooks/stripe" --events '["payment_intent.succeeded","customer.subscription.created","invoice.paid"]'
```

| Argument      | Type     | Required | Description                         |
| ------------- | -------- | -------- | ----------------------------------- |
| `url`         | string   | yes      | Endpoint URL to receive events      |
| `events`      | string[] | yes      | List of event types to subscribe to |
| `description` | string   | no       | Endpoint description                |

Returns: `id`, `url`, `status`, `secret`, `enabled_events`.

## Workflow

1. **Start with `list_customers`** or `search_customers` to find or verify customer records.
2. Create products and prices before setting up subscriptions or invoices.
3. Use `create_payment_intent` for one-time payments and `create_subscription` for recurring billing.
4. Monitor payments with `list_payment_intents` and `list_charges`.
5. Issue refunds with `create_refund` referencing the charge or payment intent.
6. Track revenue with `get_balance` and `list_balance_transactions`.
7. Set up `create_webhook_endpoint` to receive real-time event notifications.
8. Use `list_invoices` to track billing history per customer.

## Safety notes

- All monetary amounts are in the **smallest currency unit** (e.g. cents for USD: 5000 = $50.00). Double-check values before creating charges or payment intents.
- `delete_customer` permanently removes the customer and cancels all subscriptions. This cannot be undone.
- `create_payment_intent` with `confirm: true` will immediately attempt to charge. Use the two-step flow (create then confirm) for safer operation.
- Refunds can take 5-10 business days to appear on the customer's statement.
- Webhook secrets returned by `create_webhook_endpoint` are shown only once. Store them securely.
- API rate limit is 100 requests per second in live mode, 25 in test mode.
- Only resources accessible to the configured API key (test or live) are visible.
- Test mode keys (starting with `sk_test_`) never process real payments. Verify which mode you are operating in.
