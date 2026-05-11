# SOC 2 Type 1 vs. Type 2: Guidance for a First-Time B2B SaaS Startup

## Overview

SOC 2 is an audit framework governed by the AICPA (American Institute of Certified Public Accountants). It evaluates whether a service organization's controls meet the **Trust Services Criteria (TSC)**. There are five TSC categories; **Security (Common Criteria, CC1–CC9) is always required**. The others — Availability (A1), Confidentiality (C1), Processing Integrity (PI1), and Privacy (P1–P8) — are optional and added based on customer or contractual requirements.

---

## Type 1 vs. Type 2: The Core Difference

| Dimension | Type 1 | Type 2 |
|---|---|---|
| **What it tests** | Design of controls at a **single point in time** | Design AND **operating effectiveness** over a period (typically 6–12 months) |
| **Auditor question** | "Are the right controls in place today?" | "Did those controls actually work consistently over time?" |
| **Evidence required** | Policy documents, system configurations, descriptions | All Type 1 evidence PLUS transaction samples, logs, tickets, access reviews across the audit period |
| **Value to customers** | Lower — shows intent, not proof of execution | Higher — the gold standard enterprise customers want |
| **Time to complete** | 2–4 months from readiness to report | 9–18 months total (including the observation period) |
| **Cost** | Lower (typically $15K–$40K for a small org) | Higher (typically $25K–$75K+ for a small org) |

**Key distinction:** A Type 1 report says your controls are *designed* correctly. A Type 2 report says your controls actually *worked* for 6–12 months. Enterprise customers overwhelmingly want Type 2 because it demonstrates sustained operational practice, not just a snapshot.

---

## Which Should You Pursue?

**Short answer: Target Type 2, but consider Type 1 as an accelerator if timing is tight.**

### When to start with Type 1

- Your enterprise customer needs *something* within 3–4 months
- You have never had formal controls in place and need a milestone to work toward
- You want a "practice run" before committing to a full Type 2 observation period
- You can negotiate with the customer to accept Type 1 now with a commitment to Type 2 within 12 months

### When to go directly to Type 2

- You have 12–18 months before the deal closes or renewal is at risk
- You already have reasonably mature controls operating (even informally)
- Multiple customers are asking, and you want a single report that satisfies everyone
- You want the report to have long-term market value

### Practical recommendation for your situation

Since an enterprise customer is asking *now* and you have no prior SOC 2 work:

1. **Immediately begin building controls** — do not wait. The Type 2 observation period clock starts when controls are operating, not when you engage an auditor.
2. **Engage a readiness consultant or auditing firm early** — they will help scope the report and identify gaps against CC1–CC9.
3. **Consider a Type 1 as a bridge** — obtain a Type 1 report in ~4 months to give the customer something, then convert to Type 2 after your 6-month observation period.
4. **Negotiate with the customer** — many enterprise procurement teams will accept a Type 1 with a roadmap commitment to Type 2.

---

## Typical Timeline

### Path A: Type 1 Only (fastest)

```
Month 1–2:   Gap assessment against CC1–CC9
             Policy writing (Information Security, Access Control, IR, Change Mgmt, etc.)
             Control implementation (MFA, access reviews, logging, etc.)

Month 2–3:   Readiness assessment (internal or with auditor)
             Evidence collection for point-in-time snapshot
             Remediation of critical gaps

Month 3–4:   Auditor fieldwork
             Report issuance

→ Total: ~3–5 months from start to Type 1 report
```

### Path B: Type 1 → Type 2 (recommended for your situation)

```
Month 1–2:   Gap assessment, policy writing, control implementation
Month 3–4:   Type 1 audit and report issued → share with customer
Month 4–10:  Observation period — controls must operate consistently
             (access reviews, change management, incident response, vendor reviews)
Month 10–12: Type 2 fieldwork with auditor
Month 12–14: Type 2 report issued

→ Total: ~12–14 months from start to Type 2 report
```

### Path C: Direct to Type 2 (if you can negotiate time)

```
Month 1–3:   Gap assessment, policy writing, control implementation
Month 3–9:   6-month observation period (minimum typical)
Month 9–11:  Type 2 fieldwork
Month 11–13: Type 2 report issued

→ Total: ~12–13 months from start to Type 2 report
```

---

## Minimum Controls to Build First (CC Focus)

Since Security (CC1–CC9) is always required, prioritize these control areas immediately:

| TSC Criterion | What you need |
|---|---|
| **CC1** — Control Environment | Written information security policy, management accountability |
| **CC3** — Risk Assessment | Annual risk assessment with documented register |
| **CC6** — Logical Access | MFA enforcement, least privilege, quarterly access reviews, provisioning/deprovisioning procedures |
| **CC7** — System Operations | Monitoring/alerting (SIEM or equivalent), incident response plan, vulnerability scanning |
| **CC8** — Change Management | Formal change request process, code review, separation of duties in deployments |
| **CC9** — Vendor Risk | Vendor inventory, annual review of critical vendors' SOC 2 reports |

---

## Key Decisions to Make Now

1. **Scope:** Will you include only Security (CC), or add Availability (A1) or Confidentiality (C1)? Most first-time reports are Security-only. Discuss with your customer what they need.
2. **Auditor selection:** You need a licensed CPA firm to issue a formal SOC 2 report. Firms like Schellman, Coalfire, Prescient Assurance, or a regional CPA firm with SOC 2 practice are options.
3. **Readiness support:** Consider a readiness partner (compliance platform like Vanta, Drata, Secureframe, or a consultant) to accelerate gap closure.
4. **Observation period start date:** The moment you implement controls is when your observation period can begin — document this clearly.

---

## Important Note

Only a **licensed CPA firm** can issue a formal SOC 2 report. Compliance platforms and consultants can help you prepare, but the audit itself must be performed by a qualified auditor. Plan for the auditor relationship early, as capacity at reputable firms books up.
