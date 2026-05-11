# Cybersecurity Incident Response Plan (CIRP / IRP) — TSA Requirements

## What Is the IRP Under TSA Directives?

Under the TSA Security Directives (SD Pipeline-2021-02F for pipelines; SD 1580-21-01E for freight rail; SD 1582-21-01E for public transit/passenger rail), covered entities must develop and maintain a **Cybersecurity Incident Response Plan (CIRP)**, also referred to as an **Incident Response Plan (IRP)**.

The IRP is one of the four required components of the **Cyber Risk Management Program (CRMP)** — the comprehensive cybersecurity programme mandated by the substantive directives. It is a formally documented set of procedures for detecting, responding to, and recovering from cybersecurity incidents that affect **Critical Cyber Systems (CCS)**.

The IRP is not a standalone document in isolation — it must be consistent with and referenced within the entity's **Cybersecurity Implementation Plan (CIP)**, which is the master governing document submitted to TSA for approval.

---

## What Must the IRP Include?

The IRP must address all phases of incident response. TSA requires the following elements:

### 1. Roles and Responsibilities
- Defined roles for all personnel involved in incident response
- Identification of the Cybersecurity Coordinator as the primary incident response lead and CISA/TSA reporting contact
- Clear escalation paths including to executive leadership
- Identification of backup personnel for 24/7 coverage

### 2. Detection and Analysis Procedures
- How cybersecurity events are identified and triaged
- Criteria for escalating an event to a declared incident
- Integration with monitoring tools and Security Operations Centre (SOC) if applicable
- OT-specific detection considerations (e.g., anomaly detection from OT-aware monitoring tools)

### 3. Containment Procedures
- Procedures to contain an incident and prevent spread to additional systems
- Critically: procedures to **isolate IT from OT** under incident conditions — ensuring OT/ICS systems can continue to operate safely even if the IT environment is compromised
- Network isolation procedures (disabling cross-boundary connections, activating firewall rules)
- Procedures for isolating individual CCS from the broader network

### 4. Eradication and Recovery Procedures
- Steps to eliminate the root cause of the incident (removing malware, closing unauthorised access)
- System restoration procedures from verified clean backups
- Backup integrity verification — confirming that backup data is uncorrupted and restorable
- Return-to-operations procedures following incident remediation

### 5. Communication Procedures
- **CISA reporting**: Procedures to report to CISA within **24 hours of identification** of a cybersecurity incident (1-888-282-0870 or CISAgov@mail.dhs.gov)
- **TSA notification**: Separate notification to TSA
- Internal escalation: Executive leadership, Board (if applicable), Legal, PR
- External stakeholders: Regulators, customers, vendors as appropriate
- Documentation of all communications during the incident

### 6. Post-Incident Review
- Mandatory post-incident review ("lessons learned") process following any declared incident
- Timeline for completing the review
- Process for updating the IRP based on findings
- Documentation requirements for post-incident reviews

### 7. Third-Party and OT Vendor Coordination
- Procedures for engaging OT/ICS vendors during incidents (vendors may need to be involved in system restoration)
- Third-party incident response firm engagement procedures
- Procedures for revoking third-party access during an incident

---

## Annual Testing Requirement

This is one of the most operationally significant requirements: covered entities must **test at least two IRP objectives annually**.

### What Counts as IRP Testing?
Testing must be structured, documented exercises — not theoretical reviews. TSA expects evidence that the procedures actually work. Common test formats include:
- **Tabletop exercises**: Walking through a simulated scenario with key personnel
- **Functional exercises**: Actually performing specific procedures (e.g., activating network isolation, restoring from backup)
- **Full-scale simulations**: Simulating a full incident from detection through recovery

### Required Test Objectives (at least 2 per year)
Typical tested IRP objectives include:

| Test Objective | What It Validates |
|---------------|-------------------|
| **IT/OT isolation under incident conditions** | Can you safely disconnect IT from OT without disrupting OT operations? Do the procedures work as designed? |
| **Backup data integrity and restoration** | Are backups intact, accessible, and restorable? How long does restoration take? |
| **Simulated ransomware containment** | Can you identify, isolate, and begin recovery from a ransomware event on CCS? |
| **Communication and escalation validation** | Do personnel know their roles? Are contact details current? Can you reach CISA and TSA? |
| **OT vendor coordination** | Can you engage OT vendors under incident conditions? Are escalation procedures effective? |

### Testing Evidence Requirements
You must retain evidence of each test. Evidence should include:
- Date of exercise
- Scenario description
- List of participants
- Objectives tested
- Findings/gaps identified
- Corrective actions assigned (with owners and due dates)
- Sign-off by Cybersecurity Coordinator or Accountable Executive

This evidence is subject to TSA review and must be available during any compliance assessment.

---

## IRP vs. CISA 24-Hour Reporting — Critical Distinction

The IRP governs your internal response process. The 24-hour CISA reporting requirement is a **separate, parallel obligation** — it is triggered at the moment of incident identification and must not wait for the IRP process to conclude.

**The rule**: Report to CISA within 24 hours of identifying a cybersecurity incident — even if your investigation is incomplete and you have limited information. You can provide updates as the investigation progresses.

**Contact CISA at**: 1-888-282-0870 or CISAgov@mail.dhs.gov (24/7 Operations Center)

---

## IRP as Part of the CIP/COIP

The IRP does not stand alone — it is incorporated into or referenced by the **Cybersecurity Implementation Plan (CIP)** (or the Cybersecurity Operational Implementation Plan — COIP — under some directive revisions). The CIP must be submitted to TSA for approval. TSA will review the IRP as part of that approval process.

If your IRP is modified significantly (e.g., following a post-incident review or after testing reveals gaps), updates must be reflected in your CIP and may require resubmission to TSA.

---

## Summary Table

| IRP Element | Requirement |
|-------------|-------------|
| Roles and responsibilities | Required |
| Detection and analysis procedures | Required |
| Containment procedures (including IT/OT isolation) | Required |
| Eradication and recovery procedures | Required |
| CISA reporting procedure (24-hour) | Required |
| TSA notification procedure | Required |
| Post-incident review process | Required |
| Third-party/vendor coordination | Required |
| Annual testing | At least 2 objectives per year |
| Testing evidence retention | Required (date, scenario, participants, findings, corrective actions) |
| Alignment with CIP/COIP | Required |

---

*Disclaimer: This guidance is based on publicly available information about TSA Security Directives. The full directive text is Sensitive Security Information (SSI). Work directly with TSA, qualified legal counsel, and OT/ICS cybersecurity professionals to ensure compliance with the specific directives applicable to your operations.*
