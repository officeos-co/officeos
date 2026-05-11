# ISO 27701 and Its Relationship to ISO 27001

## What Is ISO 27701?

ISO 27701 (formally ISO/IEC 27701) is an international standard for **privacy information management**. Published by the International Organization for Standardization (ISO) and the International Electrotechnical Commission (IEC), it provides a framework that organizations can use to establish, implement, maintain, and continually improve a Privacy Information Management System (PIMS).

The standard addresses how organizations should manage personally identifiable information (PII) and helps demonstrate compliance with global privacy regulations such as the GDPR.

---

## How ISO 27701 Extends ISO 27001

ISO 27701 was originally designed as an **extension to ISO 27001**, the widely adopted Information Security Management System (ISMS) standard. This means:

1. **Shared management system structure**: ISO 27701 uses the same Plan-Do-Check-Act cycle and high-level structure as ISO 27001, covering the same management system clauses (context, leadership, planning, support, operations, performance evaluation, and improvement).

2. **Extended scope**: Where ISO 27001 focuses on information security — protecting the confidentiality, integrity, and availability of information assets — ISO 27701 adds a privacy layer focused specifically on protecting personally identifiable information (PII) and the rights of individuals whose data is processed.

3. **Additional controls**: ISO 27701 introduces two sets of privacy-specific controls:
   - Controls for **PII Controllers** (organizations that determine why and how personal data is processed)
   - Controls for **PII Processors** (organizations that process data on behalf of controllers)

4. **Privacy risk management**: ISO 27701 requires organizations to assess and treat privacy risks — not just information security risks — with a focus on potential harm to individuals.

---

## Key Differences: ISO 27001 vs. ISO 27701

| Aspect | ISO 27001 | ISO 27701 |
|--------|-----------|-----------|
| Focus | Information security (CIA triad) | Privacy of personally identifiable information |
| Risk perspective | Organizational harm | Harm to individuals (data subjects) |
| Regulatory alignment | General security best practice | GDPR, CCPA, and other privacy laws |
| Controls | Annex A security controls | Privacy-specific controls for controllers and processors |
| Certification | Standalone | Extends ISO 27001 (traditionally) |

---

## Additional Work Required for ISO 27701 Certification

Since you are already ISO 27001 certified, you have a significant foundation. The additional work falls into several categories:

### 1. Extend Your Management System Scope
- Update your ISMS scope document to explicitly include privacy and PII processing.
- Identify all processing activities involving personal data and map data flows.
- Determine whether you act as a controller, processor, or both — this affects which controls apply.

### 2. Develop a Privacy Policy
- Create a formal privacy policy that addresses PII processing principles, signed by top management.
- Ensure it covers purposes of processing, legal basis, data subject rights, and accountability.

### 3. Privacy Risk Assessment
- Conduct a privacy-specific risk assessment. Unlike information security risk assessment (which focuses on threats to organizational assets), privacy risk assessment focuses on potential harm to individuals.
- Document risks, assess likelihood and severity of harm to data subjects, and develop a treatment plan.

### 4. Statement of Applicability Extension
- Your existing ISO 27001 SoA will need to be extended (or a separate SoA created) to cover all privacy controls.
- Document which controller and/or processor controls apply, which are excluded, and why.

### 5. Implement Privacy Controls
Key areas that will require new or significantly enhanced controls include:

- **Records of Processing Activities (RoPA)**: A comprehensive inventory of all processing activities, purposes, data types, retention periods, and third-party recipients.
- **Data Subject Rights procedures**: Documented processes for handling access requests, erasure requests, rectification, data portability, and objection — with defined response timelines.
- **Privacy Notices**: Transparent, accessible notices telling individuals how their data is used.
- **Consent management**: Where consent is the legal basis for processing, mechanisms to capture, record, and withdraw consent.
- **Data Processing Agreements**: Contracts with all third-party processors that include required privacy terms.
- **Privacy by Design**: Integrating privacy considerations into system development and business processes from the start.
- **Data Protection Impact Assessments (DPIAs)**: Formal assessments for high-risk processing activities.
- **International data transfer mechanisms**: Documented legal mechanisms for transferring data outside the EEA (Standard Contractual Clauses, adequacy decisions, etc.).
- **Privacy incident response**: Procedures for detecting, containing, and notifying data breaches, including regulatory notification timelines.

### 6. Appoint a Privacy Lead / DPO
- Designate a Data Protection Officer or privacy lead with clear authority and resources.
- Document their role, responsibilities, and reporting lines.

### 7. Privacy Training and Awareness
- Extend your existing security awareness programme to include privacy-specific training covering PII handling, data subject rights, and breach reporting.

### 8. Extend Internal Audit and Management Review
- Update your internal audit programme to include privacy controls and PIMS-specific requirements.
- Add privacy agenda items to management review meetings (privacy KPIs, incident trends, DSR volumes).

---

## Estimated Timeline

For an organization already ISO 27001 certified with some existing privacy controls:

- **Well-established privacy programme**: 3–6 months to certification
- **Limited existing privacy controls**: 9–18 months to certification

The biggest effort typically comes from building the Records of Processing Activities, implementing Data Subject Rights procedures, and conducting DPIAs for high-risk processing activities.

---

## Summary

ISO 27701 builds directly on your ISO 27001 foundation by adding a privacy management layer. The management system infrastructure (policies, risk management, audits, management review, corrective action) can be largely reused, but significant new work is required to address privacy-specific controls — particularly around data subject rights, records of processing, privacy by design, and data protection impact assessments. The certification process typically involves a combined audit covering both your ISMS and PIMS.
