# Privacy Impact Assessment (PIA) Methodology Under ISO 27701

## Introduction

ISO 27701 requires organizations to conduct privacy risk assessments — commonly referred to as Privacy Impact Assessments (PIAs) or, in the GDPR context, Data Protection Impact Assessments (DPIAs). The standard does not prescribe a single rigid PIA methodology, but it does establish requirements for when assessments must be conducted, what they must consider, and how findings must be managed. The following guidance draws on ISO 27701 Clause 6.1.2, Annex A.7.2.5, and related provisions, as well as established international PIA practice (including guidance from ISO 29134, which is the dedicated PIA standard referenced by ISO 27701).

---

## Regulatory and Standards Foundation

- **ISO 27701 Clause 6.1.2:** Requires organizations to assess privacy risks as part of the broader information security risk assessment process, with particular consideration of risks to PII principals.
- **ISO 27701 Annex A.7.2.5:** Requires PII controllers to assess the necessity and proportionality of new processing before it begins, specifically for high-risk processing.
- **ISO 29134:2017:** The dedicated international standard for Privacy Impact Assessments, which ISO 27701 references for detailed PIA methodology guidance.
- **GDPR Article 35:** Requires DPIAs for high-risk processing (largely overlapping with ISO 27701's PIA triggers).

---

## What Triggers a PIA?

ISO 27701, consistent with ISO 29134 and GDPR guidance, identifies several categories of triggers that necessitate a PIA. A PIA is required — or strongly recommended — in any of the following circumstances:

### Mandatory Triggers

1. **New PII processing activities:** Any new system, product, service, or process that will collect, store, use, disclose, or delete PII for the first time requires a PIA before implementation.

2. **High-risk processing types, including:**
   - **Systematic and extensive profiling:** Automated processing used to evaluate, analyze, or predict aspects of an individual's behavior, location, interests, performance, or other personal characteristics.
   - **Large-scale processing of sensitive/special category data:** Health data, genetic data, biometric data, racial or ethnic origin, political opinions, religious beliefs, trade union membership, sexual orientation, or criminal records.
   - **Systematic monitoring of publicly accessible areas:** CCTV, IoT monitoring, tracking technologies in public spaces.
   - **Matching or combining datasets:** Combining PII from multiple sources in ways that individuals could not reasonably anticipate.
   - **Innovative technologies:** Processing using new or novel technologies where the privacy implications are not yet fully understood (e.g., AI/ML models, facial recognition, wearables).
   - **Processing that prevents individuals from exercising rights or accessing services:** Automated decision-making with significant effects (e.g., credit scoring, insurance risk assessment, background screening for employment).
   - **Children's data:** Any processing involving PII of minors.

3. **Material changes to existing processing:** Significant changes to an existing system or process that alter the nature, purpose, scope, or risk profile of PII processing. This includes:
   - New data sharing arrangements with third parties
   - Expansion to new geographic markets (particularly those with different privacy regulations)
   - New purposes for previously collected data
   - Technology upgrades that change how PII is processed
   - Significant increases in the volume or sensitivity of PII processed

4. **Regulatory requirement:** Where applicable privacy law (e.g., GDPR Art. 35) mandates a DPIA for specific processing types.

### Recommended Triggers (Judgment Required)

- Minor changes to existing processes where cumulative changes may aggregate to create new privacy risks
- New third-party data sharing arrangements, even for lower-risk processing
- Changes to data retention periods
- Introduction of new analytics or reporting capabilities over existing PII
- Mergers, acquisitions, or corporate restructuring affecting PII processing

### Screening / Threshold Assessment

In practice, organizations implement a two-stage approach:

1. **Screening (Pre-PIA):** A lightweight initial assessment — often a short questionnaire — completed by business or technical teams at the start of any new project or change. This screening determines whether a full PIA is required.
2. **Full PIA:** Triggered when the screening identifies one or more risk indicators above the threshold.

---

## PIA Methodology

### Phase 1: Preparation and Scoping

**Objective:** Define the scope of the assessment and assemble the assessment team.

**Activities:**
- Identify the system, process, or project under assessment
- Define the scope boundary: what PII, what processing activities, which systems, which geographies
- Identify the PIA lead (typically the Privacy Officer, DPO, or a trained privacy professional)
- Identify stakeholders: business owner, IT/development team, legal counsel, security team, relevant third parties
- Gather relevant documentation: system design documents, data flow diagrams, existing policies, legal basis analysis, vendor contracts

**Outputs:**
- Scoping document
- Stakeholder list
- Document inventory

---

### Phase 2: Information Gathering and Data Flow Mapping

**Objective:** Understand how PII flows through the system or process being assessed.

**Activities:**
- Conduct interviews with business owners and technical teams
- Map all PII data flows: collection, storage, transmission, processing, sharing, and deletion
- Document the categories of PII principals (e.g., customers, employees, children, patients)
- Document the categories of PII (e.g., contact data, financial data, health data, behavioral data)
- Identify all systems, databases, and third parties involved
- Document the legal basis for each processing activity
- Identify geographic locations where PII is processed or stored
- Document retention periods

**Outputs:**
- Data flow diagram (DFD) or data inventory
- PII categorization and sensitivity classification
- Legal basis mapping
- Third-party and sub-processor inventory

---

### Phase 3: Privacy Compliance Assessment

**Objective:** Assess whether the proposed processing is consistent with applicable legal requirements and privacy principles.

**Activities:**
- Assess lawfulness: Is there a valid legal basis for each processing activity?
- Assess necessity and proportionality: Is the processing limited to what is necessary for the stated purpose?
- Assess transparency: Are PII principals adequately informed (privacy notice review)?
- Assess purpose limitation: Will data be used only for the purpose disclosed?
- Assess data minimization: Is only the minimum necessary data collected?
- Assess retention: Are retention periods defined and appropriate?
- Assess data subject rights: Can the organization fulfill access, rectification, erasure, portability, and objection requests?
- Assess cross-border transfers: Are appropriate safeguards in place for any international transfers?
- Assess special category data: Are additional protections applied for sensitive data?
- Assess automated decision-making: Are appropriate safeguards in place for any automated decisions?

**Outputs:**
- Compliance gap list
- List of required remediation actions for compliance gaps

---

### Phase 4: Privacy Risk Assessment

**Objective:** Identify, analyze, and evaluate privacy risks to PII principals.

**Activities:**

#### 4a. Threat and Risk Identification
Identify privacy threats across the three key risk categories:
- **Unauthorized disclosure:** Data breach, unauthorized access, excessive sharing
- **Unauthorized modification:** Inaccurate or corrupted PII
- **Unavailability:** Loss of PII, inability to fulfill rights, inability to withdraw consent

For each identified threat, document:
- The threat scenario (what could go wrong)
- The threat source (external attacker, insider, third party, system failure, human error)
- The PII at risk
- The PII principals affected

#### 4b. Likelihood Assessment
For each threat, assess the likelihood of occurrence:
- **High:** Likely to occur without specific mitigating controls
- **Medium:** Possible with some mitigating controls in place
- **Low:** Unlikely given existing or planned controls

Consider factors: sensitivity of data, volume of data, threat actor capability, existing security posture, regulatory environment.

#### 4c. Impact Assessment
For each threat, assess the potential impact on PII principals (not just the organization):
- **High:** Significant harm to individuals — financial loss, physical harm, discrimination, identity theft, reputational damage, loss of employment, psychological distress
- **Medium:** Moderate inconvenience or harm — minor financial loss, temporary embarrassment, manageable disruption
- **Low:** Minimal impact — minor inconvenience, no lasting harm

#### 4d. Risk Scoring
Combine likelihood and impact to produce a risk score:

| Likelihood \ Impact | Low | Medium | High |
|---|---|---|---|
| Low | Low | Low | Medium |
| Medium | Low | Medium | High |
| High | Medium | High | Critical |

#### 4e. Risk Evaluation
Determine whether each risk is acceptable, requires treatment, or is a blocker for the project.

**Outputs:**
- Privacy risk register (listing each risk with threat, likelihood, impact, score, and risk owner)

---

### Phase 5: Risk Treatment Planning

**Objective:** Identify and select controls to reduce unacceptable privacy risks.

**Activities:**
- For each unacceptable risk, identify potential treatment options:
  - **Avoid:** Change or abandon the processing activity
  - **Mitigate:** Implement technical or organizational controls to reduce likelihood or impact
  - **Transfer:** Share risk through contractual means (e.g., insurance, supplier contracts) — note this does not remove regulatory liability
  - **Accept:** Formally accept residual risk within organizational risk tolerance (must be documented and approved)
- Select and document risk treatment controls
- Assign ownership and implementation timelines
- Calculate residual risk after treatment

**Outputs:**
- Risk treatment plan
- Updated risk register with residual risk scores
- Control implementation schedule

---

### Phase 6: Documentation and Reporting

**Objective:** Produce a formal PIA report.

**Required PIA Report Contents:**

1. **Executive Summary**
   - Purpose and scope of the assessment
   - Summary of findings
   - Overall risk level
   - Key recommendations

2. **Description of Processing**
   - System/process description
   - PII categories and data flows
   - Purposes of processing and legal bases
   - Parties involved (controllers, processors, sub-processors)

3. **Compliance Assessment Findings**
   - Results of the compliance gap analysis
   - Identified gaps and remediation actions

4. **Risk Assessment**
   - Identified threats and risks
   - Risk scores (pre-mitigation)
   - Risk treatment decisions and controls
   - Residual risk scores (post-mitigation)

5. **Conclusions and Recommendations**
   - Whether the processing can proceed
   - Conditions or requirements attached to approval (e.g., implement specific controls before go-live)
   - Outstanding risks requiring management acceptance

6. **Sign-off**
   - Privacy Officer / DPO review and approval
   - Business owner acknowledgment
   - Date of assessment and scheduled review date

**Outputs:**
- Formal PIA report
- Updated privacy risk register

---

### Phase 7: Consultation (Where Required)

**Objective:** Where required, consult with relevant stakeholders before finalizing the PIA.

**Activities:**
- If the residual risk remains high after treatment, escalate to senior management for formal risk acceptance
- Under GDPR Article 36, if a DPIA reveals a high residual risk that cannot be mitigated, prior consultation with the relevant data protection supervisory authority is required before processing begins
- Consider consulting with data subject representatives or affected communities for high-impact processing (particularly processing involving vulnerable groups)
- Consult with legal counsel for any unresolved legal compliance questions

**Outputs:**
- Evidence of consultation (meeting minutes, correspondence)
- Regulator pre-consultation records if applicable

---

### Phase 8: Implementation, Monitoring, and Review

**Objective:** Ensure that risk treatment controls are implemented and that the PIA remains current.

**Activities:**
- Track implementation of risk treatment controls against the treatment plan
- Confirm controls are implemented before the system/process goes live
- Schedule periodic review of the PIA (recommended: annually, or sooner if the processing changes materially)
- Link the PIA to the organization's change management process so that material changes trigger a PIA update
- Store the PIA in the organization's privacy management document repository

**Outputs:**
- Evidence of control implementation
- Scheduled review dates in the privacy management calendar
- PIA version control and change log

---

## Summary: What a PIA Must Include

Under ISO 27701, a complete PIA must address the following elements:

| Element | Description |
|---|---|
| Scope | What system, process, or project is being assessed |
| Data Flow Description | What PII is collected, where it flows, who processes it |
| Legal Basis Analysis | Lawfulness of processing for each activity |
| Compliance Assessment | Alignment with privacy principles and legal requirements |
| Risk Identification | Privacy threats to PII principals |
| Risk Analysis | Likelihood and impact scoring |
| Risk Treatment | Controls selected to address unacceptable risks |
| Residual Risk | Risk level after treatment |
| Stakeholder Sign-off | Approval by privacy lead and business owner |
| Review Schedule | When the PIA will next be reviewed |

---

## Key Distinctions: PIA vs DPIA

While ISO 27701 uses the term "privacy risk assessment" and references PIAs (consistent with ISO 29134), GDPR uses the term Data Protection Impact Assessment (DPIA). The two are functionally equivalent for most purposes. The main distinctions are:

- GDPR DPIA is a legal requirement for specific processing types; ISO 27701 PIA is a management system requirement.
- GDPR has specific triggers and mandatory contents under Article 35 and WP29/EDPB guidance.
- GDPR DPIAs may require prior consultation with the supervisory authority; ISO 27701 PIAs do not have an equivalent statutory obligation.
- In practice, a well-executed ISO 27701 PIA will satisfy GDPR DPIA requirements if the scope, methodology, and documentation meet the EDPB's established criteria.

---

## Conclusion

A robust PIA program under ISO 27701 provides organizations with a systematic, evidence-based method to identify and manage privacy risks before they materialize. The key success factors are: triggering PIAs early in the project lifecycle (not as an afterthought), ensuring cross-functional participation, producing well-documented and honest risk assessments, and maintaining a living record that is updated as processing activities evolve.
