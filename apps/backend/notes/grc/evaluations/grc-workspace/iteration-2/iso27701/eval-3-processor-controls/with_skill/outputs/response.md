# ISO 27701 Privacy Controls for PII Processors

## Overview

As a B2B service provider acting as a data processor for your customers, you are operating in the role of a **PII Processor** under ISO 27701. This determines which Annex A controls apply to your certification scope. PII Processors must implement:

- **Table A.2**: 18 processor-specific controls
- **Table A.3**: 29 shared information security controls

Total: **47 controls** (plus all mandatory Clause 4–10 management system requirements).

This document provides a complete reference to all A.2 processor controls using ISO 27701's own terminology — these are the exact phrases that will appear in audit evidence requests and in your customers' Data Processing Agreements (DPAs).

---

## Table A.2 — Complete PII Processor Controls

### A.2.2 — Conditions for Collection and Processing (6 controls)

These controls establish the foundational rule for processors: you process PII only under documented authority from the controller, for the controller's stated purposes, and within a written contractual framework.

---

**Control: A.2.2.2 — Customer Agreement**

- **Purpose**: Ensures every instance of processing is governed by a written contract with the PII controller (your customer). This is the processor's equivalent of the controller's DPA obligation.
- **What to implement**: Ensure a signed Data Processing Agreement (DPA) is in place with every B2B customer before processing any PII on their behalf. The DPA must specify the subject matter, nature, purpose, and duration of processing; the type of PII; and the obligations of both parties.
- **Evidence for audit**: A library of executed DPAs for all active customers; DPA template reviewed by legal counsel; process for ensuring no processing begins without a signed DPA.
- **Common pitfalls**: Processing beginning before a DPA is signed; legacy customer contracts that predate GDPR-compliant DPA clauses; DPA scope not covering all processing activities actually performed.
- **Regulatory link**: GDPR Article 28(3) — processor must process only on documented instructions of the controller.

---

**Control: A.2.2.3 — Organisation's Purposes**

- **Purpose**: Prohibits the processor from using PII it processes on behalf of controllers for its own business purposes.
- **What to implement**: Policy prohibiting use of customer PII for own analytics, product training, marketing, research, or any other purpose not specified in the controller's DPA. Technical controls (data segregation, access restrictions) to enforce the prohibition.
- **Evidence for audit**: Written policy; technical architecture showing customer data segregation; access control records; contractual prohibitions in DPAs.
- **Common pitfalls**: Using aggregated or anonymized customer data for own product improvement (may still constitute processing if not truly anonymous); using customer data for cross-marketing between clients.
- **Regulatory link**: GDPR Article 28(3)(a) — processor processes only on documented instructions.

---

**Control: A.2.2.4 — Marketing and Advertising**

- **Purpose**: Specifically prohibits using PII processed under a processing agreement for marketing or advertising purposes — even broadly — without explicit controller authorization.
- **What to implement**: Explicit contractual prohibition in all DPAs; internal policy; technical controls preventing PII from flowing into marketing or advertising systems.
- **Evidence for audit**: DPA clause; internal policy; data flow diagram showing separation of customer PII from marketing systems.
- **Common pitfalls**: Behavioral targeting or lookalike audience building using processing customers' data; cross-client targeting.
- **Regulatory link**: GDPR Article 28(3)(a).

---

**Control: A.2.2.5 — Infringing Instruction**

- **Purpose**: Requires the processor to notify the controller — and refuse execution — if a controller instruction would breach applicable privacy law.
- **What to implement**: Process for legal/compliance review of controller instructions; escalation path when instructions are potentially unlawful; documented notification procedure to inform the controller.
- **Evidence for audit**: Documented escalation procedure; training records for staff who receive instructions; records of any escalations made.
- **Common pitfalls**: Blindly executing all controller instructions without legal review; failing to escalate when instructions are ambiguous.
- **Regulatory link**: GDPR Article 28(3)(h) — processor must immediately inform the controller if an instruction infringes data protection law.

---

**Control: A.2.2.6 — Customer Obligations**

- **Purpose**: Requires the processor to actively assist the controller in meeting its obligations to PII principals (data subjects). This covers the processor's support role for Data Subject Rights (DSR) fulfilment, DPIA support, and breach notification — all within agreed timeframes.
- **What to implement**: Contractual and operational procedures to support customers' DSR processes (access, erasure, portability, restriction, rectification) within SLAs that allow the controller to meet GDPR's 30-day response window; DPIA support capability; breach notification to controller within agreed timeframes (typically 24–48 hours to allow the controller's 72-hour GDPR clock to run).
- **Evidence for audit**: DPA clauses covering DSR support obligations; documented process; SLA records; records of DSR support provided to controllers.
- **Common pitfalls**: No defined SLA for passing DSR requests to the customer; processors routing data subject inquiries directly without involving the controller; breach notification delays that cause the controller to miss the 72-hour window.
- **Regulatory link**: GDPR Article 28(3)(e) — processor must assist controller taking into account the nature of the processing.

---

**Control: A.2.2.7 — Records of Processing PII**

- **Purpose**: Requires the processor to maintain a Record of Processing Activities (RoPA) for all processing carried out on behalf of controllers. This is the processor's Article 30(2) obligation under GDPR.
- **What to implement**: Maintain a per-controller processing register documenting: controller identity and contact, processing categories, data types processed, transfers to third countries, retention periods, and security measures.
- **Evidence for audit**: Processor RoPA document; process for updating when new customers onboard or when processing activities change.
- **Common pitfalls**: Maintaining a single combined log rather than per-controller records; RoPA not updated when processing scope changes; no process for removing records when contracts terminate.
- **Regulatory link**: GDPR Article 30(2) — processors must maintain records of all categories of processing activities carried out on behalf of a controller.

---

### A.2.3 — Obligations to PII Principals (1 control)

**Control: A.2.3.2 — Obligations to PII Principals**

- **Purpose**: When individuals (data subjects / PII principals) contact the processor directly with inquiries or rights requests, the processor must handle these appropriately — typically by redirecting to the controller while ensuring no confusion or loss of the individual's request.
- **What to implement**: Training for customer-facing and support staff on how to handle DSR inquiries received directly (identify, log, redirect to the appropriate controller, and confirm the redirect to the individual). Do not attempt to fulfil rights requests without controller instruction unless specifically authorized in the DPA.
- **Evidence for audit**: Staff training records; procedure for handling direct PII principal inquiries; log of redirected inquiries.
- **Common pitfalls**: Support staff attempting to fulfil access or erasure requests without controller involvement; requests being lost rather than redirected.
- **Regulatory link**: GDPR Article 28 — the controller retains accountability for data subject rights; the processor supports and facilitates.

---

### A.2.4 — Privacy by Design and by Default (3 controls)

**Control: A.2.4.2 — Temporary Files**

- **Purpose**: Temporary processing files (caches, query results, intermediate processing artefacts) containing PII must be managed with the same care as primary PII stores and deleted per agreed schedules.
- **What to implement**: Inventory of all temporary processing locations where customer PII may land; defined retention windows for temporary data; automated deletion mechanisms; scope to include backup/staging environments.
- **Evidence for audit**: Data flow diagrams showing temporary storage locations; deletion procedures and schedules; technical evidence of automated deletion.
- **Common pitfalls**: PII in log files, analytics caches, and debugging artefacts retained indefinitely; temporary files in development/test environments.
- **Regulatory link**: GDPR Article 5(1)(e) — storage limitation.

---

**Control: A.2.4.3 — Return, Transfer or Disposal of PII**

- **Purpose**: When a processing agreement terminates, all PII must be returned to the controller or securely deleted as instructed. The processor must not retain PII after the contract ends.
- **What to implement**: Contract offboarding procedure: confirm with controller whether they want data returned or deleted; execute within defined timeframe; certify deletion and provide evidence to controller; ensure all copies (including backups and sub-processor copies) are addressed.
- **Evidence for audit**: DPA clause specifying return/deletion obligations; offboarding checklist; deletion certificates issued to customers; records of offboarding completions.
- **Common pitfalls**: PII retained in backups or archives after contract termination; no process to propagate deletion to sub-processors; no confirmation to controller.
- **Regulatory link**: GDPR Article 28(3)(g) — at the choice of the controller, delete or return all personal data upon end of services.

---

**Control: A.2.4.4 — PII Transmission Controls** *(New in 2025 edition)*

- **Purpose**: Protects PII in transit during processing operations — both between components of the processor's systems and when transmitting to/from the controller or sub-processors.
- **What to implement**: Encryption for all PII in transit (TLS 1.2+, secure file transfer); controls on email transmission of PII; API security for data exchanges; documented transmission controls per data sensitivity level.
- **Evidence for audit**: Technical security standards; TLS certificate evidence; API security documentation; email policy.
- **Common pitfalls**: PII transmitted unencrypted between internal microservices; legacy API connections without TLS; PII emailed in cleartext.
- **Regulatory link**: GDPR Article 32(1)(a) — pseudonymisation and encryption of personal data; Article 28(3)(c) — security measures.

---

### A.2.5 — PII Sharing, Transfer and Disclosure (8 controls)

This domain is critical for B2B processors — it governs your obligations around sub-processors (vendors you use to deliver your service), third-country data transfers, and handling of government access requests.

---

**Control: A.2.5.2 — Basis for PII Transfer**

- **Purpose**: Any transfer of controller PII to a third country or international organization requires a documented legal transfer mechanism.
- **What to implement**: Map all data flows that involve cross-border transfers; document the transfer mechanism for each (Standard Contractual Clauses, adequacy decision, binding corporate rules, or other GDPR Art. 44–49 mechanism); ensure SCCs are signed and up-to-date (2021 EU SCCs where applicable).
- **Evidence for audit**: Transfer impact assessments; executed SCCs; transfer mapping documentation.
- **Common pitfalls**: Data residency commitments made to customers but not technically enforced; sub-processor transfers to non-adequate countries without SCCs.
- **Regulatory link**: GDPR Articles 44–49 — transfers to third countries.

---

**Control: A.2.5.3 — Countries for PII Transfer**

- **Purpose**: Document the countries to which PII is transferred during processing operations, including via sub-processors.
- **What to implement**: Maintain a transfer mapping covering all jurisdictions where customer PII is stored, accessed, or transmitted. Include sub-processor locations. Disclose to controllers.
- **Evidence for audit**: Transfer register; sub-processor location register; DPA disclosures.
- **Common pitfalls**: Cloud provider sub-processor locations not mapped; support access from overseas offices not documented.
- **Regulatory link**: GDPR Article 30(1)(e) — records of processing must include transfers to third countries.

---

**Control: A.2.5.4 — Records of PII Disclosures**

- **Purpose**: Maintain a log of all disclosures of customer PII to third parties.
- **What to implement**: Disclosure log covering all instances where customer PII was shared with a third party; covers both routine disclosures (sub-processors) and exceptional disclosures (law enforcement).
- **Evidence for audit**: Disclosure register; process for logging disclosure events.
- **Common pitfalls**: Ad hoc disclosures not recorded; no distinction between routine sub-processor sharing and exceptional disclosures.
- **Regulatory link**: GDPR Article 5(2) accountability; Article 30 records.

---

**Control: A.2.5.5 — Notification of PII Disclosure Requests**

- **Purpose**: If a government body or law enforcement agency requests disclosure of controller PII, notify the controller before disclosing — unless legally prohibited from doing so (e.g., a gag order).
- **What to implement**: Legal procedure for handling government access requests; default position to notify the controller first and seek legal advice; transparency report process; contractual commitment to controller notification.
- **Evidence for audit**: Legal procedure; DPA clause; records of any notifications made.
- **Common pitfalls**: Automatic compliance with government requests without notifying the controller; no legal review process.
- **Regulatory link**: GDPR Article 28(3)(a) — processor notifies controller if instruction infringes law; general principle of transparency to controllers.

---

**Control: A.2.5.6 — Legally Binding PII Disclosures**

- **Purpose**: Where the processor is legally compelled to disclose PII (e.g., court order, national security), it must notify the controller where legally permitted and record the disclosure.
- **What to implement**: Legal review process; notification to controller procedure; disclosure log; senior management authorization for compelled disclosures.
- **Evidence for audit**: Legal procedure; disclosure records; records of controller notifications.
- **Common pitfalls**: Disclosures made without any notification; no legal review before compliance with informal requests.
- **Regulatory link**: GDPR Article 28(3)(a).

---

**Control: A.2.5.7 — Disclosure of Subcontractors**

This is the first of three controls using ISO 27701's specific **sub-processor** terminology.

- **Purpose**: The processor must disclose to the controller the identity and location of all sub-processors used to deliver the contracted services.
- **What to implement**: Maintain and publish a sub-processor list covering: company name, location (country), processing activities performed. Make this list available to all controllers (typically via DPA, trust page, or contractual schedule). Process for keeping the list current.
- **Evidence for audit**: Published or contractually disclosed sub-processor list; process for maintaining currency; audit trail of sub-processor changes.
- **Common pitfalls**: Sub-processor list not maintained; list not shared with controllers; list outdated when sub-processors change.
- **Regulatory link**: GDPR Article 28(2) — controller must authorize processor's use of sub-processors.

---

**Control: A.2.5.8 — Engagement of a Subcontractor** *(New in 2025 edition)*

ISO 27701 term: **"sub-processor contracts"** — this control governs both the authorization requirement and the contractual obligations that must be flowed down.

- **Purpose**: Before engaging a new sub-processor, the processor must obtain controller authorization. All sub-processors must be engaged under contracts that impose equivalent data protection obligations.
- **What to implement**:
  1. **Authorization process**: Define in DPAs whether you have general or specific authorization for sub-processors. General authorization (most common) requires prior notice and opportunity to object.
  2. **Sub-processor contracts**: Ensure all sub-processor agreements include the same data protection obligations as in your DPA with the controller. If the sub-processor fails to fulfil these obligations, the processor remains fully liable to the controller.
  3. **Contractual audit trail**: Maintain executed sub-processor DPAs for all current sub-processors.
- **Evidence for audit**: Sub-processor DPA template; executed sub-processor agreements; authorization process documentation; DPA clauses covering sub-processor authorization.
- **Common pitfalls**: Engaging new sub-processors (cloud vendors, analytics tools) without controller notification; sub-processor agreements missing data protection clauses; no data protection clauses in cloud provider ToS (use dedicated DPA addenda).
- **Regulatory link**: GDPR Article 28(2) and (4) — sub-processor obligations; Article 28(4) — same obligations imposed on sub-processor as controller imposed on processor.

---

**Control: A.2.5.9 — Change of Subcontractor** *(New in 2025 edition)*

ISO 27701 term: **"sub-processor notification and consent"** — this control specifically addresses the notification obligation when sub-processors change.

- **Purpose**: When the processor intends to replace or add a sub-processor, controllers must be notified in advance and given the opportunity to object before the change takes effect.
- **What to implement**: Prior notification mechanism (email list, trust page notification, contractual notice period — typically 30 days); process for handling controller objections; procedure for managing changes where objections are raised.
- **Evidence for audit**: Notification mechanism (e.g., email notification service, changelog with subscription); records of notifications sent; DPA clause specifying notice period and objection right.
- **Common pitfalls**: Retrospective notification after changes have already taken effect; no mechanism for controllers to object; changes to underlying infrastructure sub-processors (e.g., cloud region changes) not treated as sub-processor changes.
- **Regulatory link**: GDPR Article 28(2) — controller must give specific or general written authorization for sub-processors.

---

## The Three Critical Sub-Processor Controls in Summary

ISO 27701 uses precise terminology that maps directly to audit evidence requests and DPA contractual clauses:

| Obligation | ISO 27701 Term | Control | What It Requires |
|-----------|---------------|---------|-----------------|
| Assist controllers with individual rights requests | "**PII subject rights assistance obligations**" | A.2.3.3 (referenced in A.2.2.6) | Operational process to support controllers' DSR fulfilment within SLAs |
| Notify and obtain consent before using sub-processors | "**sub-processor** notification and consent" | A.2.5.9 / A.2.2.6 | Advance notice to controllers; opportunity to object; prior authorization |
| Written agreements with sub-processors | "**sub-processor** contracts" | A.2.5.8 | Equivalent data protection obligations flowed down to all sub-processors |

---

## Shared Security Controls (A.3) — Processor Relevance

All 29 A.3 controls apply to processors as well as controllers. The most operationally significant for a B2B processor context are:

| Control | Relevance for Processors |
|---------|--------------------------|
| A.3.9 — Access Rights | Least-privilege access to all customer PII systems; regular access reviews |
| A.3.11/A.3.12 — Incident Management | Breach notification process with controller notification SLAs built in |
| A.3.18 — Confidentiality Agreements | All staff with access to customer PII under signed confidentiality obligations |
| A.3.25 — Logging | Access to and processing of customer PII logged and tamper-protected |
| A.3.26 — Cryptography | Encryption at rest and in transit for all customer PII |
| A.3.31 — Test Information | Live customer PII not used in test environments |

---

## Certification Value for B2B Processors

ISO 27701 certification as a PII Processor has significant commercial value in B2B contexts:

1. **Reduces customer audit burden**: Your customers (controllers) can reference your certification as evidence that you meet GDPR Article 28 requirements, reducing the need for individual audit rights exercises.
2. **Differentiator in procurement**: Enterprise procurement processes increasingly require evidence of privacy certifications. ISO 27701 certification satisfies this requirement.
3. **DPA simplification**: Controllers can reference your SoA and certification report rather than requiring detailed contractual annexes on each control.
4. **Regulatory standing**: In a regulatory investigation affecting one of your customers, your ISO 27701 certification provides documented evidence that you implemented appropriate controls as a processor.
