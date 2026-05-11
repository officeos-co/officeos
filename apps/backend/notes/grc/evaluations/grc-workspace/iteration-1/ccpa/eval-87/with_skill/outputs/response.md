# Handling a Combined CCPA Right to Know + Right to Delete Request

A California resident has submitted two simultaneous requests: (1) a Right to Know (access) request and (2) a Right to Delete request. Here is a complete, step-by-step guide for handling both, including identity verification and all applicable exceptions.

---

## Step 1: Intake and Logging

**Immediately upon receipt:**
- Log both requests with a precise timestamp. The 45-calendar-day response clock starts from the date of receipt, not the date verification is completed.
- Assign a tracking ID to each request.
- Confirm receipt to the consumer in writing (email or postal mail), acknowledging both requests.
- Verify you have at least two intake channels available to the consumer per §1798.130 (e.g., toll-free phone and a web form or email).

---

## Step 2: Identity Verification

You must verify the consumer's identity before disclosing or deleting any personal information. The standard scales with the sensitivity of the data involved.

### Standard Right to Know (non-sensitive PI)
Match **at least 2 data points** the business already holds on file, for example:
- Full name + email address on record
- Full name + mailing address
- Account number + date of birth

### If the request involves Sensitive Personal Information (SPI)
(SSNs, financial credentials, precise geolocation, biometric data, health data, racial/ethnic origin, sexual orientation, etc.)

Match **at least 3 data points** plus require a **signed declaration under penalty of perjury** from the consumer confirming they are the individual whose PI is being requested.

### If the request is submitted via an Authorized Agent
- Require written, signed authorization from the consumer permitting the agent to act on their behalf.
- Verify the agent's identity independently.
- For delete requests submitted by an agent without power of attorney, you may also verify directly with the consumer.

### Failure to Verify
If the consumer cannot be verified after reasonable attempts, deny the request and notify the consumer explaining why and that they may re-submit with sufficient verification.

---

## Step 3: Handling the Right to Know Request (§1798.110 / §1798.115)

### What Must Be Disclosed
Once identity is verified, compile and deliver the following:

1. **Specific pieces of PI** collected about this consumer (names, email addresses, IP addresses, purchase history, browsing history, device identifiers, geolocation, etc.).
2. **Categories of PI** collected.
3. **Categories of sources** from which the PI was collected (e.g., directly from consumer, third-party data brokers, cookies/tracking pixels).
4. **Business or commercial purposes** for which PI was collected, sold, or shared.
5. **Categories of third parties** to whom PI was disclosed.
6. **Categories of PI sold or shared** and the categories of third parties who received it.

### Scope of the Disclosure
- Under CPRA (effective January 1, 2023), there is no longer a strict 12-month look-back limit for PI collected after January 1, 2022. You must disclose all PI collected and retained.
- For PI collected before January 1, 2022, the 12-month look-back applies.

### Where to Search
Search all systems that may hold PI: CRM, marketing automation, analytics platforms, ad tech stacks, support ticketing systems, billing systems, email platforms, data warehouses, and any third-party service providers processing PI on your behalf.

### Exceptions — When You Can Withhold Specific PI
You may decline to disclose specific pieces of PI if:

| Exception | Description |
|---|---|
| Third-party trade secrets | Disclosing would reveal trade secrets belonging to a third party |
| Legal conflict | Disclosure would conflict with federal or state law (e.g., ongoing law enforcement investigation) |
| One-time transaction | PI was collected for a single transaction and was not retained |
| Internal operations only | PI is solely used for internal purposes consistent with the context of collection |
| Transaction completion | PI is solely used to complete the transaction for which it was collected |

Document the specific exception invoked in writing and inform the consumer that some PI was withheld and why (at a category level, without revealing the withheld content).

### Format and Delivery
Deliver the response in a **portable, readily usable format** (e.g., a structured PDF or CSV). Do not require the consumer to create an account to receive the response if they did not have one when submitting the request.

**Deadline:** 45 calendar days from receipt. If you need more time, notify the consumer within the original 45-day window and take up to an additional 45 days (90 days total).

---

## Step 4: Handling the Right to Delete Request (§1798.105)

### Scope of Deletion
The deletion obligation covers:
- All PI held in your own systems and records.
- PI held by your **service providers** and **contractors** — you must direct them in writing to delete the PI as well.

### Exceptions — When You Can Retain PI

You may decline to delete (or retain specific categories of PI) if retention is necessary to:

| # | Exception | Example |
|---|---|---|
| 1 | **Complete a transaction or perform a contract** | Active subscription or pending order |
| 2 | **Detect security incidents; protect against fraud/illegal activity** | Fraud detection logs, active investigation |
| 3 | **Fix errors impairing intended functionality** | Bug fix requiring the data |
| 4 | **Free speech / another consumer's right to free speech** | Rare; applies to journalism-like contexts |
| 5 | **Legal obligation** (§1798.145(a)) | Tax records (IRS 7-year retention), financial records, litigation holds |
| 6 | **Internal purposes compatible with collection context** (CPRA, narrow) | Aggregate analytics that cannot be re-linked to the individual |
| 7 | **Research, journalism, or statistical public interest** | Academic research datasets |

**Practical note:** Legal holds are the most commonly invoked exception. If litigation is pending or reasonably anticipated, retain a litigation hold memo and preserve the relevant PI. Explain to the consumer at a high level why data cannot be deleted.

### Deletion Workflow

1. **Verify identity** (same as above — do not skip even for deletion).
2. **Identify all PI records** across all systems.
3. **Evaluate exceptions** for each category of PI — apply exceptions narrowly. Document the reasoning in writing.
4. **Execute deletion** for all PI not covered by an exception:
   - Delete from primary databases.
   - Delete from backups (or flag for deletion at the next backup cycle, with a note that the data is to be treated as deleted in the interim).
   - Delete from internal analytics or data warehouse systems.
5. **Notify service providers and contractors** in writing to delete the consumer's PI from their systems. Confirm they have done so.
6. **Respond to the consumer** within 45 days confirming:
   - What was deleted, and
   - What (if anything) was retained and under which exception.

**Deadline:** 45 calendar days (extendable by 45 more days with notice).

---

## Step 5: Non-Discrimination

Remind relevant internal teams: you **cannot** penalize this consumer for exercising their CCPA rights. Do not:
- Deny them goods or services.
- Charge a higher price.
- Provide a lower quality of service.
- Downgrade their account tier.
- Flag them internally in a way that results in worse treatment.

---

## Step 6: Record-Keeping

Retain the following for **at least 24 months** (required for businesses processing PI of 10 million or more consumers/households; recommended for all covered businesses):
- A copy of the request (both the Right to Know and Right to Delete requests).
- The verification steps taken and the outcome.
- The response provided to the consumer.
- Any exceptions invoked and the documented reasoning.
- Written instructions sent to service providers and contractors.
- Confirmation of deletion received from service providers.

Note: Retaining the deletion request itself — and records of how you processed it — does not contradict the deletion obligation. The requirement is to delete the consumer's substantive PI, not the compliance records.

---

## Summary Timeline

| Action | Deadline |
|---|---|
| Acknowledge receipt | Immediately (no statutory deadline, but best practice within 10 days) |
| Complete identity verification | As soon as possible; clock runs from request receipt, not verification |
| Respond to both requests (or notify of extension) | 45 calendar days from receipt |
| Maximum response deadline with extension | 90 calendar days from receipt |
| Notify consumer if extension needed | Within the original 45-day window |
| Service providers/contractors must delete | Instruct within your response period; confirm deletion promptly |

---

## Key Risks to Watch

- **Incomplete system search:** Missing PI in ad tech, analytics, or legacy systems is a common compliance gap. Conduct a thorough data inventory before responding.
- **Failure to notify service providers:** The deletion obligation explicitly extends to service providers and contractors. Document all instructions sent.
- **Over-invoking exceptions:** Exceptions must be applied narrowly. Blanket invocation of "legal obligation" or "fraud detection" without specific, documented justification will not withstand CPPA scrutiny.
- **Treating both requests as one:** Right to Know and Right to Delete are separate rights with separate response obligations. Document and respond to each independently.
- **Penalty exposure:** Unintentional violations are up to $2,500 per violation; intentional violations up to $7,500 per violation (Cal. Civ. Code §1798.155).
