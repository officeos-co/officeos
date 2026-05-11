# ISO 27701 Privacy Controls for Data Processors

## Introduction

ISO 27701 contains a dedicated set of controls for organizations acting as PII processors — that is, organizations that process personal data on behalf of and under the instruction of PII controllers (their B2B customers). These controls are contained in **Annex B** of the standard (formally titled "ISO/IEC 27701 Annex B — Additional ISO/IEC 27002 guidance for PII processors").

All Annex B controls are organized under the parent control domain reference 8 (B.8.x). In addition, processors must also implement the shared controls in the main clauses of ISO 27701 (Clauses 4–10), and they are subject to a subset of Annex A controls that apply in dual-role situations.

The key principle underlying all processor controls is that a PII processor must only process PII in accordance with the documented instructions of the controller. The processor has limited discretion and exists to serve the controller's purposes, not its own.

---

## Annex B Controls in Detail

### B.8.1 — Conditions for Collection and Processing

**B.8.1.1 — Customer Agreement**

The processor must have a documented agreement (contract) with each PII controller before processing begins. This is the foundational control. The agreement must specify:

- The subject matter and duration of the processing
- The nature and purpose of the processing
- The type of PII and categories of PII principals involved
- The obligations and rights of the controller
- Instructions for what the processor may and may not do with the PII

**Practical implementation:**
- Establish a standard Data Processing Agreement (DPA) template
- Ensure all B2B customer relationships have a signed DPA in place
- Maintain a register of all active DPAs
- Review DPAs when processing activities change

**B.8.1.2 — Organization's Purposes**

The processor must not use PII obtained in the course of providing services to the controller for its own purposes (e.g., analytics, product improvement, marketing) unless the controller explicitly instructs or permits this. The processor must be able to demonstrate that processing is strictly limited to the controller's instructions.

**Practical implementation:**
- Implement technical controls that prevent unauthorized secondary use of customer PII
- Ensure internal policies prohibit use of customer data for internal analytics, model training, or other purposes without explicit controller authorization
- Conduct regular audits to verify compliance

**B.8.1.3 — Marketing and Advertising**

The processor must not use PII processed under a controller agreement for direct marketing or advertising purposes unless explicitly authorized by the controller. This is a specific instantiation of B.8.1.2 targeting the advertising/marketing use case.

**Practical implementation:**
- Maintain documented prohibitions in internal policy and procedure
- Ensure marketing and product teams understand restrictions on customer data
- Implement data segregation between customer PII environments and internal marketing systems

**B.8.1.4 — Infringing Instructions**

The processor must have a process for identifying when controller instructions may violate applicable privacy law. When the processor identifies potentially infringing instructions, it must inform the controller and may refuse to carry out the instructions.

**Practical implementation:**
- Train account management and operations staff to recognize potentially unlawful instructions
- Establish an escalation path (e.g., to legal/privacy team) when questionable instructions are received
- Document instances of flagged instructions and resolutions
- Include contractual provisions giving the processor the right to refuse unlawful instructions

---

### B.8.2 — Obligations to PII Principals

**B.8.2.1 — Obligation to PII Principals**

Processors must support controllers in meeting their obligations to PII principals (data subjects). Where a data subject contacts the processor directly with a rights request (e.g., requesting deletion of their data), the processor must either:
- Redirect the individual to the controller, or
- Notify the controller of the request and await further instructions

The processor must implement technical and organizational measures that enable controllers to fulfill data subject rights requests.

**Practical implementation:**
- Implement technical capabilities for data subject rights fulfillment: search, export, delete, restrict processing
- Establish an internal process for handling misdirected data subject requests
- Document SLAs for responding to controller instructions related to rights fulfillment
- Test data subject rights fulfillment capabilities regularly

---

### B.8.3 — Privacy by Design and Privacy by Default

**B.8.3.1 — Limits on Collection**

Processors must collect only the PII that is necessary for the service being provided. Excess collection — even if the controller does not explicitly prohibit it — violates the data minimization principle.

**Practical implementation:**
- Review data collection points in your platform/service to ensure only necessary PII is collected or accessed
- Implement configuration options that limit data collection by default
- Document the minimum necessary data for each service tier

**B.8.3.2 — Limits on Processing**

Processing activities must be limited to what is necessary and must not exceed the scope of controller instructions. This applies to internal access (who within the processor organization can access PII), processing frequency, and the nature of processing operations.

**Practical implementation:**
- Implement role-based access controls (RBAC) limiting employee access to customer PII
- Log all access to customer PII environments
- Conduct periodic access reviews

---

### B.8.4 — Privacy Impact Assessment Involvement

While DPIA/PIA obligations rest primarily with the controller, processors must provide assistance to controllers conducting PIAs/DPIAs where the processing involves the processor's systems or services. Processors should:

- Provide documentation of their processing activities and controls upon request
- Respond to controller DPIA questionnaires in a timely manner
- Flag to controllers when new features or services may materially change the risk profile of PII processing

**Practical implementation:**
- Prepare and maintain a standard "processor security and privacy questionnaire" response
- Maintain a library of completed third-party assessments (SOC 2, ISO 27001/27701 reports) that can be shared with controllers
- Establish a process for notifying controllers of material changes to processing

---

### B.8.5 — Privacy Architecture and Technical Measures

**B.8.5.1 — Data Masking**

Where technically feasible, processors should implement data masking, pseudonymization, or anonymization to minimize the exposure of PII within their systems. This includes masking PII in logs, development environments, and non-production systems.

**B.8.5.2 — Encryption**

PII must be encrypted both in transit (e.g., TLS 1.2+) and at rest. Encryption key management must be documented and audited.

**B.8.5.3 — De-identification**

Where processing does not require identification of PII principals, the processor should apply de-identification techniques.

**Practical implementation:**
- Enforce encryption-at-rest for all storage containing customer PII (database encryption, file system encryption)
- Enforce TLS for all data in transit
- Implement pseudonymization in analytics, logging, and test environments
- Document encryption standards in a data security policy

---

### B.8.6 — Contracts with Sub-Processors

**B.8.6.1 — Sub-Processor Agreements**

If the processor engages sub-processors (i.e., third parties who process PII on the processor's behalf), it must:

- Obtain prior written authorization from the controller (specific or general)
- Impose equivalent data protection obligations on sub-processors as are imposed on the processor itself
- Remain fully liable to the controller for sub-processor compliance

**Practical implementation:**
- Maintain a register of all sub-processors and the categories of PII they access
- Establish a standard sub-processor DPA template that flows down controller obligations
- Notify controllers of sub-processor changes (additions or replacements) in advance
- Conduct due diligence on sub-processors (security reviews, certifications)

**B.8.6.2 — Disclosure of Sub-Processors**

Controllers must be able to obtain a current list of sub-processors upon request. Many organizations publish this as a publicly accessible sub-processor list, updated as changes occur.

**Practical implementation:**
- Publish and maintain a sub-processor list (publicly or on request)
- Establish notification mechanisms (e.g., email subscription, webpage updates) for sub-processor changes
- Provide reasonable notice periods before changes take effect (typically 10–30 days)

---

### B.8.7 — International Transfers

If PII is transferred to countries or regions outside the controller's jurisdiction (and that jurisdiction has transfer restrictions, such as GDPR's Chapter V), the processor must:

- Ensure that appropriate transfer mechanisms are in place (Standard Contractual Clauses, Binding Corporate Rules, adequacy decisions, etc.)
- Disclose to controllers where data may be processed geographically
- Not transfer PII to unauthorized jurisdictions without controller consent

**Practical implementation:**
- Map all geographic locations where customer PII may be processed (including sub-processors)
- Establish SCCs or equivalent mechanisms for cross-border transfers
- Maintain data residency documentation for customer-facing documentation

---

### B.8.8 — Records of PII Processing (Record of Processing Activities)

**B.8.8.1 — Records of Processing**

ISO 27701 Annex B.8.9 (in some editions referenced as B.8.8) requires processors to maintain a Record of Processing Activities (RoPA) from the processor's perspective. This must document:

- The name and contact details of the processor and (where applicable) sub-processors
- Categories of processing carried out on behalf of each controller
- Transfers to third countries
- A general description of technical and organizational security measures

**Practical implementation:**
- Create and maintain a processor RoPA in a structured format (spreadsheet or GRC tool)
- Update the RoPA when new controllers are onboarded or processing activities change
- Make the RoPA available to regulators upon request

---

### B.8.9 — PII Security (Linked to ISO 27001 Controls)

Processors must implement appropriate technical and organizational security measures to protect PII. ISO 27701 does not enumerate entirely new security controls here — it relies on the security controls already required by ISO 27001 — but adds the following specific requirements in the privacy context:

- Security controls must be proportionate to the risk to PII principals
- Security measures must be documented and demonstrably implemented
- Processors must support controllers in meeting their security obligations under applicable law (e.g., GDPR Art. 32)

**Key security controls applicable to processors:**
- Access control and authentication (MFA, least privilege)
- Vulnerability management
- Logging and monitoring
- Incident response
- Business continuity and disaster recovery
- Physical and environmental security

---

### B.8.10 — Data Breach Notification

**B.8.10.1 — Breach Notification to Controller**

Upon discovering a PII breach, the processor must notify the affected controller(s) without undue delay. The notification must include:
- Description of the nature of the breach
- Categories and approximate number of PII principals affected
- Categories and approximate number of PII records affected
- Likely consequences of the breach
- Measures taken or proposed to address the breach

The processor notification to the controller must be timely enough to enable the controller to meet its own regulatory notification obligations (e.g., within 72 hours to the supervisory authority under GDPR Art. 33). In practice, many DPAs specify a 24–48 hour notification requirement from processor to controller.

**Practical implementation:**
- Establish a Privacy Incident Response Plan that includes a specific workflow for customer (controller) notification
- Define a single point of contact for breach notifications (e.g., security team or DPO)
- Maintain a breach notification template for rapid deployment
- Test breach notification procedures in tabletop exercises

---

### B.8.11 — Engagement with Supervisory Authorities

Processors may be directly contacted by supervisory authorities during investigations. Processors must:
- Cooperate with supervisory authorities as required by law
- Notify the relevant controller when a supervisory authority makes inquiries relating to the controller's PII processing
- Not disclose controller-confidential information to supervisory authorities beyond what is legally required

---

### B.8.12 — Return or Deletion of PII on Contract Termination

Upon termination of the contract with a controller, the processor must:
- Return all PII to the controller in a usable format, or
- Securely destroy/delete all PII held on behalf of the controller
- Provide evidence of deletion or return upon request
- Ensure sub-processors do the same

**Practical implementation:**
- Include data return/deletion provisions in all DPAs
- Establish a documented off-boarding process for departing customers
- Use certified deletion tools and maintain deletion certificates
- Define a post-termination retention period (if applicable) and ensure it is disclosed to controllers

---

## Shared Clause Requirements (Clauses 4–10)

In addition to Annex B controls, processors must comply with the general management system requirements of ISO 27701:

| Clause | Requirement |
|---|---|
| 4 | Identify applicable privacy law, identify controllers, define context |
| 5 | Leadership commitment to privacy, privacy policy, privacy roles (e.g., DPO if required) |
| 6 | Privacy risk assessment covering processor activities; objectives for privacy |
| 7 | Competence and training for privacy; privacy awareness |
| 8 | Operational planning and control of PII processing activities |
| 9 | Monitoring and measuring privacy performance; internal audit; management review |
| 10 | Corrective action for nonconformities including privacy incidents |

---

## Implementation Priorities for a B2B Processor

For an organization acting exclusively as a B2B data processor, the highest-priority implementation actions are:

1. **DPA template and contract review:** Ensure all customer contracts contain compliant DPA terms.
2. **Sub-processor management:** Build a sub-processor register and notification process.
3. **Breach notification:** Implement a clear, fast-path notification process to controllers.
4. **Data subject rights technical capabilities:** Build search, export, and deletion capabilities into your platform.
5. **RoPA:** Document all processing activities by customer.
6. **Data return/deletion:** Establish an off-boarding process with documented deletion.
7. **Access controls and encryption:** Harden technical controls for PII environments.
8. **Staff training:** Train all customer-facing and technical staff on processor obligations.

---

## Conclusion

ISO 27701 Annex B provides a comprehensive and auditable control set for PII processors. For a B2B processor, implementing these controls demonstrates to customers and regulators that PII is handled responsibly, supports customer GDPR compliance programs, and provides a defensible evidence base in the event of a regulatory investigation or contractual audit. Many enterprise B2B customers now require ISO 27701 certification as a vendor onboarding prerequisite, making certification a commercial differentiator as well as a compliance tool.
