# DORA Digital Operational Resilience Testing Requirements

## Overview

DORA establishes mandatory digital operational resilience testing requirements for EU financial entities. The testing regime has two tiers: a basic programme applicable to all financial entities, and advanced threat-led penetration testing (TLPT) for larger, more significant institutions.

---

## Basic Testing Programme

### What Is Required

All in-scope financial entities, including banks, must implement a digital operational resilience testing programme. The types of testing required include:

- **Vulnerability assessments**: Systematic scans and assessments to identify weaknesses in ICT systems
- **Network security assessments**: Evaluation of network architecture, controls, and segmentation
- **Gap analyses**: Identification of gaps against standards and policies
- **Scenario-based tests**: Testing responses to defined adverse scenarios (e.g., system failures, cyber attacks)
- **Compatibility tests**: Testing of system integrations and interfaces
- **Performance tests**: Stress and load testing to verify system capacity

### How Often

Critical ICT systems should be tested **at least annually**. The testing programme should be documented and formally scheduled.

### Who Can Perform Tests

Tests should be conducted by independent parties — either internal teams that are independent from the systems being tested, or external third-party testers. The key requirement is independence and objectivity.

---

## TLPT — Threat-Led Penetration Testing

### What Is TLPT?

Threat-Led Penetration Testing (TLPT) is an advanced form of security testing that goes beyond standard penetration testing. Key characteristics:

- **Intelligence-led**: TLPT is based on real threat intelligence about threat actors, their techniques, tactics, and procedures (TTPs) that are relevant to your institution
- **Adversarial simulation**: TLPT simulates a full, realistic attack by a sophisticated threat actor across the full attack lifecycle
- **Live production systems**: Unlike standard pen tests which often target test environments, TLPT is conducted against live production systems
- **Red team approach**: External qualified red teams conduct the exercise, attempting to achieve defined objectives (e.g., accessing core banking systems, exfiltrating data)

TLPT is based on the **TIBER-EU framework** (Threat Intelligence-Based Ethical Red-Teaming) developed by the ECB, which many EU financial institutions will be familiar with. DORA formalises TLPT as a regulatory requirement.

### Who Must Conduct TLPT?

TLPT is not required of all financial entities — it applies to larger, more significant institutions that meet certain criteria relating to:
- Size and systemic importance
- Complexity of ICT systems
- Overall risk profile

Smaller institutions are generally not required to conduct TLPT, though they may do so voluntarily. Whether your institution is subject to TLPT will depend on whether your competent authority determines you meet the relevant thresholds.

### How Often Is TLPT Required?

DORA requires TLPT to be conducted **at least every 2–3 years** (the exact frequency is typically every 3 years). Your competent authority may require more frequent testing in specific circumstances.

### The TLPT Process

A typical TLPT process under DORA involves:

1. **Scoping**: Defining which critical functions and systems are in scope
2. **Threat intelligence gathering**: Commissioning a threat intelligence report from a qualified provider
3. **Red team simulation**: Qualified external testers conduct the adversarial simulation
4. **Reporting and remediation**: Findings are documented and remediated
5. **Supervisor involvement**: The competent authority is notified and involved in the process
6. **Attestation**: Upon completion, an attestation may be issued by the competent authority

### Tester Requirements

TLPT must be conducted by qualified external testers who:
- Have relevant technical expertise and certifications
- Have no conflicts of interest with the tested institution
- Can demonstrate a sound methodology

### Cross-Border Recognition

For institutions operating in multiple EU Member States, DORA provides for mutual recognition of TLPT results across jurisdictions, avoiding the need for duplicate testing.

---

## Summary

| Testing Type | Applies To | Frequency |
|-------------|-----------|-----------|
| Basic testing (vulnerability scans, network security, scenarios) | All financial entities | At least annually |
| TLPT | Significant institutions meeting criteria | At least every 3 years |

---

## Practical Considerations

- Document your testing programme formally with schedules and results
- Assess whether your institution meets the TLPT threshold criteria
- Engage qualified external testers for TLPT who meet DORA's requirements
- Consider whether critical cloud providers (like AWS) should be in scope for TLPT
- Use test results to drive remediation and improvement in your resilience posture
