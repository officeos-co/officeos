# ISO 27701: Extension to ISO 27001 and Certification Pathway

## What Is ISO 27701 and How Does It Extend ISO 27001?

ISO/IEC 27701 is a Privacy Information Management System (PIMS) standard that was specifically designed to help organizations demonstrate compliance with GDPR, UK GDPR, and similar global privacy regulations — this is its primary value proposition. It provides a structured framework for managing personal data (referred to as Personally Identifiable Information, or PII) and defines controls for organizations acting as PII Controllers, PII Processors, or both.

Because you already hold ISO 27001 certification, the most relevant starting point is the **2019 edition extension model**: ISO 27701:2019 was built as a direct extension to ISO 27001:2013 and could not be certified as a standalone standard. ISO 27001 certification was — and in the 2019 edition remains — a mandatory prerequisite. Your existing ISO 27001 certification is therefore not just a head start; it is a required foundation.

The current edition, **ISO 27701:2025** (published 14 October 2025), restructures the standard as a **standalone Privacy Information Management System**. ISO 27001 is no longer a prerequisite in the 2025 edition, though integration remains fully supported and is the most common implementation path for organizations that already hold an ISMS. Most certifications currently in existence are under the 2019 edition; transition to 2025 is required by **October 2028**.

---

## How ISO 27701 Extends ISO 27001

ISO 27001 focuses on **information security** — protecting the confidentiality, integrity, and availability of information assets. ISO 27701 extends this by adding a **privacy dimension**: protecting the rights and freedoms of individuals whose personal data is processed.

| Dimension | ISO 27001 | ISO 27701 Extension |
|-----------|-----------|---------------------|
| Primary focus | Security of information assets | Privacy of PII principals (individuals) |
| Risk perspective | Organizational risk | Harm to individuals |
| Core document | Statement of Applicability (SoA) for ISMS | PIMS SoA covering privacy-specific controls |
| Regulatory alignment | General security frameworks | GDPR, UK GDPR, CCPA, LGPD, and others |
| Controls | ISO 27001 Annex A (93 controls in 2022 edition) | Additional A.1 (controller), A.2 (processor), A.3 (security) controls |

In the 2019 edition, ISO 27701 added:
- **Annex A**: 28 additional controls for PII Controllers
- **Annex B**: 16 additional controls for PII Processors
- Extended the ISO 27001 management system clauses (4–10) with privacy-specific requirements

In the 2025 edition, the structure is:
- **A.1**: 31 controls for PII Controllers
- **A.2**: 18 controls for PII Processors
- **A.3**: 29 shared information security controls (rationalized from the 2019 Clause 6)

---

## What Additional Work Is Required to Achieve ISO 27701 Certification?

Given your ISO 27001 certification, the additional work falls into four areas:

### 1. Establishing Your PIMS Role

Before implementing any controls, you must determine your role:

- **PII Controller**: Your organization determines the purposes and means of processing personal data (e.g., you process employee data or customer data for your own purposes).
- **PII Processor**: You process personal data on behalf of another organization (e.g., you are a SaaS provider or outsourced service acting under customer instructions).
- **Both**: Many organizations act as both a controller (for their own data) and a processor (for customers' data).

Your role determines which Annex A controls you must implement:
- **Controller only**: A.1 (31 controls) + A.3 (29 controls) = 60 controls
- **Processor only**: A.2 (18 controls) + A.3 (29 controls) = 47 controls
- **Both**: A.1 + A.2 + A.3 = 78 controls

### 2. Management System Gap Analysis (Clauses 4–10)

Your ISO 27001 management system provides the structural foundation, but each clause needs privacy-specific additions:

| Clause | What Already Exists (ISO 27001) | What ISO 27701 Adds |
|--------|--------------------------------|----------------------|
| 4 — Context | ISMS scope, interested parties | PIMS scope (must explicitly include PII processing activities), PII data inventory, privacy-focused interested parties (PII principals, regulators, DPA) |
| 5 — Leadership | IS Policy | Privacy Policy (standalone, signed by top management); DPO appointment where required by GDPR Art. 37 |
| 6 — Planning | ISMS risk assessment, SoA | Privacy risk assessment methodology (focused on harm to individuals); PIMS Statement of Applicability (separate SoA covering privacy controls); privacy objectives |
| 7 — Support | Security training records | Privacy-specific awareness training; competence evidence for privacy roles |
| 8 — Operation | ISMS operational controls | Records of Processing Activities (RoPA), DPIA records, Data Subject Rights (DSR) procedures, processor contracts, consent management |
| 9 — Performance | ISMS audit, management review | Privacy KPIs; internal audit of PIMS scope; management review agenda items covering privacy |
| 10 — Improvement | ISMS corrective actions | Privacy nonconformity records; lessons learned from privacy incidents |

### 3. Annex A Privacy Controls Implementation

The privacy-specific controls are the core of the certification work. Key areas to address:

**For PII Controllers (Annex A.1):**

| Domain | Controls | Key Deliverables |
|--------|----------|-----------------|
| A.1.2 — Conditions for Collection and Processing | A.1.2.2–A.1.2.9 | Lawful basis register per processing activity, consent management mechanism, Records of Processing Activities (RoPA), DPIA process, DPAs with all processors |
| A.1.3 — Obligations to PII Principals | A.1.3.2–A.1.3.11 | Data Subject Rights procedure with SLAs, Privacy Notice, consent withdrawal mechanism, automated decision-making disclosure |
| A.1.4 — Privacy by Design and by Default | A.1.4.2–A.1.4.10 | Data minimisation procedures, retention schedules, secure disposal, de-identification process, PII transmission controls |
| A.1.5 — PII Sharing, Transfer and Disclosure | A.1.5.2–A.1.5.5 | International transfer mechanism documentation (SCCs, adequacy decisions), transfer log |

**For PII Processors (Annex A.2):**

| Domain | Controls | Key Deliverables |
|--------|----------|-----------------|
| A.2.2 — Conditions for Collection and Processing | A.2.2.2–A.2.2.7 | Customer DPAs, processing-under-instructions procedure, per-controller processing records |
| A.2.3 — Obligations to PII Principals | A.2.3.2 | Process for handling DSR inquiries received directly from individuals |
| A.2.4 — Privacy by Design and by Default | A.2.4.2–A.2.4.4 | Data return/disposal procedure at contract end, PII transmission controls |
| A.2.5 — PII Sharing, Transfer and Disclosure | A.2.5.2–A.2.5.9 | Sub-processor disclosure list, sub-processor authorization process, controller notification of sub-processor changes |

**For All Organizations (Annex A.3 — Security Controls):**

Since you hold ISO 27001, the 29 A.3 security controls are largely covered by your existing ISMS controls. The primary task is to **map and cross-reference** your existing ISMS evidence to the A.3 control IDs in the PIMS SoA — no new implementation work is typically needed. For example:
- A.3.25 (Logging) maps to your ISMS A.8.15/A.8.16 controls
- A.3.26 (Cryptography) maps to your ISMS A.8.24 controls
- A.3.11/A.3.12 (Incident Management) maps to your ISMS A.5.24–5.28 controls

### 4. Documentation to Produce for Certification

| Document | Clause | Notes |
|----------|--------|-------|
| PIMS Scope | 4.3 | Separate from ISMS scope; must explicitly describe PII processing activities |
| Privacy Policy | 5.2 | Signed by top management; may be separate from IS Policy or combined with privacy section |
| Privacy Risk Assessment | 6.1 | Focused on harm to individuals (not just organizational risk); documents threats to PII principals |
| Privacy Risk Treatment Plan | 6.1 | Controls selected from A.1/A.2 to mitigate privacy risks |
| PIMS Statement of Applicability (SoA) | 6.1 | Covers all A.1, A.2, and A.3 controls; justifies exclusions; references evidence |
| Records of Processing Activities (RoPA) | 8 | Complete register of all processing activities; must cover purposes, categories, lawful basis, transfers |
| Data Subject Rights Procedure | 8 | Covers all DSR types (access, erasure, portability, objection, rectification, restriction, automated decision-making); documented SLAs |
| DPIA Procedure and Records | 8 | Trigger criteria, methodology, completed DPIA records for high-risk processing |
| Processor Contracts (DPAs) | 8 | Required for all third parties processing PII on your behalf |
| Privacy Training Records | 7.3 | Role-based privacy training; records of completion |
| Consent Management Records | 8 | Where consent is a lawful basis: how consent is obtained, recorded, and withdrawn |
| Privacy Incident Response Records | 8 | Records of personal data breach investigation and notification |

### 5. Certification Process

For organizations already certified under ISO 27001, the ISO 27701 certification audit is typically conducted **jointly with the ISO 27001 surveillance or recertification audit**. Your existing certification body can usually extend their scope to include ISO 27701.

**Typical steps:**
1. Gap analysis against ISO 27701 privacy controls
2. Implement missing controls and produce required documentation
3. Internal audit of the PIMS
4. Management review covering privacy outputs
5. Stage 1 audit (document review by certification body)
6. Stage 2 audit (on-site evidence review)
7. Certification issued covering both ISO 27001 and ISO 27701

---

## Key Priority Areas

Based on common gap patterns, prioritize these areas first:

1. **Records of Processing Activities (RoPA)** — Does a complete, current RoPA exist covering all processing activities with lawful basis documented?
2. **Data Subject Rights procedure** — Is there a documented, tested procedure with response SLAs (72 hours for breaches, 30 days for most DSRs under GDPR)?
3. **Consent management** — Is the lawful basis documented for every processing activity? Where consent is the basis, is there a working withdrawal mechanism?
4. **DPIA process** — Is there a documented DPIA trigger and methodology, and have DPIAs been completed for all high-risk processing?
5. **Processor contracts** — Do all third-party processors have compliant DPAs in place?
6. **Privacy Policy** — Is there a standalone Privacy Policy signed by top management?

---

## Note on ISO 27701:2025 Transition

If you certify under the 2019 edition now, you will need to transition to the 2025 edition by **October 2028**. The 2025 edition adds a small number of new controls (A.1.4.6 De-identification, A.1.4.8 Retention, A.1.4.9 Disposal, A.1.4.10 PII Transmission Controls for controllers; A.2.4.4 and A.2.5.8–A.2.5.9 for processors) and restructures the security controls into Table A.3.

If you are beginning implementation now, you may choose to target the 2025 edition directly to avoid a transition audit — discuss this with your certification body.
