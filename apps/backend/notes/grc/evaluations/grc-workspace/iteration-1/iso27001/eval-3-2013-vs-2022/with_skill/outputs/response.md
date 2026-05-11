# ISO 27001:2013 vs ISO 27001:2022 — Key Differences & Transition Guide

> **Important Note:** The transition deadline from ISO 27001:2013 to ISO 27001:2022 was **October 31, 2025**. All ISO 27001:2013 certificates have now expired. If you were certified under the 2013 version, you must have already transitioned — or your certification is no longer valid. This guide will help you understand what changed and how to demonstrate compliance with the 2022 version.

---

## 1. High-Level Summary of Changes

| Topic | ISO 27001:2013 | ISO 27001:2022 | Impact |
|-------|----------------|----------------|--------|
| **Annex A Controls (total)** | 114 controls | 93 controls | Net reduction of 21 controls |
| **Annex A Domains/Themes** | 14 domains | 4 themes | Reorganised, not reduced in substance |
| **New controls** | — | 11 new controls | Action required for these 11 |
| **Merged controls** | — | ~57 controls merged into fewer | Lower administrative burden |
| **Removed controls** | — | 0 truly removed | All 2013 concepts are preserved or merged |
| **Clause 6** | 6.1, 6.2 only | Added **6.3** (Planning of changes) | New clause; document your change process |
| **Clause 9.2** | Single clause | Split: **9.2.1** (General) + **9.2.2** (Audit programme) | Document audit programme separately |
| **Clause 9.3** | Single clause | Split: **9.3.1** (General) + **9.3.2** (Inputs) + **9.3.3** (Results) | More granular management review documentation |
| **Control attributes** | None | Each control has 5-attribute taxonomy | Useful for filtering; not strictly mandatory |
| **Standard title** | "Requirements" | "Information security, cybersecurity and privacy protection" | Broader scope acknowledged |

---

## 2. Clause-Level Changes (Mandatory Requirements)

The clause framework (4–10) is substantially the same between versions, but three structural additions affect your documented processes:

### 2.1 New Clause 6.3 — Planning of Changes

**What it requires:** When the organisation determines a need to change the ISMS, changes must be carried out in a planned manner.

**What you need to do:**
- Document your ISMS change management process
- Ensure that when policies, scope, or controls change, there is a formal decision and approval record
- This does NOT require a heavy IT change management process — it applies to ISMS-level changes (e.g., changing scope, adopting new controls, changing risk methodology)

**Evidence for auditors:** Change request records for any ISMS-level changes, with documented rationale and approval

---

### 2.2 Clause 9.2 Split — Internal Audit

**2013:** Single clause 9.2 covering all internal audit requirements

**2022:**
- **9.2.1 General** — the organisation shall conduct internal audits at planned intervals
- **9.2.2 Internal audit programme** — requires a documented audit programme specifying frequency, methods, responsibilities, and results

**What you need to do:**
- Ensure your audit programme is documented separately (not just ad hoc)
- The programme must specify intervals, audit methods, responsibilities, and reporting
- Update your internal audit procedure to reference 9.2.1 and 9.2.2

---

### 2.3 Clause 9.3 Split — Management Review

**2013:** Single clause 9.3

**2022:**
- **9.3.1 General** — top management shall review the ISMS at planned intervals
- **9.3.2 Management review inputs** — explicit list of what must be discussed
- **9.3.3 Management review results** — decisions and actions must be documented

**What you need to do:**
- Ensure your management review minutes explicitly address all required inputs from 9.3.2
- Document decisions and actions (outputs) separately and clearly in minutes
- Ensure minutes reference the 9.3.2 input items so auditors can verify coverage

---

## 3. Annex A Control Changes — The Detail

### 3.1 Structure Change: 14 Domains → 4 Themes

| 2022 Theme | Covers | 2013 Equivalent Domains |
|------------|--------|------------------------|
| **Organisational controls** (A.5, 37 controls) | Policies, roles, supplier management, incident management, compliance | A.5, A.6.1, A.7, A.15, A.18 |
| **People controls** (A.6, 8 controls) | HR security, awareness, remote work, confidentiality | A.7, A.8 |
| **Physical controls** (A.7, 14 controls) | Physical security, equipment, clear desk | A.11 |
| **Technological controls** (A.8, 34 controls) | Access, encryption, logging, vulnerability mgmt, secure dev | A.9, A.10, A.12, A.13, A.14, A.16, A.17 |

---

### 3.2 The 11 New Controls in ISO 27001:2022

These controls did not exist (by name) in ISO 27001:2013. They must be addressed in your updated SoA — either implemented or explicitly excluded with justification.

| Control ID | Control Name | Why It Was Added | Key Implementation Actions |
|------------|--------------|-----------------|---------------------------|
| **A.5.7** | Threat intelligence | Growing threat landscape requires proactive intelligence | Subscribe to threat feeds (ISACs, vendor bulletins); document review process |
| **A.5.23** | Information security for use of cloud services | Cloud adoption had no dedicated control in 2013 | Document cloud governance policy; assess cloud providers; define cloud security requirements |
| **A.5.30** | ICT readiness for business continuity | BC needed stronger IT-specific control | Integrate IT recovery into BCP; test ICT recovery annually |
| **A.7.4** | Physical security monitoring | Physical surveillance needed formalisation | Document CCTV/access control monitoring; retain logs per policy |
| **A.8.9** | Configuration management | Config drift is a leading cause of breaches | Define configuration baselines; use tools (AWS Config, etc.) to detect drift |
| **A.8.10** | Information deletion | Data minimisation became critical (GDPR influence) | Document retention schedules; implement secure deletion procedures |
| **A.8.11** | Data masking | PII/sensitive data protection in non-production | Implement masking in dev/test environments; document approach |
| **A.8.12** | Data leakage prevention | DLP controls needed explicit coverage | Implement DLP tooling or compensating controls; document monitoring |
| **A.8.16** | Monitoring activities | Broader monitoring (beyond just logs) | Formalise monitoring programme covering networks, systems, applications |
| **A.8.23** | Web filtering | Web-based threats needed direct control | Deploy DNS/web filtering; document allowed/blocked categories |
| **A.8.28** | Secure coding | Secure development needed a specific control | Adopt OWASP or similar coding standards; include in SDLC |

---

### 3.3 Controls That Were Merged (Consolidated)

Many 2013 controls were merged into single 2022 controls. You likely already comply with these — you just need to update your SoA mapping.

Key examples:

| 2022 Control | Merged from 2013 Controls |
|--------------|--------------------------|
| A.5.15 (Access control) | A.9.1.1 + A.9.1.2 |
| A.5.16 (Identity management) | A.9.2.1 |
| A.5.17 (Authentication information) | A.9.2.4 + A.9.3.1 + A.9.4.3 |
| A.8.5 (Secure authentication) | A.9.4.2 |
| A.8.15 (Logging) | A.12.4.1 + A.12.4.2 + A.12.4.3 |
| A.8.24 (Use of cryptography) | A.10.1.1 + A.10.1.2 |
| A.5.24–5.28 (Incident management) | A.16.1.1 through A.16.1.7 |

---

### 3.4 Controls Removed or Restructured

No 2013 controls were truly deleted — their intent was absorbed into 2022 controls. However, these 2013 controls were restructured significantly:

- **A.6.1.2** (Segregation of duties) → Now embedded in A.5.3 (Segregation of duties) — same requirement, reorganised
- **A.8.1** series (Asset management) → Redistributed across A.5.9, A.5.10, A.5.11, A.5.12
- **A.14** (System acquisition, development) → Largely absorbed into A.8.25–A.8.34

---

## 4. What You Need to Do for Transition

### Step 1: Update Your Statement of Applicability (SoA)

This is the most critical transition task:
- Remap all 2013 control references to their 2022 equivalents
- Add the 11 new controls — either include or formally exclude with justification
- Update control numbering throughout all documents
- Have the updated SoA reviewed and approved

### Step 2: Address the 11 New Controls

For each of the 11 new controls:
1. Assess current state — do you have compensating controls already?
2. If yes, document evidence and reference in SoA
3. If no, implement controls and create evidence before transition audit

### Step 3: Update Documented Processes

| Document | Change Required |
|----------|----------------|
| ISMS Manual / Scope | Update references from 2013 to 2022; add Clause 6.3 |
| Internal Audit Procedure | Add separate audit programme document (9.2.2) |
| Management Review Procedure | Split into 9.3.1/9.3.2/9.3.3; ensure inputs and outputs documented |
| All policies | Update Annex A control cross-references |
| Risk Treatment Plan | Update control references to 2022 numbering |

### Step 4: Conduct a Transition Internal Audit

Before the transition audit, conduct an internal audit specifically covering:
- All 11 new controls
- Clause 6.3 implementation
- Updated clause 9.2 and 9.3 processes
- SoA accuracy

### Step 5: Transition Audit with Certification Body

Your CB will conduct a transition audit (typically equivalent to a surveillance audit in length). They will verify:
- Updated SoA
- Evidence for the 11 new controls
- Clause 6.3, updated 9.2, updated 9.3
- That your existing controls still operate effectively

---

## 5. New Control Attribute System (Informative — Not Mandatory)

ISO 27001:2022 introduced an attribute taxonomy for each Annex A control. While not mandatory to implement, it can help you filter and prioritise controls.

Each control is tagged with:

| Attribute | Categories |
|-----------|-----------|
| **Control type** | Preventive / Detective / Corrective |
| **Information security properties** | Confidentiality / Integrity / Availability |
| **Cybersecurity concepts** | Identify / Protect / Detect / Respond / Recover |
| **Operational capabilities** | Governance / Asset management / etc. |
| **Security domains** | Governance and ecosystem / Protection / Defence / Resilience |

---

## 6. Summary: Transition Checklist

- [ ] Obtain ISO 27001:2022 standard and read the full Annex A
- [ ] Remap your existing SoA from 2013 to 2022 control references
- [ ] Assess all 11 new controls for applicability and implementation status
- [ ] Implement any new controls with gaps
- [ ] Add Clause 6.3 ISMS change management process
- [ ] Update internal audit programme documentation (9.2.1 / 9.2.2)
- [ ] Update management review documentation (9.3.1 / 9.3.2 / 9.3.3)
- [ ] Update all policy cross-references to 2022 control numbering
- [ ] Conduct transition internal audit
- [ ] Schedule transition audit with certification body
- [ ] Confirm certificate reissued under ISO 27001:2022

---

*Prepared for ISO 27001:2013 → 2022 Transition | Standard: ISO/IEC 27001:2022 | Date: 2026-04-18*
