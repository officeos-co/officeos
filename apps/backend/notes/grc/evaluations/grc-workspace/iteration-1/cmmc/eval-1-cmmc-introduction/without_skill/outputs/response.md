# CMMC Level 2: A Plain-English Guide for Your Aerospace Firm

## What Is CMMC?

CMMC stands for **Cybersecurity Maturity Model Certification**. It is a Department of Defense (DoD) program that requires companies in the defense supply chain to demonstrate they are properly protecting sensitive government information on their systems. Think of it as a cybersecurity audit and certification that proves your company takes information security seriously enough to be trusted with DoD work.

The clause in your contract — DFARS 252.204-7021 — is the legal hook that requires you to achieve and maintain CMMC certification as a condition of doing business with the DoD.

---

## Why Does the DoD Require This?

The DoD shares two categories of sensitive information with contractors:

- **FCI (Federal Contract Information)**: Basic information about the contract itself — what you're building, for whom, at what price.
- **CUI (Controlled Unclassified Information)**: More sensitive technical or operational data — engineering specs, design documents, test data, export-controlled technical information (ITAR/EAR), and similar materials.

As an aerospace engineering firm, you almost certainly handle CUI. Foreign adversaries (primarily China and Russia) have spent years stealing defense-related intellectual property from contractors exactly like you. CMMC is the DoD's response to that threat.

---

## What Are the CMMC Levels?

There are three levels:

| Level | Who It Applies To | How It's Verified |
|-------|-------------------|-------------------|
| Level 1 | Companies that only handle FCI | Annual self-assessment |
| **Level 2** | **Companies that handle CUI** | **Third-party assessment (C3PAO) every 3 years** |
| Level 3 | Companies handling the most sensitive CUI (critical programs) | Government-led assessment |

Your contract requires **Level 2**, which applies to any company that receives, creates, stores, processes, or transmits CUI. This is the most common requirement across the defense industrial base.

---

## What Does Level 2 Actually Require?

CMMC Level 2 is based almost entirely on **NIST SP 800-171**, a well-established federal standard for protecting CUI. It contains **110 security practices** organized into 14 domains (categories):

1. Access Control
2. Awareness and Training
3. Audit and Accountability
4. Configuration Management
5. Identification and Authentication
6. Incident Response
7. Maintenance
8. Media Protection
9. Personnel Security
10. Physical Protection
11. Risk Assessment
12. Security Assessment
13. System and Communications Protection
14. System and Information Integrity

### What These Mean in Practice

Here are examples of what you'll need to have in place:

- **Access Control**: Only authorized employees can access systems that store or process CUI. Multi-factor authentication (MFA) is required. Privileged access (admin rights) must be tightly controlled.
- **Audit Logs**: Your systems must log who accessed what, when, and from where. Logs must be retained and reviewed.
- **Encryption**: CUI must be encrypted both when stored (at rest) and when transmitted over networks (in transit).
- **Incident Response**: You need a written plan for what to do if you suffer a cyberattack or data breach, and you must be able to report incidents to the DoD within 72 hours.
- **Configuration Management**: Systems holding CUI must be hardened (unnecessary software/services disabled) and regularly patched.
- **Risk Assessments**: You must periodically assess your cybersecurity risks and document what you find.
- **Policies and Procedures**: Written policies covering all 14 domains must exist and be followed.
- **Physical Security**: Physical access to systems containing CUI must be controlled (locked server rooms, clean-desk policies, etc.).
- **Media Protection**: Portable media (USB drives, laptops) containing CUI must be controlled, encrypted, and properly sanitized before disposal.

---

## How Is Level 2 Certification Obtained?

Unlike Level 1 (which is self-assessed), **Level 2 requires an independent third-party assessment** conducted by a **C3PAO** (Certified Third-Party Assessment Organization). The C3PAO is an accredited firm that will assess your environment against all 110 practices and issue a certification if you pass.

### The Assessment Process

1. **Scoping**: Define which systems, networks, people, and locations handle CUI. This is called your "CUI environment" or assessment scope. Keeping the scope small (isolating CUI to as few systems as possible) reduces cost and complexity.
2. **Gap Assessment**: Identify where you currently fall short of the 110 requirements. Most companies starting from scratch fail 40–70 practices initially.
3. **Remediation**: Fix the gaps — implement the missing controls, write the missing policies, configure systems correctly.
4. **Formal Assessment**: The C3PAO reviews your documentation, interviews staff, and tests your technical controls. This typically takes 1–3 weeks on-site or remote.
5. **Certification**: If you meet all 110 practices (or have an accepted Plan of Action & Milestones for minor gaps), the C3PAO submits results to the DoD's CMMC database (called SPRS). You receive your certification, valid for 3 years.

---

## How Long Will This Take?

For a 45-person firm starting from scratch, here is a realistic timeline:

| Phase | Duration |
|-------|----------|
| Scoping and gap assessment | 4–8 weeks |
| Remediation (fixing gaps) | 6–18 months (highly variable) |
| Pre-assessment readiness check | 4–8 weeks |
| Formal C3PAO assessment | 2–6 weeks |
| **Total realistic timeline** | **12–24 months** |

The wide range depends on:
- **How mature your current IT security is.** If you already have MFA, encryption, and documented policies, you're ahead. Most small engineering firms are starting close to zero.
- **Whether you engage a consultant.** A CMMC Registered Practitioner (RP) or Registered Provider Organization (RPO) can dramatically accelerate the process by guiding remediation.
- **Your scope.** If CUI lives on 3 workstations and a file server, remediation is much faster than if it touches 40 machines and a complex network.
- **Resource availability.** If you dedicate a full-time person internally to this effort, it moves faster.

### Important: Your Contract Clock Is Running

Your DFARS clause requires you to be certified before performing work involving CUI (or within a timeframe specified in the contract). Check your contract for any stated deadline. If one exists, notify your Contracting Officer (KO) immediately and begin work now — 12–24 months is not unusual, and waivers or timelines can sometimes be negotiated for good-faith effort.

---

## What Will It Cost?

Rough estimates for a 45-person firm:

| Item | Estimated Cost |
|------|----------------|
| CMMC consultant / RPO engagement | $50,000–$150,000 |
| IT infrastructure upgrades (MFA, encryption, logging tools, endpoint protection) | $30,000–$100,000 |
| Policy and documentation development | Included with consultant or $10,000–$30,000 if separate |
| C3PAO formal assessment fee | $50,000–$100,000 |
| **Total range** | **$140,000–$380,000** |

These are rough estimates. Actual costs depend on your current state, scope, and the vendors you select. Annual ongoing costs for maintaining compliance (monitoring tools, training, reassessment) will run $30,000–$80,000/year.

---

## Your Immediate Next Steps

1. **Do not ignore this.** DFARS 252.204-7021 is a contract requirement. Non-compliance can result in contract termination, suspension, or debarment from future DoD work.

2. **Identify your CUI.** Understand what sensitive government information you are receiving and where it lives (email, file servers, laptops, cloud storage). This defines your scope.

3. **Conduct a gap assessment.** Hire a CMMC Registered Practitioner or RPO to assess your current state against NIST SP 800-171. This will produce a prioritized list of what needs to be fixed.

4. **Create a System Security Plan (SSP).** This is a required document that describes your CUI environment, your systems, and how each of the 110 practices is implemented. Start drafting it early.

5. **Create a Plan of Action & Milestones (POA&M).** For every practice you haven't yet implemented, document your plan and timeline to fix it. The DoD may review your SPRS score (a self-assessed score you must submit now, even before certification).

6. **Submit your SPRS score.** Under current rules, you must self-assess against NIST SP 800-171 and report a score in the Supplier Performance Risk System (SPRS) at supplierpas.epls.fas.gsa.gov. This is required even while working toward formal CMMC certification. If you haven't done this, do it now.

7. **Select and engage a C3PAO.** The Cyber AB Marketplace (cyberab.org) lists all accredited assessment organizations. Get quotes early — C3PAO calendars fill up, and scheduling can take months.

---

## Key Terms Cheat Sheet

| Term | What It Means |
|------|---------------|
| CMMC | Cybersecurity Maturity Model Certification — the DoD's cybersecurity certification program |
| DFARS | Defense Federal Acquisition Regulation Supplement — the legal framework governing DoD contracts |
| CUI | Controlled Unclassified Information — sensitive (but not classified) government data |
| NIST SP 800-171 | The federal standard with 110 cybersecurity practices that form the basis of CMMC Level 2 |
| C3PAO | Certified Third-Party Assessment Organization — the accredited firm that certifies you |
| SSP | System Security Plan — your master document describing your security environment |
| POA&M | Plan of Action & Milestones — your remediation roadmap for gaps |
| SPRS | Supplier Performance Risk System — DoD database where you report your self-assessment score |
| RPO | Registered Provider Organization — a CMMC-accredited consulting firm that can help you prepare |
| Cyber AB | The CMMC Accreditation Body — oversees C3PAOs and certifications |

---

## Summary

CMMC Level 2 means the DoD requires you to prove — through an independent audit — that you are protecting their sensitive information using 110 specific cybersecurity practices drawn from NIST SP 800-171. For a 45-person firm starting from scratch, expect 12–24 months of preparation work, costs in the range of $140,000–$380,000, and an ongoing commitment to maintaining your security posture. The most important thing you can do right now is engage a qualified CMMC consultant and submit your SPRS self-assessment score. The process is substantial but very achievable for a firm your size.
