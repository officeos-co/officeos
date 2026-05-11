# ISO 27001:2022 Gap Assessment & Phased Roadmap
**Organisation Profile:** 120-person SaaS company | AWS-hosted | Handles PII & financial data | No prior ISMS | Target: ISO 27001 certification within 18 months

---

## Executive Summary

Based on your profile, you are likely missing the majority of ISO 27001:2022 structural requirements (Clauses 4–10) and a significant portion of the 93 Annex A controls. The good news: AWS provides a strong infrastructure foundation (many physical and environmental controls are inherited), and 18 months is a realistic timeline for a 120-person company with committed leadership. Expect to invest heavily in documentation, risk management processes, and employee awareness in the first 6 months.

---

## Gap Assessment — Mandatory Clauses (ISO 27001:2022 Clauses 4–10)

| Clause | Requirement | Likely Status | Evidence Needed | Gap Notes |
|--------|-------------|---------------|-----------------|-----------|
| 4.1 | Understanding the organisation and its context | ❌ Not Implemented | Internal/external issues register | Most startups have no documented context analysis |
| 4.2 | Understanding interested parties | ❌ Not Implemented | Stakeholder register with requirements | Customers, regulators, investors not formally documented |
| 4.3 | ISMS Scope | ❌ Not Implemented | Signed scope document | Must define boundaries — AWS environment, business systems, people |
| 4.4 | ISMS establishment | ❌ Not Implemented | ISMS framework documentation | No formal ISMS exists |
| 5.1 | Leadership commitment | 🟡 Partial | CEO mandate documented; IS Policy signed by top management | CEO intent exists but no formal policy signed |
| 5.2 | Information Security Policy | ❌ Not Implemented | Top-level IS Policy document | No formal policy documented |
| 5.3 | Roles and responsibilities | 🟡 Partial | RACI chart, role descriptions | Likely informal; needs formal IS role assignments |
| 6.1.1 | Risk and opportunity planning | ❌ Not Implemented | Risk assessment process document | No formal process |
| 6.1.2 | Information security risk assessment | ❌ Not Implemented | Risk register with likelihood/impact scoring | No formal risk register |
| 6.1.3 | Information security risk treatment | ❌ Not Implemented | Risk Treatment Plan (RTP), Statement of Applicability (SoA) | SoA is mandatory; no controls formally selected |
| 6.2 | Information security objectives | ❌ Not Implemented | Documented IS objectives with metrics | No measurable security objectives |
| 6.3 | Planning of changes | ❌ Not Implemented | Change management process | New in 2022; often overlooked |
| 7.1 | Resources | 🟡 Partial | Budget allocation evidence, tool licenses | Likely undocumented |
| 7.2 | Competence | ❌ Not Implemented | Training records, competency assessments | No formal security training records |
| 7.3 | Awareness | ❌ Not Implemented | Awareness programme records, completion logs | Likely no formal security awareness programme |
| 7.4 | Communication | ❌ Not Implemented | Communication plan | No internal security communication plan |
| 7.5 | Documented information | ❌ Not Implemented | Document control procedure, version history | No document management system |
| 8.1 | Operational planning and control | ❌ Not Implemented | Policy suite, procedure documents | Policies not documented |
| 8.2 | Information security risk assessment (results) | ❌ Not Implemented | Completed risk assessment results | No assessments performed |
| 8.3 | Information security risk treatment (results) | ❌ Not Implemented | Completed RTP with evidence of implementation | No treatment evidence |
| 9.1 | Monitoring, measurement, analysis | ❌ Not Implemented | KPI dashboard, metrics reports | No security metrics |
| 9.2.1 | Internal audit (general) | ❌ Not Implemented | Internal audit programme | No audit function |
| 9.2.2 | Internal audit programme | ❌ Not Implemented | Audit schedule, competent auditors | No qualified internal auditors |
| 9.3 | Management review | ❌ Not Implemented | Management review meeting minutes | No management review process |
| 10.1 | Continual improvement | ❌ Not Implemented | Improvement log | No formal improvement tracking |
| 10.2 | Nonconformity and corrective action | ❌ Not Implemented | Nonconformity register, CAP records | No NC management process |

---

## Gap Assessment — Annex A Controls (ISO 27001:2022, Key Themes)

### Theme 1: Organisational Controls (A.5.x)

| Control | Name | Likely Status | Gap Notes |
|---------|------|---------------|-----------|
| A.5.1 | Policies for information security | ❌ | Policy suite does not exist |
| A.5.2 | Information security roles and responsibilities | 🟡 | Informal; not documented |
| A.5.7 | Threat intelligence | ❌ | No formal threat intel programme (new in 2022) |
| A.5.9 | Inventory of information and other assets | ❌ | No asset register |
| A.5.10 | Acceptable use of information and assets | ❌ | No AUP |
| A.5.15 | Access control | 🟡 | Likely using AWS IAM but no formal policy |
| A.5.19 | Information security in supplier relationships | ❌ | No supplier security process; critical for SaaS |
| A.5.23 | Information security for use of cloud services | ❌ | AWS used but no cloud governance policy (new in 2022) |
| A.5.24 | Information security incident management planning | ❌ | No incident response plan |
| A.5.29 | Information security during disruption | ❌ | No BCP |
| A.5.30 | ICT readiness for business continuity | ❌ | No DR planning (new in 2022) |

### Theme 2: People Controls (A.6.x)

| Control | Name | Likely Status | Gap Notes |
|---------|------|---------------|-----------|
| A.6.1 | Screening | 🟡 | May do basic checks; likely not documented |
| A.6.2 | Terms and conditions of employment | 🟡 | Employment contracts exist; IS clauses may be missing |
| A.6.3 | Information security awareness, education and training | ❌ | No formal programme |
| A.6.5 | Responsibilities after termination | ❌ | Offboarding checklist likely missing IS elements |

### Theme 3: Physical Controls (A.7.x)

| Control | Name | Likely Status | Gap Notes |
|---------|------|---------------|-----------|
| A.7.1 | Physical security perimeters | ✅ (inherited) | AWS data centre controls — inherited |
| A.7.4 | Physical security monitoring | ✅ (inherited) | AWS handles — document in SoA as inherited |
| A.7.7 | Clear desk and clear screen | ❌ | No policy; remote workforce adds risk |
| A.7.8 | Equipment siting and protection | 🟡 | Office equipment; likely undocumented |

### Theme 4: Technological Controls (A.8.x)

| Control | Name | Likely Status | Gap Notes |
|---------|------|---------------|-----------|
| A.8.1 | User endpoint devices | 🟡 | MDM may exist; policy likely missing |
| A.8.2 | Privileged access rights | 🟡 | AWS IAM used; formal PAM policy missing |
| A.8.5 | Secure authentication | 🟡 | MFA may be enabled; policy not documented |
| A.8.7 | Protection against malware | 🟡 | Tools likely exist; policy missing |
| A.8.9 | Configuration management | ❌ | No formal config baseline/management (new in 2022) |
| A.8.10 | Information deletion | ❌ | No data retention/deletion policy (new in 2022) |
| A.8.11 | Data masking | ❌ | PII/financial data masking likely not implemented (new in 2022) |
| A.8.12 | Data leakage prevention | ❌ | No DLP controls (new in 2022) |
| A.8.15 | Logging | 🟡 | AWS CloudTrail likely enabled; log review process missing |
| A.8.16 | Monitoring activities | ❌ | No formal monitoring process (new in 2022) |
| A.8.24 | Use of cryptography | 🟡 | TLS likely in use; encryption policy missing |
| A.8.25 | Secure development lifecycle | ❌ | No formal SDLC security policy |
| A.8.28 | Secure coding | ❌ | No secure coding standards (new in 2022) |
| A.8.32 | Change management | 🟡 | Informal change process likely exists |
| A.8.33 | Test information | ❌ | Production data likely used in test environments |

---

## Critical Gaps Summary (Priority Order)

1. **No ISMS Scope or formal IS Policy** — Clauses 4.3 and 5.2 are prerequisites for everything else
2. **No Risk Assessment process or Risk Register** — Clause 6.1.2; blocks SoA and treatment plan
3. **No Statement of Applicability (SoA)** — Clause 6.1.3d; mandatory certification artifact
4. **No documented policy suite** — ~15 policies needed across all Annex A themes
5. **No security awareness programme** — Clause 7.3, A.6.3; will be tested by auditor
6. **No incident response plan** — A.5.24; especially critical given PII/financial data
7. **No supplier security management** — A.5.19; critical for SaaS vendor chain
8. **No cloud governance policy** — A.5.23; AWS-specific control added in 2022
9. **No internal audit function** — Clause 9.2; required before Stage 2 audit
10. **No management review process** — Clause 9.3; required evidence for certification

---

## Phased 18-Month Roadmap to ISO 27001:2022 Certification

### Phase 1: Foundation (Months 1–4)

**Goal:** Establish ISMS structure and core documentation

| Activity | Owner | Deliverable | Clause/Control |
|----------|-------|-------------|----------------|
| Appoint ISO 27001 project lead / CISO | CEO | Role documented | 5.3 |
| Define ISMS scope | Project lead | Scope document | 4.3 |
| Conduct stakeholder and context analysis | Project lead | Context register | 4.1, 4.2 |
| Draft and sign Information Security Policy | CEO + CISO | Signed IS Policy | 5.2, A.5.1 |
| Establish document control system | CISO | Document management procedure | 7.5 |
| Define IS roles and responsibilities | HR + CISO | RACI chart | 5.3, A.5.2 |
| Launch security awareness programme | HR | Training calendar, completion logs | 7.3, A.6.3 |
| Conduct initial risk assessment training | CISO | Team competency records | 7.2 |
| Begin asset inventory | IT | Asset register | A.5.9 |
| Draft Acceptable Use Policy | CISO | AUP v1.0 | A.5.10 |

**Milestone:** ISMS structure established; CEO commitment documented

---

### Phase 2: Risk & Controls (Months 3–9)

**Goal:** Complete risk assessment, select controls, build policy suite

| Activity | Owner | Deliverable | Clause/Control |
|----------|-------|-------------|----------------|
| Conduct formal risk assessment | CISO | Risk register (all assets) | 6.1.2, 8.2 |
| Select risk treatment options | CISO + business | Risk Treatment Plan | 6.1.3 |
| Draft Statement of Applicability | CISO | SoA v1.0 | 6.1.3d |
| Set IS objectives and KPIs | CISO + CEO | IS objectives document | 6.2 |
| Draft full policy suite (~15 policies) | CISO | Policy library | 8.1, Annex A |
| Implement Access Control Policy + IAM review | IT | IAM audit report, policy | A.5.15–5.18 |
| Implement Incident Response Plan + runbooks | SecOps | IRP document | A.5.24–5.28 |
| Implement Supplier Security Policy + vendor reviews | Legal + CISO | Supplier register, contracts | A.5.19–5.22 |
| Enable and formalise AWS logging/monitoring | IT | CloudTrail config, SIEM | A.8.15, A.8.16 |
| Implement MFA everywhere | IT | MFA evidence | A.8.5 |
| Establish change management process | Engineering | Change log, CAB records | A.8.32, 6.3 |
| Conduct BCP/DR planning | IT + Ops | BCP document, DR test | A.5.29, A.5.30 |
| Implement data masking for PII/financial data | Engineering | Masking config | A.8.11 |
| Define data retention and deletion policy | Legal + IT | Retention schedule | A.8.10 |
| Draft Secure Development Lifecycle policy | Engineering | SDLC policy, secure coding standards | A.8.25, A.8.28 |
| Implement configuration management baselines | IT | Config baseline docs | A.8.9 |

**Milestone:** Risk register, SoA, and full policy suite complete; controls implemented

---

### Phase 3: Operate & Measure (Months 8–13)

**Goal:** Demonstrate operational effectiveness; build audit evidence

| Activity | Owner | Deliverable | Clause/Control |
|----------|-------|-------------|----------------|
| Operate under ISMS for minimum 3 months | All | Operational records | 8.1 |
| Collect security metrics / KPI data | CISO | Metrics dashboard | 9.1 |
| Conduct management review meetings (x2) | CEO + CISO | Meeting minutes | 9.3 |
| Run tabletop incident response exercise | SecOps | Exercise report | A.5.24 |
| Complete employee awareness training (100% completion) | HR | Training completion log | A.6.3 |
| Conduct supplier audits / questionnaires | CISO | Supplier assessment records | A.5.19 |
| Review and update risk register | CISO | Updated risk register | 8.2 |
| Perform vulnerability scanning | IT | Scan reports | A.8.8 |
| Run penetration test (recommended for SaaS/PII) | External vendor | Pen test report + remediation | A.8.8 |

**Milestone:** 3+ months of operational evidence; metrics collected

---

### Phase 4: Internal Audit & Pre-Certification (Months 12–15)

**Goal:** Identify and close gaps before Stage 1 audit

| Activity | Owner | Deliverable | Clause/Control |
|----------|-------|-------------|----------------|
| Conduct internal audit (all clauses + selected controls) | Internal auditor / consultant | Internal audit report | 9.2 |
| Log all nonconformities | CISO | Nonconformity register | 10.2 |
| Implement corrective actions | Owners | CAP records with evidence | 10.2 |
| Conduct pre-certification management review | CEO + CISO | Final management review minutes | 9.3 |
| Update SoA if controls changed | CISO | SoA v2.0 | 6.1.3d |
| Engage certification body; submit Stage 1 documentation | CISO | Stage 1 document review | — |

**Milestone:** Stage 1 audit passed; Stage 2 scheduled

---

### Phase 5: Certification Audit (Months 15–18)

**Goal:** Pass Stage 2 (on-site) audit and achieve certification

| Activity | Owner | Notes |
|----------|-------|-------|
| Stage 2 audit (on-site by CB) | Certification body | 2–3 day audit; all staff should be briefed |
| Close any major/minor nonconformities | CISO | Typically 90 days to close major NCs |
| Receive ISO 27001:2022 certificate | — | Valid 3 years; surveillance audits year 1 and 2 |

---

## Key AWS-Specific Considerations

Since you are AWS-hosted, leverage these inherited controls in your SoA:
- **A.7.1–A.7.6** (Physical security): Inherited from AWS; document with AWS compliance reports (SOC 2, ISO 27001 certificate for AWS)
- **A.8.14** (Redundancy): AWS multi-AZ architecture; document your RTO/RPO
- Use **AWS Config**, **Security Hub**, and **GuardDuty** to evidence A.8.16 (Monitoring) and A.8.15 (Logging)
- Obtain and retain **AWS Artifact** compliance reports annually
- Review **shared responsibility model** — document what AWS owns vs. what you own in your SoA

---

## Mandatory Documentation Checklist

Before Stage 1 audit, ensure you have all of the following:

- [ ] ISMS Scope document (4.3)
- [ ] Information Security Policy signed by CEO (5.2)
- [ ] Risk assessment methodology document (6.1.2)
- [ ] Risk register with scores (6.1.2, 8.2)
- [ ] Risk Treatment Plan (6.1.3)
- [ ] Statement of Applicability (6.1.3d)
- [ ] Information security objectives (6.2)
- [ ] Evidence of competence (training records) (7.2)
- [ ] Awareness training logs (7.3)
- [ ] Operational planning evidence — policy suite, procedures (8.1)
- [ ] Security metrics / monitoring results (9.1)
- [ ] Internal audit programme and results (9.2)
- [ ] Management review minutes (x2 minimum) (9.3)
- [ ] Nonconformity and corrective action records (10.2)

---

## Budget Considerations

| Item | Estimated Cost |
|------|----------------|
| ISO 27001 consultant / vCISO (12 months) | $40,000–$80,000 |
| Internal audit (external firm) | $8,000–$15,000 |
| Penetration test | $15,000–$30,000 |
| Security awareness platform | $5,000–$10,000/yr |
| Certification body audit (Stage 1 + Stage 2) | $15,000–$25,000 |
| GRC / ISMS tooling (optional) | $5,000–$20,000/yr |
| **Total estimated** | **$88,000–$180,000** |

---

*Version: 1.0 | Prepared for: 120-person SaaS Company | Standard: ISO/IEC 27001:2022 | Date: 2026-04-18*
