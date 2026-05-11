# PCI DSS Penetration Testing Requirements

## Overview

PCI DSS requires regular penetration testing as part of its broader mandate to regularly test security systems and processes. Penetration testing is distinct from vulnerability scanning — it involves actively exploiting vulnerabilities to demonstrate actual risk, not just identifying them.

## How Often Must You Test?

### Annual Testing (Minimum)
PCI DSS requires penetration testing at least **once per year** for both:
- **External penetration testing** — testing from outside your network perimeter
- **Internal penetration testing** — testing from inside your network (simulating an insider threat or attacker who has already gained a foothold)

### After Significant Changes
Beyond the annual requirement, penetration testing must also be performed after:
- Significant infrastructure upgrades or changes
- Major application changes or new deployments
- Changes to network architecture or segmentation

### Segmentation Testing (Important Addition)
If you use network segmentation to isolate your Cardholder Data Environment (CDE) from other networks (which is strongly recommended to reduce scope), you must also test that your segmentation controls actually work. This segmentation testing:
- Must be performed at least annually for most merchants
- Must be performed more frequently (every six months) for service providers
- Must attempt to breach the segmentation controls, not just verify firewall rules on paper

## What Must Be In Scope?

The penetration test scope must cover:

### Network Layer
- External perimeter of the CDE (all externally accessible systems)
- Internal network components within the CDE
- Firewall rules and access controls protecting the CDE
- Network segmentation controls (if used)

### Application Layer
PCI DSS requires testing at the application layer, not just the network layer. This includes:
- Web applications that handle cardholder data
- APIs that process or transmit card data
- Authentication mechanisms protecting CDE access

### Both Internal and External Perspectives
- **External test**: Simulates an attacker on the internet trying to reach your CDE
- **Internal test**: Simulates an attacker who is already inside your network (e.g., compromised employee workstation)

## Tester Qualifications

PCI DSS requires that penetration testing be conducted by a **qualified, independent tester**. Key requirements:

### Independence
- The tester must be independent of the systems being tested
- An IT administrator who manages the firewalls cannot test those same firewalls
- Can be an internal employee from a different team, or an external firm
- Internal testers must not have a conflict of interest

### Expertise
The tester must have demonstrated expertise in penetration testing methodologies. While PCI DSS doesn't mandate specific certifications, common credentials that demonstrate qualification include:

- **OSCP** (Offensive Security Certified Professional) — highly regarded, hands-on
- **CEH** (Certified Ethical Hacker)
- **GPEN** (GIAC Penetration Tester)
- **CREST** certifications — often required by UK/EU entities and some card brands

### Methodology
Testing must follow an industry-accepted methodology such as:
- PTES (Penetration Testing Execution Standard)
- OWASP Testing Guide (for web applications)
- NIST SP 800-115

## What Happens After Testing?

### Remediation Required
All exploitable vulnerabilities identified during penetration testing must be remediated. There is no option to simply "note" critical findings and continue.

### Retesting
After remediation, you must retest to verify that vulnerabilities have actually been fixed. Testing must continue until no exploitable vulnerabilities remain.

### Documentation
Retain penetration test reports, remediation evidence, and retest results. Your QSA will review this documentation during your compliance assessment.

## Penetration Testing vs. Vulnerability Scanning

These are different requirements often confused:

| Aspect | Penetration Testing | Vulnerability Scanning (ASV) |
|--------|--------------------|-----------------------------|
| Frequency | At least annually | Quarterly |
| Who performs it | Qualified tester (internal or external) | PCI SSC-approved Approved Scanning Vendor (ASV) |
| What it does | Actively exploits vulnerabilities | Identifies and catalogs vulnerabilities |
| Requirement | PCI DSS Req 11.3 | PCI DSS Req 11.3.2 |

Both are required — quarterly ASV scans are in addition to, not a replacement for, annual penetration testing.

## Summary

| Requirement | Frequency |
|------------|-----------|
| External penetration test | At least annually + after significant changes |
| Internal penetration test | At least annually + after significant changes |
| Segmentation testing | At least annually (every 6 months for service providers) |
| Tester qualifications | Independent, demonstrated expertise |
| Scope | Network + application layer, internal + external |
| Post-test | Remediate all exploitable findings + retest |
