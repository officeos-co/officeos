# Privacy Impact Assessment (PIA) Methodology Under ISO 27701

## Overview

ISO 27701 includes requirements for conducting Privacy Impact Assessments (PIAs), also commonly referred to as Data Protection Impact Assessments (DPIAs) in the GDPR context. The standard requires organizations to conduct PIAs for processing activities that are likely to result in significant privacy risks to individuals.

A PIA is a systematic process for identifying, assessing, and mitigating privacy risks associated with specific processing activities. Under both ISO 27701 and GDPR, it must be conducted **before** a high-risk processing activity commences — not retrospectively.

---

## What Triggers a PIA?

ISO 27701 aligns closely with GDPR Article 35 on DPIA triggers. A PIA is required when processing activities are **likely to result in high risk** to individuals' rights and freedoms.

### Mandatory Triggers

A PIA is generally required when processing involves one or more of the following:

1. **Systematic profiling with significant effects** — including profiling for credit assessments, behavioural advertising, employee performance monitoring, or medical/insurance risk scoring where automated decisions produce significant effects.

2. **Special categories of personal data processed at scale** — health data, genetic data, biometric data, religious beliefs, political opinions, trade union membership, sexual orientation, racial or ethnic origin.

3. **Systematic monitoring of public areas** — CCTV systems, location tracking, employee monitoring in workplaces.

4. **Large-scale processing of personal data** — where the volume, scope, or geographic extent of processing affects a significant portion of a population.

5. **New or innovative technologies** — any use of untested technologies with unknown privacy implications, such as AI systems, facial recognition, IoT devices aggregating personal data.

6. **Processing that may prevent individuals from exercising rights** — fraud blacklisting, financial exclusion, access denials based on automated assessment.

7. **Processing of data relating to vulnerable individuals** — children, patients, elderly individuals, or employees (due to the power imbalance with employers).

8. **Combining or matching data sets** — merging data from multiple sources in ways that were not anticipated by individuals at the time of collection.

### Best Practice Triggers

Even where not strictly mandatory, a PIA should be conducted for:
- Any new data processing activity not previously assessed
- Significant changes to existing processing activities (new purpose, new system, new data type)
- Changes in who has access to personal data
- Processing activities rated as high-risk in your privacy risk register

---

## PIA Methodology: Step-by-Step

### Step 1: Determine Whether a PIA Is Required

Screen the processing activity against the trigger criteria above. Document your screening decision — both when a PIA is required and when it is not (with rationale).

Create a **PIA screening form** that is completed for every new or significantly changed processing activity.

---

### Step 2: Define the Scope and Assemble the Team

**Who should be involved:**
- Privacy Officer or Data Protection Officer (DPO)
- Processing owner / business lead
- IT / systems owner
- Legal counsel
- Security team (where relevant)
- Third-party vendors (where their processing is in scope)

**Define the scope:** Clearly identify the processing activity, systems involved, and boundaries of what will be assessed.

---

### Step 3: Systematic Description of the Processing

Document a comprehensive description of the processing activity, including:

- **Purpose(s)**: Why is the data being processed? What is the business objective?
- **Nature of processing**: What operations are performed? (Collection, storage, use, sharing, deletion)
- **Lawful basis**: What is the legal basis for processing? (Consent, contract, legitimate interest, legal obligation, vital interests, public task)
- **Data types**: What categories of personal data are involved? Are special categories included?
- **Data subjects**: Who are the individuals affected? How many? Are any vulnerable groups included?
- **Data sources**: Where does the data come from?
- **Data flows**: How does data flow through the organization and to third parties? (A data flow diagram is recommended)
- **Recipients**: Who receives the data internally and externally?
- **International transfers**: Is data transferred outside the country or EEA? What safeguards apply?
- **Retention**: How long is data retained? What is the basis for the retention period?
- **Systems**: Which IT systems, applications, or databases are involved?

---

### Step 4: Assess Necessity and Proportionality

Evaluate whether the processing is justified relative to its privacy impact:

- Is the processing **necessary** to achieve the stated purpose, or could the purpose be achieved with less data or less intrusive methods?
- Is it **proportionate** — is the privacy intrusion proportionate to the benefit?
- Is the data **minimised** — only the minimum necessary data being collected?
- Is the **retention period** the shortest that meets the business need?
- Is the **lawful basis** the most appropriate available?
- Can data subjects exercise their **rights** (access, erasure, etc.) with respect to this processing?

---

### Step 5: Identify and Assess Privacy Risks

This is the core analytical step. Identify privacy risks from the perspective of **harm to individuals**, covering:

**Risk categories to assess:**
- Unauthorised access to personal data (internal or external breach)
- Accidental loss or destruction of personal data
- Excessive or unlawful use of data beyond the stated purpose
- Inaccurate data leading to incorrect decisions affecting individuals
- Failure to respect data subject rights
- Discrimination or unfair outcomes from profiling or automated decision-making
- Reputational, financial, or physical harm to individuals from data misuse or breach

**For each identified risk, document:**
- Description of the risk scenario
- Likelihood of the risk materialising (High / Medium / Low)
- Severity of harm to individuals if the risk materialises (High / Medium / Low)
- Overall risk rating
- Existing controls already in place
- Proposed additional controls

---

### Step 6: Identify Measures to Mitigate Risks

For each identified risk, document specific measures to reduce the risk to an acceptable level:

**Technical measures:**
- Encryption of data at rest and in transit
- Pseudonymisation or anonymisation where full identification is unnecessary
- Access controls and role-based permissions
- Logging and monitoring
- Automatic data deletion after retention period

**Organisational measures:**
- Staff training and awareness
- Data handling procedures
- Data Processing Agreements with third parties
- Privacy by design processes
- Regular review of access rights

**Process measures:**
- Consent collection and management
- Procedure for data subject rights requests
- Incident response and breach notification procedure

---

### Step 7: Consult the DPO

Where a Data Protection Officer (DPO) has been appointed, they must be consulted on the PIA. Document the DPO's advice and whether it was followed. If the DPO's advice is not followed, the rationale must be documented.

---

### Step 8: Evaluate Residual Risk and Decide Whether to Proceed

After applying proposed mitigating controls, assess the residual risk:

- If residual risk is **acceptable**: Document approval decision and proceed with implementation (with controls in place).
- If residual risk remains **high**: You may need to consult the supervisory authority (e.g., ICO under GDPR Article 36) before commencing processing. Alternatively, redesign the processing activity to reduce risk, or decide not to proceed.

---

### Step 9: Document, Approve, and Retain the PIA

The completed PIA must be:
- Reviewed and approved by the processing owner and DPO
- Retained as a record (ISO 27701 requires records of PIA outcomes)
- Referenced in your Records of Processing Activities

---

### Step 10: Review and Update the PIA

PIAs are not static documents. They should be reviewed when:
- The processing activity changes significantly
- A data breach or incident occurs related to this processing
- Regulatory guidance changes
- A regular review period expires (typically annually for high-risk activities)

---

## What a Completed PIA Must Include

| Section | Content |
|---------|---------|
| Processing description | Purpose, nature, data types, subjects, flows, recipients, retention |
| Necessity assessment | Proportionality, minimisation, lawful basis evaluation |
| Risk register | Identified risks, likelihood, severity, existing controls |
| Proposed measures | Technical and organisational mitigations with owners |
| DPO advice | DPO opinion, whether followed |
| Residual risk | Post-mitigation risk rating |
| Approval | Sign-off by processing owner and DPO |
| Review history | Dates and outcomes of subsequent reviews |

---

## Summary

Under ISO 27701, a Privacy Impact Assessment is a mandatory process for high-risk processing activities. The methodology involves: screening to determine necessity, systematic description of processing, necessity and proportionality assessment, risk identification and rating from an individual harm perspective, identification of mitigating controls, DPO consultation, residual risk evaluation, and documented approval. PIAs must be completed before high-risk processing begins and reviewed whenever processing or context changes.
