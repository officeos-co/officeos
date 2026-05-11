# CCPA: Handling a Combined Right to Know + Right to Delete Request

A California resident has submitted two concurrent requests: (1) a Right to Know (access) request and (2) a Right to Delete request. Below is a step-by-step walkthrough of how to handle both, covering intake, identity verification, response obligations, exceptions, and documentation.

---

## 1. Confirm the Request is a "Verifiable Consumer Request"

Under the California Consumer Privacy Act (CCPA) as amended by the California Privacy Rights Act (CPRA), both rights may only be honored once the consumer's identity is verified to a reasonable degree. The request must:

- Be submitted through a designated method (web form, toll-free phone number, or other disclosed channel in your Privacy Policy).
- Contain enough information to enable verification.
- Be accompanied by a declaration, if required, attesting that the information provided is true under penalty of perjury (required for specific-pieces requests and deletion requests by regulation).

---

## 2. Initial Acknowledgment (Within 10 Business Days)

Upon receipt, you must acknowledge the request within **10 business days** and inform the consumer:

- That you received the request.
- How you will verify their identity.
- The expected timeline for responding (up to 45 calendar days, extendable once by another 45 days with notice).

---

## 3. Identity Verification

### Why it matters
Verification protects the consumer (and you) against fraudulent requests. The standard is "reasonable" verification, calibrated to the sensitivity of the information involved and the type of request.

### Matching information to records
Collect 2–3 data points the consumer provides and match them against information already in your systems. Common data points:

- Email address used to create an account.
- First and last name.
- Phone number or postal address on record.
- Date of account creation or last transaction date.

### Tiered verification standards

| Request Type | Verification Standard |
|---|---|
| Right to Know – Categories only | Match at least 2 data points with moderate confidence |
| Right to Know – Specific pieces of PI | Match at least 3 data points with high confidence; require a signed declaration under penalty of perjury |
| Right to Delete | Match at least 2–3 data points depending on sensitivity; signed declaration recommended |

### Non-account holders (unregistered consumers)
If the consumer does not have an account, you may use a three-step process:
1. Provide a timestamp verification link sent to the email address provided.
2. Request 2 additional data points to match against transaction or interaction records.
3. If you cannot verify, you may deny the request but must inform the consumer they can exercise rights through an authorized agent or alternative channel.

### Authorized agents
If an agent submits on the consumer's behalf:
- Require proof of signed written permission from the consumer.
- Verify the consumer's identity directly as well (unless the consumer has provided a notarized power of attorney).

---

## 4. Responding to the Right to Know (Access) Request

### Scope of disclosure
Upon verified request, disclose:

1. **Categories** of personal information (PI) collected about the consumer.
2. **Categories of sources** from which the PI was collected.
3. **Business or commercial purpose** for collecting, selling, or sharing the PI.
4. **Categories of third parties** to whom PI was disclosed.
5. **Specific pieces of PI** collected (if requested; this triggers the higher verification standard above).

### Format and delivery
- Deliver in a readily usable format (e.g., structured JSON, CSV, or a secure online portal download).
- Do not require the consumer to create a new account to receive the response.
- Provide the information free of charge (first two requests within a 12-month period must be free).

### Timeline
- Respond within **45 calendar days** of receiving the verifiable consumer request.
- Extend by an additional 45 days (one extension only) if reasonably necessary; notify the consumer before the initial deadline.

---

## 5. Responding to the Right to Delete Request

### General obligation
Upon a verified deletion request, you must delete the consumer's PI from your records and direct your **service providers, contractors, and third parties** to do the same (subject to their own exceptions).

### Deletion process steps
1. Confirm the verified identity before proceeding.
2. Identify all systems, databases, backups, and data stores holding the consumer's PI.
3. Delete or deidentify the PI in each system.
4. Issue deletion instructions to all service providers and relevant third parties that received the PI from you.
5. Confirm to the consumer in writing that deletion has occurred (or identify any exceptions invoked).

---

## 6. Exceptions You May Invoke

Both the access and deletion requests are subject to statutory and regulatory exceptions. Review each piece of PI carefully before blanket deletion.

### Exceptions to the Right to Delete (Cal. Civ. Code § 1798.105(d))

The business may decline to delete PI (in whole or in part) if retaining it is necessary to:

1. **Complete a transaction** – Fulfill a contract or provide a good/service the consumer requested, or reasonably anticipated in the context of an ongoing business relationship.
2. **Security and fraud detection** – Detect security incidents, protect against malicious, deceptive, fraudulent, or illegal activity, or prosecute those responsible.
3. **Debugging** – Identify and repair errors that impair existing intended functionality.
4. **Free speech / legal rights** – Exercise free speech, ensure another consumer's right to exercise their own free speech rights, or exercise another right provided by law.
5. **Research purposes** – Engage in public or peer-reviewed scientific, historical, or statistical research in the public interest that adheres to all other applicable ethics and privacy laws, where deletion would seriously impair the research.
6. **Legal obligation** – Comply with a legal obligation (e.g., tax records, AML/BSA records, healthcare records, employment records required by law).
7. **Internal uses reasonably aligned with consumer expectations** – Use the information internally in a lawful manner that is compatible with the context in which the consumer provided it.

### Exceptions to the Right to Know

- **Trade secrets**: You may withhold specific pieces of PI if disclosure would reveal a trade secret; inform the consumer that some information was withheld and the general reason.
- **Third-party PI**: If a specific-pieces response would reveal PI of another individual, redact that information.
- **Publicly available information**: You are not required to comply with requests for information that is lawfully made available from federal, state, or local government records.

### Partial fulfillment
If an exception applies to only part of the PI, delete (or disclose) the remainder and document exactly what was withheld and why. Inform the consumer that their request was partially fulfilled and identify the category of exception (without revealing information that itself triggers the exception).

---

## 7. Sale/Sharing Opt-Out — Supplemental Obligation

Since the consumer has submitted a deletion request, also check whether any of their PI is being **sold or shared** for cross-context behavioral advertising. If so:

- Honor any implicit opt-out of sale/sharing embedded in the deletion request.
- Instruct third-party data brokers or advertising partners to cease use of the consumer's PI.

---

## 8. Non-Discrimination Requirement

You may not deny goods or services, charge a different price, or provide a different quality of service because the consumer exercised their CCPA rights (Cal. Civ. Code § 1798.125). Document that no adverse action was taken.

---

## 9. Record-Keeping and Documentation

Regulations require businesses that receive 100,000+ consumer requests per year to maintain records for **24 months**. Even if below that threshold, retain internally:

- Date request was received.
- Type of request (Know + Delete).
- Date of acknowledgment.
- Verification method used and outcome.
- Date of response.
- Any exceptions invoked and the specific PI excluded.
- Confirmation of service provider deletion instructions sent.

---

## 10. Summary Timeline

| Milestone | Deadline |
|---|---|
| Acknowledge request | Within 10 business days of receipt |
| Verify identity | As soon as practicable (before responding) |
| Respond to both requests | Within 45 calendar days of the verifiable request |
| Extension notice (if needed) | Before the initial 45-day deadline |
| Maximum response window | 90 calendar days from verifiable request |

---

## Quick-Reference Checklist

- [ ] Acknowledge request within 10 business days.
- [ ] Collect identity verification data points (2–3; signed declaration for specific pieces / deletion).
- [ ] Match data points against internal records; document the outcome.
- [ ] For Right to Know: compile categories + specific pieces; review trade-secret and third-party exceptions; prepare structured disclosure.
- [ ] For Right to Delete: identify all data stores; apply exception analysis for each category of PI; execute deletion; send deletion instructions to service providers and third parties.
- [ ] Notify the consumer of any partial fulfillments and exceptions invoked.
- [ ] Verify no discriminatory action taken.
- [ ] Retain documentation for at least 24 months.
- [ ] Update internal metrics/logs for regulatory reporting.
