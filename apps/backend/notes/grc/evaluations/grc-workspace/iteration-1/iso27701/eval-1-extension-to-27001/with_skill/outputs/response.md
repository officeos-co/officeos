# ISO 27701: Extension of ISO 27001 and Certification Path

## What Is ISO 27701?

ISO/IEC 27701 is the international standard for **Privacy Information Management Systems (PIMS)**. It provides a framework for establishing, implementing, maintaining, and continually improving a privacy management system, with a specific focus on protecting Personally Identifiable Information (PII).

**Important version note:** There are two active editions:
- **ISO 27701:2019** — the original edition, which functioned as an *extension* to ISO 27001:2013. It required ISO 27001 certification as a prerequisite.
- **ISO 27701:2025** — the current edition (published 14 October 2025), now a *standalone* standard. ISO 27001 certification is no longer a prerequisite, though integration is still fully supported and recommended. All 2019 certifications must transition to the 2025 edition by **October 2028**.

Since your question references extending ISO 27001, this response covers both the 2019 extension model and the 2025 integrated path.

---

## How ISO 27701 Extends ISO 27001

### Structural Relationship (2019 Edition)

In the 2019 edition, ISO 27701 extended the ISO 27001 Information Security Management System (ISMS) by:

1. **Augmenting ISMS clauses** — Adding privacy-specific requirements to Clauses 4–10 of ISO 27001 (Context, Leadership, Planning, Support, Operation, Performance Evaluation, Improvement).
2. **Adding new Annex controls** — Annex A (PII Controller controls) and Annex B (PII Processor controls) supplemented the existing ISO 27002 security controls.

### Structural Relationship (2025 Edition — Current)

The 2025 edition adopts the **ISO High-Level Structure (HLS)** — the same framework used by ISO 27001:2022 and ISO 42001:2023. When integrated with ISO 27001:

- PIMS Clauses 4–10 run in parallel with (and complement) ISMS Clauses 4–10.
- The PIMS scope sits within or alongside the ISMS scope.
- Management system infrastructure (internal audit, management review, corrective action) can be shared between ISO 27001 and ISO 27701, reducing duplication.
- Risk treatment feeds both the Information Security Risk Treatment Plan and the Privacy Risk Treatment Plan.

**Annex A structure (2025 — 78 total controls):**

| Annex | Audience | Controls | Domains |
|-------|----------|----------|---------|
| A.1 | PII Controllers | 31 controls | 4 domains |
| A.2 | PII Processors | 18 controls | 4 domains |
| A.3 | Shared security controls | 29 controls | — |

---

## Your Role: Controller, Processor, or Both?

Before scoping the additional work, you must determine your organizational role:

- **PII Controller**: You determine the purposes and means of processing personal data (e.g., you process your own customers' data).
- **PII Processor**: You process personal data on behalf of another organization (e.g., a SaaS provider processing client data under a Data Processing Agreement).
- **Both**: Many organizations act as controller for employee data and processor for customer data.

Your role determines which Annex A controls you must implement:
- Controller only: A.1 (31) + A.3 (29) = **60 controls**
- Processor only: A.2 (18) + A.3 (29) = **47 controls**
- Both: A.1 + A.2 + A.3 = **78 controls**

---

## Additional Work Required to Achieve ISO 27701 Certification

As an ISO 27001-certified organization, you have a significant head start. Your existing ISMS infrastructure — policies, risk management, audit programme, corrective action — maps directly to ISO 27701 requirements. However, substantial privacy-specific work remains.

### Phase 1: Scope and Context Extension (Clause 4)

**What you likely have:** ISMS scope document, asset inventory, interested parties register.

**What you still need:**
- Extend the ISMS scope to become a PIMS scope, explicitly addressing PII processing activities.
- Create or formalise a **PII data inventory** identifying all personal data types, processing purposes, data flows, and retention periods.
- Update the interested parties register to focus on **PII principals** (data subjects), regulatory bodies (e.g., supervisory authorities), and customers.
- Define your organization's role (controller/processor/both) formally.

### Phase 2: Leadership and Privacy Governance (Clause 5)

**What you likely have:** Information security policy signed by top management.

**What you still need:**
- A separate **Privacy Policy** (or extension to the existing policy) covering PII processing principles, signed by top management (Clause 5.2 / Control A.1.2.2).
- Formal appointment of a **Data Protection Officer (DPO)** or Privacy Officer, with documented authority and resources.
- Documented **privacy roles and responsibilities** for all staff handling PII.

### Phase 3: Privacy Risk Assessment and Planning (Clause 6)

**What you likely have:** Information security risk assessment methodology and Statement of Applicability (SoA) for ISO 27001.

**What you still need:**
- A **privacy-specific risk assessment methodology** — privacy risks focus on harm to PII principals (individuals), not just organizational harm. This is a materially different risk perspective from information security risk.
- A **Privacy Risk Treatment Plan** selecting controls from Annex A.1, A.2, and/or A.3.
- An extended or separate **Statement of Applicability (SoA)** covering all 78 Annex A controls (or the subset applicable to your role), with applicability decisions and justifications for exclusions.
- Documented **privacy objectives** with measurable targets.

### Phase 4: Support — Training and Awareness (Clause 7)

**What you likely have:** Information security training programme.

**What you still need:**
- **Privacy-specific training** for all staff — covering PII handling, data subject rights, privacy by design, and incident reporting.
- **Competence evidence** for staff in privacy roles (DPO, privacy champions, developers).
- Updated **documented information procedures** for privacy-specific processes.

### Phase 5: Operational Controls (Clause 8) — The Largest Gap Area

This is typically where most of the new work lies. You will need to implement and evidence:

| Deliverable | Key Control(s) | Notes |
|-------------|---------------|-------|
| Records of Processing Activities (RoPA) | A.1.2.9 / A.2.2.7 | Mandatory under GDPR Article 30; must be current and complete |
| Consent management / lawful basis documentation | A.1.3.1 | Lawful basis documented for every processing activity |
| Privacy Notices (transparency notices) | A.1.3.3, A.1.3.4 | External-facing notices meeting transparency requirements |
| Data Subject Rights (DSR) procedure | A.1.3.5–A.1.3.11 | Covering access, rectification, erasure, portability, objection, restriction, automated decisions |
| DSR handling records | A.1.3.5–A.1.3.11 | Evidence of responses within required timescales |
| Data Processing Agreements (DPAs) with all processors | A.1.2.7 | All third-party processors under written contract with required privacy clauses |
| Privacy by Design (PbD) procedure | A.1.4.2–A.1.4.10 | Embedded in SDLC / product development; privacy reviewed at design stage |
| Data transfer procedure | A.1.5.2–A.1.5.5 | Transfer mechanisms documented per transfer (SCCs, adequacy, BCRs) |
| DPIA procedure and completed DPIAs | A.1.2.6 | All high-risk processing activities assessed; records retained |
| Privacy incident response plan | A.3.11, A.3.12 | Linked to your existing incident response; includes breach notification obligations |

### Phase 6: Performance Evaluation (Clause 9)

**What you likely have:** Internal audit programme and management review for ISO 27001.

**What you still need:**
- Extend the **internal audit programme** to cover PIMS-specific clauses and Annex A controls.
- Extend the **management review agenda** to include privacy KPIs, DSR metrics, DPIA outcomes, and regulatory developments.
- Define **privacy KPIs and monitoring metrics** (e.g., DSR response rate, overdue DPIAs, training completion rates).

### Phase 7: Improvement (Clause 10)

**What you likely have:** Corrective action process.

**What you still need:**
- Privacy-specific **nonconformity records** and corrective action log entries.
- Lessons learned from privacy incidents documented and fed back into the PIMS.

---

## Summary: ISO 27001 to ISO 27701 Gap at a Glance

| Area | Reusable from ISO 27001 | New for ISO 27701 |
|------|------------------------|------------------|
| Management system clauses (4–10) | Framework reusable | Privacy-specific content required in each clause |
| Risk assessment | Methodology reusable | New privacy risk perspective (harm to individuals) required |
| Statement of Applicability | Existing SoA reusable as base | Must add all Annex A privacy controls |
| Internal audit | Programme reusable | Must extend scope to PIMS |
| Management review | Process reusable | Must add privacy agenda items |
| Policy | Info security policy reusable | Privacy Policy required separately |
| RoPA | Not typically in ISO 27001 | Must be created from scratch |
| DSR procedures | Not in ISO 27001 | Must be created from scratch |
| DPIA process | Not in ISO 27001 | Must be created from scratch |
| Privacy by Design | Not typically in ISO 27001 | Must be embedded in SDLC |
| Consent/lawful basis records | Not in ISO 27001 | Must be created from scratch |

---

## Recommended Certification Approach

1. **Clarify version**: Target ISO 27701:2025 directly (transition deadline from 2019 is October 2028, so certifying to 2025 now avoids a future transition audit).
2. **Determine role**: Formally document controller/processor/both status.
3. **Conduct gap analysis**: Assess all PIMS clauses 4–10 and applicable Annex A controls against your current state.
4. **Build your PIMS scope and RoPA**: These are foundational — nothing else can be properly scoped without them.
5. **Develop privacy risk assessment**: Run a full privacy risk assessment using PII principal harm as the risk perspective.
6. **Produce SoA**: Document all 47, 60, or 78 controls with applicability and status.
7. **Implement operational controls**: Prioritise RoPA, DSR procedure, DPAs with processors, DPIA process, and Privacy by Design.
8. **Run internal audit and management review**: With extended PIMS scope.
9. **Stage 1 certification audit**: Document review by certification body.
10. **Stage 2 certification audit**: On-site/remote evidence review.

A typical timeline from ISO 27001 baseline to ISO 27701 certification is **6–12 months**, depending on the maturity of your existing privacy programme and the number of processing activities in scope.
