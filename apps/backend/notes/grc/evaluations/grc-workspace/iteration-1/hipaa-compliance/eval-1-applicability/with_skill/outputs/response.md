# HIPAA Applicability Analysis: Healthcare Patient App

## HIPAA Compliance Review

**Scope:** Covered Entity (CE) — with potential Business Associate (BA) relationships for vendors
**Rules Applicable:** Privacy Rule (45 CFR Part 164, Subparts A & E), Security Rule (45 CFR Part 164, Subparts A & C), Breach Notification Rule (45 CFR Part 164, Subparts A & D)

---

## Short Answer

**Yes, you almost certainly need to comply with HIPAA.** Your app handles Protected Health Information (PHI) — specifically, medical records and doctor-patient communications — and the entity operating it likely qualifies as a Covered Entity or is directly serving one.

---

## Are You a Covered Entity or a Business Associate?

### Covered Entity (CE) — 45 CFR §160.103

A Covered Entity is one of three types of organizations:

1. **Healthcare Providers** — Any provider that transmits health information electronically in connection with standard transactions (e.g., claims, eligibility checks). This includes hospitals, clinics, physician practices, and telehealth platforms that employ or contract with physicians.
2. **Health Plans** — Insurers, HMOs, employer-sponsored health plans.
3. **Healthcare Clearinghouses** — Entities that process nonstandard health data into standard formats.

**If your app is operated by or on behalf of a healthcare provider (e.g., a hospital system, clinic network, or physician group), you are operating as or for a Covered Entity.** Patient portals that allow patients to view records and message their doctors are a classic Covered Entity use case.

### Business Associate (BA) — 45 CFR §160.103

A Business Associate is any person or entity that:
- Creates, receives, maintains, or transmits PHI **on behalf of** a Covered Entity
- Performs functions or activities for a CE that involve PHI (e.g., data storage, analytics, billing)

**If you are a third-party vendor building or hosting this app for a healthcare provider, you are a Business Associate.** As a BA, you must:
- Sign a **Business Associate Agreement (BAA)** with the CE before handling any PHI
- Comply with the HIPAA Security Rule in full (45 CFR §164.302–318)
- Comply with the Privacy Rule provisions applicable to BAs (45 CFR §164.502(e), §164.504(e))
- Report breaches to the CE under the Breach Notification Rule (45 CFR §164.410)

### Subcontractors

If your app uses third-party cloud storage, analytics, or infrastructure vendors that will have access to PHI, those vendors become **Subcontractors** — who are themselves treated as Business Associates. You must obtain BAAs from each of them (45 CFR §164.308(b)(3)).

---

## What PHI Does Your App Handle?

Under 45 CFR §160.103, PHI is individually identifiable health information that relates to:
- The past, present, or future physical or mental health condition of an individual
- The provision of healthcare to an individual
- Past, present, or future payment for healthcare

Your app processes at minimum:
| Data Element | PHI? | HIPAA Identifier |
|---|---|---|
| Patient name | Yes | Name identifier |
| Medical records / diagnoses | Yes | Health condition data |
| Doctor-patient messages | Yes | Health condition + treatment data |
| Appointment dates | Yes | Date identifier |
| IP addresses / device IDs | Yes | Electronic identifiers |

**All data flowing through your app is PHI and subject to full HIPAA protections.**

---

## Key Obligations by Entity Type

### If You Are (or Operate for) a Covered Entity:

1. **Notice of Privacy Practices (NPP)** — Must be provided to patients at first service (45 CFR §164.520)
2. **Minimum Necessary Standard** — Access to PHI must be limited to the minimum necessary for each purpose (45 CFR §164.502(b))
3. **Patient Rights** — Must support rights to access, amend, and request restrictions on their PHI (45 CFR §164.524–528)
4. **Workforce Training** — All staff with PHI access must be trained (45 CFR §164.530(b))
5. **Security Rule Compliance** — Must implement administrative, physical, and technical safeguards for ePHI (45 CFR §164.302–318)
6. **Breach Notification** — Must notify HHS and affected individuals within 60 days of discovering a breach (45 CFR §164.400–414)

### If You Are a Business Associate:

1. **Execute a BAA** with the Covered Entity before receiving PHI
2. **Security Rule** — Full compliance required (administrative, physical, technical safeguards)
3. **Breach Notification to CE** — Must notify the CE without unreasonable delay and within 60 days (45 CFR §164.410)
4. **Subcontractor BAAs** — Required for any downstream vendors handling PHI

---

## HITECH Act Considerations

The **Health Information Technology for Economic and Clinical Health (HITECH) Act of 2009** strengthened HIPAA enforcement and extended direct liability to Business Associates. Key impacts:

- BAs are directly liable for HIPAA Security Rule violations (not just contractually via the CE)
- Increased civil monetary penalties up to **$1.9 million per violation category per year**
- Strengthened breach notification requirements
- Enabled state attorneys general to bring HIPAA enforcement actions

---

## Practical Next Steps

1. **Confirm your legal status** — Determine whether you are the CE or a BA serving a CE
2. **Execute BAAs** — With your CE client (if you are a BA) and with all vendors you use (cloud, analytics, etc.)
3. **Conduct a Risk Analysis** — Required under 45 CFR §164.308(a)(1)(ii)(A)
4. **Implement Technical Safeguards** — Encryption (AES-256 at rest, TLS 1.2+ in transit), audit logging, access controls, automatic logoff
5. **Draft or Obtain an NPP** — Required if operating as a CE
6. **Establish Breach Response Procedures** — Required under the Breach Notification Rule

---

> **Disclaimer:** This guidance is for informational purposes only and does not constitute legal advice. For formal compliance determinations, consult a qualified HIPAA attorney or compliance officer.
