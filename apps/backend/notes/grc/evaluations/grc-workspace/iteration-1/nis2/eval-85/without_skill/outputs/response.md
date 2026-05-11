# NIS2 and DORA: Interaction, Precedence, and Compliance Programme Design for European Banks

## Executive Summary

As CISO of a large European bank subject to both the Network and Information Security Directive 2 (NIS2) and the Digital Operational Resilience Act (DORA), you are operating within a carefully designed regulatory framework where DORA takes legal precedence over NIS2 for your institution — but this does not eliminate your NIS2 obligations, and the two frameworks are more complementary than conflicting. You do not need two fully separate compliance programmes; a unified, integrated programme is both feasible and recommended. However, you do need to be precise about which regulation governs which obligation and how to evidence compliance to two potentially different supervisory bodies.

---

## 1. Understanding the Two Frameworks

### NIS2 (Directive (EU) 2022/2555)

NIS2 is a directive that member states transposed into national law by 17 October 2024. It establishes cybersecurity risk management and incident reporting obligations for "essential" and "important" entities across critical sectors. Large banks are classified as "essential entities" under Annex I (financial market infrastructures) or under the financial sector category.

Key obligations under NIS2 include:
- Cybersecurity risk management measures (Article 21)
- Supply chain security
- Incident reporting to national Computer Security Incident Response Teams (CSIRTs) or competent authorities within defined timeframes (24-hour early warning, 72-hour notification, one-month final report)
- Management body accountability and training
- Use of appropriate technical and organisational measures (encryption, MFA, access control, vulnerability handling, etc.)

### DORA (Regulation (EU) 2022/2554)

DORA is a regulation (directly applicable across the EU without transposition) that has applied since 17 January 2025. It establishes a comprehensive ICT risk management and digital operational resilience framework specifically for financial entities, including credit institutions (banks), investment firms, payment institutions, and others.

Key obligations under DORA include:
- ICT risk management framework (Chapter II)
- ICT-related incident classification, management, and reporting (Chapter III) — including to competent financial supervisory authorities
- Digital operational resilience testing, including Threat-Led Penetration Testing (TLPT) for significant institutions (Chapter IV)
- ICT third-party risk management and contractual requirements (Chapter V)
- Information sharing arrangements (Chapter VI)
- Oversight of Critical ICT Third-Party Providers (CTPPs) by ESAs (EBA, ESMA, EIOPA)

---

## 2. Which Takes Precedence? The Lex Specialis Principle

### DORA as Lex Specialis

DORA explicitly establishes itself as the sector-specific law (lex specialis) for financial entities with respect to ICT risk and digital operational resilience. Recital 16 and Article 2(1) of DORA confirm its scope, and critically, **Article 1(2) of NIS2** provides that where sector-specific Union legal acts require financial entities to adopt cybersecurity risk management measures or incident reporting obligations that are "at least equivalent" to those in NIS2, the relevant NIS2 provisions do not apply to those entities.

This means:
- DORA, being a directly applicable regulation (not a directive) with more detailed and sector-specific requirements, supersedes the equivalent NIS2 obligations for financial entities covered by DORA.
- Financial entities are effectively carved out of the relevant NIS2 operational obligations (Articles 21 and 23 of NIS2 — risk management measures and incident reporting) to the extent DORA covers the same ground.

### What This Means in Practice

| Area | Governing Regulation |
|------|---------------------|
| ICT risk management framework | DORA (Chapter II) |
| ICT incident reporting to financial supervisor | DORA (Chapter III) — to EBA/NCAs |
| Incident reporting to CSIRT/NIS2 authority | NIS2 still applies for network/information security incidents outside DORA's scope |
| TLPT / penetration testing | DORA (Chapter IV) |
| ICT third-party risk (contractual requirements) | DORA (Chapter V) |
| Supply chain security (broader) | NIS2 (Article 21(2)(d)) — supplements DORA |
| Management body accountability | Both — DORA Article 5 and NIS2 Article 20 |
| Vulnerability disclosure | NIS2 (Article 12) |
| National CSIRT cooperation | NIS2 |
| Sanctions/enforcement | DORA: financial supervisors (NCAs, ESAs); NIS2: national NIS competent authorities |

### Important Nuance: NIS2 Is Not Fully Displaced

The NIS2 carve-out is not a blanket exemption. NIS2 obligations that fall outside DORA's scope — such as obligations relating to the general security of network and information systems beyond ICT risk, broader supply chain obligations, vulnerability handling at a national level, and coordination with national CSIRTs — continue to apply. Member states may also impose additional NIS2-derived obligations in their national transpositions that go beyond the baseline, which requires monitoring.

Additionally, DORA's equivalence must genuinely be assessed. The European Supervisory Authorities (EBA, ESMA, EIOPA) have issued regulatory technical standards (RTS) and implementing technical standards (ITS) under DORA. If any specific NIS2 requirement is not demonstrably covered by DORA and its subordinate legislation, NIS2 continues to apply to fill that gap.

---

## 3. Do You Need Two Separate Compliance Programmes?

**No — but you need an integrated programme with careful dual-mapping.**

Running two entirely separate compliance programmes would be duplicative, resource-inefficient, and likely counterproductive (creating inconsistencies between frameworks). The better approach is a single, unified ICT and cyber resilience governance programme that:

### a) Uses DORA as the Primary Structural Framework

Because DORA is more prescriptive and detailed for financial entities, build your governance architecture, risk management lifecycle, incident response, third-party risk, and testing programme around DORA's requirements. This ensures you meet the higher bar of your primary sectoral regulator (the NCA and ESAs).

### b) Maps NIS2 Residual Obligations onto the DORA Structure

Identify where NIS2 requirements are not fully addressed by DORA (e.g., national CSIRT reporting channels, broader supply chain considerations, vulnerability disclosure coordination) and integrate these as supplementary controls or process steps within your existing DORA-compliant framework. This is an additive layer, not a parallel structure.

### c) Maintains Clear Regulatory Reporting Lanes

DORA and NIS2 have different reporting channels:
- **DORA incidents**: Report to your financial sector competent authority (e.g., ECB for significant credit institutions under SSM, or national banking supervisor) using DORA's harmonised incident templates (per EBA ITS on major incident reporting).
- **NIS2 incidents**: Report to the national NIS2 competent authority and/or CSIRT. In practice, the regulators are working on coordination mechanisms (e.g., Article 42 of DORA mandates cooperation between financial supervisors and NIS2 authorities), but operationally, you must understand which body receives which notification and within what timeframe.

### d) Addresses Management Accountability Jointly

Both DORA (Article 5) and NIS2 (Article 20) impose obligations on the management body. A single board-level ICT and cyber risk governance framework, with appropriate training and oversight mechanisms, satisfies both simultaneously.

### e) Third-Party Risk: Leverage DORA's Deeper Requirements

DORA's Chapter V on ICT third-party risk management is more prescriptive than NIS2's supply chain security provisions. A DORA-compliant third-party risk programme (including mandatory contractual clauses for ICT service providers) will generally meet or exceed NIS2's supply chain requirements.

---

## 4. Key Practical Recommendations

1. **Conduct a gap/overlap analysis** mapping DORA requirements (and associated RTS/ITS) against NIS2 Articles 21 and 23, and identify any NIS2 residual obligations not covered by DORA in your jurisdiction's national transposition law.

2. **Establish dual notification SOPs**: Your incident response playbooks should identify at decision points which regulator(s) must be notified, on what timeline, using what format. DORA and NIS2 timelines differ and regulators are distinct; operational clarity here is essential.

3. **Engage both your financial supervisor and NIS2 competent authority** to clarify their expectations around the lex specialis carve-out, particularly for incidents that may span both frameworks. Some national authorities have issued joint guidance.

4. **Monitor DORA RTS/ITS implementation**: Several DORA technical standards (e.g., on TLPT, on major incident reporting, on third-party risk) are either final or in consultation. Ensure your programme incorporates these as they take effect.

5. **Track national NIS2 transpositions**: As a large bank operating potentially across multiple EU jurisdictions, the NIS2 residual obligations may vary by member state. Your NIS2 exposure is defined by national law, not the directive text directly.

6. **Coordinate with your DPO and legal team**: Where incidents involve personal data, GDPR notification obligations (72-hour reporting to data protection authorities) layer on top of DORA and NIS2. All three frameworks may be triggered simultaneously by a significant incident.

---

## 5. Summary

| Question | Answer |
|----------|--------|
| Does DORA take precedence over NIS2? | Yes, as lex specialis for financial entities under Article 1(2) of NIS2, DORA governs ICT risk and incident reporting obligations where it is at least equivalent |
| Are you fully exempt from NIS2? | No. NIS2 continues to apply where DORA does not cover the same ground (e.g., national CSIRT reporting, some supply chain/vulnerability obligations) |
| Do you need two separate programmes? | No. Build one integrated programme on the DORA framework and add NIS2 residual obligations as supplementary elements |
| Who supervises what? | Financial supervisor (NCA/ECB) for DORA; national NIS2 authority for residual NIS2 obligations |
| What is the greatest operational risk? | Dual incident reporting failures — ensure your SOPs clearly specify reporting lanes, timelines, and formats for both regulators |

The regulatory design intent is clear: DORA was created precisely to give financial entities a coherent, sector-specific resilience framework, avoiding the duplication and fragmentation that would result from applying generic cybersecurity directives on top of detailed sectoral rules. Your compliance programme design should reflect that intent.
