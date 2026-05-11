# ISO 27701 to GDPR Mapping

## How ISO 27701 Supports GDPR Compliance

ISO 27701 was specifically designed to help organizations demonstrate compliance with GDPR, UK GDPR, and similar global privacy regulations — this alignment is the core purpose of the standard. ISO 27701:2025 includes an updated correspondence annex (based on the 2019 edition's Annex D GDPR mapping) that maps controls directly to GDPR articles. The mapping works in both directions: GDPR articles map to ISO 27701 controls, and ISO 27701 controls address specific GDPR obligations.

However, an important caveat must be understood from the outset: **ISO 27701 certification is not a GDPR safe harbor**. Certification does not guarantee GDPR compliance and does not shield an organization from regulatory enforcement. What it does provide is strong documented evidence of Article 24/32 technical and organisational measures (TOMs) and accountability documentation under Article 5(2). This is covered in detail at the end of this document.

---

## GDPR Article 5 Principles → ISO 27701 Controls

GDPR Article 5 establishes the core data protection principles. Every processing activity must comply with all six principles, plus the accountability meta-principle.

| GDPR Principle | Article 5 | ISO 27701:2025 Controls | How the Control Addresses the Principle |
|---------------|-----------|------------------------|----------------------------------------|
| Lawfulness, fairness, transparency | Art. 5(1)(a) | A.1.2.3 (Identify Lawful Basis), A.1.3.3–A.1.3.4 (Transparency Notices) | Documents the lawful basis per activity; requires provision of information to individuals before or at collection |
| Purpose limitation | Art. 5(1)(b) | A.1.2.2 (Identify and Document Purpose), A.1.4.3 (Limit Processing) | Requires purposes to be stated before collection; prohibits secondary use without additional legal basis |
| Data minimisation | Art. 5(1)(c) | A.1.4.2 (Limit Collection), A.1.4.5 (PII Minimisation) | Requires collecting only data necessary for the stated purpose; systematic minimisation review in SDLC |
| Accuracy | Art. 5(1)(d) | A.1.4.4 (Accuracy and Quality), A.1.3.7 (Correction) | Requires data quality controls; requires correction mechanisms for individuals |
| Storage limitation | Art. 5(1)(e) | A.1.4.8 (Retention), A.1.4.9 (Disposal), A.1.4.6 (De-identification and Deletion) | Requires defined retention schedules per data category; secure disposal at end of retention period |
| Integrity and confidentiality | Art. 5(1)(f) | A.3.26 (Cryptography), A.3.25 (Logging), A.3.9 (Access Rights) | Encryption at rest and in transit; access logging; least-privilege access to PII systems |
| Accountability | Art. 5(2) | A.1.2.9 (RoPA), SoA, Privacy Risk Assessment, Management Review | Records of processing demonstrate accountability; SoA documents control selection; management review evidences governance |

---

## GDPR Articles 6–22: Controller Obligations → ISO 27701 Controls

### Lawful Basis and Consent (Articles 6–9)

| GDPR Obligation | Article | ISO 27701:2025 Controls | Gap / Limitation |
|----------------|---------|------------------------|-----------------|
| Lawful basis for processing | Art. 6 | A.1.2.3 | ISO 27701 requires lawful basis to be documented; but identifying the correct basis (especially legitimate interests) requires legal judgment beyond the standard |
| Consent requirements | Art. 7 | A.1.2.4 (Determine Consent), A.1.2.5 (Obtain and Record Consent) | Standard requires compliant consent mechanisms; but GDPR's definition of freely given, specific, informed, and unambiguous consent requires legal interpretation in context |
| Conditions for consent — children | Art. 8 | A.1.2.4, A.1.2.5 | ISO 27701 does not have a dedicated control for children's consent — this is a gap where GDPR goes beyond the standard |
| Special categories of data | Art. 9 | A.1.2.3 (identify explicit consent), A.1.2.6 (DPIA for sensitive data), A.1.4.5 (PII minimisation) | No dedicated special category control; organizations must apply stricter lawful basis requirements under GDPR Art. 9(2) as a matter of legal compliance |

### Transparency (Articles 13–14)

| GDPR Obligation | Article | ISO 27701:2025 Controls | Notes |
|----------------|---------|------------------------|-------|
| Information to be provided at collection | Art. 13 | A.1.3.3 (Information for PII Principals), A.1.3.4 (Providing Information) | Controls require provision of required information before or at collection |
| Information to be provided when data not obtained from subject | Art. 14 | A.1.3.3, A.1.3.4 | Same controls apply; the content of the notice differs |

### Individual Rights (Articles 15–22)

These controls map to the A.1.3 domain (Obligations to PII Principals):

| GDPR Right | Article | ISO 27701:2025 Controls | Gap / Limitation |
|-----------|---------|------------------------|-----------------|
| Right of access | Art. 15 | A.1.3.9 (Providing Copy of PII), A.1.3.10 (Handling Requests) | Controls require access procedures and SLAs; GDPR's one-month response time is a legal requirement not specified in the standard |
| Right to rectification | Art. 16 | A.1.3.7 (Access, Correction or Erasure), A.1.3.8 (Inform Third Parties) | Controls require correction mechanisms and notification of third parties |
| Right to erasure (right to be forgotten) | Art. 17 | A.1.3.7, A.1.4.6 (De-identification and Deletion), A.1.4.9 (Disposal) | Controls require deletion procedures; GDPR's exceptions to the right to erasure require legal judgment |
| Right to restriction of processing | Art. 18 | A.1.3.6 (Object to PII Processing), A.1.3.10 (Handling Requests) | Controls require restriction mechanisms; GDPR's specific grounds for restriction go beyond control guidance |
| Right to data portability | Art. 20 | A.1.3.9 (Providing Copy of PII) | ISO 27701 covers portability broadly; GDPR specifically requires structured, commonly used, machine-readable format |
| Right to object | Art. 21 | A.1.3.6 (Object to PII Processing) | Controls require objection mechanism; GDPR's balancing test for legitimate interests overrides requires legal judgment |
| Rights re: automated decision-making and profiling | Art. 22 | A.1.3.11 (Automated Decision Making) | Controls require disclosure and appeal mechanism; GDPR's specific conditions for lawful automated decisions require legal analysis |

### Accountability and Governance (Articles 24–30)

| GDPR Obligation | Article | ISO 27701:2025 Controls | Notes |
|----------------|---------|------------------------|-------|
| Responsibility of the controller — TOMs | Art. 24 | All A.1 and A.3 controls | ISO 27701 SoA and certification serve as evidence of TOMs |
| Data protection by design and by default | Art. 25 | A.1.4.2–A.1.4.10 (Privacy by Design domain) | The entire A.1.4 domain addresses Art. 25; certification provides strong evidence |
| Joint controllers | Art. 26 | A.1.2.8 (Joint PII Controller) | Control requires documented joint controller arrangement; GDPR requires the arrangement to be made available to data subjects |
| Processor contracts | Art. 28 | A.1.2.7 (Contracts with PII Processors) | Control requires written DPA with all processors; GDPR Art. 28(3) specifies mandatory DPA clauses |
| Records of processing activities | Art. 30 | A.1.2.9 (Records of Processing PII) | Direct mapping; RoPA is a mandatory deliverable under both |

### DPO, DPIA, and Breach (Articles 33–37)

| GDPR Obligation | Article | ISO 27701:2025 Controls | Gap / Limitation |
|----------------|---------|------------------------|-----------------|
| Data Protection Impact Assessment | Art. 35 | A.1.2.6 (Privacy Impact Assessment) | Direct mapping; ISO 27701 requires DPIAs for high-risk processing; GDPR specifies mandatory consultation with supervisory authority where DPIA shows high residual risk |
| Prior consultation with supervisory authority | Art. 36 | A.1.2.6 (indirectly) | GDPR requires supervisory authority consultation in certain cases; ISO 27701 does not explicitly mandate this |
| Data Protection Officer | Art. 37–39 | Clause 5 (Leadership roles and responsibilities) | ISO 27701 Clause 5 requires privacy roles; GDPR mandates DPO appointment in specific circumstances (public authority, large-scale monitoring, special category data) |
| Personal data breach notification (controller to SA) | Art. 33 | A.3.11 (Incident Management), A.3.12 (Security Incident Response) | Controls require breach response; GDPR's 72-hour notification window is a legal requirement |
| Personal data breach notification (controller to subjects) | Art. 34 | A.3.11, A.3.12 | Controls require notification; GDPR specifies risk thresholds for subject notification |

### International Transfers (Articles 44–49)

| GDPR Obligation | Article | ISO 27701:2025 Controls | Notes |
|----------------|---------|------------------------|-------|
| General principle for transfers | Art. 44 | A.1.5.2 (Basis for PII Transfer) | Requires documented transfer mechanism per transfer |
| Adequacy decision transfers | Art. 45 | A.1.5.2 | Adequacy decision documented as transfer basis |
| Transfers with appropriate safeguards (SCCs, BCRs) | Art. 46 | A.1.5.2, A.1.5.3 | SCCs/BCRs documented; countries of transfer recorded |
| Transfer records | Art. 30 (Art. 30(1)(e)) | A.1.5.4 (Records of PII Transfer), A.1.5.5 (Records of PII Disclosures) | Transfer log maintained |

---

## GDPR Article 28: Processor Obligations → ISO 27701 Controls

GDPR Article 28 governs the obligations of data processors. ISO 27701's A.2 (Processor Controls) maps directly to these requirements:

| GDPR Art. 28 Obligation | Article | ISO 27701:2025 Controls | ISO 27701 Term |
|------------------------|---------|------------------------|----------------|
| Processing under written contract | Art. 28(3) | A.2.2.2 (Customer Agreement) | "processing under controller authority" |
| Processing only on controller instruction | Art. 28(3)(a) | A.2.2.3, A.2.2.5 | A.2.2.5 requires notification if instruction would infringe law |
| Confidentiality of processing | Art. 28(3)(b) | A.3.18 (Confidentiality Agreements), A.2.2.6 | Confidentiality agreements for all staff with PII access |
| Security measures | Art. 28(3)(c) | A.3 (all 29 security controls) | All A.3 controls contribute |
| Sub-processor controls | Art. 28(2), (4) | A.2.5.7 (Disclosure), A.2.5.8 (Engagement), A.2.5.9 (Change) | "sub-processor notification and consent" (A.2.2.6); "sub-processor contracts" (A.2.5.8) |
| Assist controller with data subject rights | Art. 28(3)(e) | A.2.2.6, A.2.3.2 | "PII subject rights assistance obligations" (A.2.3.3) |
| Deletion or return at end of contract | Art. 28(3)(g) | A.2.4.3 (Return, Transfer or Disposal of PII) | |
| Provide information / allow audits | Art. 28(3)(h) | A.2.2.6 | Processor must provide information to enable controller compliance verification |
| Records of processing (processor RoPA) | Art. 30(2) | A.2.2.7 (Records of Processing PII) | Per-controller processing records |
| Notify controller of breaches | Art. 33(2) | A.3.11, A.3.12 | Processor breach notification feeds controller's 72-hour GDPR obligation |
| Law enforcement disclosure — notify controller | Art. 28(3)(a) | A.2.5.5 (Notification of PII Disclosure Requests), A.2.5.6 (Legally Binding PII Disclosures) | |

---

## Where GDPR Goes Beyond ISO 27701 — Identified Gaps

While ISO 27701 provides excellent coverage, there are areas where GDPR requires more than the standard alone can address:

| Gap Area | GDPR Requirement | ISO 27701 Coverage |
|----------|-----------------|-------------------|
| DPO mandatory appointment | GDPR Art. 37 — mandatory DPO for public authorities, large-scale monitoring, special categories | ISO 27701 Clause 5 requires privacy roles but does not mandate DPO appointment |
| Children's consent | GDPR Art. 8 — parental consent for under-16s (or national lower age) | No dedicated control; must be addressed through A.1.2.4/A.1.2.5 |
| Special category lawful basis | GDPR Art. 9(2) — specific grounds (explicit consent, employment law, vital interests, etc.) | No dedicated control; A.1.2.3 requires lawful basis but the specific Art. 9(2) grounds require legal judgment |
| Supervisory authority consultation | GDPR Art. 36 — mandatory prior consultation after DPIA shows high residual risk | Not explicitly in A.1.2.6 |
| Portability format requirement | GDPR Art. 20 — structured, commonly used, machine-readable format | A.1.3.9 covers the right but not the specific format requirement |
| Legal basis for national derogations | GDPR recitals and member state laws | Not within scope of international standard |
| Supervisory authority cooperation | GDPR Art. 31 | Not addressed in ISO 27701 |
| Binding corporate rules | GDPR Art. 47 | No dedicated control; BCRs referenced through A.1.5.2 |

---

## What ISO 27701 Certification Provides for GDPR Compliance

In practical terms, achieving ISO 27701 certification gives you:

1. **Evidence of Article 24/32 Technical and Organisational Measures (TOMs)**: The SoA and certification report are strong evidence that your organization has implemented appropriate privacy controls — directly addressing the accountability requirement.

2. **Accountability documentation under Article 5(2)**: The PIMS management system (risk assessments, management reviews, internal audits, SoA) creates the paper trail that regulators expect to see when demonstrating the accountability principle.

3. **Recognized framework**: EU supervisory authorities and the UK ICO view ISO 27701 favorably as evidence of commitment to data protection. It will be considered positively in any regulatory investigation or enforcement action.

4. **Structured compliance programme**: The PIMS provides the systematic, documented approach that regulators expect — as opposed to ad hoc compliance measures.

5. **GDPR Art. 28 processor assurance**: If you are also a processor, ISO 27701 certification provides your controller clients with recognized evidence that you meet Art. 28 requirements — often reducing the need for individual audit rights.

---

## Summary Assessment

ISO 27701:2025's mapping to GDPR is comprehensive and covers the majority of GDPR obligations at a control level. It is strongest in the areas of accountability, transparency, data subject rights, data minimisation, security, and processor contract requirements. The gaps are relatively narrow — principally around DPO mandatory appointment, children's consent, special category processing, and specific supervisory authority interactions — and require supplementary legal and regulatory work alongside the standard implementation.

Treat ISO 27701 as the backbone of your GDPR compliance programme, not as a substitute for legal analysis. The combination of ISO 27701 certification plus legal review of the identified gaps provides the strongest overall GDPR compliance posture.
