# ISO 27001:2022 Risk Assessment — Complete Methodology Guide

> **Standard Reference:** ISO 27001:2022 Clauses 6.1.2 (Risk Assessment) and 6.1.3 (Risk Treatment), supported by ISO 27005 (risk management guidance)

This guide walks you through conducting your first formal ISO 27001 risk assessment from start to finish.

---

## 1. What Is an ISO 27001 Risk Assessment?

A risk assessment is the systematic process of identifying, analysing, and evaluating information security risks — and it is the backbone of your entire ISMS. Without a completed risk assessment, you cannot produce a valid Statement of Applicability (SoA) or Risk Treatment Plan (RTP), both of which are mandatory for certification.

ISO 27001 does **not** prescribe a specific methodology — you have freedom to choose your approach — but you must **document** your chosen methodology and apply it consistently.

The standard requires you to (Clause 6.1.2):
- Define and apply a risk assessment process
- Identify risks associated with the loss of CIA (Confidentiality, Integrity, Availability) of information
- Analyse and evaluate risks based on defined criteria
- Retain documented information (results) as evidence

---

## 2. Before You Start — Define Your Risk Criteria

You must document your risk criteria before starting the assessment. This includes:

### 2.1 Risk Acceptance Criteria
Define what level of risk your organisation is willing to accept (the "risk appetite"). Example:

> "Risks scoring 15 or above on a 25-point scale require treatment. Risks scoring 10–14 require management review. Risks scoring below 10 may be accepted."

### 2.2 Scoring Scale
Choose your scales. We recommend **5×5 (Likelihood × Impact)** as it is widely accepted and straightforward:

| Score | Likelihood | Meaning |
|-------|-----------|---------|
| 1 | Rare | Less than once every 5 years |
| 2 | Unlikely | Once every 2–5 years |
| 3 | Possible | Once per year |
| 4 | Likely | Multiple times per year |
| 5 | Almost certain | Monthly or more |

| Score | Impact | Meaning |
|-------|--------|---------|
| 1 | Negligible | Minimal impact; no customer data affected |
| 2 | Minor | Small disruption; limited data exposure; minor financial impact |
| 3 | Moderate | Significant disruption; regulatory notification possible; moderate financial impact |
| 4 | Major | Significant data breach; regulatory fines; reputational damage |
| 5 | Catastrophic | Business-threatening; mass data breach; severe regulatory/legal consequences |

**Risk Score = Likelihood × Impact** (range: 1–25)

Document these criteria in your **Risk Assessment Methodology document** before beginning.

---

## 3. Step-by-Step Methodology

### Step 1: Define the Scope

Confirm your ISMS scope (from Clause 4.3). Your risk assessment must cover all assets, processes, and locations within scope. Do not assess risks outside your defined scope.

---

### Step 2: Build Your Asset Inventory (Prerequisite)

You cannot assess risks without knowing what you are protecting. Build an asset register first (Annex A control A.5.9).

Asset categories to cover:
- **Information assets** — databases, files, contracts, customer data, financial records
- **Software assets** — applications, operating systems, development tools, SaaS tools
- **Physical assets** — servers, laptops, network equipment
- **People** — employees, contractors (as they carry knowledge and access)
- **Services** — cloud providers, internet connectivity, third-party services

For each asset, assign an **owner** — the person accountable for that asset's security.

---

### Step 3: Identify Threats

For each asset (or group of assets), identify realistic threats. A threat is any event or action that could cause harm.

Common threat categories for most organisations:

| Threat Category | Examples |
|-----------------|---------|
| Malicious external actors | Ransomware, phishing, credential stuffing, SQL injection |
| Malicious insiders | Data theft, sabotage, unauthorised access |
| Accidental insiders | Misrouted email, accidental deletion, misconfiguration |
| Third-party/supplier failures | Data breach at vendor, service outage |
| System/technology failures | Hardware failure, software bugs, infrastructure outage |
| Environmental | Power outage, natural disaster, fire |
| Legal/regulatory | Non-compliance leading to fines, contractual breach |

---

### Step 4: Identify Vulnerabilities

For each threat, identify the vulnerabilities that would allow the threat to materialise.

A **vulnerability** is a weakness that could be exploited by a threat. Examples:

| Threat | Example Vulnerability |
|--------|----------------------|
| Ransomware | Unpatched systems, lack of endpoint protection, poor backup hygiene |
| Phishing | Lack of MFA, poor security awareness |
| Insider data theft | Excessive access rights, no DLP controls, no monitoring |
| Vendor breach | No supplier security assessments, excessive data shared with vendor |
| Infrastructure outage | Single points of failure, no DR plan |

---

### Step 5: Score Each Risk

For each identified risk (asset + threat + vulnerability combination), score:

1. **Likelihood (1–5)** — How probable is it that this threat exploits this vulnerability?
2. **Impact (1–5)** — If this risk occurred, how severe would the consequences be?
3. **Risk Score = Likelihood × Impact**

Assess likelihood *before* considering existing controls (inherent risk), then assess again *after* existing controls (residual risk). This shows the value of your current controls.

---

### Step 6: Evaluate and Prioritise Risks

Compare each risk score against your acceptance criteria:

| Score Range | Rating | Action |
|-------------|--------|--------|
| 20–25 | Critical | Must treat; escalate to management |
| 15–19 | High | Treatment required; track closely |
| 10–14 | Medium | Treatment recommended; review with management |
| 5–9 | Low | Accept or monitor; document acceptance |
| 1–4 | Very Low | Accept; note in register |

---

## 4. The Risk Register — Structure and Columns

Your risk register is the core output. Maintain it as a living document. Recommended columns:

| Column | Description |
|--------|-------------|
| **Risk ID** | Unique identifier (e.g., RISK-001) |
| **Asset** | The information asset affected |
| **Threat** | The threat scenario |
| **Vulnerability** | The weakness being exploited |
| **Existing Controls** | Controls already in place |
| **Likelihood (Inherent)** | 1–5, before existing controls |
| **Impact (Inherent)** | 1–5 |
| **Inherent Risk Score** | L × I |
| **Likelihood (Residual)** | 1–5, after existing controls |
| **Impact (Residual)** | 1–5 |
| **Residual Risk Score** | L × I |
| **Risk Rating** | Critical / High / Medium / Low |
| **Treatment Option** | Mitigate / Accept / Transfer / Avoid |
| **Treatment Actions** | Specific control(s) to implement |
| **Annex A Control(s)** | Relevant ISO 27001:2022 Annex A control(s) |
| **Risk Owner** | Person accountable for this risk |
| **Due Date** | Deadline for treatment action |
| **Status** | Open / In Progress / Closed |
| **Notes** | Any additional context |

### Example Risk Register Entry

| Field | Example Value |
|-------|---------------|
| Risk ID | RISK-007 |
| Asset | Customer PII database (AWS RDS) |
| Threat | Unauthorised external access |
| Vulnerability | No MFA on database admin accounts; overly permissive IAM roles |
| Existing Controls | TLS encryption in transit; VPC network segmentation |
| Likelihood (Inherent) | 4 |
| Impact (Inherent) | 5 |
| Inherent Risk Score | 20 (Critical) |
| Likelihood (Residual) | 2 |
| Impact (Residual) | 5 |
| Residual Risk Score | 10 (Medium) |
| Treatment Option | Mitigate |
| Treatment Actions | Implement MFA on all DB admin accounts; review and restrict IAM roles to least privilege |
| Annex A Control(s) | A.5.15 (Access control), A.5.16 (Identity management), A.8.2 (Privileged access rights) |
| Risk Owner | Head of Engineering |
| Due Date | 2026-06-30 |
| Status | In Progress |

---

## 5. Risk Treatment Options

For each risk requiring treatment, select one of four options:

| Option | When to Use | Example |
|--------|-------------|---------|
| **Mitigate** | Implement controls to reduce likelihood or impact | Add MFA; patch systems; encrypt data |
| **Accept** | Risk is within appetite; cost of treatment exceeds benefit | Low-scoring risks with expensive mitigations |
| **Transfer** | Shift financial risk to a third party | Cyber insurance; contractual liability transfer to vendor |
| **Avoid** | Eliminate the activity that creates the risk | Stop storing certain data; exit a risky market |

Document the rationale for every treatment decision — auditors will ask.

---

## 6. Linking Risk Treatment to the Statement of Applicability (SoA)

This is one of the most commonly misunderstood requirements. Your SoA and risk treatment plan must be **linked**:

1. For each risk you choose to mitigate, you select one or more Annex A controls
2. Those controls are then included in your SoA
3. The SoA shows *which* controls you have selected, *why* (justification — typically "to treat RISK-XXX"), and *implementation status*

Controls in your SoA can also be included for reasons other than risk treatment (legal requirements, contractual obligations), but every control selected for risk treatment must appear in the SoA.

**SoA columns:**

| Control ID | Control Name | Applicable? | Justification for Inclusion/Exclusion | Implementation Status |
|------------|-------------|-------------|--------------------------------------|----------------------|
| A.5.15 | Access control | Yes | Risk treatment: RISK-007, RISK-012 | Implemented |
| A.5.7 | Threat intelligence | No | Excluded: insufficient scale to justify dedicated programme; compensating monitoring in place | N/A |

---

## 7. What to Document (Mandatory Evidence)

ISO 27001:2022 requires you to retain documented information (evidence) for:

| Document | Clause Reference |
|----------|-----------------|
| Risk assessment methodology document | 6.1.2 |
| Completed risk register (with scores) | 6.1.2, 8.2 |
| Risk treatment plan | 6.1.3, 8.3 |
| Statement of Applicability | 6.1.3d |
| Evidence of risk owner sign-off | 6.1.3e |

---

## 8. How to Determine Which Risks Need Treatment

Apply this decision process:

```
Risk Score ≥ Risk Acceptance Threshold?
    YES → Treatment REQUIRED. Select mitigate/avoid/transfer.
    NO  → Can you accept this risk?
              YES → Document formal acceptance with risk owner sign-off
              NO  → Treat anyway (management decision)
```

Key principle: **All acceptance decisions must be documented and signed off by a risk owner.** Simply ignoring a risk is not acceptance — it is a nonconformity.

---

## 9. Common Mistakes to Avoid

| Mistake | Why It Matters | Fix |
|---------|----------------|-----|
| Assessing risk at asset level only, not threat/vulnerability level | Misses specific exploits | Always use Asset + Threat + Vulnerability triplet |
| Using only "after controls" scoring | Hides inherent risk; looks artificially clean | Score both inherent and residual risk |
| No risk owner assigned | Auditor will raise NC; nobody accountable | Every risk must have a named owner |
| SoA not linked to risk register | Major NC; cannot demonstrate control selection rationale | Cross-reference risk IDs in SoA |
| Risk register never updated | Certification body expects updates at minimum annually | Review register at least annually or after significant change |
| All risks scored as "Medium" | Appears manipulated; not credible | Use full range; some risks should be Critical |
| Accepting high risks without documentation | Auditor will raise NC | Formal acceptance must be signed off |

---

## 10. Risk Assessment Process Summary

```
1. Document your risk methodology → 2. Build asset register
       ↓
3. Identify threats → 4. Identify vulnerabilities
       ↓
5. Score inherent risk (L × I) → 6. Note existing controls
       ↓
7. Score residual risk → 8. Evaluate against acceptance threshold
       ↓
9. Select treatment option → 10. Build Risk Treatment Plan
       ↓
11. Link to SoA → 12. Assign owners + due dates
       ↓
13. Implement controls → 14. Review annually (or on change)
```

---

## 11. Review and Monitoring

ISO 27001:2022 requires that risk assessments are:
- Performed at planned intervals (at minimum annually)
- Performed when significant changes occur (new product, new data types, breach, major organisational change)
- Results retained as documented information

Schedule your next risk assessment review date and record it in your ISMS calendar before closing the current assessment.

---

*Version: 1.0 | Standard: ISO/IEC 27001:2022 | Clauses: 6.1.2, 6.1.3, 8.2, 8.3 | Date: 2026-04-18*
