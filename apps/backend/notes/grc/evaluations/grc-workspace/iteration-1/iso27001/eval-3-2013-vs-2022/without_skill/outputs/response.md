# ISO 27001:2013 vs ISO 27001:2022 — Key Differences

## Overview

ISO 27001:2022 was published in October 2022 and represents the most significant revision to the standard since the 2013 version. The transition deadline was October 2025, meaning all certifications needed to migrate to the 2022 version by then.

The core mandatory clauses (4–10) remain largely the same, but the Annex A controls were significantly restructured.

---

## Structural Changes to Annex A

### Control Count

| Version | Number of Controls | Number of Domains/Themes |
|---------|-------------------|--------------------------|
| 2013 | 114 controls | 14 domains |
| 2022 | 93 controls | 4 themes |

The reduction from 114 to 93 controls does not mean security requirements got weaker — many controls were merged or consolidated. No security requirements were genuinely dropped.

### Themes vs Domains

**2013 had 14 domains**, including:
- A.5 Information security policies
- A.6 Organisation of information security
- A.7 Human resource security
- A.8 Asset management
- A.9 Access control
- A.10 Cryptography
- A.11 Physical and environmental security
- A.12 Operations security
- A.13 Communications security
- A.14 System acquisition, development and maintenance
- A.15 Supplier relationships
- A.16 Information security incident management
- A.17 Business continuity management
- A.18 Compliance

**2022 has 4 themes:**
- **Organisational controls** (A.5) — 37 controls
- **People controls** (A.6) — 8 controls
- **Physical controls** (A.7) — 14 controls
- **Technological controls** (A.8) — 34 controls

---

## New Controls in 2022

ISO 27001:2022 introduced 11 entirely new controls that had no direct equivalent in 2013. These cover modern security concerns:

1. **Threat intelligence** — Formal processes for gathering and acting on threat information
2. **Information security for use of cloud services** — Specific requirements for cloud governance
3. **ICT readiness for business continuity** — IT-specific business continuity requirements
4. **Physical security monitoring** — Surveillance and monitoring requirements
5. **Configuration management** — Managing and maintaining secure system configurations
6. **Information deletion** — Secure and compliant data deletion processes
7. **Data masking** — Protecting sensitive data in non-production environments
8. **Data leakage prevention** — Controls to prevent unauthorised data exfiltration
9. **Monitoring activities** — Broader network and system monitoring
10. **Web filtering** — Controls for web access
11. **Secure coding** — Security requirements in the software development process

---

## Changes to Mandatory Clauses (4–10)

Most clause requirements are unchanged. The notable additions are:

### New Clause 6.3 — Planning of Changes
When changes to the ISMS are needed, they must be carried out in a planned manner. This is a new sub-clause requiring you to document how you manage changes to the ISMS itself (not just IT changes).

### Clause 9.2 — Internal Audit (Split)
The 2022 version splits this into two sub-clauses, requiring a more formally documented internal audit programme that specifies frequency, methods, and responsibilities.

### Clause 9.3 — Management Review (Split)
This is split into three sub-clauses with more explicit requirements around documenting inputs to and outputs from management reviews.

---

## Controls That Were Merged

Many 2013 controls that covered related topics were merged into single 2022 controls. For example:
- The 2013 access control domain (A.9) was consolidated and reorganised
- Multiple incident management controls (A.16 in 2013) are now in A.5.24–A.5.28
- Cryptography controls are now a single control (A.8.24) rather than two

---

## New Attribute System

A significant change in 2022 is the addition of attributes for each control. Each control is tagged with:
- **Control type:** Preventive, Detective, or Corrective
- **Information security properties:** Confidentiality, Integrity, Availability
- **Cybersecurity concepts:** Aligned to NIST CSF (Identify, Protect, Detect, Respond, Recover)
- **Operational capabilities:** What area the control addresses
- **Security domains:** Governance, Protection, Defence, Resilience

While using these attributes is not mandatory, they help organisations filter controls by type and prioritise implementation.

---

## What You Need to Do to Transition

### 1. Update Your Statement of Applicability (SoA)
Remap all 2013 control references to their 2022 equivalents. Add the 11 new controls — assess each for applicability and document your decision (include or exclude with justification).

### 2. Implement the 11 New Controls
For each new control, assess whether you already have compensating controls in place, or whether you need to implement something new. The most commonly impactful for most organisations are cloud services security, configuration management, data masking, and secure coding.

### 3. Update Your Documentation
- Update all policies to reference the new control numbers
- Update your risk treatment plan
- Add documentation for the new Clause 6.3 change management requirement
- Update your internal audit procedure to address 9.2.2 (audit programme)
- Update management review minutes templates to address 9.3.2 inputs and 9.3.3 outputs

### 4. Internal Audit Focused on Gaps
Conduct an internal audit specifically looking at the transition gaps before your certification body transition audit.

### 5. Schedule Transition Audit with Your Certification Body
The transition audit is typically similar in scope to a surveillance audit. Your CB will verify the SoA updates, evidence for new controls, and continued compliance with existing controls.

---

## Summary

The 2022 revision modernises the standard to reflect current threats (cloud, data leakage, secure development) and simplifies the control structure. The clause requirements are broadly similar, with minor structural changes to Clauses 6, 9.2, and 9.3. The main work for transition is updating your SoA, implementing the 11 new controls, and refreshing your documentation.
