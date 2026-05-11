# FedRAMP Authorization Process: Start to Finish

*Use official FedRAMP templates from fedramp.gov — this content should be inserted into the appropriate template section.*

---

## Overview

FedRAMP (Federal Risk and Authorization Management Program) provides a standardized approach to security assessment, authorization, and continuous monitoring for cloud products and services used by US federal agencies. As a Cloud Service Provider (CSP), you will pursue authorization for your Cloud Service Offering (CSO) through one of two primary paths.

---

## The Authorization Package

Regardless of path, your authorization package must include:

```
Authorization Package
├── System Security Plan (SSP) + Appendices A–Q
├── Security Assessment Plan (SAP) + Appendices A–D    [3PAO-prepared]
├── Security Assessment Report (SAR) + Appendices A–F  [3PAO-prepared]
└── Plan of Action & Milestones (POA&M)                [SSP Appendix O]
```

All documents must use the **official FedRAMP PMO templates** (Rev 5, updated December 2024), available at https://www.fedramp.gov/rev5/documents-templates/. Non-standard submissions risk rejection or significant delays. As of September 2026, all CSPs must also submit OSCAL machine-readable packages per RFC-0024.

---

## Step-by-Step Authorization Process

### Phase 1: Preparation & Readiness

1. **Define your Cloud Service Offering (CSO)** — Identify what is being authorized: IaaS, PaaS, or SaaS offering.
2. **Determine your target impact level** — Based on FIPS 199 categorization, select Low, Moderate, or High (see impact level guidance).
3. **Define your authorization boundary** — Everything that processes, stores, or transmits federal data must be inside the boundary. This is one of the most common sources of delays and findings.
4. **Conduct a gap assessment** — Map your current security posture to the applicable NIST SP 800-53 Rev 5 control baseline:
   - Low: ~156 controls
   - Moderate: 323 controls
   - High: 421 controls
5. **Engage a 3PAO** — Select an accredited Third Party Assessment Organization from the FedRAMP marketplace. The 3PAO will prepare the SAP and SAR.
6. **Consider a Readiness Assessment Report (RAR)** — Optional but recommended; the FedRAMP PMO reviews the RAR and designates the CSP as "FedRAMP Ready" before full assessment begins.

### Phase 2: Documentation

7. **Draft the System Security Plan (SSP)** — The SSP is the cornerstone document. It includes:
   - System description and boundary
   - Data flow diagrams and network architecture
   - Control implementation narratives for all in-scope controls
   - Appendices A–Q (including POA&M as Appendix O, CIS/CRM workbook, etc.)
8. **Complete inherited control documentation** — If leveraging FedRAMP-authorized IaaS/PaaS (e.g., AWS GovCloud, which is FedRAMP High authorized), document inherited, shared, and customer-responsible controls in the Customer Responsibility Matrix (CRM).

### Phase 3: Assessment

9. **3PAO conducts assessment** — The 3PAO executes the Security Assessment Plan (SAP) and produces the Security Assessment Report (SAR). This includes:
   - Control testing
   - Vulnerability scanning (OS, DB, web app, containers)
   - Penetration testing
   - Documentation review
10. **Remediate findings** — Address all findings before submitting, or document residual items in the POA&M with milestone dates.

### Phase 4: Authorization Decision

11. **Package submission and review** — Submitted to the authorizing official (Agency AO or JAB).
12. **Authorization decision** — Issuance of Authority to Operate (ATO) or Provisional ATO (P-ATO).

### Phase 5: Continuous Monitoring (ConMon)

13. **Monthly ConMon activities** — Submit vulnerability scan results, POA&M updates, inventory changes, and a ConMon Monthly Executive Summary to agency Authorizing Officials.
14. **Annual activities** — Full 3PAO re-assessment using the Annual Assessment Controls Selection Worksheet, updated SSP and appendices, tested IRP and CP, updated SAR and POA&M.

---

## Agency ATO vs. JAB P-ATO

### Agency Authorization (Agency ATO)

| Attribute | Details |
|---|---|
| **Sponsor** | A specific federal agency acts as your sponsor and Authorizing Official (AO) |
| **Review body** | The agency's own security team + FedRAMP PMO review |
| **Outcome** | Agency-specific ATO; other agencies may reuse via FedRAMP Marketplace |
| **Timeline** | Typically 12–18 months end-to-end |
| **Reusability** | Once listed on the FedRAMP Marketplace, other agencies can issue their own ATOs based on the existing package (reciprocity) |
| **Best for** | CSPs with an existing agency customer relationship or a clear target agency |

**Process highlights:**
- Agency identifies you as a cloud service they want to use
- Agency AO sponsors the authorization
- You work directly with the agency's security team throughout
- FedRAMP PMO provides oversight and must accept the package into the FedRAMP Marketplace

### JAB P-ATO (Joint Authorization Board Provisional ATO)

| Attribute | Details |
|---|---|
| **Review body** | Joint Authorization Board: DoD, DedRAMP, DHS CISOs |
| **Outcome** | Government-wide Provisional ATO — maximizes reusability across all agencies |
| **Status (2025–2026)** | **Effectively suspended since 2024** — the JAB has not been actively reviewing new CSPs; verify current status with the FedRAMP PMO before pursuing this path |
| **Best for** | High-demand, horizontal platforms serving many agencies (when active) |

> **Critical note**: As of 2024–2026, the JAB P-ATO path has been largely suspended. Do not plan your authorization strategy around JAB unless the FedRAMP PMO confirms it is active again.

---

## Which Path Should You Choose for a First Authorization?

**Recommendation: Agency Authorization Path**

For a first FedRAMP authorization, the Agency Authorization path is strongly recommended for the following reasons:

1. **JAB is effectively suspended** — Planning for JAB P-ATO is not viable given current FedRAMP PMO operations as of 2025–2026.
2. **Existing relationships** — If you are already in conversations with a federal agency prospect, that agency can serve as your sponsor and AO, creating a clear path to authorization.
3. **Faster feedback loop** — Working directly with one agency provides faster, more targeted feedback than a government-wide review.
4. **Reusability after authorization** — Once listed on the FedRAMP Marketplace, your authorization package can be reused by other agencies, effectively providing the same reach as a P-ATO.
5. **Consider FedRAMP 20x pilot** — A modernization initiative (FedRAMP 20x) is in progress that may offer a more streamlined path; evaluate whether your system qualifies for the pilot program.

### Recommended First Steps

1. Identify your target impact level (most CSPs target **Moderate**, which covers the majority of non-classified government data)
2. Secure an agency sponsor — reach out to federal prospects and confirm one will act as AO
3. Conduct a readiness gap assessment against NIST SP 800-53 Rev 5 Moderate baseline (323 controls)
4. Engage an accredited 3PAO early in the process
5. Begin drafting the SSP using official FedRAMP PMO templates
6. Consider pursuing a Readiness Assessment Report (RAR) to obtain "FedRAMP Ready" designation before full assessment
