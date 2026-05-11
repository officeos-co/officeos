# DPDPA Cross-Border Data Transfer Requirements

## The Core Mechanism: Blacklist Approach (Section 16)

The DPDPA takes a fundamentally different approach to cross-border data transfers than GDPR. Under **Section 16**, the default position is **permissive**:

> A Data Fiduciary may transfer personal data of a Data Principal to **any country or territory outside India**, except those **specifically notified by the Central Government as restricted**.

This is a **blacklist approach** — transfers are permitted everywhere unless a specific country appears on a restricted list published by the Central Government. This is the opposite of GDPR, which uses a whitelist/adequacy approach requiring a positive transfer mechanism for every cross-border transfer.

---

## Current Status: Are Transfers to the US Permitted?

**Yes — transfers to your US servers are currently permissible.**

As of April 2026, the Central Government has **not notified any countries as restricted** under Section 16. This means:

- Transfers to the United States are currently lawful under the DPDPA
- No transfer mechanism equivalent to GDPR Standard Contractual Clauses (SCCs) is required
- No adequacy assessment or approval is needed for the transfer itself

**However, this position can change.** The Central Government retains the power to restrict transfers to specific countries or territories at any time by notification in the Official Gazette. Your legal team should monitor the **MeitY Official Gazette** for any future notifications.

---

## Comparison with GDPR: Blacklist vs. Whitelist

| Dimension | GDPR | DPDPA Section 16 |
|-----------|------|-----------------|
| **Default position** | **Restrictive** — transfers require a positive mechanism | **Permissive** — transfers allowed unless country is restricted |
| **Transfer mechanism required** | Adequacy decision, SCCs, BCRs, binding corporate rules, derogations (Art. 46–49) | **None currently required** |
| **Restricted countries** | Countries without adequacy decision (unless SCCs/BCRs used) | Countries specifically notified by Central Government (none as of April 2026) |
| **Documentation requirement** | Detailed SCC/BCR documentation, transfer impact assessments | Contractual safeguards recommended but not mandated |
| **Legal certainty** | High — established mechanisms | Currently high (no restrictions), but potentially uncertain as notifications may come |
| **US transfers** | Require SCCs (EU-US Data Privacy Framework for qualified entities, or SCCs) | Currently **permitted without any mechanism** |

---

## What You Should Do Now (Even Though Transfers Are Currently Permitted)

The absence of restrictions today does not mean you should operate without any safeguards. Best practice guidance and potential future compliance requirements suggest:

### 1. Maintain Data Flow Maps
Document all transfers of Indian personal data to US servers:
- Categories of data transferred
- Volume and frequency
- Purpose of transfer
- Recipients (internal systems, third-party processors)
- Retention periods offshore

This inventory positions you to respond quickly if the Central Government restricts US transfers.

### 2. Implement Basic Contractual Protections
While DPDPA does not currently require SCCs or equivalent mechanisms, include reasonable data protection obligations in contracts with US-side recipients and processors:
- Security requirements aligned with Rule 7 (encryption, access controls, MFA)
- Restrictions on secondary use
- Breach notification obligations
- Deletion/return of data on termination

These are required anyway under **Section 8(1) and Rule 16** (Data Processing Agreements with processors), so your processor contracts should already incorporate these terms.

### 3. Comply with Significant Data Fiduciary (SDF) Data Localisation Requirements
If your company is designated as a **Significant Data Fiduciary (SDF)** under Section 10, the Central Government may require certain categories of personal data to remain within India. This is separate from the Section 16 transfer restrictions. SDF localisation obligations depend on future notifications — monitor these closely if you process large volumes of Indian personal data.

### 4. Monitor MeitY Notifications
Set up a process to monitor the Official Gazette and MeitY announcements. If the US is added to the restricted list, you would need to:
- Cease transfers until a compliant mechanism is established (or the restriction is lifted)
- Potentially implement data localisation for Indian user data
- Restructure your infrastructure to store Indian data within India

---

## Practical Risk Assessment

| Risk Factor | Current Status | Future Risk |
|-------------|---------------|------------|
| US transfers permitted? | **Yes** | May change if US added to restricted list |
| Transfer mechanism required? | No | May be required in future |
| SCC equivalent needed? | No | No DPDPA equivalent exists yet |
| Data localisation for SDFs? | Possible if designated SDF | High risk if SDF designation received |
| Penalty for non-compliant transfer | ₹50 crore (general violations) | Up to ₹250 crore if security safeguards also deficient |

---

## Key Takeaway

Your current transfers of Indian user data to US servers are **lawful under the DPDPA** as of today. The DPDPA's blacklist approach is significantly less restrictive than GDPR's whitelist model — you do not need SCCs, adequacy decisions, or transfer impact assessments at this time.

However, treat this as a window of opportunity to:
1. Document your data flows
2. Implement contractual protections with US processors
3. Build monitoring processes to detect future transfer restrictions
4. Assess whether SDF designation is likely (which brings additional localisation obligations)

Full compliance (including any transfer restrictions that may be notified) is required from **13 May 2027**.
