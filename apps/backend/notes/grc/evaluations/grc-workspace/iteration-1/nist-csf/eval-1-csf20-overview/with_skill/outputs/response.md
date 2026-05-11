# NIST Cybersecurity Framework 2.0 — Overview

## What Is CSF 2.0?

NIST Cybersecurity Framework 2.0 (CSF 2.0) was released in **February 2024**, replacing CSF 1.1 (April 2018). It is a voluntary framework designed to help organizations of all sizes, sectors, and maturity levels manage and reduce cybersecurity risk. A significant expansion from its predecessor, CSF 2.0 explicitly broadens its intended audience from critical infrastructure organizations to **all organizations**, including small businesses, enterprises, government agencies, and non-profits.

---

## What Changed from CSF 1.1 to CSF 2.0?

| Topic | CSF 1.1 | CSF 2.0 |
|-------|---------|---------|
| **Number of Functions** | 5 (Identify, Protect, Detect, Respond, Recover) | **6** — adds **Govern (GV)** as a new top-level function |
| **Govern Function** | Governance concepts embedded loosely within Identify | Standalone **GV** function with 6 categories and dedicated subcategories |
| **Supply Chain Risk** | Limited — addressed only in ID.SC (4 subcategories) | Expanded significantly: **GV.SC** in Govern function with 6 dedicated subcategories |
| **Total Subcategories** | 108 | **106** (restructured, not just incremental) |
| **Profiles** | Basic concept introduced | Strengthened — Organizational Profile templates provided; Community Profiles concept formalized |
| **Audience** | Explicitly scoped to critical infrastructure | Explicitly designed for **all organizations**, all sizes, all sectors |
| **Implementation Tiers** | 4 tiers (same structure) | 4 tiers — same structure with refined descriptions |
| **Informative References** | Embedded directly in the framework document | Moved to a separate, continuously-updated online **NIST CSF Reference Tool** |
| **Quick Start Guides** | Not provided | Added for SMBs, enterprises, risk managers, and government agencies |

### Key Conceptual Shifts

1. **Governance Elevated**: In CSF 1.1, governance concepts were scattered. CSF 2.0 places governance at the *center* of the framework — the Govern function establishes the strategic context that all other functions operate within.

2. **Supply Chain Risk Maturity**: GV.SC addresses third-party and supply chain risk management with dedicated subcategories covering supplier selection, contractual requirements, monitoring, and incident response for supply chain events.

3. **Profile Formalization**: CSF 2.0 formalizes the concept of Current Profiles and Target Profiles, providing templates to help organizations document their current cybersecurity posture and their desired future state.

4. **All-Sector Applicability**: The explicit broadening of audience means CSF 2.0 is the appropriate baseline for any organization, not just those in critical infrastructure sectors.

---

## The Six Core Functions of CSF 2.0

### 1. Govern (GV) — NEW in CSF 2.0
**Purpose**: Establish and monitor the organization's cybersecurity risk management strategy, expectations, and policy.

The Govern function provides the strategic foundation for all other functions. It addresses organizational context, risk management strategy, roles and responsibilities, policy, oversight, and supply chain risk management.

**Key Categories:**
- **GV.OC** — Organizational Context: Understanding the organization's mission, stakeholders, dependencies, and legal/regulatory obligations
- **GV.RM** — Risk Management Strategy: Establishing risk tolerance, appetite, and priorities
- **GV.RR** — Roles, Responsibilities, and Authorities: Defining who is accountable for cybersecurity
- **GV.PO** — Policy: Cybersecurity policy established, communicated, and enforced
- **GV.OV** — Oversight: Management oversight of cybersecurity risk management results
- **GV.SC** — Cybersecurity Supply Chain Risk Management: Managing risks from suppliers, vendors, and third parties

**Key Outputs**: Cybersecurity policy, documented risk tolerance, defined roles and responsibilities, supply chain risk strategy

---

### 2. Identify (ID)
**Purpose**: Understand cybersecurity risks to systems, assets, data, people, and capabilities.

**Key Categories:**
- **ID.AM** — Asset Management: Inventory of hardware, software, data, and systems
- **ID.RA** — Risk Assessment: Identifying, analyzing, and prioritizing cybersecurity risks
- **ID.IM** — Improvement: Plans to improve cybersecurity based on assessments and lessons learned

**Key Outputs**: Asset inventory, risk register, improvement roadmap

---

### 3. Protect (PR)
**Purpose**: Implement safeguards to manage cybersecurity risk and ensure delivery of services.

**Key Categories:**
- **PR.AA** — Identity Management, Authentication, and Access Control
- **PR.AT** — Awareness and Training
- **PR.DS** — Data Security
- **PR.PS** — Platform Security (patching, configuration management)
- **PR.IR** — Technology Infrastructure Resilience (redundancy, backup, recovery architecture)

**Key Outputs**: Access controls, trained workforce, hardened systems, protected data

---

### 4. Detect (DE)
**Purpose**: Find and analyze cybersecurity events and anomalies.

**Key Categories:**
- **DE.CM** — Continuous Monitoring: Monitoring networks, systems, and users for adverse events
- **DE.AE** — Adverse Event Analysis: Analyzing detected events to determine impact and root cause

**Key Outputs**: Alerts, event logs, threat detection capabilities, security monitoring program

---

### 5. Respond (RS)
**Purpose**: Take action on detected cybersecurity incidents.

**Key Categories:**
- **RS.MA** — Incident Management: Managing and tracking incidents through lifecycle
- **RS.AN** — Incident Analysis: Investigating and understanding incidents
- **RS.CO** — Incident Response Reporting and Communication
- **RS.MI** — Incident Mitigation: Containing and eradicating incidents

**Key Outputs**: Incident response plan, containment actions, stakeholder communications, post-incident analysis

---

### 6. Recover (RC)
**Purpose**: Restore assets and operations affected by a cybersecurity incident.

**Key Categories:**
- **RC.RP** — Incident Recovery Plan Execution: Restoring systems and operations
- **RC.CO** — Incident Recovery Communication: Communicating recovery status to stakeholders

**Key Outputs**: Restored operations, recovery documentation, lessons-learned integration

---

## How the Functions Relate

```
          ┌─────────────────────────────┐
          │         GOVERN (GV)         │  ← Strategic Foundation
          │  Strategy · Policy · Roles  │
          └─────────────┬───────────────┘
                        │
    ┌───────────┬────────┴──────────┬───────────┐
    ▼           ▼                   ▼           ▼
[IDENTIFY]  [PROTECT]          [DETECT]    [RESPOND]
[RECOVER] ←───────────────────────────────────────
```

The Govern function sits above and informs all other functions. Identify feeds into Protect (you protect what you know about). Detect feeds into Respond. Recover feeds back into Govern and Identify via lessons learned.

---

## Which Version Should You Use?

Use **CSF 2.0** for all new implementations. It is the current version as of February 2024. If your organization is operating under CSF 1.1, NIST provides migration guidance and subcategory-level mapping between versions.

> **Note**: This guidance is based on NIST CSF 2.0 (February 2024), a publicly available voluntary framework. Organizations in regulated industries should validate CSF alignment with their sector-specific regulatory requirements.
