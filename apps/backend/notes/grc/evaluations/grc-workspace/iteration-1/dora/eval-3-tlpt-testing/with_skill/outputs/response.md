# DORA Digital Operational Resilience Testing Requirements

## Overview

DORA's digital operational resilience testing requirements are set out in **Chapter IV (Art. 24–27)**. The framework has two tiers: (1) a **basic testing programme** applicable to all in-scope financial entities, and (2) **advanced TLPT** applicable to significant financial entities meeting specific criteria.

---

## Tier 1 — Basic Digital Operational Resilience Testing Programme (Art. 24–25)

### Who Must Comply

All in-scope financial entities, including your bank, must implement a basic digital operational resilience testing programme (Art. 24(1)).

### Required Testing Types (Art. 24–25)

| Testing Type | Description |
|-------------|-------------|
| Vulnerability assessments and scans | Systematic identification of vulnerabilities in ICT systems and infrastructure |
| Source code reviews | Review of application code for security vulnerabilities (where applicable) |
| Scenario-based testing | Testing response to defined adverse scenarios |
| Compatibility tests | Testing system interactions and interfaces |
| Performance tests | Stress testing under expected and peak load conditions |
| End-to-end tests | Testing of complete business process chains |
| Network security assessments | Assessment of network architecture, segmentation and controls |
| Gap analyses | Identification of gaps against policy, standards and best practice |

### Frequency

Critical ICT systems must be tested at least **once a year** (Art. 24(1)).

### Independence Requirement

Tests must be performed by **independent internal or external parties** — internal testers must be operationally independent from the systems being tested (Art. 24(4)).

---

## Tier 2 — Advanced Testing: TLPT (Threat-Led Penetration Testing) (Art. 26)

### What Is TLPT?

TLPT (Threat-Led Penetration Testing) is **not** a standard penetration test. It is an **intelligence-led, adversarial simulation** that:
- Uses real threat intelligence about actual threat actors targeting the financial sector
- Simulates the full attack lifecycle (reconnaissance, initial access, persistence, lateral movement, objectives)
- Is conducted against **live production systems** — not test environments
- Uses qualified external red teams

TLPT is aligned with the **TIBER-EU framework** (which many EU central banks have operated informally). Under DORA Art. 26, TLPT formally supersedes TIBER-EU for in-scope entities. Results are **mutually recognised across EU jurisdictions** for cross-border entities (Art. 26(5)), avoiding duplicate testing.

### Who Must Conduct TLPT?

TLPT is required for financial entities meeting the criteria in Art. 26(8), which relate to:
- **Size and overall risk profile** of the entity
- **Scale and complexity** of ICT systems
- **Relevance to financial stability**

The detailed criteria are further specified in **CDR (EU) 2025/1190**. As a bank of any meaningful size, you should formally assess whether you meet these thresholds — most significant credit institutions do.

### TLPT Frequency

TLPT must be conducted at least **once every 3 years** (Art. 26(1)).

Your competent authority may also require TLPT on specific systems on a more frequent basis (Art. 26(7)).

### Scope of TLPT (Art. 26(2)–(3))

- Must cover **critical or important functions** and their underlying ICT systems
- Must be conducted against **live production systems** (not sandboxes or test environments)
- ICT third-party service providers supporting critical functions **may be included in scope**, with their consent (Art. 26(3)) — for example, AWS supporting a critical function could be in scope

### TLPT Process (Art. 26 + CDR (EU) 2025/1190)

| Phase | Description |
|-------|-------------|
| 1. Scoping | Define critical functions and systems in scope; notify competent authority |
| 2. Threat intelligence | Commission a threat intelligence report from a qualified provider identifying relevant threat actors, techniques, tactics, and procedures (TTPs) |
| 3. Red team test | External red team conducts adversarial simulation using threat intelligence as a guide against live production systems |
| 4. Remediation | Entity remediates findings from the red team exercise |
| 5. Competent authority notification | Notify before and after TLPT completion |
| 6. Attestation | Competent authority issues an attestation letter upon satisfactory completion |
| 7. Mutual recognition | Results shared across EU jurisdictions as applicable |

### TLPT Tester Requirements (Art. 27)

- Must demonstrate capability, integrity, and a sound risk methodology (Art. 27(1))
- Must hold **relevant professional certifications** (Art. 27(2))
- Must have **no conflicts of interest** with the tested entity (Art. 27(3))
- Competent authority maintains a list of qualified testers (Art. 27(4))

Key RTS: **CDR (EU) 2025/1190** specifies detailed TLPT requirements and tester qualifications.

---

## Summary: Testing Requirements at a Glance

| Requirement | Basis | Applies To | Frequency |
|------------|-------|-----------|-----------|
| Vulnerability assessments | Art. 24–25 | All entities | At least annually for critical systems |
| Source code reviews | Art. 25 | All entities (where applicable) | At least annually |
| Scenario-based testing | Art. 25 | All entities | At least annually |
| Network security assessments | Art. 24 | All entities | At least annually |
| TLPT | Art. 26, CDR 2025/1190 | Significant entities meeting Art. 26(8) criteria | At least every 3 years |

---

## Practical Implications for Your Bank

1. **Assess TLPT applicability now**: Formally document whether you meet the Art. 26(8) criteria under CDR 2025/1190. Most banks above micro-enterprise size will qualify.
2. **Establish your basic testing programme**: If you do not have a formal, documented annual testing schedule covering the Art. 25 test types, this is a compliance gap.
3. **Engage qualified testers**: For TLPT, you must use external testers with no conflicts of interest; internal security teams cannot conduct TLPT.
4. **Include third parties in scope**: Consider whether cloud providers (AWS) supporting critical functions should be included in TLPT scope (with their consent).
5. **Notify your competent authority**: TLPT requires pre- and post-notification to your competent authority; this is not a purely internal exercise.
