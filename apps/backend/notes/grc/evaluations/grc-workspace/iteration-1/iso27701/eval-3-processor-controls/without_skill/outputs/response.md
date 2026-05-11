# ISO 27701 Privacy Controls for Data Processors

## Your Role as a Data Processor

Since you act as a data processor for your B2B customers, ISO 27701 classifies you as a **PII Processor** — an organization that processes personally identifiable information on behalf of and under the instructions of another organization (the PII Controller, i.e., your customers).

ISO 27701 recognizes this distinction explicitly and provides a separate set of controls that apply specifically to processors. You would not be required to implement all of the controller-specific controls, but you would need to implement both the processor-specific controls and the shared security controls.

---

## Two Sets of Controls Apply to PII Processors

### 1. PII Processor-Specific Controls

These are the controls uniquely applicable to organizations acting as processors. They govern how you process data on behalf of your customers.

### 2. Shared Information Security Controls

These apply to both controllers and processors and address the broader security environment in which personal data is processed.

---

## Key PII Processor Controls

### Customer Agreements and Instructions

ISO 27701 requires that all PII processing you perform for customers must be governed by a written agreement — typically a Data Processing Agreement (DPA). The DPA must specify:

- The nature, purpose, and duration of processing
- The types of personal data being processed
- The categories of data subjects
- Your obligations and rights as processor
- The controller's obligations and rights

You must process personal data only in accordance with the documented instructions in this agreement. Processing beyond the agreed scope is prohibited.

### Records of Processing Activities

As a processor, you must maintain a Record of Processing Activities (RoPA) that documents:

- The name and contact details of each controller on whose behalf you process
- The categories of processing carried out for each controller
- Any transfers of personal data to third countries and the safeguards applied
- A general description of the technical and organisational security measures

This is separate from any RoPA your customers maintain as controllers.

### Sub-Processor Management

If you engage any third-party sub-processors (e.g., cloud hosting providers, analytics platforms) that have access to customer personal data, you must:

- Obtain prior authorization from customers (either specific consent for named sub-processors or general authorization with notification of changes)
- Conduct due diligence on sub-processors' privacy and security practices
- Enter into written agreements with sub-processors that impose equivalent data protection obligations to those in your customer DPAs
- Maintain a register of all sub-processors
- Notify customers of intended sub-processor changes in advance

### Data Subject Rights Assistance

Your customers, as data controllers, are responsible for responding to data subject rights requests (access, erasure, rectification, portability, etc.). However, you must have the technical capability and documented processes to assist them, including:

- Ability to locate, export, correct, or delete specific data records for individual data subjects
- Response timelines that allow your customers to meet their own regulatory deadlines
- A defined process for receiving and actioning DSR assistance requests from customers

### Privacy by Design and Default

ISO 27701 requires that privacy protections be embedded into your services and systems from the design stage:

- Review all new products, features, and system changes for privacy implications before development
- Configure default settings to minimize data collection and access
- Implement minimum necessary data principles — collect only what is needed for the agreed purpose
- Document privacy design decisions for each system or service

### Data Return and Deletion at Contract End

When a customer contract ends, you must have a documented process for:

- Returning personal data to the customer in a usable format, and/or
- Securely deleting/destroying all personal data in your possession
- Providing a deletion certificate if requested
- Documenting any retention required by law (with disclosure to the customer)

Your DPA should specify the timeline and method for data return or deletion.

### Cooperation with Customer Audits

Your customers, as data controllers, have a right under GDPR Article 28 to audit your processing activities. ISO 27701 requires you to:

- Include audit rights in your DPAs
- Cooperate with customer-initiated audits or inspections
- Alternatively, provide your ISO 27701 certification and audit reports as evidence of compliance (widely accepted in practice as an alternative to direct audits)

### Support for Privacy Impact Assessments

When your customers need to conduct Data Protection Impact Assessments for processing activities that involve your services, you must be able to provide:

- Information about your processing activities, data flows, and security measures
- Assistance in assessing risks to data subjects arising from your part of the processing

---

## Shared Security Controls

In addition to the processor-specific controls, ISO 27701 requires you to implement a comprehensive set of security controls covering:

**Access Control**: Role-based access to personal data; privileged access management; authentication standards; regular access reviews.

**Cryptography**: Encryption of personal data at rest and in transit; key management.

**Physical Security**: Physical access controls to environments where personal data is processed or stored.

**Operations Security**: Malware protection; security monitoring and logging; patch management; vulnerability management.

**Communications Security**: Secure network configurations; controls on data transmission.

**Incident Management**: Privacy incident detection, response, containment, and notification procedures — including processes to notify customers (as controllers) within the timeframe needed for them to meet GDPR breach notification deadlines (72 hours to supervisory authority).

**Business Continuity**: Ensuring availability and resilience of systems processing personal data.

---

## Documentation You Need to Create

| Document | Purpose |
|----------|---------|
| Standard Data Processing Agreement (DPA) template | Customer contracts |
| Processor Records of Processing Activities | Mandatory record |
| Sub-processor register | Track and manage third parties |
| Sub-processor due diligence process | Assess and approve new sub-processors |
| Data Subject Rights Assistance Procedure | Support customer DSR responses |
| Data Deletion/Return Procedure | End-of-contract data handling |
| Privacy by Design checklist/procedure | Embed privacy in SDLC |
| Privacy Incident Response Plan | Breach detection and notification |
| Privacy training programme | Staff awareness of processor obligations |

---

## Key Distinctions from Controller Obligations

As a processor, you do **not** need to:

- Issue public privacy notices to data subjects (that is the controller's obligation)
- Determine the lawful basis for processing (that is the controller's obligation)
- Respond directly to data subject rights requests (you support the controller, who responds)
- Conduct DPIAs as the lead party (though you assist when required)

You are, however, directly responsible for:

- Processing only within the scope of customer instructions
- Security of data under your custody
- Sub-processor management
- Breach notification to customers
- Cooperation with customer audits

---

## Practical Starting Point

For B2B processors, the most impactful first steps are typically:

1. Audit all existing customer contracts — do DPAs exist and are they adequate?
2. Build your Processor RoPA documenting all customer processing activities
3. Identify and contract all sub-processors with appropriate DPAs
4. Establish a data deletion procedure for contract end
5. Ensure technical capability to support DSR requests (locate, export, delete individual records)
6. Implement a privacy incident response plan with customer notification procedures
