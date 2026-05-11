# KYC-SA Annual Attestation Process — A1 Architecture Bank

## Overview

As an A1 architecture bank (a bank that manages its own SWIFT infrastructure on-premises), you are required to submit a KYC-SA (Know Your Customer - Security Attestation) attestation annually through the SWIFT Customer Security Programme (CSP). The attestation confirms your compliance with the Customer Security Controls Framework (CSCF). With your July 31 deadline, here is a complete walkthrough of the process.

---

## 1. What You Need to Prepare

### Understanding Your Scope (A1 Architecture)

As an A1 architecture type, your SWIFT footprint includes:
- SWIFT messaging interface software (e.g., Alliance Gateway, Alliance Access, or equivalent)
- Local SWIFT infrastructure including HSMs, operator PCs, and messaging servers
- The secure zone protecting these components
- Any jump servers or management systems in the SWIFT secure zone

All mandatory and advisory CSCF controls applicable to A1 environments must be addressed.

### Pre-Attestation Documentation

Prepare the following before beginning the KYC-SA submission:

**Policies and Procedures:**
- Information security policy covering SWIFT-related assets
- Access control policy and procedures
- Change management policy
- Incident response plan (specifically mentioning SWIFT)
- Privileged access management procedures
- Password management policy

**Technical Evidence:**
- Network diagrams showing the SWIFT secure zone and isolation controls
- Firewall rules and configurations restricting access to SWIFT components
- Asset inventory of all components within the SWIFT secure zone
- Vulnerability scan results (recent, within the last 12 months)
- Penetration test results (if applicable, within the required timeframe)
- Anti-malware solution configuration and update logs
- System hardening documentation for SWIFT servers and operator workstations
- Multi-factor authentication (MFA) configuration evidence
- User access review records
- Software version records confirming supported/patched SWIFT components
- Patch management logs

**Operational Evidence:**
- Evidence of completed staff security awareness training
- Privileged account review and recertification records
- Logs showing segregation of duties in SWIFT operations
- Third-party risk assessments for any service providers with SWIFT access

### CSCF Control Mapping

Compile a control-by-control mapping that documents for each CSCF mandatory control:
- Whether it is implemented (Compliant / Non-Compliant)
- Evidence reference
- Any compensating controls for gaps (with rationale)

For CSCF v2025 (applicable for the July 31, 2026 attestation cycle), ensure you are using the current version of the framework and have addressed any newly mandatory controls that may have transitioned from advisory.

---

## 2. Who Can Act as Independent Assessor

### Mandatory Independent Assessment for A1

As an A1 architecture entity, SWIFT requires an **independent assessment** — you cannot self-attest without third-party validation for mandatory controls. Your assessor must be:

### Option A: SWIFT-Listed CSP Assessor (Preferred / Most Common)

SWIFT maintains a list of approved Community Standard Assessment (CSA) providers on swift.com. These are:
- External cybersecurity or audit firms vetted and listed by SWIFT
- Must hold a specific CSP assessor designation from SWIFT
- Examples of firm types include Big Four accounting firms, major cybersecurity consultancies (e.g., firms with ISO 27001 audit capability), and specialist financial sector security firms

**Requirements for a SWIFT-listed assessor:**
- Must be independent from your organization (cannot be an internal team or a firm with conflicting business interests)
- Must have completed SWIFT's CSP assessor training/certification
- Must use SWIFT's standardized assessment methodology

### Option B: Internal Audit (with Conditions)

Some architectures permit internal audit to perform the assessment under specific conditions:
- The internal audit function must be genuinely independent from the IT/operations teams responsible for SWIFT
- The internal audit team must demonstrate competency in cybersecurity assessments
- However, for A1 architecture, SWIFT's current guidance strongly recommends or requires an external independent assessor — verify the exact requirement in the current CSCF documentation, as SWIFT has progressively tightened this requirement

### Assessor Independence Criteria

Regardless of assessor type, the assessor must:
- Have no conflict of interest with the systems being assessed
- Not be the same team that designed or implemented the SWIFT controls
- Be able to objectively challenge and verify evidence
- Issue a formal assessment report with findings and conclusions

### Engaging Your Assessor

- Engage the assessor well before July 31 — assessments typically take 4–8 weeks depending on complexity
- Provide the assessor with your scope documentation, architecture diagrams, and pre-prepared evidence package
- Agree on the assessment methodology upfront (document review, interviews, technical testing)
- Obtain a formal assessment report confirming the scope, methodology, findings, and overall conclusion

---

## 3. How to Complete the KYC-SA Form

### Accessing the KYC-SA Portal

1. Log in to **SWIFT's KYC Registry** via swift.com using your SWIFT credentials
2. Navigate to the **KYC-SA section** within the KYC Registry portal
3. Ensure you have the appropriate role/permissions — typically a "KYC-SA Administrator" or equivalent role is required

### Completing the Attestation Form

The KYC-SA form is structured around the CSCF controls. For each control:

**Step 1: Select Compliance Status**
- **Compliant** — the control is fully implemented
- **Not Compliant** — the control is not implemented or partially implemented
- **Not Applicable** (only for advisory controls where applicability criteria are met)

**Step 2: Provide Implementation Details**
- For each control, you enter a brief description of how the control is implemented
- Reference your evidence and procedures without uploading raw documents (the form captures attestation, not the underlying evidence, though you must retain evidence)

**Step 3: Assessor Information**
- Enter details of your independent assessor:
  - Assessor name / firm name
  - Assessor type (external firm vs. internal audit)
  - Assessment date
  - Whether the assessor is a SWIFT-listed CSP assessor

**Step 4: Review Architecture Type**
- Confirm your architecture type is correctly set to A1
- The system will display only the controls applicable to your architecture

**Step 5: Mandatory vs. Advisory Controls**
- Mandatory controls: must be marked Compliant or Non-Compliant (non-compliant results will be visible to your counterparties)
- Advisory controls: best practice; non-compliance does not trigger the same visibility consequences but reflects maturity

**Step 6: Submit the Attestation**
- Review all entries for completeness and accuracy
- Have the appropriate authorized signatory within your organization review and approve
- Click **Submit** — the attestation is timestamped and recorded

### Key Form Deadlines

- SWIFT's annual attestation deadline is typically **December 31** of each year, but your organization has set an internal target of July 31 — this is a best practice approach giving a comfortable buffer
- Attestations submitted after the SWIFT deadline may result in your entity being flagged as non-attested in the KYC Registry, which counterparties can see

---

## 4. What Happens After Submission

### Visibility in the KYC Registry

Once submitted, your attestation becomes visible in the **SWIFT KYC Registry**:
- **Counterparties** (other SWIFT members you correspond with) can view your attestation status
- They will see:
  - Whether you have attested (attested / not attested)
  - Your overall compliance level (fully compliant / partially compliant)
  - The date of your last attestation
  - Compliance status for each mandatory control (compliant or non-compliant)
- Counterparties use this information in their own correspondent banking due diligence and risk management decisions

### Non-Compliance Handling

If you have reported non-compliant mandatory controls:
- Counterparties can see this and may raise concerns or restrict your relationship
- SWIFT may reach out for clarification or a remediation plan
- You should have a documented remediation plan with target dates for any non-compliant controls
- You can update your attestation during the year if you achieve compliance on a previously non-compliant control

### SWIFT Oversight and Verification

- SWIFT does not automatically audit every attestation but operates a **spot-check and oversight program**
- SWIFT or its designated parties may request supporting evidence for your attestation claims
- Misrepresentation of compliance status is a serious violation of SWIFT's policies and can result in sanctions, including suspension from the SWIFT network

### Retention Requirements

- Retain all underlying evidence and assessment documentation for a minimum of **5 years** (or longer if required by local regulation)
- This includes assessor reports, evidence packages, and all correspondence related to the assessment
- Store evidence securely and ensure it can be produced promptly if SWIFT requests verification

### Annual Renewal

- The attestation covers a 12-month period
- You must repeat the full process annually
- SWIFT publishes the updated CSCF for the next cycle, typically mid-year, so begin reviewing the updated framework shortly after submitting this year's attestation
- Track any new mandatory controls transitioning from advisory status for the next cycle

---

## Summary Timeline for July 31 Target

| Milestone | Recommended Date |
|---|---|
| Review CSCF v2025 controls applicable to A1 | Now — April/May |
| Complete internal evidence gathering | May — June |
| Engage independent assessor | April/May (book early) |
| Assessor conducts assessment | May — June |
| Receive assessor report | June — early July |
| Complete KYC-SA form in portal | July 1–25 |
| Internal authorization and review | July 25–28 |
| Submit attestation | By July 31 |
| Retain all documentation | Ongoing (5+ years) |

---

## Important Notes

- Always use the **current CSCF version** published by SWIFT for the applicable attestation year — using an outdated version is a common error
- Ensure your SWIFT software versions are **within supported release** — running an unsupported version is typically an automatic mandatory control failure
- Check that your **HSM firmware** and **operator PC OS** versions are supported and patched
- Review SWIFT's **Security Bulletins** for any late-breaking requirements that may affect your attestation
- If you have subsidiaries or affiliates also using SWIFT under your BIC, confirm whether they require separate attestations

---

*This guidance is based on the SWIFT Customer Security Programme (CSP) and Customer Security Controls Framework (CSCF) as generally understood. Always refer to the official SWIFT documentation and the current CSCF version published on swift.com for authoritative requirements applicable to your specific attestation cycle.*
