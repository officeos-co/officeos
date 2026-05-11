# Privacy Impact Assessment (PIA) / DPIA Methodology Under ISO 27701

## Terminology Note

ISO 27701 uses the term **"privacy impact assessment"** (PIA) as its framework-neutral terminology, while GDPR uses **"Data Protection Impact Assessment" (DPIA)**. Under ISO 27701:2025, the controlling reference is **Control A.1.2.6** (for PII Controllers), which maps directly to GDPR Article 35. For PII Processors, you support controller-led DPIAs under **Control A.2.2.5**.

This response addresses the methodology, triggers, and required content from both the ISO 27701 and GDPR perspectives.

---

## Legal Requirement Overview

| Framework | Requirement | Reference |
|-----------|------------|-----------|
| ISO 27701:2025 | DPIA/PIA mandatory for high-risk processing | A.1.2.6 |
| GDPR | DPIA mandatory before commencing high-risk processing | Article 35 |
| GDPR | Prior consultation required if high residual risk remains | Article 36 |

Under both frameworks, the DPIA/PIA must be conducted **before** the high-risk processing activity commences. Retrospective assessments have limited compliance value.

---

## Part 1: DPIA Trigger Criteria

A DPIA is required when processing is **likely to result in a high risk** to the rights and freedoms of natural persons. ISO 27701 Control A.1.2.6 aligns with GDPR Article 35 and the EDPB guidelines on mandatory DPIAs.

### Mandatory Triggers (one criterion is sufficient to require a DPIA)

| Trigger Criterion | Examples |
|-----------------|---------|
| Systematic and extensive profiling with significant effects | Credit scoring, behavioural advertising, HR performance profiling with automated decisions |
| Processing of special category data at large scale | Health data processing by an insurer; genetic data processing; religious beliefs tracking |
| Systematic monitoring of publicly accessible areas | CCTV surveillance systems; location tracking |
| Large-scale processing of personal data | Any processing of data on a significant portion of a national/regional population |
| Novel or untested technologies | AI-driven processing; biometric systems; IoT data aggregation |
| Processing that prevents exercise of rights | Fraud blacklisting; financial exclusion systems |
| Matching or combining data sets from different sources | Data broker activities; cross-system identity resolution |
| Processing of vulnerable individuals' data | Children's data; patients; elderly; employees (power imbalance) |

### Recommended Triggers (best practice even if not legally mandatory)

- Any new system, product, or process that involves personal data processing not previously assessed
- Significant changes to existing processing activities (new purpose, new data type, new system, new third-party access)
- Processing activities identified as high-risk in the privacy risk register

**ISO 27701 approach**: The standard requires a documented **DPIA trigger assessment** as part of the privacy risk assessment process (Clause 6.1). Every new or changed processing activity should be screened against the trigger criteria before a determination is made on whether a full DPIA is required.

---

## Part 2: DPIA Methodology

ISO 27701:2025 (Control A.1.2.6) and GDPR Article 35(7) together define the required methodology. The following is the recommended structured approach:

### Step 1: Initiation and Scoping

**Who**: DPO/Privacy Officer + Processing Owner + Legal

**Activities**:
- Define the processing activity being assessed (name, system, business process)
- Confirm DPIA is required (trigger criteria assessment documented)
- Assign DPIA lead and participants
- Set timeline — DPIA must be completed before processing begins

**Documentation**: DPIA initiation record with trigger assessment, scope definition, and team assignment

---

### Step 2: Systematic Description of the Processing

**Requirement**: GDPR Article 35(7)(a) — "a systematic description of the envisaged processing operations and the purposes of the processing"

**What to document**:

| Element | Description |
|---------|------------|
| Processing activity name | Clear, descriptive name |
| Controller and processor details | Who is responsible for each element |
| Nature of processing | Collection, storage, use, disclosure, erasure, etc. |
| Purposes | Every purpose for which data is processed |
| Lawful basis | Legal basis for each purpose (consent, legitimate interest, contract, etc.) |
| Data types | Categories of personal data (including special categories if applicable) |
| Data subjects | Categories of individuals affected (customers, employees, public, vulnerable groups) |
| Volume and frequency | Approximate number of records; frequency of processing |
| Data sources | Where data comes from (directly from subject, third parties, public sources) |
| Data flows | Diagram or description of data flow through systems and to third parties |
| Recipients | Internal teams, external processors, sub-processors, international transfers |
| Retention | How long data is retained and the retention rationale |
| Systems involved | IT systems, databases, third-party platforms involved |

**Tip**: A data flow diagram is highly effective and almost always expected by auditors reviewing DPIA quality.

---

### Step 3: Assessment of Necessity and Proportionality

**Requirement**: GDPR Article 35(7)(b) — "an assessment of the necessity and proportionality of the processing operations in relation to the purposes"

**Questions to answer** (documented in the DPIA):

1. **Necessity**: Is this processing necessary to achieve the stated purpose? Could the purpose be achieved with less data or less intrusive processing?
2. **Proportionality**: Is the extent of processing proportionate to the benefit achieved?
3. **Purpose limitation**: Is processing limited to stated purposes? Could data be repurposed in future?
4. **Data minimisation**: Is only the minimum necessary data being collected?
5. **Retention**: Is the retention period the minimum necessary?
6. **Lawful basis**: Is the identified lawful basis the most appropriate? Is it valid?
7. **Data subject rights**: Can data subjects exercise their rights with respect to this processing? Are there any conflicts?

---

### Step 4: Privacy Risk Assessment

**Requirement**: GDPR Article 35(7)(c) — "an assessment of the risks to the rights and freedoms of data subjects"

This is the core analytical section. Risks must be assessed from the perspective of **potential harm to individuals** (PII principals), not organizational risk.

**ISO 27701 Privacy Risk Register format (for DPIA context)**:

| Risk | Threat / Scenario | Likelihood (1–5) | Severity of Harm (1–5) | Risk Score (L × S) | Risk Level | Controls Proposed | Residual Risk |
|------|------------------|-----------------|----------------------|-------------------|-----------|------------------|---------------|

**Privacy harm categories to assess**:

| Harm Type | Examples |
|-----------|---------|
| Physical harm | Domestic abuse cases where location is disclosed; safety risks from data breach |
| Material harm | Financial loss; identity theft; discrimination leading to job loss |
| Reputational harm | Embarrassing disclosures; social stigma from health or belief data exposure |
| Psychological harm | Distress, anxiety, loss of trust caused by unexpected data use or breach |
| Loss of autonomy | Profiling that limits individual choices; surveillance chilling effect |
| Discrimination | Use of special category data leading to adverse treatment |
| Loss of rights | Processing that undermines ability to exercise legal rights |

**For each identified risk**:
- Describe the specific threat scenario (e.g., "Unauthorised access to health records by support staff")
- Assess likelihood given existing controls
- Assess severity of harm to affected individuals if the risk materialises
- Calculate risk score
- Identify proposed mitigating controls
- Assess residual risk after controls are applied

---

### Step 5: Proposed Measures to Address Risks

**Requirement**: GDPR Article 35(7)(d) — "the measures envisaged to address the risks, including safeguards, security measures and mechanisms to ensure the protection of personal data"

For each identified risk, document:
- The specific technical or organisational control proposed
- The owner responsible for implementing the control
- The implementation deadline
- How effectiveness will be monitored

**Control categories to consider**:
- Technical: Encryption, pseudonymisation, access control, data minimisation, automatic deletion
- Organisational: Training, access policies, data handling procedures, processor contracts
- Legal: Consent mechanisms, legitimate interest assessments, DPAs with processors
- Process: DPIA review triggers, data breach response, retention enforcement

---

### Step 6: DPO Consultation

ISO 27701 Clause 5 requires the DPO to be involved in DPIA review. GDPR Article 35(2) mandates DPO consultation where a DPO has been appointed.

**DPO role in DPIA**:
- Review the DPIA for completeness and adequacy
- Advise on whether risks are adequately addressed
- Advise on whether processing should proceed
- Document the DPO's advice (whether followed or not, and if not, why)

---

### Step 7: Data Subject Consultation (Where Appropriate)

GDPR Article 35(9) requires controllers to "seek the views of data subjects or their representatives, where appropriate." This does not mean consulting every individual, but may involve:
- User research or focus groups
- Consulting employee representatives for workplace processing
- Consulting patient advocacy groups for health data processing
- Reviewing publicly available feedback or complaints

Document whether consultation was conducted and its outcome, or document the justification for not consulting.

---

### Step 8: Prior Consultation with Supervisory Authority

**Requirement**: GDPR Article 36 — "Where a data protection impact assessment under Article 35 indicates that the processing would result in a high risk in the absence of measures taken by the controller to mitigate the risk, the controller shall, prior to processing, consult the supervisory authority."

**Trigger**: If, after all proposed mitigating controls are applied, the residual risk remains HIGH, you must consult the relevant supervisory authority (e.g., ICO in the UK, relevant DPA in the EU member state) **before** commencing processing.

The authority has 8 weeks (extendable by 6 weeks for complex cases) to respond.

**Decision point**: If residual risk is HIGH, either:
(a) Apply additional mitigating controls to reduce residual risk to acceptable level, or
(b) Proceed to prior consultation with the supervisory authority, or
(c) Decide not to proceed with the processing activity.

---

### Step 9: Sign-off and Approval

The completed DPIA should be:
- Reviewed and signed by the processing owner (accountability)
- Reviewed by the DPO (with their advice documented)
- Approved by senior management / Legal where required by your internal governance
- Stored in the PIMS record management system

---

### Step 10: Monitoring and Review

DPIAs are not one-time documents. ISO 27701 (Clause 9.1) and GDPR Article 35(11) require DPIAs to be reviewed when the processing changes or when a review period expires.

**Review triggers**:
- Change in processing purpose, data type, or system
- New recipients or sub-processors added
- Change in volume or frequency of processing
- Data breach or near-miss affecting this processing activity
- Passage of time (annual review recommended for high-risk activities)
- Change in regulatory guidance or interpretation

---

## DPIA Document Template Structure

```
DPIA Reference: [Unique ID]
Processing Activity: [Name]
Version: [X.X]
Date: [Date]
Next Review Date: [Date]
DPIA Lead: [Name/Role]
DPO: [Name]
Approved By: [Name/Role]

1. TRIGGER ASSESSMENT
   [Trigger criteria checked; basis for DPIA decision]

2. SYSTEMATIC DESCRIPTION
   2.1 Purpose and Nature
   2.2 Lawful Basis
   2.3 Data Types and Data Subjects
   2.4 Data Flow Description / Diagram
   2.5 Recipients and Transfers
   2.6 Retention

3. NECESSITY AND PROPORTIONALITY ASSESSMENT
   [Responses to necessity/proportionality questions]

4. PRIVACY RISK ASSESSMENT
   [Risk register table]

5. PROPOSED MEASURES AND MITIGATING CONTROLS
   [Control table with owners and deadlines]

6. DPO ADVICE
   [DPO opinion; whether followed and why]

7. DATA SUBJECT CONSULTATION
   [Outcome or justification for non-consultation]

8. PRIOR CONSULTATION DECISION
   [Residual risk assessment; prior consultation required? Y/N]

9. APPROVAL SIGN-OFF
   [Signatures / approvals]

10. REVIEW HISTORY
    [Review dates, changes, outcomes]
```

---

## Summary: DPIA Requirements at a Glance

| Element | ISO 27701 Reference | GDPR Reference |
|---------|-------------------|---------------|
| Trigger assessment | A.1.2.6 | Art. 35(1), (3) |
| Systematic description | A.1.2.6 | Art. 35(7)(a) |
| Necessity and proportionality | A.1.2.6 | Art. 35(7)(b) |
| Risk assessment (harm to individuals) | Clause 6.1 / A.1.2.6 | Art. 35(7)(c) |
| Mitigating measures | A.1.2.6 | Art. 35(7)(d) |
| DPO consultation | Clause 5 | Art. 35(2) |
| Data subject views | A.1.2.6 | Art. 35(9) |
| Prior supervisory authority consultation | A.1.2.6 | Art. 36 |
| Review and monitoring | Clause 9.1 | Art. 35(11) |
| Records retention | Clause 8 | Art. 35 (by implication) |
