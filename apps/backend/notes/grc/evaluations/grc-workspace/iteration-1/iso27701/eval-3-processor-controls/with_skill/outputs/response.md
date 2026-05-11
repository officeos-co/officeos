# ISO 27701 Privacy Controls for PII Processors

## Your Role as a PII Processor

Since you act as a data processor for your B2B customers, you are a **PII Processor** under ISO 27701 terminology — an organization that processes personally identifiable information on behalf of, and under instructions from, a PII Controller (your customers).

Under ISO 27701:2025, PII Processors must implement:
- **Annex A.2**: 18 PII Processor-specific controls across 4 domains
- **Annex A.3**: 29 Shared information security controls

**Total controls applicable to you: 47** (A.2 + A.3)

Note: Annex A.1 (31 PII Controller controls) does not apply unless you also process personal data for your own purposes (e.g., employee data, your own marketing database). If that is the case, you would also need to implement applicable A.1 controls for those controller activities.

---

## Annex A.2: PII Processor Controls — Full Breakdown

### A.2.2 — Conditions for Collection and Processing

These controls govern the fundamental relationship between processor and controller.

---

**Control A.2.2.1 — Customer Agreements**

- **Purpose**: Ensure processing occurs only within the documented scope of the controller's instructions; prevents unauthorized or excessive processing.
- **What to implement**:
  - Every customer relationship involving personal data must be governed by a signed Data Processing Agreement (DPA).
  - The DPA must specify: subject matter, duration, nature and purpose of processing, type of personal data, categories of data subjects, and controller obligations and rights.
  - Your standard DPA template should be reviewed by legal counsel to ensure it satisfies GDPR Article 28 requirements.
  - Maintain a register of all customer DPAs, including scope, effective date, and review date.
- **Evidence for audit**: Signed DPA with each customer; DPA register; process for ensuring DPAs are in place before processing begins.
- **Common pitfalls**: Processing begins before DPA is signed; DPA scope does not match actual processing; DPA templates are outdated and do not reflect current data flows.
- **Regulatory link**: GDPR Article 28 (Processor obligations).

---

**Control A.2.2.2 — Organisation's Purposes**

- **Purpose**: Ensure personal data is processed only for purposes specified by the controller; no secondary or own-purposes processing.
- **What to implement**:
  - Documented policy stating the organization processes PII only for the purposes specified in customer agreements.
  - Technical controls limiting processing to agreed purposes (e.g., logical separation of customer data sets, access controls preventing cross-customer data use).
  - Staff training on the prohibition against using customer data for own purposes.
- **Evidence for audit**: Written policy; access control configurations; training records.
- **Regulatory link**: GDPR Article 28(3)(a) — process only on documented instructions.

---

**Control A.2.2.3 — Privacy Notice to PII Principals**

- **Purpose**: Support controllers in meeting transparency obligations.
- **What to implement**: As a processor, you support the controller's transparency obligations rather than issuing your own public privacy notice for controller-directed processing. Internally, document that the controller is responsible for privacy notices and that your DPA clarifies this.
- **Note**: You may still need a privacy notice for your own controller activities (e.g., employee data, direct marketing).
- **Regulatory link**: GDPR Articles 13–14 (controller responsibility for notices).

---

**Control A.2.2.4 — Privacy by Design and Default**

- **Purpose**: Embed privacy protections into your services, systems, and processes by default.
- **What to implement**:
  - Privacy reviewed at the design stage of all new products, services, and system changes that will process customer PII.
  - Default settings configured to minimize data collection and access (e.g., minimum data fields, strict default access controls, automatic data purge at contract end).
  - Privacy Design Review as part of the SDLC gate process.
  - Documentation of privacy design decisions for each product/system.
- **Evidence for audit**: SDLC documentation showing privacy review gates; privacy design records; product default configurations.
- **Regulatory link**: GDPR Article 25 (Data protection by design and by default).

---

**Control A.2.2.5 — Assisting the PII Controller**

- **Purpose**: Ensure you can fulfill your contractual and legal obligation to assist controllers when they need to respond to data subject rights requests and other compliance obligations.
- **What to implement**:
  - Documented procedure for receiving and acting on controller instructions regarding data subject rights (access, erasure, rectification, portability, restriction, objection).
  - Response SLAs in your DPA that allow the controller to meet their own 30-day GDPR deadline.
  - Technical capability to export, correct, restrict, and delete individual data records on request.
  - Procedure for assisting with DPIAs when your processing contributes to high-risk activities.
  - Process for notifying controller of privacy incidents and security breaches.
- **Evidence for audit**: DPA clauses covering DSR assistance; technical capability demonstrations; DSR assistance log.
- **Common pitfalls**: DPA requires assistance but technical systems cannot extract individual records without manual intervention; no defined SLA for controller assistance requests.
- **Regulatory link**: GDPR Article 28(3)(e) and (f) — assist with DSRs, security, DPIAs, breach notification.

---

**Control A.2.2.6 — Use of Sub-Processors (Disclosure)**

- **Purpose**: Ensure controllers are informed about and have consented to the use of sub-processors.
- **What to implement**:
  - Maintain a register of all sub-processors and the categories of processing they perform.
  - Your DPA must require controller consent (specific or general) before engaging new sub-processors.
  - Procedure for notifying controllers of intended sub-processor changes in advance, giving controllers an opportunity to object.
  - Written notification process (typically 30 days' advance notice) embedded in DPA.
- **Evidence for audit**: Sub-processor register; DPA clauses on sub-processor notification; notification records sent to customers.
- **Regulatory link**: GDPR Article 28(2) — sub-processor consent requirement.

---

**Control A.2.2.7 — Processor Records of Processing Activities (RoPA)**

- **Purpose**: Maintain a documented inventory of all processing activities conducted on behalf of controllers.
- **What to implement**:
  - A formal Processor RoPA covering all processing activities performed for each controller.
  - Required fields: Controller name, contact details, categories of processing, third countries transferred to, transfer safeguards, description of technical and organisational security measures.
  - RoPA must be kept up to date — reviewed when new customer services are onboarded or existing services change.
  - RoPA must be available to supervisory authorities on request.
- **Evidence for audit**: Completed Processor RoPA; evidence of last review date; process for maintaining currency.
- **Regulatory link**: GDPR Article 30(2) — Processor records of processing activities.

---

**Control A.2.2.8 — PII Principal Rights (Handling Assistance)**

Covered under A.2.2.5 above. Extends to ensuring the technical and operational capability to respond to every DSR type relevant to your processing activities.

---

### A.2.3 — Privacy by Design

**Control A.2.3.1 — Limit Processing to Purpose**

- **Purpose**: Ensure personal data processed for the controller is not used beyond the scope specified.
- **What to implement**: Technical controls (logical separation, role-based access, data tagging) ensuring customer data cannot be accessed or used beyond the agreed processing purpose. Regular audits of access logs to detect unauthorized use.
- **Regulatory link**: GDPR Article 5(1)(b) — purpose limitation.

**Control A.2.3.2 — Minimisation**

- **Purpose**: Ensure only the minimum personal data necessary for the agreed purpose is collected and retained.
- **What to implement**: Review data fields collected in your service against the stated purpose — remove unnecessary fields. Automatic purge rules for data beyond retention periods. Anonymisation/pseudonymisation where full identification is not required.
- **Regulatory link**: GDPR Article 5(1)(c) — data minimisation.

---

### A.2.4 — Privacy by Default

**Control A.2.4.1 — Privacy Configuration**

- **Purpose**: Ensure your services default to the most privacy-protective settings.
- **What to implement**: Audit all customer-facing configuration options — default settings must represent minimum data exposure. Users (controllers) must affirmatively opt in to enable expanded data collection, not opt out. Document default settings for each product with privacy justification.
- **Regulatory link**: GDPR Article 25(2) — data protection by default.

---

### A.2.5 — Sub-Processor Management

**Control A.2.5.7 — Engaging Sub-Processors**

- **Purpose**: Ensure sub-processors are subject to appropriate privacy controls.
- **What to implement**:
  - A formal sub-processor due diligence process before engagement (privacy and security questionnaire, review of their own DPA terms).
  - Preference for ISO 27701-certified or SOC 2 Type II-certified sub-processors.
  - Approval process (documented sign-off) before engaging any new sub-processor.
- **Evidence for audit**: Sub-processor assessment records; due diligence questionnaire; approval sign-off records.
- **Regulatory link**: GDPR Article 28(2) — processor accountability for sub-processors.

**Control A.2.5.8 — Sub-Processor Contracts**

- **Purpose**: Ensure sub-processors are contractually bound to the same data protection obligations as you are.
- **What to implement**:
  - Written Data Processing Agreement with every sub-processor imposing at minimum the same obligations you carry under your customer DPAs.
  - Sub-processor DPAs reviewed regularly and updated when your customer DPA obligations change.
  - Sub-processor DPA register.
- **Evidence for audit**: Signed sub-processor DPAs; DPA clause comparison showing equivalent obligations; review records.
- **Regulatory link**: GDPR Article 28(4) — sub-processors bound by equivalent obligations.

**Control A.2.5.9 — Sub-Processor Location and Transfers**

- **Purpose**: Ensure data transferred to sub-processors in third countries (outside EEA) is covered by appropriate transfer mechanisms.
- **What to implement**:
  - Document the country location of every sub-processor.
  - For sub-processors outside the EEA: implement transfer mechanisms (Standard Contractual Clauses, adequacy decision, BCRs).
  - Transfer mechanisms documented in your Processor RoPA and disclosed to controllers.
- **Regulatory link**: GDPR Articles 44–49 — international transfer requirements.

---

### A.2.6 — Return, Transfer, and Disposal of PII

**Control A.2.6.1 — Return or Destruction at Contract End**

- **Purpose**: Ensure customer data is returned or securely destroyed at the end of the service contract.
- **What to implement**:
  - DPA clause specifying the process and timeline for data return or deletion at contract end.
  - Documented deletion/destruction procedure with a defined secure method (e.g., cryptographic erasure, secure overwrite).
  - Deletion certificate provided to customer upon request.
  - Any retention beyond contract end (e.g., for legal obligation) documented and disclosed.
- **Evidence for audit**: DPA data deletion clause; deletion procedure document; deletion certificates; audit logs of deletion actions.
- **Regulatory link**: GDPR Article 28(3)(g) — delete or return PII at end of contract.

---

### A.2.7 — Audit and Compliance

**Control A.2.7.1 — Cooperation with Controllers on Audits**

- **Purpose**: Fulfill the legal obligation to allow controllers to audit your processing activities.
- **What to implement**:
  - DPA clause granting controllers audit rights (direct audit or via third-party auditor).
  - Defined audit process — notice period, scope limits, confidentiality, format.
  - Alternative: provide ISO 27701 certification and audit reports as a substitute for direct audits (widely accepted in practice).
  - Designated point of contact for audit requests.
- **Evidence for audit**: DPA audit clause; audit cooperation records; certification reports provided to customers.
- **Regulatory link**: GDPR Article 28(3)(h) — make available all information necessary to demonstrate compliance and allow for audits.

---

## Annex A.3: Shared Security Controls (29 controls)

In addition to the A.2 processor-specific controls, you must implement the 29 shared security controls in Annex A.3. These cover:

- **Access control**: User access management, privileged access, authentication
- **Cryptography**: Encryption of PII at rest and in transit
- **Physical security**: Protecting physical environments containing PII
- **Operations security**: Malware protection, logging, vulnerability management
- **Communications security**: Network controls, secure transfer
- **Supplier security**: Managing security in supplier relationships
- **Incident management**: Privacy incident detection, response, and notification (A.3.11–A.3.12 — critical for breach notification)
- **Business continuity**: Availability of PII processing systems

---

## Priority Implementation Order for a PII Processor

| Priority | Control Area | Rationale |
|----------|-------------|-----------|
| 1 | A.2.2.1 — Customer DPAs | Foundation of all processor obligations |
| 2 | A.2.2.7 — Processor RoPA | Mandatory record; required by GDPR Art. 30 |
| 3 | A.2.5.7–A.2.5.9 — Sub-processor management | High regulatory risk if unmanaged |
| 4 | A.2.2.5 — DSR assistance capability | Customer SLA dependency |
| 5 | A.3.11–A.3.12 — Incident response | Breach notification obligations |
| 6 | A.2.6.1 — Data return/deletion | Contract end compliance |
| 7 | A.2.2.4 / A.2.3 / A.2.4 — PbD and minimisation | Systemic privacy embedding |
| 8 | A.2.7.1 — Audit cooperation | Customer audit rights |

---

## Statement of Applicability Note

As a PII Processor only, your SoA covers **47 controls** (A.2 + A.3). Any A.1 controller controls are **not applicable** and should be documented as excluded in the SoA with justification: "Organization acts solely as PII Processor for this scope; controller obligations do not apply." If you process employee data as a controller, a separate SoA section (or separate PIMS scope) may be needed for those controller activities.
