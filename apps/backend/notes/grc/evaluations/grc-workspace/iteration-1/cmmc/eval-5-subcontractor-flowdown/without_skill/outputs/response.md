# DFARS 252.204-7021 Obligations for Subcontractors Handling CUI

**Date:** 2026-04-25
**Context:** Prime contractor with two subcontractors handling CUI under a DoD contract

---

## Overview of DFARS 252.204-7021 Flowdown Requirements

DFARS 252.204-7021 ("Cybersecurity Maturity Model Certification Requirements") mandates that prime contractors flow down CMMC requirements to subcontractors at all tiers that will process, store, or transmit Covered Unclassified Information (CUI) or that provide security protection for such systems. The required CMMC level flowed down must be the same level specified in the prime contract, or as appropriate to the information handled by the subcontractor.

Key obligations under DFARS 252.204-7021(c) for prime contractors:

1. **Flow down the CMMC requirement** to all subcontractors (at any tier) that will handle CUI or provide security protection for CUI systems.
2. **Verify that subcontractors have a current and valid CMMC certificate or conditional certificate** at the required level before awarding a subcontract or task/delivery order, and ensure they maintain that certification throughout performance.
3. **Not award a subcontract** or task/delivery order to a subcontractor required to have a CMMC certification unless the subcontractor has a current CMMC certificate (or conditional CMMC certificate, as applicable).
4. **Report to the Contracting Officer** if a subcontractor's CMMC certification lapses or is revoked during contract performance.

---

## Subcontractor 1: Storing CUI on a Personal Google Drive

### Problem Assessment

This is a **critical and immediate violation** on multiple levels:

- **CMMC/NIST SP 800-171 violation:** NIST SP 800-171 (the technical standard underlying CMMC Level 2) requires that CUI be stored only in authorized systems with appropriate access controls, encryption, and audit logging. A personal Google Drive is not a Federal Risk and Authorization Management Program (FedRAMP)-authorized environment configured for CUI, and is almost certainly not operated under a System Security Plan (SSP).
- **Applicable controls violated include at minimum:**
  - 3.1.1 – Limit system access to authorized users
  - 3.1.3 – Control the flow of CUI
  - 3.13.8 – Implement cryptographic mechanisms to prevent unauthorized disclosure of CUI during transmission
  - 3.13.16 – Protect the confidentiality of CUI at rest
  - 3.1.22 – Control CUI posted or processed on publicly accessible systems
- **DFARS 252.204-7012 spillage risk:** If CUI has been exposed to an unauthorized system, there may be a reportable cyber incident obligation under DFARS 252.204-7012 within 72 hours.
- **Contract risk:** The subcontractor almost certainly does not have a valid CMMC Level 2 certification covering this practice.

### Your Obligations as Prime Contractor

Under DFARS 252.204-7021:

1. You must not continue to allow this subcontractor to handle CUI under current conditions.
2. You are obligated to ensure the subcontractor has a valid CMMC certification covering their actual environment and practices — which they do not have if CUI is stored on a personal Google Drive.
3. You may be liable to the Contracting Officer if you knowingly allow a non-compliant subcontractor to continue handling CUI.

### Immediate Steps

1. **Issue a stop-work directive** to the subcontractor for any further processing, storage, or transmission of CUI until the situation is remediated.
2. **Conduct a data spillage assessment:** Determine what CUI was stored, for how long, and who may have had access to the personal Google Drive account.
3. **Report to your Contracting Officer** as required under DFARS 252.204-7012 if the exposure constitutes a reportable cyber incident (unauthorized access or potential access to CUI on a non-authorized system).
4. **Notify the subcontractor in writing** of the violation, citing contractual requirements (DFARS 252.204-7021 and 252.204-7012 flowdown clauses).
5. **Require the subcontractor to provide a Corrective Action Plan (CAP)** with a timeline to migrate CUI to an authorized system (e.g., a FedRAMP Moderate or equivalent environment with proper configuration).
6. **Review your subcontract:** Determine whether this violation constitutes grounds for cure notice, termination for default, or other remedies.
7. **Document everything** — your notifications, the subcontractor's responses, and steps taken — to demonstrate your own due diligence to the Contracting Officer.

---

## Subcontractor 2: CMMC Level 2 Self-Assessment Score of 42 in SPRS

### Problem Assessment

This situation is serious but somewhat different in character:

- **SPRS score of 42 is far below the required threshold:** For CMMC Level 2, a contractor must meet all 110 NIST SP 800-171 practices. The SPRS score is calculated starting at 110 and deducting points for each unimplemented practice. A score of 42 means the subcontractor has significant unimplemented controls — roughly equivalent to approximately 68 points of deductions, indicating a very large number of missing or non-compliant security practices.
- **Self-assessment vs. third-party assessment:** A self-assessment score of 42 is particularly concerning because it is self-reported and not validated by a C3PAO (CMMC Third-Party Assessment Organization). The actual compliance posture could be worse.
- **CMMC Level 2 certification requirement:** Under DFARS 252.204-7021, for contracts requiring CMMC Level 2, subcontractors that handle CUI must have either:
  - A CMMC Level 2 certification from a C3PAO (for "critical" programs), or
  - A valid CMMC Level 2 self-assessment submitted to SPRS with an affirmation (for non-critical programs).
  - In either case, the score must reflect full implementation (score of 110, or all practices implemented with any gaps covered by a Plan of Action and Milestones (POA&M) within permitted thresholds).
- **A score of 42 does not meet the minimum threshold** for a valid self-assessment under CMMC Level 2 requirements, even under the most permissive interpretation.

### Key Distinction: POA&M Eligibility

DoD has established that certain practices may be on a POA&M (Plan of Action and Milestones) rather than fully implemented at time of contract award, but:

- POA&M practices are limited to specific lower-weighted controls.
- High-weighted practices (those deducting 5 points each) must be fully implemented.
- A score of 42 strongly suggests that many high-weighted practices are not implemented, which would preclude a compliant self-assessment.

### Your Obligations as Prime Contractor

Under DFARS 252.204-7021:

1. You must not award or continue a subcontract to a subcontractor that does not have a current and valid CMMC Level 2 self-assessment or certification on file in SPRS that meets requirements.
2. A self-assessment score of 42 does not constitute a valid, passing CMMC Level 2 assessment — you cannot treat this subcontractor as compliant.
3. You are obligated to notify your Contracting Officer if the subcontractor's compliance status is inadequate.

### Immediate Steps

1. **Verify the SPRS record:** Log into SPRS and confirm the subcontractor's current score, assessment date, and whether an affirmation has been submitted. Confirm whether a POA&M is on file and what its closure date is.
2. **Assess the gap:** Request the subcontractor's current System Security Plan (SSP) and POA&M to understand which of the 110 NIST SP 800-171 controls are not implemented and what the remediation timeline is.
3. **Notify your Contracting Officer** of the subcontractor's non-compliant SPRS score and your assessment of their status. Transparency is critical to protect your position as prime.
4. **Issue a written notice to the subcontractor** requiring them to submit a remediation plan with specific milestones to reach a compliant score, and an affirmation of commitment to CMMC requirements.
5. **Consider restrictions on CUI access** pending remediation — limit the subcontractor's access to the minimum CUI necessary while they remediate.
6. **Set a contractual deadline** for the subcontractor to achieve a compliant SPRS score (or C3PAO certification, as applicable), with consequences for non-compliance including potential termination.
7. **Do not award any new task orders or work** to this subcontractor that involves CUI handling until they achieve a compliant score.
8. **Monitor remediation progress** — require monthly or quarterly SPRS updates and review of POA&M closure milestones.

---

## Summary Table

| Issue | Subcontractor 1 (Google Drive) | Subcontractor 2 (SPRS Score 42) |
|---|---|---|
| Severity | Critical — active CUI exposure | High — systemic control gaps |
| Immediate action | Stop-work, incident assessment | Restrict CUI access, demand CAP |
| CO notification required | Yes (potential 7012 incident) | Yes (7021 non-compliance) |
| Continue subcontract? | No, until remediated | No new CUI work until compliant |
| Root obligation | DFARS 252.204-7021(c) flowdown | DFARS 252.204-7021(c) flowdown |

---

## Prime Contractor's Broader Obligations and Risk

- **False Claims Act exposure:** If you knowingly allow non-compliant subcontractors to handle CUI and affirm compliance to the government, you may face False Claims Act liability. Prompt self-reporting and corrective action are critical mitigants.
- **Document your diligence:** Maintain written records of all steps taken — notifications, responses, CAPs, and communications with the Contracting Officer.
- **Review your entire subcontractor base:** These two cases suggest a need for a systematic review of all subcontractors that may handle CUI to verify CMMC compliance status in SPRS.
- **Update subcontract language:** Ensure all current and future subcontracts include enforceable CMMC flowdown clauses, SPRS score thresholds, rights to audit, and remediation/termination provisions.

---

*This response reflects general knowledge of DFARS 252.204-7021, CMMC program requirements, NIST SP 800-171, and associated DoD guidance as of April 2026. Program-specific requirements may vary; always consult your Contracting Officer and legal counsel for contract-specific obligations.*
