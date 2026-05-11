# ISO 27701 and Its Extension of ISO 27001

## What Is ISO 27701?

ISO/IEC 27701:2019 is an international privacy information management standard published by the International Organization for Standardization (ISO) and the International Electrotechnical Commission (IEC). It is formally titled "Security techniques — Extension to ISO/IEC 27001 and ISO/IEC 27002 for privacy information management — Requirements and guidelines."

ISO 27701 establishes requirements and provides guidance for a Privacy Information Management System (PIMS). It is designed to help organizations manage personal data (referred to in the standard as Personally Identifiable Information, or PII) in a structured, risk-based manner. It applies to organizations that act as PII controllers, PII processors, or both.

The standard was developed in part to provide a privacy-specific framework that could be mapped to global privacy regulations such as the EU General Data Protection Regulation (GDPR), and it serves as a bridge between information security management and privacy management.

---

## How ISO 27701 Extends ISO 27001

ISO 27701 is explicitly designed as an extension to ISO 27001 — it cannot stand alone. It extends both ISO 27001 (requirements) and ISO 27002 (controls guidance) in the following ways:

### 1. Scope Extension
ISO 27001 focuses on protecting the confidentiality, integrity, and availability of information assets in general. ISO 27701 narrows and deepens this focus specifically on the processing of PII, incorporating privacy principles such as purpose limitation, data minimization, and individual rights management.

### 2. Additional Requirements to Clause Structure
ISO 27701 maps to the same high-level structure (HLS/Annex SL) as ISO 27001. For each clause of ISO 27001 (Clauses 4–10), ISO 27701 adds privacy-specific requirements:

- **Clause 4 (Context):** Organizations must additionally identify PII principals (data subjects), PII controllers and processors relevant to the organization, and applicable privacy legislation and regulation.
- **Clause 5 (Leadership):** Top management must demonstrate commitment to privacy, including establishing a privacy policy and assigning privacy roles (e.g., a Privacy Officer or equivalent).
- **Clause 6 (Planning):** Risk assessments must be extended to cover privacy risks, including risks to PII principals. Privacy objectives must be established.
- **Clause 7 (Support):** Awareness and competence requirements extend to privacy-specific knowledge.
- **Clause 8 (Operation):** Operational controls must address the full PII processing lifecycle.
- **Clause 9 (Performance Evaluation):** Monitoring and auditing must cover privacy management performance.
- **Clause 10 (Improvement):** Nonconformities must be assessed for privacy impact; corrective actions must address privacy breaches.

### 3. Additional Control Sets (Annexes)
ISO 27701 introduces new privacy-specific control sets in its annexes, which supplement the controls in ISO 27002 Annex A:

- **Annex A:** Additional controls for PII controllers
- **Annex B:** Additional controls for PII processors
- **Annex C:** Mapping to ISO 29100 (Privacy Framework)
- **Annex D:** Mapping to GDPR
- **Annex E:** Mapping to ISO 29151
- **Annex F:** Mapping to ISO 27018

These annexes provide specific guidance on obligations such as consent management, data subject rights, purpose limitation, data minimization, third-party sharing, and cross-border data transfers.

---

## Additional Work Required for ISO 27701 Certification (Given Existing ISO 27001 Certification)

Being ISO 27001 certified provides a strong foundation, as ISO 27701 inherits and builds on the entire ISO 27001 management system. However, significant additional work is still required. Below is a structured breakdown.

### 1. Gap Assessment
Conduct a formal gap assessment comparing your current ISMS against ISO 27701 requirements. This should cover:
- All extended clause requirements (4–10)
- All applicable Annex A controls (if you are a PII controller)
- All applicable Annex B controls (if you are a PII processor)
- Both sets if you act in dual capacity

### 2. Scope Definition for PIMS
Define the scope of your Privacy Information Management System (PIMS). This should specify:
- Which systems and processes involve PII processing
- The categories of PII principals (e.g., customers, employees, users)
- Whether the organization acts as a controller, processor, or both
- Applicable privacy regulations (e.g., GDPR, CCPA, LGPD)

### 3. Privacy Policy Development
Develop or update a privacy policy that is distinct from (but aligned with) your information security policy. This policy must reflect the organization's commitment to PII protection, articulate privacy principles, and be approved by top management.

### 4. PII Inventory and Data Mapping
Create or formalize a comprehensive PII inventory (sometimes called a data map or Record of Processing Activities under GDPR). This should document:
- What PII is collected and processed
- Purposes of processing
- Legal bases for processing
- Retention periods
- Data flows (including third-party transfers)
- Categories of PII principals

### 5. Extended Privacy Risk Assessment
Extend your existing ISO 27001 risk assessment methodology to include:
- Privacy-specific threats and vulnerabilities
- Risks to PII principals (not just the organization)
- Assessment of likelihood and impact from the perspective of data subjects
- Privacy risk treatment options

### 6. PII Controller Controls (Annex A)
If you act as a PII controller, implement the Annex A controls, which include:
- **A.7.2:** Identifying lawful bases for PII processing
- **A.7.3:** Determining when and how consent is obtained
- **A.7.4:** Privacy notice/transparency requirements
- **A.7.5:** Providing privacy choices and opt-out mechanisms
- **A.7.6–7.8:** PII principal rights (access, correction, deletion, portability, objection)
- **A.7.9:** PII principal complaint handling
- **A.7.10:** Automated decision-making controls
- **A.7.11:** Data minimization controls
- **A.7.12:** Purpose limitation and de-identification
- **A.7.13–7.14:** Data accuracy and retention/disposal
- **A.7.15–7.16:** Cross-border transfer restrictions and controls
- **A.7.17:** Disclosure of PII to third parties

### 7. PII Processor Controls (Annex B)
If you act as a PII processor, implement the Annex B controls, including:
- **B.8.1:** Agreements with PII controllers (contract terms)
- **B.8.2:** Ensuring purpose limitation as instructed
- **B.8.3:** Marketing and advertising restrictions
- **B.8.4:** Infringing instructions handling
- **B.8.5:** PII principal rights support for the controller
- **B.8.6:** Disclosure restrictions
- **B.8.7–8.8:** Sub-processing controls
- **B.8.9:** Record of PII processing activities
- **B.8.10–8.11:** PII security controls, data breach notifications to controllers
- **B.8.12:** Data return and deletion upon contract termination

### 8. Third-Party and Supply Chain Privacy Management
Extend your supplier/vendor management procedures to include privacy requirements. Ensure that:
- Data processing agreements (DPAs) are in place with all sub-processors and suppliers
- Privacy obligations flow down through the supply chain
- Third-party privacy performance is monitored

### 9. Data Subject Rights Procedures
Develop formal, documented procedures for handling data subject rights requests, including:
- Access requests (Subject Access Requests)
- Rectification requests
- Erasure requests (right to be forgotten)
- Data portability requests
- Objection and restriction of processing requests
- Defined response timelines aligned with applicable law

### 10. Consent Management
Implement mechanisms for obtaining, recording, and managing consent where it is the legal basis for processing. This includes:
- Granular consent capture
- Withdrawal of consent mechanisms
- Audit trails of consent

### 11. Privacy by Design and by Default
Embed privacy considerations into the design of systems, products, and processes. Document how privacy-by-design principles are applied during system development and procurement (often linked to your SDLC process).

### 12. Privacy Impact Assessments (PIAs / DPIAs)
Establish a formal PIA/DPIA methodology and process. Define triggers, methodology, documentation requirements, and review cadence. ISO 27701 requires that high-risk processing activities be subject to privacy risk assessment.

### 13. Data Breach Notification Procedures
Update or create incident response procedures specifically for PII breaches, including:
- Internal escalation paths
- Notification timelines to regulators (if applicable)
- Notification obligations to affected individuals
- Coordination with PII controllers (if acting as processor)

### 14. Training and Awareness
Develop and deliver privacy-specific training for employees, including:
- Privacy principles and the organization's obligations
- Handling of PII in daily operations
- Recognizing and reporting privacy incidents
- Data subject rights handling

### 15. Statement of Applicability (SoA) Extension
Extend your existing ISO 27001 Statement of Applicability to include ISO 27701 controls (Annex A and/or B), with justifications for inclusion or exclusion of each control.

### 16. Internal Audit Extension
Extend your internal audit program to include ISO 27701 requirements and controls. Auditors must have privacy-specific competence.

### 17. Management Review Extension
Update your management review agenda to include privacy management performance, including PII breach statistics, data subject rights metrics, and privacy risk status.

### 18. Certification Audit
Engage an accredited certification body (CB) that offers ISO 27701 certification. The audit will typically be conducted in conjunction with an ISO 27001 surveillance or recertification audit, though it can also be conducted separately. The CB will perform:
- Stage 1: Documentation review
- Stage 2: On-site/remote audit of implementation effectiveness

---

## Summary Timeline and Effort Estimate

| Phase | Activity | Typical Effort |
|---|---|---|
| 1 | Gap assessment | 2–4 weeks |
| 2 | Scope definition and PII inventory | 4–8 weeks |
| 3 | Policy and procedure development | 4–8 weeks |
| 4 | Control implementation (Annex A/B) | 8–16 weeks |
| 5 | Risk assessment extension | 2–4 weeks |
| 6 | Training and awareness | Ongoing |
| 7 | Internal audit | 2–4 weeks |
| 8 | Management review | 1 week |
| 9 | Certification audit | As scheduled with CB |

Organizations with a mature ISO 27001 ISMS typically require 6–12 months of additional work to achieve ISO 27701 certification, depending on the size of the organization, complexity of PII processing activities, and readiness of privacy practices.

---

## Key Takeaway

ISO 27701 does not replace ISO 27001 — it extends it. Your existing ISMS provides the management system foundation (policies, risk assessment, internal audit, management review, corrective action), and ISO 27701 adds a privacy-specific layer on top. The incremental investment is significant but leverages everything already in place.
