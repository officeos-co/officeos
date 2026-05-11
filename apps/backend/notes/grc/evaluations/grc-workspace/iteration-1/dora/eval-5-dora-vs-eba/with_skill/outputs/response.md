# DORA vs. EBA Guidelines on ICT and Security Risk Management: Key Differences

## Critical Preliminary Point: EBA/GL/2019/04 Has Been Superseded

The most important thing to understand is that **EBA/GL/2019/04 (EBA Guidelines on ICT and Security Risk Management) no longer applies to you as a DORA in-scope entity.** Since **17 January 2025** — DORA's application date under Art. 64 — DORA is the governing framework for in-scope EU financial entities. The EBA Guidelines were soft law guidance. DORA is a directly applicable EU Regulation (Regulation (EU) 2022/2554).

Being "EBA Guidelines compliant" does **not** mean you are DORA compliant. There are substantial new requirements in DORA with no equivalent in the EBA Guidelines. This response maps the key differences to help you identify your residual gaps.

---

## Comparison Table: DORA vs. EBA/GL/2019/04

### 1. Legal Status and Binding Nature

| Dimension | EBA/GL/2019/04 | DORA (Regulation (EU) 2022/2554) |
|-----------|---------------|----------------------------------|
| Legal form | Soft law guidelines (comply-or-explain) | Directly applicable EU Regulation — binding law |
| Enforcement | National supervisor discretion | Supervisory powers mandated by DORA Art. 46–56; fines and sanctions |
| Article-level obligations | General principles | Specific, granular Article-level obligations with RTS/ITS specifying detailed requirements |

**Gap:** EBA Guidelines compliance cannot be relied upon as DORA compliance. DORA creates mandatory obligations, not best practice guidance.

---

### 2. ICT Risk Management Framework

| Dimension | EBA/GL/2019/04 | DORA Art. 5–16 |
|-----------|---------------|----------------|
| Board accountability | Recommended; principles-based | Art. 5(1): Explicit board-level legal responsibility for ICT risk |
| Board approval of ICT policy | Encouraged | Art. 5(2)(b): Mandatory — board must formally approve ICT security policies |
| RMF documentation | Required at high level | Art. 6(1): Comprehensive documented RMF with mandatory elements per CDR (EU) 2024/1774 |
| Annual RMF review | Encouraged | Art. 6(5): Mandatory annual review and review after major incidents |
| ICT asset register | Mentioned | Art. 8(4): Mandatory; must be mapped to critical/important functions |
| Backup restore testing | Good practice | Art. 12(3): Mandatory — restorability of backups must be tested |
| RTO/RPO | Mentioned | Art. 11(2): Mandatory RTO and RPO definition for critical functions |

**Key gaps if EBA-compliant only:**
- Board may not have formally approved ICT risk appetite as a distinct, documented obligation
- ICT RMF may lack the granular elements specified in CDR 2024/1774
- Asset register may exist but not be mapped to critical/important functions per Art. 8 requirements
- Backup restore testing may be informal or undocumented

---

### 3. ICT-Related Incident Management and Reporting

This is an area of **substantial new obligation** under DORA with no equivalent in EBA/GL/2019/04.

| Dimension | EBA/GL/2019/04 | DORA Art. 17–23 |
|-----------|---------------|-----------------|
| Incident classification criteria | General guidance | Art. 18(1)(a)–(f): Six mandatory classification criteria with quantitative thresholds in CDR (EU) 2024/1772 |
| Mandatory major incident reporting | Not required | Art. 19: Mandatory three-stage reporting to competent authority |
| Reporting timelines | Not specified | Art. 19: 4h initial / 72h intermediate / 1 month final |
| Reporting templates | None | CIR (EU) 2025/302: Mandatory standard forms for submission |
| Voluntary cyber threat reporting | Not covered | Art. 19(2): Optional voluntary reporting of significant cyber threats |
| Payment-related incidents | PSD2/EBA separate | Art. 23: Integrated within DORA for credit institutions |

**Key gaps if EBA-compliant only:**
- No formal incident classification framework using the Art. 18(1) / CDR 2024/1772 criteria
- No three-stage reporting process or documented reporting SOP
- No templates aligned with CIR 2025/302
- Competent authority reporting channel and contact not pre-identified
- 4h/72h/1-month timelines not embedded in incident response procedures

---

### 4. Digital Operational Resilience Testing

Another area of **entirely new obligations** under DORA.

| Dimension | EBA/GL/2019/04 | DORA Art. 24–27 |
|-----------|---------------|-----------------|
| Testing requirements | General security testing recommended | Art. 24–25: Mandatory annual testing programme with specific test types |
| TLPT | Not covered | Art. 26: Mandatory for significant entities; at least every 3 years; requires threat intelligence and live production system testing |
| Tester qualifications | Not specified | Art. 27: Certification requirements, conflict-of-interest rules |
| Competent authority involvement | None | Art. 26: Pre- and post-TLPT notification; competent authority attestation |

**Key gaps if EBA-compliant only:**
- No formal annual resilience testing programme as defined in Art. 24–25
- TLPT has almost certainly never been conducted; entity likely has not assessed whether it is subject to TLPT
- No relationship with competent authority on TLPT scheduling and attestation

---

### 5. ICT Third-Party Risk Management

DORA's Chapter V is **substantially more prescriptive** than EBA/GL/2019/04's third-party provisions.

| Dimension | EBA/GL/2019/04 | DORA Art. 28–44 |
|-----------|---------------|-----------------|
| Third-party risk policy | Required | Art. 28(1): Required; detailed content specified in CDR (EU) 2024/1773 |
| Register of Information | Not required in this form | Art. 28(3) + CIR 2024/2956: Mandatory Register with specific mandatory fields; submitted to competent authority |
| Contractual provisions | High-level guidance | Art. 30(2)(a)–(i): Nine mandatory contractual provisions per CDR 2024/1773 |
| Subcontracting provisions | Mentioned | CDR (EU) 2025/532: Detailed subcontracting rules; prior consent required |
| Concentration risk | Mentioned | Art. 28(6) + Art. 29: Formal concentration risk assessment mandatory |
| Exit strategies | Mentioned | Art. 28(7): Mandatory documented exit strategies for critical arrangements |
| Critical TPSP oversight | None | Art. 31–44: EU-level oversight of CTPPs by Lead Overseer; JON; JETs |

**Key gaps if EBA-compliant only:**
- Existing vendor management framework likely lacks CIR 2024/2956 Register of Information fields
- Existing contracts predate DORA; likely missing Art. 30(2)(e) audit rights, (f) termination rights, (h) data portability, (i) subcontracting consent
- No formal concentration risk documentation
- Exit strategies may be absent or informal
- No awareness of Critical TPSP designations relevant to your providers

---

### 6. Information Sharing

| Dimension | EBA/GL/2019/04 | DORA Art. 45 |
|-----------|---------------|--------------|
| Cyber threat sharing | Not specifically covered | Art. 45: Voluntary cyber threat intelligence sharing arrangements explicitly permitted and regulated |

---

## Summary: Your Key Residual Gaps

Based on EBA Guidelines compliance as a starting point, your likely material gaps under DORA are:

| Gap Area | DORA Obligation | Priority |
|---------|----------------|----------|
| Three-stage incident reporting process | Art. 19, CDR 2025/301, CIR 2025/302 | HIGH — new obligation with no EBA equivalent |
| Incident classification using CDR 2024/1772 thresholds | Art. 18, CDR 2024/1772 | HIGH |
| Annual resilience testing programme | Art. 24–25 | HIGH |
| TLPT applicability assessment and scheduling | Art. 26, CDR 2025/1190 | HIGH |
| Register of Information per CIR 2024/2956 | Art. 28(3), CIR 2024/2956 | HIGH |
| Contract remediation for Art. 30(2)(a)–(i) provisions | Art. 30, CDR 2024/1773 | HIGH — likely requires contract renegotiation |
| Concentration risk formal assessment | Art. 28(6), Art. 29 | MEDIUM |
| Exit strategies for critical arrangements | Art. 28(7) | MEDIUM |
| Board-level ICT governance formalisation | Art. 5(1)–(2) | MEDIUM — likely requires governance process changes |
| CDR 2024/1774 RMF elements gap | Art. 6, CDR 2024/1774 | MEDIUM |

---

## Important Note

Do not treat EBA Guidelines compliance as a shortcut to DORA compliance. DORA is a distinct regulation with specific Article-level obligations, many of which have no equivalent in EBA/GL/2019/04. A formal DORA gap analysis against the full Chapter II–V framework is strongly recommended to identify all residual obligations.
