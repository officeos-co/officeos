# CMMC Level 2 — A Plain-English Guide for Your Aerospace Firm

## What Just Happened?

Your contract includes **DFARS 252.204-7021**, which is the DoD clause that formally requires CMMC certification. Since you are an aerospace engineering firm working on a DoD contract, you almost certainly handle **Controlled Unclassified Information (CUI)** — technical drawings, engineering specs, design data, and similar material that the government considers sensitive but not classified. Because your contract specifies CMMC Level 2, you must demonstrate that you adequately protect that CUI before you can fully perform on the contract (and before you can win future DoD contracts).

---

## What Is CMMC 2.0?

CMMC stands for **Cybersecurity Maturity Model Certification**. The DoD created it to ensure that defense contractors — even small firms like yours — protect sensitive government information from theft or compromise. It became legally effective in December 2024 under 32 CFR Part 170.

There are three levels:

| Level | Who It's For | How Many Requirements |
|-------|-------------|----------------------|
| Level 1 — Foundational | Any DoD contractor with basic Federal Contract Information (FCI) | 17 practices |
| **Level 2 — Advanced** | **Contractors handling CUI (this is you)** | **110 practices** |
| Level 3 — Expert | Contractors on the most sensitive national security programs | 110+ practices |

You need **Level 2**, which means meeting all **110 cybersecurity practices** drawn from the federal standard **NIST SP 800-171 Rev 2**.

---

## What Does "110 Practices" Actually Mean?

Think of the 110 practices as a comprehensive checklist covering every major area of IT security. They are organized into 17 domains (categories). Here is a plain-English summary of each area and what it means for a 45-person engineering firm:

| Domain | Plain-English Summary | Example Requirement |
|--------|----------------------|---------------------|
| **Access Control (AC)** | Only the right people get into systems and CUI files. | Employees only access the data they need for their job. |
| **Awareness & Training (AT)** | Everyone who touches CUI gets security training. | Annual cybersecurity awareness training for all staff. |
| **Audit & Accountability (AU)** | Log who does what, and keep those logs. | All logins, file access, and admin actions are recorded and retained. |
| **Configuration Management (CM)** | Know exactly what software and hardware you have, and lock it down. | Maintain an inventory of all computers and what software is on them. |
| **Identification & Authentication (IA)** | Verify who people are before letting them in — including with MFA. | Every employee uses multi-factor authentication (MFA) to access CUI systems. |
| **Incident Response (IR)** | Have a plan for when something goes wrong. | A written incident response plan, tested at least annually. |
| **Maintenance (MA)** | Control who works on your systems and how. | Vendors doing remote maintenance must use MFA and supervised sessions. |
| **Media Protection (MP)** | Protect and track physical and digital media containing CUI. | Laptops with CUI must be encrypted; USB drives must be controlled. |
| **Physical Protection (PE)** | Control who can walk into your facility. | Badge access to server rooms; visitor escort policies. |
| **Personnel Security (PS)** | Screen employees before giving them CUI access; manage departures. | Background checks before granting CUI access; account termination checklists. |
| **Risk Assessment (RA)** | Regularly look for vulnerabilities and fix them. | Monthly vulnerability scans of all CUI-connected systems. |
| **Security Assessment (CA)** | Regularly test your own security controls. | Annual internal security review; a written System Security Plan (SSP). |
| **System & Communications Protection (SC)** | Encrypt data in transit and at rest; protect your network boundaries. | Use only FIPS 140-2/3 validated encryption (not just any "AES-256" product). |
| **System & Information Integrity (SI)** | Keep systems patched and monitored for attacks. | Antivirus on all endpoints; SIEM or equivalent monitoring for intrusions. |

---

## What Is CUI, and Why Does It Matter?

**Controlled Unclassified Information (CUI)** is government-sensitive data that is not classified but still requires protection by law or regulation. For an aerospace engineering firm, CUI typically includes:

- **Controlled Technical Information (CTI)**: Technical drawings, CAD files, specifications, test data, and software related to defense systems
- **Export-Controlled Data (ITAR/EAR)**: Design data or software that is controlled under International Traffic in Arms Regulations
- **Procurement/Acquisition data**: Pre-award contract information

Your first task is to identify exactly which data in your environment is CUI — this is called **CUI scoping**. This determines which of your systems, laptops, servers, and cloud services fall inside the CMMC assessment boundary.

**Important**: Only systems that store, process, or transmit CUI are in scope. If you can isolate CUI to a small number of systems, your assessment burden shrinks considerably.

---

## What Is a C3PAO and Why Do You Need One?

Because DFARS 252.204-7021 requires Level 2 certification, you cannot simply self-assess — you need an independent audit by a **CMMC Third-Party Assessment Organization (C3PAO)**. These are private companies accredited by the Cyber AB (the CMMC Accreditation Body) to conduct formal assessments.

Think of a C3PAO like a financial auditor: they review your evidence, interview your staff, test your technical controls, and issue a formal certification. The certification is valid for **three years**, with an annual affirmation required each year.

To find an authorized C3PAO: **cyberAB.us**

---

## The Three Key Documents You Must Build

Before a C3PAO will assess you, you need these three artifacts:

### 1. System Security Plan (SSP)
A written document that describes:
- What systems are in scope (your CUI boundary)
- How you implement each of the 110 practices
- Who is responsible for each control
- Network diagrams and data flow maps showing where CUI moves

This is the central document of your CMMC program. Plan 2–4 months to draft it properly.

### 2. Plan of Action & Milestones (POA&M)
A tracked list of security gaps — practices you have not yet fully implemented — along with your plan and timeline to fix them. A POA&M is not a sign of failure; it is required and expected. However, certain critical practices (see below) must be fully implemented before certification can be granted — they cannot sit in a POA&M.

### 3. SPRS Score
The **Supplier Performance Risk System** score is your self-calculated cybersecurity score, starting at 110 and deducting points for every unmet practice. You must submit this score to the DoD's SPRS portal even before your C3PAO assessment begins. Contracting Officers can see this score when evaluating you.

---

## The Seven "Must Fix Before Assessment" Practices

Seven practices are considered critical by the DoD — they must be fully implemented at the time of your assessment. You cannot receive conditional certification with these in a POA&M:

| Practice ID | What It Means in Plain English |
|-------------|-------------------------------|
| **AC.L2-3.1.3** | Control which people and systems can access CUI (need-to-know enforcement) |
| **IA.L2-3.5.3** | Multi-factor authentication (MFA) for all accounts that access CUI |
| **SC.L2-3.13.8** | Encrypt all CUI data in transit using FIPS-validated encryption |
| **SC.L2-3.13.11** | Use only FIPS 140-2/3 validated cryptography throughout your environment |
| **SI.L2-3.14.6** | Monitor your systems to detect attacks and anomalous activity |
| **AU.L2-3.3.1** | Create and retain audit logs of all activity on CUI systems |
| **IR.L2-3.6.1** | Have an operational incident response capability |

For a 45-person firm, MFA deployment and FIPS-compliant encryption are often the fastest wins and should be prioritized immediately.

---

## Important Compliance Obligations Beyond the 110 Practices

Even while you prepare for Level 2 assessment, certain obligations are already active under DFARS 252.204-7012:

- **72-Hour Incident Reporting**: If you discover a cyber incident involving CUI (unauthorized access, malware, suspected data theft), you must report it to the DoD via the **DIBNET portal** (dibnet.dod.mil) within **72 hours** of discovery. This is not optional.

- **Cloud Services**: If you use cloud services (Microsoft 365, Google Workspace, AWS, etc.) to store or process CUI, those services must be **FedRAMP Authorized at Moderate** baseline or equivalent. Standard commercial cloud tiers generally do not meet this requirement. Microsoft 365 GCC (Government Community Cloud) or GCC High are commonly used compliant options.

- **Subcontractors**: If you use any subcontractors who will have access to CUI from this contract, you are required to flow down CMMC requirements to them. They must hold the appropriate CMMC certification before you award them subcontract work involving CUI.

---

## Rough Timeline: How Long Will This Take?

For a 45-person firm starting from scratch, the realistic timeline to achieve CMMC Level 2 certification is **12 to 18 months**. Here is a phased breakdown:

### Phase 1: Scoping and Gap Assessment (Months 1–2)
- Identify all CUI data in your environment
- Define your CUI asset boundary (which systems are in scope)
- Conduct a gap assessment against all 110 practices
- Calculate your initial SPRS score
- Submit your SPRS score to the portal (required even before remediation is complete)

**What you need**: An experienced CMMC consultant or Registered Practitioner (RP) to lead the gap assessment.

### Phase 2: Remediation (Months 2–10)
- Fix critical gaps, starting with the seven must-fix practices above
- Deploy MFA across all CUI-touching accounts
- Implement FIPS-compliant encryption
- Deploy logging/SIEM capability
- Draft and finalize your SSP
- Manage open items in your POA&M
- Address cloud CUI storage if currently on non-FedRAMP services

**What you need**: IT resources (internal or managed service provider) to implement technical controls; a policy writer to draft the required security policies.

### Phase 3: Pre-Assessment Preparation (Months 10–12)
- Finalize SSP and ensure it covers all 110 practices
- Close as many POA&M items as possible (especially critical practices)
- Conduct internal mock assessment
- Train staff on what to expect during the C3PAO interviews
- Select and contract with a C3PAO (negotiate scope, sign NDA)

### Phase 4: C3PAO Assessment (Months 12–14)
- **Documentation Review** (2–4 weeks): C3PAO reviews your SSP, policies, and evidence remotely
- **Active Assessment** (1–3 weeks): Interviews, technical testing, configuration reviews
- **Findings and Reporting**: C3PAO issues findings; you have a limited window to provide additional evidence

### Phase 5: Certification and Annual Maintenance (Month 14–18 and ongoing)
- Receive certification (valid 3 years) or conditional certification with remaining POA&M items (must remediate within 180 days)
- Submit annual affirmation to the CMMC-AB each year
- Update SSP and POA&M as your environment changes
- Plan for re-assessment in year 3

---

## Estimated Costs (Rough Ranges for a 45-Person Firm)

These are rough industry estimates and vary significantly based on current posture:

| Activity | Estimated Cost Range |
|----------|---------------------|
| Gap assessment (external consultant) | $15,000 – $40,000 |
| Remediation (technical implementation) | $50,000 – $200,000+ depending on gaps |
| SSP/policy documentation (consultant) | $20,000 – $50,000 |
| C3PAO assessment fee | $50,000 – $150,000 |
| Ongoing compliance tools (SIEM, MFA, vulnerability scanning) | $20,000 – $60,000/year |

---

## Your Immediate Next Steps (This Week)

1. **Identify your CUI**: Ask your contract Program Manager what CUI categories are covered under this contract. Look at the contract's DD Form 254 (Contract Security Classification Specification) if one is attached.

2. **Do not use personal or standard commercial cloud tools for CUI**: If your engineers are currently using standard Gmail, Dropbox, or consumer Microsoft 365 for contract work, stop immediately. This is a compliance violation.

3. **Engage a CMMC consultant**: A Registered Practitioner (RP) or Registered Practitioner Organization (RPO) — find them at cyberAB.us — can conduct your gap assessment and guide the remediation process. Given your timeline, start this process now.

4. **Submit an SPRS score**: Even a low or negative score is better than no score. Your Contracting Officer may require it before contract performance begins.

5. **Check your subcontractors**: If any vendors will touch CUI, notify them that they will need CMMC certification. Start that conversation early — they need time too.

---

## Summary

CMMC Level 2 means your 45-person firm must demonstrate that it meets 110 cybersecurity practices to protect the sensitive defense information you handle under this contract. You will need to hire a C3PAO to formally assess and certify you. The process typically takes 12–18 months and requires significant investment in documentation, technical controls, and staff readiness. The good news: this process — once completed — is your competitive advantage, as CMMC will be required across most DoD contracts and many of your competitors have not yet started.

**Applicable regulatory references**: 32 CFR Part 170 (CMMC 2.0 Final Rule), NIST SP 800-171 Rev 2, DFARS 252.204-7012, DFARS 252.204-7019, DFARS 252.204-7020, DFARS 252.204-7021.
