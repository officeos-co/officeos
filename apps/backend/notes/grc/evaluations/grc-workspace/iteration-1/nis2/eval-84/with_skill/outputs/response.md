# ISO 27001:2022 and NIS2 Compliance: Gap Analysis

## Short Answer

ISO 27001:2022 certification is a strong foundation and provides meaningful evidence of NIS2 compliance for many Art. 21 cybersecurity risk management measures — but it does **not** make you NIS2 compliant on its own. NIS2 imposes direct legal obligations that go beyond what ISO 27001 addresses, most significantly in governance accountability, incident reporting timelines, mandatory MFA, and supply chain coordination with EU authorities. You will need targeted remediation in at least six areas.

---

## Why ISO 27001 Is Not Sufficient Alone

ISO 27001 is a voluntary international standard for an Information Security Management System (ISMS). NIS2 (Directive (EU) 2022/2555) is EU law — a legal obligation imposed directly on in-scope entities by Member State transposition legislation (deadline: 17 October 2024). Holding certification demonstrates that your ISMS meets the standard's requirements; it does not demonstrate that you have fulfilled your legal obligations under NIS2.

The European Union Agency for Cybersecurity (ENISA) and national competent authorities accept ISO 27001 as compliance evidence for the technical and organisational controls in Art. 21, but they will still assess your organisation against NIS2's specific requirements during supervisory proceedings.

---

## Preliminary Step: Confirm Your Entity Classification

Before conducting a gap assessment, confirm whether you are classified as:

- **Essential Entity (EE)** — Annex I sectors: energy, transport, banking, financial market infrastructure, health, drinking water, wastewater, digital infrastructure, ICT service management (B2B), public administration, space.
- **Important Entity (IE)** — Annex II sectors: postal/courier, waste management, chemicals, food, manufacturing (medical devices, computers, electronics, machinery, motor vehicles), digital providers, research.

Size thresholds (Art. 3): medium-sized or larger organisations (≥50 employees OR ≥€10M annual turnover) are automatically in scope. Some Member States have extended coverage to smaller entities in critical sectors.

This matters because:
- **EE** are subject to **proactive (ex-ante) supervision** under Art. 32 — including on-site inspections, security audits, and targeted scans. They face penalties up to €10,000,000 or 2% of global annual turnover.
- **IE** are subject to **reactive (ex-post) supervision** under Art. 33 — triggered by evidence of non-compliance. Penalties up to €7,000,000 or 1.4% of global annual turnover.

Most Member States also require in-scope entities to **self-register** with the national competent authority. If you have not done this, it is an immediate action item regardless of other gaps.

---

## ISO 27001 to NIS2 Art. 21 Control Mapping

The ten Art. 21 measures map well to ISO 27001:2022 Annex A. The table below shows where your certification provides strong coverage and where residual gaps remain.

| NIS2 Art. 21 Measure | ISO 27001:2022 Annex A Coverage | Gap Assessment |
|---|---|---|
| M1 — Risk Analysis & IS Policies | A.5.1 Policies; Clause 6.1 Risk assessment; Clause 8.2 IS risk assessment | **Low gap** — ISMS risk methodology directly satisfies M1. Ensure policies explicitly reference network and information systems in scope for NIS2. |
| M2 — Incident Handling | A.5.24–A.5.28 (incident planning, assessment, response, review, evidence) | **Moderate gap** — ISO 27001 covers the process; NIS2 adds legally mandated reporting timelines (see Art. 23 below). IRP needs a NIS2 notification track. |
| M3 — BCP / DR / Crisis Management | A.5.29 IS during disruption; A.5.30 ICT readiness; A.8.13 Backup; A.8.14 Redundancy | **Low gap** — ISO 27001 strongly covers this. Verify RTO/RPO are defined per critical service and tested annually; involve management in crisis exercises. |
| M4 — Supply Chain Security | A.5.19–A.5.23 (supplier relationships, agreements, ICT supply chain, cloud) | **Moderate gap** — ISO 27001 covers supplier controls generally; NIS2 Art. 21(2)(d) specifically ties to ENISA-coordinated supply chain risk assessments under Art. 26. You must monitor and act on ENISA/national authority advisories on high-risk vendors. |
| M5 — Secure Acquisition & Development | A.8.25–A.8.30; A.5.8 | **Low gap** — Secure SDLC and vulnerability management are well-covered. Ensure a formal coordinated vulnerability disclosure (CVD) policy exists aligned with ENISA guidance. |
| M6 — Effectiveness Assessment | A.5.35–5.36; Clause 9.1–9.3 | **Low gap** — ISO 27001's management review and internal audit cycle directly satisfies M6. Ensure cybersecurity KPIs are reported to the management body formally. |
| M7 — Cyber Hygiene & Training | A.6.3 IS awareness/training; A.5.7 Threat intelligence | **Low gap** — ISO 27001 requires awareness training. NIS2 Art. 20 requires that the **management body itself** undergoes regular cybersecurity training. Verify board-level training records exist. |
| M8 — Cryptography | A.8.24 Use of cryptography | **Low gap** — ISO 27001 covers this. Confirm your cryptographic policy specifies approved algorithms (AES-256, TLS 1.2+, SHA-256+) and key management lifecycle. Consider post-quantum readiness review. |
| M9 — HR Security, Access Control & Asset Management | A.5.9–A.5.11; A.8.2–A.8.4; A.6.1; A.6.5 | **Low gap** — ISO 27001 covers this well. Confirm least-privilege, RBAC, quarterly access reviews, and same-day leaver revocation are operational. |
| M10 — MFA & Secure Communications | A.8.5 Secure authentication; A.8.20 Network security; A.8.24 Cryptography | **Significant gap** — ISO 27001 covers authentication generally; NIS2 **explicitly mandates MFA**. ISO 27001 does not universally require MFA. You must deploy MFA for remote access, privileged accounts, cloud management consoles, and email. |

---

## Key Gaps Where NIS2 Exceeds ISO 27001

### Gap 1 — Art. 20: Management Body Governance and Personal Liability (HIGH PRIORITY)

**ISO 27001 position:** Leadership commitment is required (Clause 5), but personal liability for individual board members is not addressed.

**NIS2 requirement:** Art. 20 mandates that the management body (board, executive committee, or equivalent):
- Approves cybersecurity risk management measures in writing.
- Oversees implementation and can be held personally liable under Member State law for failures.
- Completes **regular cybersecurity training**.

**Remediation actions:**
- Document board-level approval of the NIS2 cybersecurity policy (separate board minute or resolution).
- Establish a board cybersecurity oversight charter with defined roles and accountability.
- Implement and record annual cybersecurity training programme specifically for the management body.
- Confirm with legal counsel what personal liability provisions your Member State's transposition law contains.

---

### Gap 2 — Art. 23: Incident Reporting Timelines (HIGH PRIORITY)

**ISO 27001 position:** A.5.24–A.5.26 require incident management processes, but ISO 27001 does not specify legally mandated notification timelines to external authorities.

**NIS2 requirement:** For **significant incidents** (substantial service disruption, material financial loss, cross-border impact, or damage to other persons), you must notify the national CSIRT or competent authority:

| Timeline | Requirement |
|---|---|
| **24 hours** | Early warning — confirm suspected malicious cause and/or cross-border impact |
| **72 hours** | Incident notification — initial severity assessment, impact scope, indicators of compromise |
| **1 month** | Final report — detailed description, threat type, root cause, mitigations applied, cross-border impact assessment |

"Significant incident" thresholds include: service disruption affecting a significant number of users, duration exceeding a few hours for critical services, geographic spread, or reputational/financial/safety impact.

**Remediation actions:**
- Add a NIS2 notification track to your existing Incident Response Plan (IRP).
- Pre-draft CSIRT notification templates for the 24h and 72h submissions.
- Define internal escalation triggers that activate the 24h clock (e.g., detection of malicious intrusion, ransomware, DDoS affecting service availability).
- Build the 1-month post-incident report template incorporating all Art. 23(4) mandatory fields.
- Identify your national CSIRT and competent authority and establish contact channels in advance.
- Test the full reporting workflow in annual tabletop exercises.

---

### Gap 3 — Art. 21(2)(j): Mandatory MFA (HIGH PRIORITY)

**ISO 27001 position:** A.8.5 covers secure authentication; MFA is recommended but not universally mandated.

**NIS2 requirement:** Art. 21(2)(j) explicitly requires multi-factor authentication (or continuous authentication), secured communications, and secured emergency communication systems.

**Remediation actions:**
- Deploy MFA for **all remote access** (VPN, remote desktop).
- Deploy MFA for **all privileged accounts** (system administrators, cloud IAM, database administrators).
- Deploy MFA for **cloud management consoles** (AWS, Azure, GCP, SaaS platforms holding sensitive data).
- Deploy MFA for **email access** and collaboration platforms.
- Apply **phishing-resistant MFA** (FIDO2 hardware tokens, passkeys) for highest-privilege accounts.
- Establish encrypted out-of-band emergency communication channels for incident coordination.
- Document any exceptions with compensating controls and risk acceptance.

---

### Gap 4 — Art. 26: Supply Chain Risk Assessments Coordinated by ENISA (MODERATE PRIORITY)

**ISO 27001 position:** A.5.19–A.5.23 cover supplier security; they address your bilateral supplier relationships.

**NIS2 requirement:** Art. 26 establishes a coordinated EU-level process for assessing risks in critical ICT supply chains. ENISA and national authorities conduct targeted risk assessments of specific vendors and products and may classify them as high-risk. Entities must integrate these assessments into their own risk management.

**Remediation actions:**
- Subscribe to ENISA supply chain risk assessment publications and advisories.
- Monitor national competent authority guidance on high-risk ICT vendors (e.g., 5G vendor risk assessments under the EU 5G Toolbox).
- Maintain a critical ICT supplier register and flag vendors subject to ENISA/national authority advisories.
- Include contractual obligations in supplier agreements: NIS2-aligned security baselines, incident notification SLAs, right-to-audit.
- Review and update supplier risk assessments when new ENISA coordinated assessments are published.

---

### Gap 5 — Self-Registration with National Competent Authority (ADMINISTRATIVE)

Most Member State transposition laws require in-scope entities to **proactively register** with the designated national competent authority or CSIRT. This is independent of any technical or organisational control implementation.

**Remediation action:** Identify the national competent authority in your Member State(s) of establishment and complete any required self-registration. Registration typically requires: organisation name, sector classification, contact details, and the identity of the designated NIS2 point of contact.

---

### Gap 6 — Proportionality Documentation for NIS2 (ADMINISTRATIVE)

NIS2 Art. 21(1) requires measures to be "appropriate and proportionate" based on risk exposure, entity size, likelihood and severity of incidents, and implementation cost. ISO 27001's risk-based approach partially satisfies this, but for NIS2 supervisory proceedings, you should:

**Remediation actions:**
- Document a NIS2 proportionality assessment justifying your control selection relative to Art. 21 measures.
- Ensure your risk assessment explicitly references NIS2 in-scope network and information systems.
- Map each Art. 21 measure to implemented controls with evidence references (policies, audit reports, test results).

---

## Prioritised Remediation Roadmap

| Priority | Action | Owner | Timeline |
|---|---|---|---|
| 1 | Confirm entity classification (EE or IE) and complete national authority self-registration | Legal / Compliance | Immediate |
| 2 | Document board approval of NIS2 cybersecurity policy and establish management training records | Board / CISO | 30 days |
| 3 | Add NIS2 notification track to IRP with pre-drafted 24h/72h templates and CSIRT contact details | Security Operations | 30–60 days |
| 4 | Conduct MFA gap assessment and deploy MFA to remote access, privileged accounts, and cloud consoles | IT / IAM | 30–90 days |
| 5 | Subscribe to ENISA supply chain advisories and update supplier register and contracts | Procurement / Legal | 60 days |
| 6 | Run NIS2 tabletop exercise testing the 24h/72h/1-month reporting workflow | Security Operations | 90 days |
| 7 | Draft NIS2 proportionality assessment document | Compliance / CISO | 60–90 days |
| 8 | Brief management body on personal liability under Member State transposition law | Legal | 60 days |

---

## Summary Assessment

| Area | ISO 27001 Coverage | NIS2 Gap |
|---|---|---|
| Technical & organisational security controls (Art. 21 M1–M9) | Strong | Low — minor alignment work |
| Mandatory MFA (Art. 21 M10) | Partial | Significant — explicit legal mandate |
| Governance and personal liability (Art. 20) | None | Critical — requires board action |
| Incident reporting timelines (Art. 23) | Partial | Critical — legal deadlines not addressed by ISO 27001 |
| ENISA supply chain coordination (Art. 26) | Partial | Moderate — ongoing monitoring obligation |
| Entity registration | Not applicable | Administrative — likely required by transposition law |

Your ISO 27001:2022 ISMS provides an excellent base. The work required to achieve NIS2 compliance is targeted rather than transformational — but the governance (Art. 20), incident reporting (Art. 23), and MFA (Art. 21(2)(j)) gaps carry the highest regulatory risk if left unaddressed, particularly for Essential Entities under proactive supervision.

---

*This analysis is based on the NIS2 Directive (EU) 2022/2555 as adopted and ISO/IEC 27001:2022. Member State transposition laws may impose additional or more prescriptive requirements. Always validate against the specific national implementing legislation applicable to your establishment(s).*
