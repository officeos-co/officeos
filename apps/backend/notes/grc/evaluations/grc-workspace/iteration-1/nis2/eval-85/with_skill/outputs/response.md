# NIS2 and DORA: Interaction, Precedence, and Compliance Programme Design for a European Bank

## Executive Summary

As a large European bank, you are subject to both the NIS2 Directive (Directive (EU) 2022/2555) and the Digital Operational Resilience Act (DORA, Regulation (EU) 2022/2554). These two instruments are deliberately designed to coexist through a lex specialis relationship: where DORA applies to your sector, it generally takes precedence over NIS2 for the specific obligations it addresses. However, this does not mean NIS2 is irrelevant — a carefully integrated programme is both permissible and strongly advisable. You do not need two entirely separate compliance programmes, but you do need a dual-framework governance structure that maps obligations, avoids duplication of effort, and closes gaps that exist between the two instruments.

---

## 1. Your NIS2 Classification: Essential Entity (Banking Sector)

Under NIS2 Art. 3 and Annex I, **banking** is explicitly listed as an essential entity sector. As a large European bank (almost certainly exceeding 250 employees and €50M turnover), you are an **Essential Entity (EE)** under NIS2.

Key consequences:
- **Proactive (ex-ante) supervision** under Art. 32 — your competent NIS2 authority can conduct on-site inspections, security audits, and targeted scans without a prior complaint or incident trigger.
- **Maximum penalties:** up to €10,000,000 or **2% of global annual turnover** (whichever is higher) under Art. 34.
- **Management body personal liability** under Art. 20 — board members and senior executives can be held personally liable under national law for failure to approve and oversee cybersecurity risk management measures.

---

## 2. The Lex Specialis Relationship: DORA Takes Precedence (with Limits)

### The Governing Provision: NIS2 Art. 4

NIS2 Art. 4(1) establishes a **lex specialis carve-out**: where a sector-specific EU legal act imposes requirements relating to cybersecurity risk management or incident reporting that are "at least equivalent" to the obligations in NIS2, the sector-specific act applies instead of (or alongside) NIS2.

DORA, which entered into application on **17 January 2025**, is explicitly such a lex specialis instrument for financial entities in scope, including credit institutions (banks). The European Supervisory Authorities (ESAs — EBA, ESMA, EIOPA) have confirmed this alignment in their joint supervisory statements.

### Practical Meaning

| Obligation Area | Governing Instrument | Notes |
|---|---|---|
| ICT risk management framework | DORA (Arts. 5–16) | Equivalent to NIS2 Art. 21; DORA is more prescriptive |
| Major ICT incident reporting | DORA (Arts. 17–23) | Replaces NIS2 Art. 23 timelines for financial entities |
| Digital operational resilience testing | DORA (Arts. 24–27, TLPT) | No NIS2 equivalent at this level of detail |
| ICT third-party risk / supply chain | DORA (Arts. 28–44) | More granular than NIS2 Art. 26 for financial entities |
| Governance and management body accountability | **Both** DORA and NIS2 Art. 20 | NIS2 adds personal liability dimension; confirm national transposition |
| Incident cross-border coordination | **Both** — different notification channels | NIS2: CSIRT; DORA: competent financial authority (e.g., ECB, national CA) |
| Supervisory cooperation | **Both** — different supervisors | NIS2 competent authority vs. financial prudential supervisor |

### Where NIS2 Still Bites

Even with DORA as lex specialis, NIS2 is **not fully displaced** for banks:

1. **Governance (Art. 20):** Member State transpositions may impose personal liability on management body members that goes beyond DORA's governance chapter. You must check your jurisdiction's NIS2 transposition law.
2. **Supply chain (Art. 26):** ENISA-coordinated critical ICT supply chain risk assessments under NIS2 can still identify your organisation and impose additional requirements.
3. **Notification to NIS2 CSIRT:** Where an incident has cross-border impact beyond the financial sector (e.g., systemic IT infrastructure outage affecting non-financial services), NIS2 notification obligations to the national CSIRT may apply concurrently.
4. **National transposition variations:** Not all EU Member States have transposed NIS2 identically. If you operate branches or subsidiaries across multiple EU jurisdictions, each Member State's NIS2 implementing law may treat the lex specialis carve-out differently.
5. **Residual NIS2 obligations:** If a Member State's NIS2 transposition does not fully map DORA as equivalent (which is a live policy question in some jurisdictions as of 2025–2026), both sets of obligations may apply in parallel.

---

## 3. Incident Reporting: Two Channels, Not One

This is the area where the interaction is most operationally complex.

### DORA Incident Reporting (Arts. 17–23)
- **Initial notification:** Within 4 hours of classification as a major incident; no later than 24 hours after becoming aware.
- **Intermediate report:** Within 72 hours.
- **Final report:** Within 1 month.
- Reported to: **Competent financial authority** (e.g., ECB for significant institutions under the SSM, or national competent authority for less significant institutions, national banking regulator).

### NIS2 Incident Reporting (Art. 23)
- **Early warning:** Within **24 hours** (suspected malicious act? cross-border impact?).
- **Incident notification:** Within **72 hours** (initial assessment, severity, IoCs).
- **Final report:** Within **1 month**.
- Reported to: **National CSIRT** or designated competent NIS2 authority.

### Practical Implication

For a major operational or cybersecurity incident at your bank, you may need to file notifications with **two different authorities** (financial supervisor and NIS2 CSIRT) on **overlapping but not identical timelines**, with **different content requirements**. Your incident response playbooks and SOC escalation procedures must explicitly handle both channels.

**Recommendation:** Establish a unified incident classification matrix that triggers both reporting streams simultaneously, with a single owner (typically the CISO or delegated Head of Cyber Incident Response) coordinating both filings. Engage proactively with both your financial competent authority and your national CSIRT to agree on joint notification protocols where permissible.

---

## 4. Do You Need Two Separate Compliance Programmes? No — But You Need a Dual-Framework Programme

Running two entirely separate compliance programmes would create duplicative effort, inconsistent controls, and governance confusion. The recommended approach is a **single integrated ICT and cyber resilience programme** with explicit dual-framework mapping.

### Programme Architecture

**Tier 1: Unified Control Framework**
Build your control library once, mapped to both DORA regulatory technical standards (RTS) and NIS2 Art. 21 measures. ISO 27001:2022 provides a useful common control reference, as its Annex A maps closely to NIS2 Art. 21 (and ISO 27001 certification provides strong evidence of NIS2 compliance, though it does not substitute formal NIS2 obligations).

**Tier 2: Regulatory Obligation Register**
Maintain a consolidated register that for each obligation records:
- Source instrument (DORA article / NIS2 article)
- Implementing control(s)
- Responsible owner
- Evidence artefact
- Relevant supervisory authority

**Tier 3: Separate Supervisory Interfaces**
Despite integrated controls, you need distinct interfaces for:
- Financial supervisor (DORA): ECB/SSM or national banking regulator
- NIS2 competent authority: national designated authority (often the national cybersecurity agency or sector regulator)
- National CSIRT: for incident early warnings

### NIS2 Art. 21 Measures vs. DORA Equivalents

| NIS2 Art. 21 Measure | DORA Equivalent | Gap? |
|---|---|---|
| 1. Risk analysis and information system security policies | DORA Art. 5 (ICT risk management framework) | Generally covered; DORA more prescriptive |
| 2. Incident handling | DORA Arts. 17–23 | DORA more detailed; maintain NIS2 CSIRT notification |
| 3. Business continuity, BCP, DR, crisis management | DORA Arts. 11–12 (BCP, backup, recovery) | Broadly equivalent |
| 4. Supply chain security | DORA Arts. 28–44 (third-party risk) | DORA more granular; NIS2 Art. 26 ENISA assessment still applies |
| 5. Security in NIS acquisition, development, maintenance | DORA Art. 9 (information security), Art. 10 (detection) | Check patching/vulnerability management gap |
| 6. Effectiveness assessment policies | DORA Art. 6 (ICT risk management framework review) | Broadly equivalent |
| 7. Cyber hygiene and training | DORA Art. 13 (learning and evolving), Art. 5(4) (training) | Broadly covered; verify NIS2 management training requirement |
| 8. Cryptography and encryption policies | DORA Art. 9(2) (data integrity/encryption) | Check for explicit cryptography policy documentation gap |
| 9. HR security, access control, asset management | DORA Art. 9 (general) | May need to explicitly document HR security elements |
| 10. MFA, continuous authentication, secured comms | DORA Art. 9(4) | Generally covered; verify MFA scope documentation |

**Key gap area:** NIS2 Art. 20 management body training and personal liability obligations — DORA requires management body "ultimate responsibility" for ICT risk but national NIS2 transpositions may add explicit personal liability exposure not present in DORA. This must be verified jurisdiction by jurisdiction.

---

## 5. Governance: Art. 20 and Management Body Obligations

Under NIS2 Art. 20, your **management body** (board of directors / supervisory board) must:
1. **Approve** cybersecurity risk management measures.
2. **Oversee** their implementation.
3. **Complete regular cybersecurity training** — and ensure senior management does too.
4. Be personally accountable under national law.

DORA Art. 5 similarly requires the management body to define and approve the ICT risk management framework and bear ultimate responsibility. However, personal liability is a NIS2 concept that DORA does not replicate.

**Action:** Establish a board-level Cyber and Digital Resilience Oversight Charter that explicitly references both DORA and NIS2 obligations. Ensure board cybersecurity training programmes cover both regulatory frameworks. Obtain documented board approval of both your DORA ICT risk management framework and your NIS2 cybersecurity risk management measures — this can be a single document with dual-framework references.

---

## 6. Supervisory Interaction and Coordination

A large European bank may be subject to multiple supervisors simultaneously:
- **ECB / national banking regulator** (DORA supervisor, potentially also NIS2 competent authority if the Member State designates the financial regulator)
- **National NIS2 competent authority** (often the national cybersecurity agency, e.g., BSI in Germany, ANSSI in France, NCSC-NL in Netherlands)
- **National CSIRT** for incident early warnings

In some Member States, the financial sector NIS2 competent authority has been designated as the financial regulator, which reduces but does not eliminate the dual-supervisor complexity.

**Recommendation:** Map your full supervisory matrix by jurisdiction for every entity in your group. Engage proactively with all relevant authorities to establish communication protocols, understand their expectations on evidence of DORA-as-NIS2-equivalence, and agree on joint inspection coordination mechanisms where possible.

---

## 7. Penalty Exposure Analysis

As an Essential Entity under NIS2, your maximum NIS2 penalty exposure is:
- **€10,000,000 or 2% of global annual turnover** (whichever is higher)

For a large European bank, global turnover will almost certainly make the 2% figure the binding cap — this represents very substantial financial exposure.

DORA penalties are set by Member States and vary, but are similarly material for systemically important institutions.

**Critical note:** Penalties under DORA and NIS2 are imposed by different authorities and are not mutually exclusive. A single major ICT incident could in principle trigger both DORA enforcement by the financial supervisor and NIS2 enforcement by the cybersecurity authority, resulting in penalties under both instruments. This double-jeopardy risk underscores the importance of maintaining documented compliance evidence against both frameworks.

---

## 8. Recommended Next Steps

1. **Jurisdiction mapping:** For each EU Member State where you have regulated entities, confirm (a) how NIS2 has been transposed, (b) which authority is designated as NIS2 competent authority for banking, and (c) whether that authority has formally confirmed DORA as equivalent lex specialis.

2. **Obligation gap analysis:** Using the Art. 21 vs. DORA mapping table above, identify any obligations in NIS2 not fully addressed by your existing DORA programme, particularly: management body personal liability documentation, cryptography policy documentation, HR security, and CSIRT notification procedures.

3. **Unified incident response playbook:** Update your incident response procedures to explicitly include a dual-notification workflow (DORA channel to financial supervisor + NIS2 channel to CSIRT), with clear escalation triggers, timeline tracking, and content requirements for each channel.

4. **Board-level documentation:** Obtain explicit board approval of cybersecurity risk management measures framed to satisfy both DORA Art. 5 and NIS2 Art. 20. Document management body cybersecurity training completion.

5. **Supply chain review:** Ensure your third-party ICT risk programme (DORA Chapter V) also addresses the NIS2 Art. 26 supply chain risk assessment requirements, including any ENISA-coordinated critical supply chain assessments that may affect your sector.

6. **Integrated control register:** Build or update your GRC tooling to map each control to both DORA RTS requirements and NIS2 Art. 21 measures, enabling single-pane-of-glass compliance evidence.

7. **Cross-border coordination:** If you operate in multiple EU Member States, establish a cross-border NIS2 coordination mechanism for significant incidents with cross-border impact, as required under NIS2.

---

## Summary

| Question | Answer |
|---|---|
| Does NIS2 apply to you? | Yes — you are an Essential Entity (banking, Annex I) |
| Does DORA take precedence over NIS2? | Generally yes, for specific ICT risk management and incident reporting obligations (lex specialis, Art. 4 NIS2), but NIS2 is not fully displaced |
| Which areas remain NIS2-specific? | Management body personal liability; CSIRT notification; ENISA supply chain assessments; national transposition variations; cross-border incident coordination |
| Do you need two separate programmes? | No — build one integrated programme with dual-framework mapping |
| What is your biggest risk? | Dual penalty exposure (NIS2 + DORA) from a major incident; management body personal liability under NIS2; multi-jurisdiction supervisory complexity |

The most important immediate action is to confirm — jurisdiction by jurisdiction — whether your national NIS2 transposition formally recognises DORA as the lex specialis equivalent, and to close any governance and notification gaps that exist between the two frameworks in the interim.
