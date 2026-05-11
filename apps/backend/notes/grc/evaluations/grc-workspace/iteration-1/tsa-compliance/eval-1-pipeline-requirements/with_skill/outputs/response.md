# TSA Cybersecurity Requirements for Natural Gas Pipeline Operators

## Applicable Directives

As a natural gas pipeline operator under TSA jurisdiction, you are subject to two primary Security Directive series, both of which apply to owners/operators of hazardous liquid and natural gas pipeline and LNG facilities designated as critical by TSA:

| Directive | Current Revision | Focus |
|-----------|-----------------|-------|
| **SD Pipeline-2021-01** | G (January 2026) | Immediate foundational measures: incident reporting, cybersecurity coordinator designation, baseline practices review |
| **SD Pipeline-2021-02** | F (latest) | Comprehensive Cyber Risk Management Program (CRMP): network segmentation, access controls, monitoring, patching, CIP, IRP, ADR, CAP |

> **Note**: TSA Security Directives are Sensitive Security Information (SSI). The summaries below are based on publicly available information. You should obtain and read the specific directive text provided to you by TSA.

---

## SD Pipeline-2021-01 (Revision G): Immediate Measures

This directive establishes the foundational, immediate cybersecurity requirements. All covered pipeline operators must:

### 1. Report Cybersecurity Incidents to CISA Within 24 Hours
Any cybersecurity incident that results in — or is reasonably likely to result in — operational disruption or unauthorised access to a Critical Cyber System (CCS) must be reported to CISA **within 24 hours of identification**.

**What qualifies as a reportable incident:**
- Unauthorised access to IT or OT systems
- Discovery of malware or ransomware on CCS
- Denial of service affecting operational capability
- Phishing or social engineering with confirmed system access

**How to report:**
- CISA 24/7 Operations Center: **1-888-282-0870**
- Email: **CISAgov@mail.dhs.gov**
- TSA must also be separately notified

**Critical rule**: Do NOT delay reporting while internal investigation is ongoing. The initial report can be based on limited information; updates follow as the investigation matures.

### 2. Designate a Cybersecurity Coordinator
You must designate a primary and backup **Cybersecurity Coordinator** who:
- Is available 24/7 (or has an available backup designee)
- Serves as the primary point of contact between your company, TSA, and CISA
- Coordinates internal cybersecurity incident response
- Oversees implementation of the Cybersecurity Implementation Plan (CIP)
- Ensures incidents are reported to CISA within required timelines

Coordinator contact information must be submitted to TSA via the designated TSA reporting system.

### 3. Review of Cybersecurity Practices
Conduct a review of current cybersecurity practices covering all systems and processes related to Critical Cyber Systems (CCS) — including access controls, monitoring, patching, incident response, network architecture, and third-party access — and identify any gaps. This review forms the baseline for your Cybersecurity Implementation Plan.

---

## SD Pipeline-2021-02 (Revision F): Comprehensive CRMP Requirements

This is the substantive directive that requires a full **Cyber Risk Management Program (CRMP)** with four major components:

### Component 1: Cybersecurity Implementation Plan (CIP)
The governing document describing how you will meet all CRMP requirements. It must be submitted to TSA for review and approval and must include:
- Leadership structure (Accountable Executive, Cybersecurity Coordinator)
- Complete CCS inventory
- Network architecture description
- Baseline cybersecurity measures for each of the four technical domains
- Incident detection and response procedures
- Annual review process

### Component 2: Incident Response Plan (IRP)
Documented procedures for detecting, responding to, and recovering from cybersecurity incidents. Must be tested annually — at least **two IRP objectives** must be tested each year. Typical test objectives include isolating IT from OT, testing backup restoration, and validating communication/escalation procedures.

### Component 3: Architecture Design Review (ADR)
An annual structured review of your IT/OT network architecture to identify gaps, vulnerabilities, and segmentation deficiencies. Must produce an updated network diagram, a findings report, and a remediation action plan.

### Component 4: Cybersecurity Assessment Plan (CAP)
A formal plan for assessing the effectiveness of your CRMP annually. CAP results must be reported to TSA annually.

---

## Four Technical Security Domains

The substantive directives require implementation across four technical domains:

| Domain | Key Requirements |
|--------|-----------------|
| **1. Network Segmentation** | IT/OT boundary enforced via firewalls, DMZ, or physical separation; no direct routable connections from corporate IT to OT/ICS without security controls; remote OT access only through DMZ or jump server |
| **2. Access Controls** | MFA for all remote access and privileged access to CCS; unique user accounts (no shared accounts); least privilege; PAM for OT admin accounts; vendor access via time-limited monitored sessions |
| **3. Continuous Monitoring** | OT-aware IDS/network monitoring; log collection from CCS; anomaly detection against OT baseline; alerting and escalation procedures |
| **4. Patch Management** | Risk-based patch SLAs; OT-specific patching process (vendor approval, non-prod testing); compensating controls for unpatchable legacy systems |

---

## Critical Cyber Systems (CCS) — Defining Your Scope

Your compliance programme centres on identifying your **Critical Cyber Systems** — systems whose compromise could cause:
- Operational disruption (inability to safely operate or control physical assets)
- Safety impact to employees, the public, or environment
- Environmental impact (uncontrolled release of natural gas)
- National security impact

CCS include both OT systems (SCADA, DCS, PLCs, RTUs, HMIs, safety instrumented systems) and IT systems that touch or interface with OT. The CCS boundary must be formally defined, documented, and kept current.

---

## 2024 NPRM: What's Coming

In November 2024, TSA published a **Notice of Proposed Rulemaking (NPRM)** that would convert the current Security Directive requirements into permanent federal regulations under 49 CFR. The comment period closed February 5, 2025. Key aspects:
- Formalises all current directive requirements as binding federal regulation
- Requires annual enterprise-wide cybersecurity evaluation using NIST CSF 2.0
- Aligns with CISA Cross-Sector Cybersecurity Performance Goals (CPGs)
- Final rule has not yet been published; current directives remain fully in force

---

## Immediate Action Items

If you have not already done so, your highest-priority actions are:

1. **Confirm designation**: Verify with TSA that your facility is formally designated as covered
2. **Designate your Cybersecurity Coordinator** and submit contact information to TSA
3. **Establish a CISA reporting pathway** — know how to reach CISA's 24/7 Operations Center
4. **Define your CCS inventory** — identify all OT and IT systems in scope
5. **Submit your CIP to TSA** for review and approval
6. **Test your IRP** — at least two objectives annually

---

*Disclaimer: This guidance is based on publicly available information about TSA Security Directives. TSA Security Directives contain Sensitive Security Information (SSI). Work directly with TSA, qualified legal counsel, and OT/ICS cybersecurity professionals to ensure compliance with the specific directives applicable to your operations.*
