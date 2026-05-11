# SOC 2 Audit Exception: Former Employee Access Not Revoked for 45 Days

## How Serious Is This?

This is a **significant finding** — not a minor paperwork issue. A 45-day window where a terminated employee retained system access is exactly the kind of control failure SOC 2 auditors focus on. Here's why it matters:

- **45 days is far too long.** Best practice for involuntary terminations is same-day or same-hour revocation. For voluntary resignations, the last day of employment is the target. 45 days means your controls either didn't trigger or weren't monitored.
- **It's a real security risk.** During that period, the former employee could theoretically have accessed company systems, customer data, or source code. Even if no misuse occurred, auditors treat the exposure as significant.
- **It undermines the core purpose of access control.** The whole point of access management in SOC 2 is to ensure only authorized people have access. A terminated employee is, by definition, no longer authorized.

Expect this to appear in the audit report as an exception to your access control controls. Enterprise customers reading your report will see it.

---

## What Control Failures Does It Represent?

This exception typically represents failures at multiple points:

### 1. Offboarding Process Failure
Your offboarding procedure either doesn't exist in a formal way, wasn't followed, or had a gap in the handoff between HR and IT. Most commonly, the IT team simply didn't get notified of the termination in a timely manner.

### 2. Access Control (Provisioning / Deprovisioning)
SOC 2 requires that access is removed promptly when someone's employment ends. A 45-day delay is a direct failure of this control, regardless of whether access was used.

### 3. Lack of Detective Controls
If no one caught the active account for 45 days, it suggests your periodic access reviews either weren't happening or didn't cover this account. Quarterly access reviews should catch stale accounts — this slipping through indicates a gap there too.

### 4. Process Gaps Between HR and IT
This type of failure almost always points to a handoff problem: HR knew the person was terminated, but IT didn't get notified. This is a process design gap.

---

## How Serious Is It for Your Report?

**It will appear in the report.** SOC 2 Type 2 reports document exceptions when controls don't operate effectively. This means customers who receive your report will see this finding.

The damage depends on:
- Was this the only exception of this type, or did multiple former employees retain access?
- What level of access did they have (admin vs. read-only)?
- Is there evidence the access was used during the 45-day window?
- How strong is your management response?

A single isolated exception with a credible management response is manageable. A pattern of similar exceptions is a much bigger problem.

---

## How to Write the Management Response

Your management response is your formal statement included in the SOC 2 report. This is what customers will read alongside the finding. It needs to be specific, honest, and forward-looking.

### Structure of a Strong Management Response

1. **Acknowledge the exception** — don't minimize it or be defensive
2. **State the root cause** — explain specifically what went wrong
3. **Describe what you found when you investigated** — e.g., confirm whether the account was accessed
4. **List specific remediation steps with dates** — be concrete, not vague
5. **Note preventive measures going forward** — show you've fixed the process

### Sample Management Response

---

**Exception:** One former employee's system access was not revoked until 45 days after the date of termination.

**Management's Response:**

Management acknowledges this exception. Upon investigation, we determined that the root cause was a manual notification gap between the HR/People team and IT/Security. The employee's departure was not communicated to the IT team through our standard offboarding ticket process, resulting in the delayed access revocation.

We reviewed system access logs for the 45-day period and found no evidence of unauthorized access or use of the former employee's credentials during this time. The affected account did not have administrative or privileged access.

**Remediation actions:**

1. **Automated deprovisioning (completed [DATE]):** We have implemented an automated integration between our HR system and Okta so that all terminations automatically trigger account suspension the same day, eliminating reliance on manual notification.

2. **Updated offboarding procedure (completed [DATE]):** We revised our offboarding checklist to require IT/Security confirmation of account closure before the HR team closes the offboarding record.

3. **Enhanced access reviews:** We have increased the frequency of our access reviews to monthly for production systems and implemented a quarterly automated report flagging any accounts belonging to individuals no longer in our HR system.

Management is confident these corrective actions address the root cause of this exception and will prevent recurrence.

*Signed: [CISO / Head of Security], [Date]*

---

## Tips for the Management Response

**Do:**
- Be specific about what changed and when it changed
- Reference concrete systems and dates where possible
- Acknowledge if the account was accessed (you'll need to know this before writing)
- Frame remediation as already done, not just planned

**Don't:**
- Use vague language like "we have strengthened our controls"
- Be defensive or try to explain away why 45 days wasn't that bad
- Commit to controls you can't actually sustain
- Ignore the monitoring/detection gap — auditors will notice if you don't address how this went undetected for 45 days

---

## Immediate Steps to Take

1. **Confirm the account is now disabled** — if not already done, do it immediately
2. **Pull access logs** for the 45-day period to check for any activity on that account
3. **Identify the root cause** so you can write an accurate management response
4. **Implement automation** — if you're using Okta, there are direct integrations with most HRIS systems (Workday, BambooHR, etc.) that automate offboarding
5. **Run an access review now** — check whether any other former employees have active accounts

This exception, while serious, is recoverable with a strong management response and demonstrated remediation. The worst outcome is if this becomes a repeat finding on your next audit.
