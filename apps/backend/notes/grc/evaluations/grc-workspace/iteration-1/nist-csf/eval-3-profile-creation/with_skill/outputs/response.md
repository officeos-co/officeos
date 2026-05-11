# Creating NIST CSF 2.0 Organizational Profiles
## Current Profile and Target Profile — Step-by-Step Guide

---

## What Is a CSF Profile?

A **CSF Organizational Profile** describes the alignment between your organization's cybersecurity activities and outcomes, your business requirements, risk tolerance, and available resources.

CSF 2.0 defines two complementary profiles:

- **Current Profile**: The cybersecurity outcomes your organization is achieving *today* — an honest assessment of current state
- **Target Profile**: The cybersecurity outcomes your organization *wants to achieve* based on business goals, risk appetite, and regulatory requirements
- **Gap**: The delta between Current and Target — this drives your prioritized action plan and investment roadmap

> Profiles are expressed as a table of CSF subcategories rated against their current and target implementation states. The gap analysis between them becomes the basis for an Implementation Roadmap.

---

## Step-by-Step Profile Creation Process

### Step 1: Establish Business Context (GV.OC)

Before assessing any controls, you must understand the organizational context that will shape your risk tolerance and target state. This maps directly to **GV.OC** (Organizational Context) in CSF 2.0.

Gather the following inputs:

**Mission and Business Objectives**
- What does your organization do? What are your most critical business processes?
- What would it mean if those processes were disrupted for 1 hour? 1 day? 1 week?

**Legal and Regulatory Obligations**
- Which regulations apply? (e.g., HIPAA, PCI DSS, SOX, GDPR, CMMC, NERC CIP)
- What contractual security requirements do you have with customers or partners?
- These create *mandatory* target states for specific subcategories — you have no discretion on them.

**Stakeholder Requirements**
- What do your board, customers, insurers, and investors require from your cybersecurity program?
- Has your cyber insurer specified minimum controls?

**Crown Jewels / Critical Assets**
- What are your most valuable or sensitive assets? (customer data, IP, production systems, financial records)
- These inform where you must set a higher target state regardless of cost.

**Output from Step 1**: A documented Organizational Context statement covering mission, regulatory obligations, risk tolerance, and critical assets.

---

### Step 2: Define Risk Tolerance by Function (GV.RM)

Risk tolerance determines *how much gap is acceptable* in each CSF function. Risk tolerance statements per function might look like:

| Function | Risk Tolerance Statement |
|----------|--------------------------|
| Govern | Low tolerance — governance gaps create systemic vulnerability |
| Identify | Low tolerance — unknown assets and unassessed risks are unacceptable |
| Protect | Medium-to-low — critical assets require full implementation; lower criticality can be partial |
| Detect | Low tolerance — delayed detection increases incident impact |
| Respond | Low tolerance — unplanned response creates regulatory and reputational risk |
| Recover | Medium — accept some recovery time for non-critical systems; critical systems require RTO < 4 hours |

**Output from Step 2**: Risk tolerance statements per function that will anchor your target state ratings.

---

### Step 3: Map Regulatory Requirements to Subcategories

Before scoring, identify which subcategories are *mandatory* due to regulatory or contractual obligations. These subcategories must have a Target state of "Fully Implemented" regardless of your general risk tolerance.

**Example Mapping:**

| Regulation | Relevant CSF Subcategories |
|------------|---------------------------|
| HIPAA Security Rule | PR.AA (Access Control), PR.DS (Data Security), DE.CM (Monitoring), RS.MA (Incident Management) |
| PCI DSS | PR.AA, PR.DS, DE.CM, RS.MA, GV.PO |
| SOX (IT controls) | GV.OV (Oversight), ID.AM, PR.AA, DE.CM |
| CMMC Level 2 | ID.AM, PR.AA, PR.DS, PR.PS, DE.CM, RS.MA |
| Cyber Insurance | ID.AM, PR.AA (MFA requirement), PR.IR (backup), DE.CM |

**Output from Step 3**: A regulatory requirements matrix showing which subcategories have mandatory target states.

---

### Step 4: Build the Current Profile

The Current Profile is your honest, evidence-based assessment of where you are today. Use these rating definitions:

| Rating | Symbol | Definition |
|--------|--------|------------|
| Fully Implemented | FI | Control/practice is in place, documented, consistently applied, and operating effectively |
| Largely Implemented | LI | Mostly in place but with minor gaps, inconsistencies, or documentation gaps |
| Partially Implemented | PI | Some evidence exists but significant gaps remain or is inconsistently applied |
| Not Implemented | NI | No evidence of implementation |
| Not Applicable | N/A | Not applicable to this organization with documented rationale |

**How to assess current state:**
- Conduct interviews with IT, security, legal, HR, and operations staff
- Review existing policies, procedures, and technical configurations
- Review audit findings, penetration test results, and incident history
- Walk through evidence for each subcategory — do not self-certify without evidence

**Sample Current Profile (partial):**

| Function | Category | Subcategory ID | Subcategory Description | Current State | Evidence/Notes |
|----------|----------|----------------|------------------------|---------------|----------------|
| GV | Organizational Context | GV.OC-01 | Organizational mission understood and informs cybersecurity | PI | Mission documented; security alignment informal |
| GV | Risk Management Strategy | GV.RM-01 | Risk tolerance and appetite documented | NI | No formal risk tolerance statement exists |
| GV | Policy | GV.PO-01 | Cybersecurity policy established and communicated | PI | Policy exists but not reviewed in 3+ years |
| ID | Asset Management | ID.AM-01 | Hardware asset inventory maintained | PI | CMDB exists but incomplete; no OT assets |
| ID | Asset Management | ID.AM-02 | Software asset inventory maintained | NI | No formal software inventory |
| ID | Risk Assessment | ID.RA-01 | Vulnerabilities identified and documented | LI | Quarterly scanning with Qualys; some gaps |
| PR | Identity Mgmt & Access | PR.AA-01 | Identities managed for authorized users | LI | Active Directory managed; some service accounts undocumented |
| PR | Identity Mgmt & Access | PR.AA-03 | MFA implemented for user access | PI | MFA on email only; not on VPN or critical apps |
| PR | Awareness & Training | PR.AT-01 | Security awareness training provided | PI | Annual training only; no phishing simulations |
| DE | Continuous Monitoring | DE.CM-01 | Networks and assets monitored for anomalies | PI | Basic network monitoring; no behavioral analytics |
| RS | Incident Management | RS.MA-01 | Incident response plan exists | PI | IRP documented but not tested in 2 years |
| RC | Recovery | RC.RP-01 | Recovery plan exists and is executable | NI | No formal recovery plan; ad hoc only |

---

### Step 5: Build the Target Profile

The Target Profile defines where you want to be based on:
- Your risk tolerance statements (Step 2)
- Regulatory mandatory requirements (Step 3)
- Business objectives and resource constraints

**Key principles for setting target states:**
- Regulatory/contractual requirements → always set to Fully Implemented
- Crown jewel assets → set to Fully Implemented
- Supporting/lower-criticality areas → can accept Largely Implemented where risk tolerance allows
- Target Profile should be achievable within your planning horizon (typically 12–24 months)

**Sample Target Profile (continuing from above):**

| Function | Category | Subcategory ID | Current State | Target State | Regulatory Driver | Priority |
|----------|----------|----------------|---------------|--------------|-------------------|----------|
| GV | Organizational Context | GV.OC-01 | PI | FI | Board requirement | High |
| GV | Risk Management Strategy | GV.RM-01 | NI | FI | Cyber insurance | High |
| GV | Policy | GV.PO-01 | PI | FI | — | Medium |
| ID | Asset Management | ID.AM-01 | PI | FI | CMMC | High |
| ID | Asset Management | ID.AM-02 | NI | LI | CMMC | High |
| ID | Risk Assessment | ID.RA-01 | LI | FI | SOX | Medium |
| PR | Identity Mgmt & Access | PR.AA-01 | LI | FI | PCI, CMMC | High |
| PR | Identity Mgmt & Access | PR.AA-03 | PI | FI | Cyber insurance | Critical |
| PR | Awareness & Training | PR.AT-01 | PI | LI | — | Medium |
| DE | Continuous Monitoring | DE.CM-01 | PI | LI | HIPAA | High |
| RS | Incident Management | RS.MA-01 | PI | FI | HIPAA, PCI | High |
| RC | Recovery | RC.RP-01 | NI | FI | — | High |

---

### Step 6: Identify and Prioritize the Gaps

The gap is the difference between Current State and Target State. Calculate the gap size and assign priority:

| Gap Size | Definition | Typical Priority |
|----------|------------|------------------|
| Large | NI → FI or PI → FI | High or Critical |
| Medium | PI → LI or LI → FI | Medium |
| Small | LI → FI (minor documentation gap) | Low |

**Sample Gap Summary:**

| Function | # Subcategories | Critical Gaps | High Gaps | Medium Gaps | Total Gaps |
|----------|----------------|---------------|-----------|-------------|------------|
| Govern | 20 | 3 | 5 | 4 | 12 |
| Identify | 21 | 2 | 6 | 3 | 11 |
| Protect | 35 | 4 | 8 | 5 | 17 |
| Detect | 10 | 2 | 3 | 1 | 6 |
| Respond | 13 | 3 | 4 | 2 | 9 |
| Recover | 7 | 3 | 2 | 1 | 6 |

---

### Step 7: Generate the Implementation Roadmap

The gap analysis feeds directly into a prioritized roadmap. Key sequencing principles from CSF 2.0:

**Phase 1 (Prerequisites — must come first):**
- **GV.OC** — You need organizational context before anything else is meaningful
- **GV.RM** — Risk strategy must precede all risk-based decisions
- **ID.AM** — Asset inventory must precede protection decisions (you protect what you know)
- **ID.RA** — Risk assessment must precede prioritization

**Phase 2 (Protect based on Phase 1 priorities):**
- PR.AA controls (access control, MFA)
- PR.DS (data security based on asset classification from ID.AM)
- PR.PS (platform security / patching based on ID.RA findings)
- PR.IR (resilience / backup)

**Phase 3 (Detect and Respond):**
- DE.CM (monitoring — builds on protected assets)
- RS.MA (incident management — builds on monitoring)

**Phase 4 (Recover and Continuous Improvement):**
- RC.RP (recovery planning)
- Feed lessons back into GV and ID (continuous loop)

---

## Profile Document Template

Use this template to produce your formal profile document:

```
ORGANIZATION: [Name]
PROFILE DATE: [Date]
CSF VERSION: 2.0
PROFILE OWNER: [CISO / Risk Officer]
REVIEW CYCLE: Annual

ORGANIZATIONAL CONTEXT:
- Mission: [Statement]
- Critical Assets: [List]
- Regulatory Obligations: [List]
- Risk Tolerance (overall): [Low / Medium / High]

CURRENT PROFILE SUMMARY:
- Functions assessed: 6
- Total subcategories assessed: 106
- Fully Implemented: [N] ([%])
- Largely Implemented: [N] ([%])
- Partially Implemented: [N] ([%])
- Not Implemented: [N] ([%])

TARGET PROFILE SUMMARY:
- Target fully implemented: [N] ([%])
- Planning horizon: [12 / 24 months]

GAP SUMMARY:
- Critical gaps: [N]
- High gaps: [N]
- Medium gaps: [N]
- Low gaps: [N]

NEXT STEPS: [Link to Implementation Roadmap]
```

---

## Common Pitfalls to Avoid

1. **Self-assessment inflation**: Rating controls as "Fully Implemented" without evidence is the most common mistake. Require documented evidence for every rating above NI.

2. **Skipping GV.OC and GV.RM first**: Without understanding organizational context and risk tolerance, target states are arbitrary.

3. **Setting unrealistic targets**: A Target Profile that requires all 106 subcategories at Fully Implemented in 12 months is not achievable. Be realistic about resources.

4. **Treating the profile as a one-time exercise**: CSF Profiles should be reviewed at least annually and after significant incidents or organizational changes.

5. **Ignoring N/A justifications**: Every N/A designation needs documented rationale — auditors will scrutinize these.

> **Note**: This guidance is based on NIST CSF 2.0 (February 2024). Profile creation should involve qualified cybersecurity and risk management professionals for accuracy and defensibility.
