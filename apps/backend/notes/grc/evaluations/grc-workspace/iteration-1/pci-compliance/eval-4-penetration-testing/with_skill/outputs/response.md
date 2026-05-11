# PCI DSS Penetration Testing Requirements (Requirement 11.3)

**PCI DSS Version: v4.0.1 (current, June 2024)**

---

## Overview

Penetration testing requirements fall under **PCI DSS Requirement 11: Test Security of Systems and Networks Regularly**, which is part of the fifth goal: *Regularly Monitor and Test Networks*. Requirement 11.3 specifically governs penetration testing.

PCI DSS v4.0 significantly enhanced the penetration testing requirements compared to v3.2.1, adding segmentation testing requirements, greater scope clarity, and more explicit methodology requirements.

---

## Requirement 11.3 — Penetration Testing

### 11.3.1 — External Penetration Test

**What it requires:**
- External penetration testing is performed at least **once every 12 months**
- Testing must occur after any significant infrastructure or application upgrade or change
- Testing covers the **external perimeter** of the CDE and any critical systems

**Scope:**
- All externally accessible IP addresses and services in scope for PCI DSS
- Web applications that handle cardholder data
- APIs that touch cardholder data or CDE systems

---

### 11.3.1.1 — Remediation and Retesting

**What it requires:**
- All exploitable vulnerabilities found during penetration testing are corrected
- Retesting is performed to verify vulnerabilities have been remediated
- Testing is repeated until exploitable vulnerabilities no longer exist
- Results are retained and available for review

---

### 11.3.1.2 — Internal Penetration Test

**What it requires:**
- Internal penetration testing is performed at least **once every 12 months**
- Testing must occur after any significant infrastructure or application upgrade or change
- Covers the **internal network** including CDE-adjacent systems

**Scope:**
- All systems and network components in the CDE
- Systems that could be leveraged to attack the CDE
- Internal interfaces between the CDE and other network segments

---

### 11.3.1.3 — Segmentation Testing (Critical New Requirement Area)

**What it requires:**
- If network segmentation is used to isolate the CDE from other networks, penetration testing must **validate that segmentation controls are effective** and operational
- Segmentation testing must be performed at least:
  - **Once every 12 months** for standard merchants
  - **Once every 6 months** for **service providers** (this is a higher frequency requirement)
- Segmentation tests must attempt to exploit segmentation controls to confirm that out-of-scope systems cannot reach in-scope CDE systems

**Why this matters:** Organisations frequently claim network segmentation to reduce PCI DSS scope but have never validated whether the segmentation actually works. Requirement 11.3.1.3 closes this gap by requiring proof of effective segmentation.

---

## Scope of Penetration Testing

Under PCI DSS v4.0.1, the penetration test scope must include:

| Scope Area | Details |
|-----------|---------|
| **CDE perimeter** | All systems at the boundary of the CDE — firewalls, web proxies, load balancers |
| **All CDE systems** | Any system that stores, processes, or transmits cardholder data |
| **Connected systems** | Systems that are not in the CDE but could impact CDE security if compromised |
| **Network segmentation controls** | Firewalls, VLANs, ACLs used to isolate the CDE |
| **Application layer (web/API)** | Applications handling cardholder data; must test at the application layer, not just network layer |
| **All supporting infrastructure** | DNS, DHCP, authentication systems (AD/LDAP) used by CDE systems |

**The scope must cover both the network layer and the application layer.** Network-only penetration tests are insufficient for PCI DSS compliance.

---

## Penetration Testing Methodology

PCI DSS v4.0.1 (Req 11.3) requires that testing be based on an **industry-accepted penetration testing methodology**. Acceptable frameworks include:

- **PTES** (Penetration Testing Execution Standard)
- **OWASP Testing Guide** (for web application components)
- **NIST SP 800-115** (Technical Guide to Information Security Testing)
- **OSSTMM** (Open Source Security Testing Methodology Manual)

The methodology used must:
1. Cover the entire CDE perimeter and internal systems
2. Include testing from both inside and outside the network (simulating internal and external attackers)
3. Include application-layer testing
4. Validate network segmentation (Req 11.3.1.3)
5. Define how findings will be rated and prioritised (typically CVSS scoring)
6. Include exploitation attempts, not just vulnerability scanning

**Important distinction:** Penetration testing is not the same as vulnerability scanning (Req 11.3.2). Penetration testing involves active exploitation attempts to demonstrate actual risk. ASV scanning (Req 11.3.2) is separate and runs quarterly.

---

## Tester Qualifications

PCI DSS v4.0.1 requires that penetration testing be performed by a **qualified internal resource or qualified external third party**. Specifically:

### Qualification Requirements (Req 11.3.1 guidance):

| Requirement | Details |
|------------|---------|
| **Organisational independence** | The tester must be independent of the system being tested. An internal team member who maintains the systems they are testing does not qualify. |
| **Specialised expertise** | The tester must have demonstrated expertise in penetration testing methodology |
| **No conflict of interest** | If using internal resources, they must not be responsible for managing the systems under test |

### Recommended Certifications

While PCI DSS does not mandate specific certifications, widely accepted credentials for PCI penetration testers include:

| Certification | Body | Notes |
|--------------|------|-------|
| **OSCP** (Offensive Security Certified Professional) | Offensive Security | Highly regarded; hands-on exploitation focus |
| **CREST** | CREST International | Required by some card brands and regulators; common in UK/EU |
| **GPEN** (GIAC Penetration Tester) | GIAC/SANS | Well-recognised; covers methodology and techniques |
| **CEH** (Certified Ethical Hacker) | EC-Council | Common but considered less rigorous than OSCP/CREST |
| **QSA with penetration testing specialisation** | PCI SSC | QSA credentials alone do not qualify someone as a penetration tester |

For **Level 1 merchants and service providers**, card brands often prefer or require testers with **CREST** or equivalent credentials and a formal statement of scope and methodology.

---

## What Changed from PCI DSS v3.2.1?

| Area | v3.2.1 | v4.0 / v4.0.1 |
|------|--------|--------------|
| **Segmentation testing** | Required, but frequency not differentiated by entity type | **Service providers must test segmentation every 6 months** (merchants every 12 months) |
| **Methodology documentation** | Industry-accepted methodology required | More explicit requirements for what methodology must cover; application layer explicitly required |
| **Roles and responsibilities** | Not explicitly formalised | New sub-requirement to formally document and assign roles for Req 11 activities |
| **Remediation timeframes** | Correction required; retesting required | Stronger emphasis on retesting until no exploitable vulnerabilities remain |
| **Scope guidance** | CDE perimeter and internal | More explicit guidance that all CDE-connected systems are in scope, not just CDE itself |
| **Future-dated (now mandatory)** | — | All v4.0 future-dated requirements related to Req 11 became mandatory March 31, 2025 |

---

## Frequency Summary

| Test Type | Merchant Frequency | Service Provider Frequency |
|-----------|-------------------|--------------------------|
| External penetration test | At least annually; after significant changes | At least annually; after significant changes |
| Internal penetration test | At least annually; after significant changes | At least annually; after significant changes |
| Segmentation test | At least annually | **At least every 6 months** |
| ASV external vulnerability scan (Req 11.3.2) | Quarterly | Quarterly |

---

## Evidence Required for QSA Assessment

A QSA reviewing your penetration testing program will request:

1. **Written penetration testing scope document** — what systems were tested, from what network positions
2. **Penetration test report** — executive summary, findings, evidence of exploitation, CVSS ratings
3. **Segmentation test results** — demonstrating that out-of-scope systems cannot reach CDE systems
4. **Remediation evidence** — tickets, change records showing vulnerabilities were fixed
5. **Retest results** — confirmation that remediated vulnerabilities no longer exist
6. **Tester qualifications** — CV or certifications of the person who performed the test
7. **Methodology documentation** — which standard was followed

---

> **Disclaimer:** This guidance is based on PCI DSS v4.0.1 (PCI SSC, June 2024). Penetration testing must be designed and executed by qualified professionals. Formal compliance validation requires a QSA or ISA assessment. Verify against the official standard at pcisecuritystandards.org.
