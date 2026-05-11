# ISO 27701 to GDPR Mapping

## Overview

ISO 27701 was designed with GDPR compliance firmly in mind. Annex D of the standard provides an explicit mapping between ISO 27701 clauses and controls and GDPR articles. This mapping demonstrates that achieving ISO 27701 certification provides substantial structural support for GDPR compliance, although it does not constitute legal proof of full GDPR compliance by itself.

The table below maps key GDPR obligations to the corresponding ISO 27701 requirements and controls. Following the table is a narrative analysis of the scope, depth, and limitations of this mapping.

---

## Mapping Table: GDPR Articles to ISO 27701

| GDPR Article | GDPR Obligation | ISO 27701 Clause / Control |
|---|---|---|
| Art. 5 | Principles of processing (lawfulness, fairness, transparency, purpose limitation, data minimization, accuracy, storage limitation, integrity & confidentiality, accountability) | Clauses 5.2, 6.1.2, 7.2; Annex A.7.2 (lawfulness), A.7.4 (transparency), A.7.11 (minimization), A.7.12 (purpose limitation), A.7.13 (accuracy), A.7.14 (retention/disposal) |
| Art. 6 | Lawful basis for processing | Annex A.7.2.1–A.7.2.8 |
| Art. 7 | Conditions for consent | Annex A.7.3 |
| Art. 8 | Children's consent | Annex A.7.3.3 (referenced implicitly through consent controls) |
| Art. 9 | Special categories of data | Annex A.7.2 (extended to sensitive categories); risk assessment requirements in Clause 6.1.2 |
| Art. 12 | Transparent information and communication to data subjects | Annex A.7.4 |
| Art. 13 | Information to be provided at collection | Annex A.7.4.1 |
| Art. 14 | Information where data not collected directly | Annex A.7.4.1 |
| Art. 15 | Right of access | Annex A.7.6 |
| Art. 16 | Right to rectification | Annex A.7.7 |
| Art. 17 | Right to erasure (right to be forgotten) | Annex A.7.8, A.7.14 |
| Art. 18 | Right to restriction of processing | Annex A.7.8 |
| Art. 19 | Notification obligation regarding rectification, erasure, restriction | Annex A.7.6, A.7.7 |
| Art. 20 | Right to data portability | Annex A.7.9 |
| Art. 21 | Right to object | Annex A.7.10 |
| Art. 22 | Automated decision-making and profiling | Annex A.7.10.2 |
| Art. 24 | Responsibility of the controller | ISO 27701 Clause 5 (Leadership), Clause 9.1 (performance evaluation) |
| Art. 25 | Data protection by design and by default | Annex A.7.11.2, A.7.12.1; ISO 27701 Clause 6.1.1 |
| Art. 26 | Joint controllers | Annex A.7.2.6 (addressed in context of lawful basis and contracts) |
| Art. 27 | Representatives of controllers/processors not established in the EU | Not directly addressed; organizational context (Clause 4.1) |
| Art. 28 | Processor obligations and contracts with controllers | Annex B.8.1 (contract requirements), B.8.2, B.8.3, B.8.4, B.8.6, B.8.7 |
| Art. 29 | Processing under authority of controller | Annex B.8.2, B.8.4 |
| Art. 30 | Records of processing activities (RoPA) | Annex A.7.2.8 (controllers), B.8.9 (processors) |
| Art. 32 | Security of processing | ISO 27001 Annex A controls (inherited); ISO 27701 Annex A.7 and B.8 security controls |
| Art. 33 | Notification of data breach to supervisory authority | Annex A.7.17, B.8.11 (processor breach notification to controller) |
| Art. 34 | Communication of data breach to data subjects | Annex A.7.17 |
| Art. 35 | Data Protection Impact Assessment (DPIA) | ISO 27701 Clause 6.1.2 (privacy risk assessment), Annex A.7.2.5 |
| Art. 36 | Prior consultation | Clause 6.1.2 (high-risk processing escalation) |
| Art. 37–39 | Data Protection Officer (DPO) | ISO 27701 Clause 5.3 (privacy roles and responsibilities) |
| Art. 40–43 | Codes of conduct and certification | ISO 27701 is itself the certification mechanism |
| Art. 44–49 | Transfers to third countries | Annex A.7.15, A.7.16 (cross-border transfer controls and safeguards) |

---

## Narrative Analysis by GDPR Principle

### 1. Lawfulness, Fairness, and Transparency (Art. 5, 6, 12–14)

ISO 27701 Annex A.7.2 requires PII controllers to identify and document the lawful basis for each processing activity. The standard enumerates six potential bases directly mirroring GDPR Article 6: consent, contract, legal obligation, vital interests, public task, and legitimate interests. The transparency controls in Annex A.7.4 require organizations to provide privacy notices that disclose what data is collected, for what purpose, the legal basis, retention period, and data subject rights — directly operationalizing GDPR Articles 12–14.

### 2. Purpose Limitation and Data Minimization (Art. 5(1)(b) and (c))

ISO 27701 Annex A.7.12 mandates that PII collected for a specific purpose not be processed for incompatible secondary purposes. Annex A.7.11 requires data minimization: collecting only what is necessary for the stated purpose. These controls directly implement two of GDPR's core processing principles.

### 3. Storage Limitation and Data Retention (Art. 5(1)(e))

Annex A.7.14 requires organizations to establish and enforce PII retention schedules, including secure disposal of PII at the end of the retention period. This maps directly to GDPR's storage limitation principle.

### 4. Data Subject Rights (Art. 15–22)

ISO 27701 provides the most granular mapping in this area. The standard requires organizations to establish documented procedures for responding to each category of data subject right:

- **Access (Art. 15):** Annex A.7.6 — Organizations must be able to provide data subjects with copies of their PII upon request.
- **Rectification (Art. 16):** Annex A.7.7 — Procedures for correcting inaccurate PII.
- **Erasure (Art. 17):** Annex A.7.8 — Controls for deletion of PII, linked to retention controls.
- **Portability (Art. 20):** Annex A.7.9 — Providing PII in a machine-readable format.
- **Objection and automated decisions (Art. 21, 22):** Annex A.7.10 — Handling objections to processing and automated decision-making controls.

### 5. Accountability (Art. 5(2), 24)

ISO 27701's management system approach — including scope definition, policy, risk assessment, internal audit, management review, and corrective action — operationalizes GDPR's accountability principle. The standard requires documented evidence of privacy governance, which is the foundation of an accountability-based compliance program. The SoA (Statement of Applicability) and risk treatment plan serve as core accountability documentation.

### 6. Data Protection by Design and by Default (Art. 25)

Annex A.7.11.2 requires organizations to implement technical and organizational measures that embed privacy into the design of systems and processes. Annex A.7.12.1 reinforces purpose limitation as a by-default measure. This is one of the cleaner mappings between the two frameworks.

### 7. Processor Obligations (Art. 28, 29, 30)

ISO 27701 Annex B is dedicated entirely to PII processor controls. The controls in B.8.1 mirror the Article 28 requirement for a written Data Processing Agreement (DPA), specifying the required contract terms (subject matter, duration, nature and purpose of processing, type of PII, obligations and rights of the controller). B.8.9 requires processors to maintain records of processing activities, directly implementing Article 30(2).

### 8. Data Breach Notification (Art. 33, 34)

Annex A.7.17 addresses breach notification from controller to supervisory authority (within 72 hours as required by GDPR) and to affected data subjects. Annex B.8.11 addresses the processor obligation to notify the controller of a breach without undue delay, enabling the controller to meet its 72-hour obligation. ISO 27701 links these to the broader incident management process inherited from ISO 27001.

### 9. Data Protection Impact Assessment (Art. 35)

ISO 27701 Clause 6.1.2 requires organizations to conduct privacy risk assessments for processing activities, with particular attention to high-risk processing. Annex A.7.2.5 addresses assessments before engaging in new forms of PII processing. This maps conceptually and practically to the GDPR's DPIA requirement, though ISO 27701 does not use the term "DPIA" and its methodology may differ from the specific GDPR DPIA requirements (which require consultation with the supervisory authority for very high-risk processing).

### 10. International Data Transfers (Art. 44–49)

Annex A.7.15 and A.7.16 require organizations to implement controls for cross-border transfers of PII, ensuring that transfers to countries without adequate protection are subject to appropriate safeguards (such as Standard Contractual Clauses, Binding Corporate Rules, or other approved mechanisms). This maps to the GDPR Chapter V transfer restrictions.

---

## What ISO 27701 Does Well in Supporting GDPR

1. **Systematic and auditable:** The certification process provides independently verified evidence of a functioning privacy management system, which directly supports GDPR accountability.
2. **Records and documentation:** ISO 27701 drives the creation of the documentation GDPR requires: RoPA, privacy notices, consent records, DPAs, DPIA records, breach logs, and data subject rights request logs.
3. **Risk-based approach:** Both frameworks are risk-based. ISO 27701's risk assessment requirements align with GDPR's risk-proportionate approach to compliance.
4. **Processor supply chain:** ISO 27701's processor controls (Annex B) provide a structured framework for managing the downstream processor obligations that GDPR Article 28 imposes.

---

## Limitations: What ISO 27701 Does Not Fully Cover

1. **Legal determinations:** ISO 27701 cannot determine whether a specific processing activity is lawful under GDPR. Legal analysis by qualified privacy counsel is still required for complex processing scenarios.
2. **Supervisory authority interaction:** GDPR requires consultation with supervisory authorities in certain high-risk scenarios (Art. 36). ISO 27701 does not operationalize this interaction.
3. **Specific GDPR provisions:** Some GDPR provisions lack precise mapping — for example, Article 27 (non-EU representatives), certain profiling-specific obligations, and the children's data requirements under Article 8 are not fully operationalized in ISO 27701.
4. **Certification is not a legal safe harbor:** ISO 27701 certification does not create a legal presumption of GDPR compliance. Data protection authorities retain full investigative authority regardless of certification status.
5. **Jurisdiction-specific nuances:** GDPR is implemented differently across EU member states. ISO 27701's generic framework cannot account for these national variations.

---

## Conclusion

ISO 27701 certification provides a comprehensive operational framework that substantially overlaps with GDPR obligations. For organizations seeking to demonstrate a systematic, auditable approach to privacy, certification is a strong signal of maturity. However, it should be treated as a compliance enabler and evidence of good practice, not as a substitute for legal advice or a guarantee of full GDPR compliance.
