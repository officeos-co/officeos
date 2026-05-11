# Prime Contractor Obligations Under DFARS 252.204-7021: Subcontractor Flow-Down Analysis

## Scenario Summary

You are a prime contractor on a DoD contract containing DFARS 252.204-7021. Two subcontractors handle CUI:

- **Subcontractor A**: Stores CUI on a personal Google Drive account
- **Subcontractor B**: Holds a CMMC Level 2 self-assessment with an SPRS score of 42

---

## Part 1: DFARS 252.204-7021 Flow-Down Obligations

DFARS 252.204-7021(c) places direct legal obligations on prime contractors. As the prime, you must:

1. **Include CMMC requirements in all subcontracts** where the subcontractor processes, stores, or transmits CUI — regardless of tier. This means the CMMC requirement flows through your entire supply chain.

2. **Specify the required CMMC level** in each subcontract. Both subcontractors described above handle CUI, placing them squarely in scope for CMMC Level 2 (110 practices per NIST SP 800-171 Rev 2).

3. **Verify subcontractor certification before award** (for Level 2/3). For a C3PAO-assessed Level 2, you must confirm the certification appears in both SPRS and the Cyber AB registry. For a self-assessed Level 2, the subcontractor's SPRS score and senior official affirmation must be current.

4. **Flow-down applies to all tiers**. Sub-subcontractors are not exempt; your subcontractors must impose equivalent requirements on their own lower-tier subcontractors.

**Legal exposure**: As the prime, DoD holds you accountable for your supply chain. If a subcontractor mishandles CUI, the prime contractor may face contract termination, DFARS violations, and potential False Claims Act liability if certifications were falsely affirmed.

---

## Part 2: Subcontractor A — Personal Google Drive CUI Violation

### The Violation

Storing CUI on a personal Google Drive account is a **direct violation of DFARS 252.204-7012**, which requires that any external cloud service used to process, store, or transmit CUI must be:

- **FedRAMP Authorized at the Moderate impact level** or higher, or
- Must meet security requirements equivalent to FedRAMP Moderate as described in the DoD Cloud Computing Security Requirements Guide (SRG)

**Google Drive (personal/consumer accounts)** is **not FedRAMP Authorized**. Even Google Workspace for Government (FedRAMP Moderate) is a separate, specifically authorized offering — a personal consumer Google Drive account has no such authorization and provides no CUI safeguards.

### Key CMMC Practices Violated

The use of personal Google Drive for CUI likely violates multiple NIST SP 800-171 practices, including:

| Practice ID | Domain | Description | Why It Applies |
|-------------|--------|-------------|----------------|
| SC.L2-3.13.15 | System & Comms | Control and monitor communications at external boundaries | CUI leaving to uncontrolled external system |
| AC.L2-3.1.3 | Access Control | Control CUI flow in accordance with approved authorizations | No access controls on personal account |
| MP.L2-3.8.1 | Media Protection | Protect system media containing CUI | Cloud storage = system media |
| CM.L2-3.4.6 | Config Management | Employ principle of least functionality | Unauthorized cloud tool in use |
| SC.L2-3.13.11 | System & Comms | Employ FIPS-validated cryptography | No assurance of FIPS encryption |
| SI.L2-3.14.6 | System & Info Integrity | Monitor organizational systems for malware | No monitoring of personal account |

### Severity

This is a **critical finding**. The DoD's CMMC pitfall guidance explicitly states: "Using non-FedRAMP cloud for CUI violates DFARS 7012 enclave requirements." This is not a gray area — it is an active, ongoing violation that may constitute a reportable cyber incident depending on whether unauthorized parties could access the CUI.

---

## Part 3: Subcontractor B — SPRS Score of 42

### Interpreting the Score

The SPRS scoring scale runs from **+110** (all 110 practices fully met) to **-203** (all practices unmet). A score of **42** represents a substantial shortfall from the maximum of 110, indicating that this subcontractor has failed to implement a significant number of NIST SP 800-171 practices.

**Point gap**: 110 - 42 = **68 points of deficiencies**. Given that practices carry weights of 1–5 points each, this could represent anywhere from roughly 14 to 68+ unimplemented practices, depending on which practices are missing.

### Regulatory Status

Under DFARS 252.204-7021 and 7020:
- Submitting an SPRS score is required, and Subcontractor B has done so.
- However, a score of 42 **does not mean the subcontractor is compliant** — it means they have self-assessed and documented significant gaps.
- DoD Contracting Officers (COs) actively review SPRS scores; a score this low will attract scrutiny.
- If the contract requires a **C3PAO-assessed Level 2 certification** (not just a self-assessment), a score of 42 almost certainly means the subcontractor **cannot obtain certification** until critical gaps are remediated — particularly the critical practices (AC.L2-3.1.3, IA.L2-3.5.3, SC.L2-3.13.8, SC.L2-3.13.11, SI.L2-3.14.6) which cannot have POA&M items at time of certification.

### POA&M Requirement

A subcontractor with a score this low should have a **Plan of Action & Milestones (POA&M)** documenting all gaps. The prime contractor should:
- Obtain and review the subcontractor's current POA&M
- Verify milestones are realistic and on track
- Understand which specific practices are unmet (especially the 7 critical practices that block certification)

---

## Part 4: Immediate Steps — Priority Action Plan

### Step 1: Quarantine CUI on Google Drive (Subcontractor A) — Within 24 Hours

- **Immediately notify Subcontractor A** in writing to cease all use of personal Google Drive for CUI.
- Direct Subcontractor A to **identify, enumerate, and inventory all CUI stored on the personal Google Drive account**.
- Instruct Subcontractor A to **migrate CUI to a FedRAMP Moderate-authorized environment** immediately (e.g., Microsoft 365 GCC High, Google Workspace for Government — FedRAMP Moderate authorized, or an equivalent DoD-approved enclave).
- **Delete or purge** all CUI from the personal Google Drive once migration is confirmed.
- Issue a formal **cure notice** or **show cause notice** to Subcontractor A per your subcontract terms.

### Step 2: Assess Whether a Cyber Incident Must Be Reported — Within 24–48 Hours

Determine whether the Google Drive exposure constitutes a **reportable cyber incident** under DFARS 252.204-7012:
- Was the personal Google Drive account accessible to unauthorized individuals (e.g., personal account shared with family, set to public, or the account was compromised)?
- Was the CUI potentially exfiltrated or accessed by unauthorized parties?

If a reportable incident occurred, **you (the prime) must report to DIBNET within 72 hours** of discovery at dibnet.dod.mil. Do not delay while awaiting information from the subcontractor — report what is known and update as more information becomes available.

You must also **notify your Contracting Officer (CO)** of the situation, even if the incident threshold is unclear.

### Step 3: Obtain Subcontractor A's Remediation Plan — Within 48–72 Hours

- Require Subcontractor A to submit a written **remediation plan** with specific milestones showing how they will establish a FedRAMP Moderate-compliant CUI handling environment.
- Set a hard deadline for full remediation (recommend 30 days maximum given the severity).
- If Subcontractor A cannot demonstrate a credible path to compliance, consider **suspending their access to CUI** and evaluating replacement options.

### Step 4: Review Subcontractor B's POA&M and SPRS Score — Within 1 Week

- Request Subcontractor B's **complete SSP and current POA&M** in writing.
- Verify that the SPRS score of 42 in SPRS matches their self-assessment documentation.
- **Map which of the 7 critical practices** (AC.L2-3.1.3, IA.L2-3.5.3, SC.L2-3.13.8, SC.L2-3.13.11, SI.L2-3.14.6, AU.L2-3.3.1, IR.L2-3.6.1) are unmet — if any critical practices are unimplemented, the subcontractor cannot achieve Level 2 C3PAO certification until they are fully remediated.
- Establish **quarterly review checkpoints** to track remediation progress.

### Step 5: Evaluate Contract Performance Risk — Within 1–2 Weeks

- Determine if your DoD contract requires **Level 2 C3PAO certification** (not just self-assessment) for subcontractors. If so, Subcontractor B with a score of 42 likely cannot currently perform contract work involving CUI lawfully.
- Consult your contracts/legal team about whether either subcontractor's current status constitutes a **material breach** of your subcontract.
- Consider whether to **suspend CUI access** for Subcontractor B pending a clear remediation timeline toward certification-readiness.

### Step 6: Update Your Supply Chain Security Program Documentation — Within 30 Days

- Document the findings and corrective actions taken for both subcontractors in your **supply chain risk management records**.
- Update your **prime contractor CUI flow mapping** to reflect actual CUI handling at each subcontractor.
- Ensure your subcontracts (and your subcontractors' subcontracts) include the required DFARS 252.204-7021 flow-down clause language.
- Conduct a review of all other subcontractors handling CUI to identify any similar issues.

---

## Summary Table: Violations and Required Actions

| Issue | Regulation Violated | Severity | Immediate Action |
|-------|---------------------|----------|------------------|
| Google Drive CUI storage (Sub A) | DFARS 252.204-7012 (non-FedRAMP cloud) | Critical — Active Violation | Cease use immediately; migrate CUI; assess incident reporting obligation |
| Google Drive CUI storage (Sub A) | NIST SP 800-171 (multiple practices) | Critical | Require FedRAMP-compliant environment within 30 days |
| SPRS score of 42 (Sub B) | DFARS 252.204-7021 (CMMC requirement) | High — Significant compliance gap | Obtain POA&M; assess critical practices; impose milestone tracking |
| Prime flow-down gap (both) | DFARS 252.204-7021(c) | High | Issue cure notices; update subcontracts; document corrective actions |

---

## Regulatory Reference Summary

| Document | Relevance to This Scenario |
|----------|---------------------------|
| DFARS 252.204-7012 | Prohibits non-FedRAMP cloud for CUI; requires 72-hr DIBNET incident reporting |
| DFARS 252.204-7020 | Requires SPRS score submission; DoD COs review scores |
| DFARS 252.204-7021 | Requires CMMC certification; mandates prime-to-sub flow-down for CUI handling |
| 32 CFR Part 170 | CMMC 2.0 final rule (effective December 16, 2024) |
| NIST SP 800-171 Rev 2 | 110 CUI protection requirements — basis for SPRS scoring |
| DoD Assessment Methodology v2.2 | Governs SPRS score calculation (110 starting score, deductions per unmet practice) |
