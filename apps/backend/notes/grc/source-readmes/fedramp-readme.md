# FedRAMP Certification Skill for Claude

A Claude skill that provides expert, end-to-end guidance for FedRAMP authorization —
from initial readiness assessment through ATO documentation, control mapping, cloud
architecture review, and ongoing continuous monitoring.

---

## What Does the Skill Do?

This skill turns Claude into a knowledgeable FedRAMP advisor. It covers the full
authorization lifecycle for Cloud Service Providers (CSPs) pursuing or maintaining
FedRAMP certification under the current **NIST SP 800-53 Rev 5** baseline.

At a high level, the skill enables Claude to:

- Conduct **readiness and gap assessments** against a 75+ item checklist across 14 security domains
- Guide the authoring of core **ATO documentation**: SSP, POA&M, SAP, SAR, and all required appendices (A–Q)
- **Map controls** from all 20 NIST 800-53 Rev 5 control families to specific system implementations
- Provide **architecture guidance** for AWS GovCloud, Azure Government, and Google Cloud — including common findings, inheritance patterns, and design recommendations
- Support **continuous monitoring (ConMon)** obligations: monthly deliverables, annual assessments, POA&M management, and deviation request handling

The skill is current as of 2025–2026, incorporating the Rev 5 transition, the September 2026 OSCAL mandate, the January 2026 Security Inbox requirement, and the December 2024 template updates.

---

## Intended Audiences

| Audience | How They Benefit |
|---|---|
| **Cloud Service Providers (CSPs)** | Navigate the authorization process, write SSP narratives, manage POA&M, prepare for 3PAO assessments |
| **ISSOs / Security Engineers** | Map controls to implementations, identify gaps, review SAR findings, manage ConMon activities |
| **Compliance Managers / GRC Teams** | Understand FedRAMP requirements, track remediation SLAs, coordinate documentation across teams |
| **Cloud Architects** | Design systems that satisfy FedRAMP requirements from the ground up; understand boundary scoping |
| **Federal Agency Personnel** | Understand what to expect from a CSP's authorization package; evaluate SSP and SAR quality |
| **3PAO Assessors** | Reference control family requirements, test case scope, and document structure expectations |
| **Consultants / Advisory Firms** | Quickly provide accurate FedRAMP guidance to clients across impact levels and authorization phases |

---

## Common Use Cases

### 1. Readiness Assessment
> *"We're a SaaS company on AWS GovCloud targeting FedRAMP Moderate. Are we ready?"*

The skill walks through a structured gap analysis — asking about your boundary, encryption posture, logging, IAM, policies, and incident response — and produces a prioritized gap table with recommendations.

### 2. Writing SSP Control Narratives
> *"Help me write the control implementation statement for AC-2 (Account Management)."*

The skill generates detailed, reviewer-ready prose that addresses every verb in the control requirement, distinguishes CSP vs. customer responsibilities, and references specific tools and policies.

### 3. POA&M Entry Creation
> *"The 3PAO found that we don't have MFA on one of our admin interfaces. Help me write the POA&M entry."*

The skill produces a complete POA&M row with all required fields: risk rating, remediation plan, milestone dates, owner, and deviation request guidance if applicable.

### 4. Impact Level and Control Scoping
> *"Our system processes law enforcement data. What impact level do we need and how many controls apply?"*

The skill applies FIPS 199 categorization logic to recommend an impact level (Low / Moderate / High / LI-SaaS) and explains the corresponding control baseline and count.

### 5. NIST 800-53 Control Mapping
> *"Which controls cover encryption in transit, and how should we implement them on Azure Government?"*

The skill maps the question to specific control families (SC, IA), describes FedRAMP parameter requirements, and provides Azure-specific implementation guidance.

### 6. Architecture Review
> *"Review our architecture for FedRAMP compliance gaps."*

Given a description of the system, the skill identifies common findings — undocumented external connections, FIPS non-compliance, IAM over-permissioning, logging gaps — and recommends remediations.

### 7. Rev 4 → Rev 5 Transition Guidance
> *"We're currently authorized under Rev 4. What do we need to do to transition?"*

The skill explains key changes (new PT and SR families, password control revisions, MITRE ATT&CK integration), the control selection worksheet process, and transition timeline considerations.

### 8. Continuous Monitoring Support
> *"What do we need to submit to our agency AO every month?"*

The skill outlines monthly and annual ConMon obligations, POA&M SLA requirements, and how to handle vendor dependencies and deviation requests.

### 9. 3PAO Engagement Guidance
> *"How do we select and work with a 3PAO?"*

The skill explains FedRAMP Marketplace requirements, the A2LA R311 advisory/assessment separation rule, and CSP responsibilities for reviewing and approving SAP and SAR documents.

### 10. OSCAL Readiness
> *"What is OSCAL and what do we need to do before September 2026?"*

The skill explains the OSCAL mandate (RFC-0024), what machine-readable authorization packages require, and how to begin structuring SSP data for OSCAL export.

---

## How to Use the Skill

### Installation
1. Download `fedramp.skill` from this repository
2. In Claude, navigate to **Settings → Skills**
3. Upload the `.skill` file
4. The skill is now active — Claude will automatically use it for FedRAMP-related questions

### Triggering the Skill
The skill activates automatically when you mention topics like:
- FedRAMP, ATO, Authority to Operate
- SSP, SAP, SAR, POA&M
- NIST 800-53, control families, control mapping
- FedRAMP impact levels (Low, Moderate, High, LI-SaaS)
- 3PAO, continuous monitoring, ConMon
- Cloud security for federal government, AWS GovCloud, Azure Government

### Example Prompts
```
"Assess our FedRAMP readiness. We're a SaaS company running on AWS GovCloud targeting Moderate."

"Write an SSP control narrative for SC-8 (Transmission Confidentiality and Integrity)."

"Create a POA&M entry for a High finding: missing MFA on the admin portal."

"Map the AU control family to our AWS CloudTrail and Security Hub setup."

"What architecture changes do we need to meet FedRAMP High requirements?"

"Explain the key differences between Rev 4 and Rev 5 for our engineering team."
```

---

## Skill Implementation Details

### File Structure

```
fedramp/
├── SKILL.md                          # Main skill — routing logic, all five domains
└── references/
    ├── readiness-checklist.md        # 75+ item readiness checklist (14 categories)
    ├── ssp-guide.md                  # SSP section-by-section writing guide + appendices A–Q
    ├── poam-guide.md                 # POA&M fields, deviation types, SLA table
    └── sap-sar-guide.md              # SAP/SAR structure, 3PAO guidance, CSP review tips
```

### How It Was Built

The skill was authored using the **Claude Skill Creator** framework, which structures knowledge into a layered, progressively-disclosed format:

- **SKILL.md** (primary file): Contains a routing table to direct Claude to the right domain based on user intent, a current-state summary of FedRAMP as of 2025–2026, and five domain sections (Readiness, ATO Docs, Control Mapping, Architecture, ConMon)
- **Reference files**: Loaded on demand when deeper guidance is needed — keeping the main skill lean and focused

**Inputs used to construct the skill:**
- FedRAMP Rev 5 baseline documentation and transition guides (fedramp.gov)
- NIST SP 800-53 Rev 5 control families and parameter requirements
- FedRAMP CSP Authorization Playbook (v4.2, November 2025)
- FedRAMP Annual Assessment Guidance (v3.0, February 2024)
- FedRAMP Rev 5 template release notes (December 2024)
- RFC-0024 OSCAL mandate documentation
- FedRAMP SSP, POA&M, SAP, SAR template structures
- A2LA R311 3PAO advisory/assessment separation requirements
- FedRAMP Jan 2026 Security Inbox directive

**Design decisions:**
- The skill uses a **quick-reference routing table** at the top so Claude can immediately navigate to the right section rather than reading the full document for every query
- **Output format is matched to request type** (tables for gap assessments, prose for control narratives, structured rows for POA&M) — ensuring outputs are immediately usable
- Reference files are **modular and loaded on demand**, keeping the main skill under 500 lines per the Skill Creator guidelines
- All guidance is pinned to **current FedRAMP state** (Rev 5, 2025–2026 requirements) with explicit callouts for Rev 4→5 changes to avoid outdated advice

### Dependencies
- No external tools or APIs required
- Works entirely within Claude's context window
- Official FedRAMP templates referenced by URL (https://www.fedramp.gov/rev5/documents-templates/) — not embedded, as these are periodically updated by the FedRAMP PMO

---

## Author

**Hemant Naik**
[LinkedIn](https://www.linkedin.com/in/tanaji-naik/) · [hemant.naik@gmail.com](mailto:hemant.naik@gmail.com)
March 2026
