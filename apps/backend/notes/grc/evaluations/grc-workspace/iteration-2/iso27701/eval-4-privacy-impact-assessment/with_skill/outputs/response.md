# Privacy Impact Assessment (PIA) Methodology Under ISO 27701

## Overview

ISO 27701 requires Privacy Impact Assessments (PIAs) — referred to as Data Protection Impact Assessments (DPIAs) under GDPR — as a mandatory operational control for PII Controllers (control A.1.2.6). The PIA is not optional: it is a required element of the Clause 8 operational controls and must be triggered for new or significantly changed processing activities that present elevated privacy risk.

This document covers the ISO 27701 PIA methodology, trigger criteria, required elements, and the relationship between the ISO 27701 PIA requirement and GDPR's mandatory DPIA obligation under Article 35.

---

## ISO 27701 Control: A.1.2.6 — Privacy Impact Assessment

**Control objective**: Conduct Privacy Impact Assessments for all new or significantly changed processing activities before they begin; document the findings and decisions; record evidence of completion.

**Scope**: Applies to PII Controllers. Organizations acting as PII Processors support their customers' PIA/DPIA processes (via A.2.2.6 — customer obligations) but typically do not conduct standalone PIAs for processing under a controller's authority.

---

## Relationship to GDPR DPIA (Article 35)

The ISO 27701 PIA and the GDPR DPIA are closely aligned but not identical:

| Aspect | ISO 27701 PIA (A.1.2.6) | GDPR DPIA (Art. 35) |
|--------|------------------------|---------------------|
| Applies to | All new/changed processing with privacy implications | Processing "likely to result in high risk" to individuals |
| Trigger | Risk-based — any new or significantly changed processing | Mandatory for high-risk categories (list in GDPR/EDPB guidance) |
| Consultation | Internal | Plus mandatory supervisory authority consultation where high residual risk remains (Art. 36) |
| Legal obligation | ISO certification requirement | Legal obligation under GDPR — failure triggers enforcement |
| Documentation standard | PIMS records (Clause 8) | Must be maintained by the controller; available on request |
| Output | PIA record with risk treatment decisions | DPIA record; must include Art. 36 consultation if applicable |

**Practical guidance**: Align your PIA process with GDPR DPIA requirements so that every DPIA is also a compliant PIA — completing both in a single process. A PIA that meets the GDPR DPIA standard automatically satisfies A.1.2.6.

---

## DPIA Trigger Criteria

### Mandatory Triggers Under GDPR (also triggering ISO 27701 PIA obligation)

A DPIA is mandatory — and therefore a PIA under ISO 27701 is automatically required — when processing falls into any of the following categories. The European Data Protection Board (EDPB) and most supervisory authorities identify these as high-risk processing types:

| Trigger | Description | Examples |
|---------|-------------|---------|
| Systematic profiling with significant effects | Automated evaluation of personal aspects (performance, economic situation, health, preferences, behavior, location) with legal or similarly significant effects on individuals | Credit scoring, insurance pricing, recruitment screening, behavioral advertising |
| Processing of special category data at scale | Large-scale processing of health, genetic, biometric, religious, political, racial/ethnic, sexual orientation data, or criminal records | Employee health monitoring, clinical data platforms, biometric access systems |
| Systematic monitoring of publicly accessible areas | Large-scale CCTV, tracking technologies in public or semi-public spaces | City-wide surveillance, workplace monitoring, tracking technologies in retail |
| New technologies with untested privacy implications | Processing using novel or innovative technologies where the privacy risk profile is not well understood | AI-based decision systems, IoT data aggregation, new forms of location tracking |
| Processing that prevents exercise of rights or use of a service | Processing that could lead to exclusion or denial of service based on personal data | Automated denial of credit, access, or benefits |
| Large-scale processing of personal data | Processing at a scale that, by volume alone, creates significant risk | National-scale data processing, population health analytics |
| Matching or combining datasets | Combining personal data from multiple sources in ways the individual would not expect | Cross-referencing internal and external datasets for profiling |
| Vulnerable subjects | Processing data of children, employees (power imbalance), vulnerable adults | Children's platforms, employee monitoring, mental health applications |

### ISO 27701 A.1.2.6 Triggers (broader than mandatory DPIA triggers)

ISO 27701 applies the PIA requirement more broadly — not just when GDPR mandates a full DPIA, but also when:

- A **new processing activity** is being introduced
- An **existing processing activity is significantly changed** (new data types, new purposes, new technologies, new recipients, or significantly increased scale)
- A **new system or product** that processes PII is being developed or deployed
- A **new third-party processor or sub-processor** is being engaged for a material processing activity
- **Data transfers** to new jurisdictions are being introduced
- The **privacy risk profile** of an existing activity materially changes (e.g., due to new threat intelligence or regulatory guidance)

**Best practice**: Embed PIA triggers into your SDLC, change management, procurement, and project initiation processes. A privacy gate review at project initiation is the most effective way to catch triggers early.

---

## PIA / DPIA Methodology

### Phase 1: Screening — Determine Whether a Full PIA Is Required

Not every new activity requires a full DPIA. Start with a **screening questionnaire** to determine whether high risk is likely:

| Screening Question | If Yes |
|-------------------|--------|
| Does the activity involve special category data? | Proceed to full PIA |
| Does the activity involve profiling or automated decision-making? | Proceed to full PIA |
| Does the activity involve systematic monitoring of individuals? | Proceed to full PIA |
| Does the activity involve new or unfamiliar technology? | Proceed to full PIA |
| Does the activity involve children or other vulnerable groups? | Proceed to full PIA |
| Could the processing lead to exclusion or denial of rights? | Proceed to full PIA |
| Does the activity involve large-scale processing? | Proceed to full PIA |
| Does the activity involve a new cross-border transfer? | Proceed to full PIA |
| Does the activity combine datasets from multiple sources? | Proceed to full PIA |

If two or more criteria apply, a full DPIA is generally mandatory under GDPR.

---

### Phase 2: Processing Description

Document the processing activity in detail:

**Required elements of the processing description:**

1. **Nature of the processing**: What is being done to the data? (collection, recording, structuring, storage, alteration, retrieval, consultation, use, disclosure, erasure, destruction)
2. **Scope**: Volume of data, categories of individuals affected, geographic scope, frequency
3. **Context**: What is the broader operational context? Is there a power imbalance? Are vulnerable groups involved?
4. **Purpose(s)**: What is the legitimate purpose? What is the stated lawful basis (GDPR Art. 6 and, where applicable, Art. 9)?
5. **Data types**: Specific personal data categories collected or processed
6. **Data flows**: Where data originates, where it flows, who has access, where it is stored, when it is deleted
7. **Recipients**: Internal and external recipients; sub-processors; international transfers
8. **Retention**: How long data is retained and why

---

### Phase 3: Necessity and Proportionality Assessment

Before assessing risks, assess whether the processing is justified:

| Question | Assessment Criteria |
|----------|-------------------|
| Is the processing necessary for the stated purpose? | Could the purpose be achieved with less or no personal data? |
| Is the processing proportionate? | Is the privacy intrusion proportionate to the benefit? |
| Is the lawful basis clearly established? | Is there a valid GDPR Art. 6 (and Art. 9) basis? |
| Are individuals adequately informed? | Does the privacy notice cover this processing? |
| Are retention periods appropriate? | Is data deleted when no longer needed? |
| Could data minimisation be applied? | Can fields be removed, pseudonymised, or anonymised? |

If the processing is not necessary or proportionate, the appropriate response is to redesign the processing activity — not just to mitigate risk.

---

### Phase 4: Privacy Risk Assessment

Assess risks to PII principals (individuals) — not just organizational risks. This is a key distinction from the ISO 27001 risk assessment, which focuses on organizational harm.

**Risk Register for PIA:**

| Processing Activity | Data Types | Individuals Affected | Threat | Vulnerability | Likelihood (1–5) | Severity of Harm (1–5) | Risk Score | Treatment | Control(s) | Owner | Due Date | Residual Risk |
|---|---|---|---|---|---|---|---|---|---|---|---|---|

**Privacy-specific threats to assess:**

| Threat Category | Examples |
|----------------|---------|
| Unauthorized access / disclosure | Data breach, insider threat, phishing, unauthorized access |
| Loss or destruction | System failure, accidental deletion, ransomware |
| Excessive collection | More data collected than necessary |
| Secondary use | Data used for purposes beyond original intent |
| Data quality failure | Inaccurate data used for significant decisions |
| Retention failure | Data held beyond retention period |
| Rights denial | Inability to fulfil data subject rights |
| Transfer to unsafe jurisdiction | Cross-border transfer without adequate safeguards |
| Profiling harm | Discriminatory or unfair automated decisions |
| Re-identification | Anonymised data re-linked to individuals |

**Severity of harm scale (individual-focused):**

| Level | Description | Examples |
|-------|-------------|---------|
| 1 — Negligible | Minor inconvenience, no lasting impact | Spam email |
| 2 — Limited | Temporary impact, reversible with effort | Minor embarrassment, temporary data exposure |
| 3 — Significant | Significant impact on individual's life | Financial loss, relationship damage, professional harm |
| 4 — Serious | Serious lasting impact | Discrimination, identity theft, significant financial harm |
| 5 — Severe | Irreversible, life-altering | Physical harm, severe discrimination, destruction of reputation |

---

### Phase 5: Risk Treatment

For each risk identified, document treatment decisions:

| Treatment | Description | ISO 27701 Controls |
|-----------|-------------|-------------------|
| **Mitigate** | Implement controls to reduce likelihood or severity | A.3.26 (encryption), A.3.9 (access control), A.1.4.5 (minimisation) |
| **Avoid** | Redesign or discontinue the processing activity | Stop collecting unnecessary fields; abandon high-risk processing |
| **Transfer** | Transfer risk via insurance or contract | Processor contracts; insurance |
| **Accept** | Accept residual risk with documented decision and approval | Document justification; obtain senior management sign-off |

**Treatment decisions must be reflected in the SoA** — the controls selected through the PIA risk treatment process inform the PIMS Statement of Applicability.

---

### Phase 6: Consultation

**Internal consultation:**
- DPO / Privacy Officer — must be consulted; their opinion must be recorded (GDPR Art. 35(2))
- Legal / compliance review
- Technical leads (security, engineering)
- Business owners

**External consultation (where required):**
- **Data subjects or their representatives** — GDPR Art. 35(9) requires consultation of data subjects where appropriate
- **Supervisory authority (GDPR Art. 36)** — mandatory prior consultation when the DPIA shows that high residual risk cannot be mitigated. The supervisory authority has 8 weeks to provide written advice (extendable to 14 weeks).

---

### Phase 7: Decision and Documentation

**Required DPIA record elements** (GDPR Art. 35(7), incorporated into ISO 27701 A.1.2.6 records):

1. A systematic description of the processing operations and the purposes, including the legitimate interest pursued
2. An assessment of the necessity and proportionality of the processing in relation to the purposes
3. An assessment of the risks to the rights and freedoms of data subjects
4. The measures envisaged to address the risks, including safeguards, security measures, and mechanisms to ensure protection of personal data

**Additional documentation for ISO 27701 audit evidence:**
- Date of PIA completion
- Names and roles of those who conducted and approved the PIA
- DPO opinion and date of consultation
- Risk treatment decisions and responsible owners
- Residual risk assessment
- Sign-off by accountable senior management
- Review trigger (i.e., under what conditions will this PIA be reviewed)

---

### Phase 8: Review and Monitoring

PIAs are not one-time documents. GDPR and ISO 27701 both require review when:

- The processing activity materially changes
- A new risk emerges (e.g., new threat intelligence, data breach)
- There is a significant change in the regulatory environment
- At defined intervals as part of ongoing PIMS management (at least annually for high-risk processing)

---

## PIA Process Summary

| Phase | Key Output | Evidence for Audit |
|-------|-----------|-------------------|
| 1. Screening | Screening decision record | Completed screening questionnaire |
| 2. Processing Description | Processing description document | Data flow diagram, RoPA entry |
| 3. Necessity & Proportionality | Justification record | Documented assessment |
| 4. Risk Assessment | Privacy risk register | Completed risk register |
| 5. Risk Treatment | Treatment plan | Control selections in SoA |
| 6. Consultation | Consultation records | DPO opinion; data subject consultation notes |
| 7. Decision | Signed DPIA record | DPIA document with management sign-off |
| 8. Review | Review schedule | Dated review trigger |

---

## Mandatory Documentation Checklist for ISO 27701 PIA Records (Clause 8)

- [ ] PIA trigger screening record for all new/changed processing activities
- [ ] Completed DPIA for each high-risk processing activity
- [ ] DPO consultation records for all DPIAs
- [ ] Evidence of data subject consultation (where conducted)
- [ ] Risk register per DPIA with likelihood, severity, and treatment
- [ ] Management sign-off on residual risk acceptance
- [ ] Supervisory authority consultation records (where triggered under GDPR Art. 36)
- [ ] DPIA review schedule and records of reviews
