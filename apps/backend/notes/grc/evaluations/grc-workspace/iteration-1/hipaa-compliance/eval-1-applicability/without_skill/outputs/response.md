# HIPAA Applicability: Healthcare Patient App

## Do You Need to Comply with HIPAA?

**Yes, almost certainly.** An app that allows patients to view their medical records and communicate with doctors handles sensitive health information that is protected under HIPAA (the Health Insurance Portability and Accountability Act of 1996).

---

## What Is HIPAA and Who Does It Cover?

HIPAA establishes federal standards to protect sensitive patient health information. It applies to two main types of organizations:

### Covered Entities
Covered entities are organizations that directly provide or pay for healthcare:
- **Healthcare providers** — hospitals, clinics, doctor's offices, telehealth platforms with providers
- **Health plans** — insurance companies, HMOs
- **Healthcare clearinghouses** — organizations that process health information

If your app is built and operated by (or as part of) a hospital, clinic, physician group, or health plan, you are most likely a **Covered Entity** and have full HIPAA obligations.

### Business Associates
A Business Associate is any vendor, contractor, or service provider that handles protected health information (PHI) on behalf of a Covered Entity. Examples include:
- Technology companies building patient portals for hospitals
- Cloud storage providers hosting medical records
- Analytics firms processing health data

**If you are building this app for a healthcare organization rather than running it yourself as a provider, you are likely a Business Associate.** This means you must sign a Business Associate Agreement (BAA) with the healthcare organization before you can handle any patient data.

---

## What is PHI (Protected Health Information)?

PHI is any health information that can identify a specific individual. This includes:
- Names, addresses, phone numbers, email addresses
- Medical record numbers, diagnoses, treatment information
- Doctor-patient messages about health conditions
- Dates of service, appointment records
- Basically any health data tied to an identifiable person

Your app clearly processes PHI: it shows medical records and facilitates health-related communications between patients and doctors.

---

## Key Obligations

### If You Are a Covered Entity:
- Provide patients with a Notice of Privacy Practices
- Obtain patient authorization for uses of PHI beyond treatment, payment, and operations
- Implement security controls to protect patient data
- Notify patients and the government if a data breach occurs
- Train your workforce on HIPAA requirements
- Respect patient rights to access and amend their records

### If You Are a Business Associate:
- Sign a BAA with the covered entity (the healthcare provider)
- Implement security safeguards to protect the PHI you handle
- Report any breaches to the covered entity
- Ensure any subvendors you use also sign BAAs with you

---

## Practical Recommendation

1. Determine whether your organization is a healthcare provider (Covered Entity) or a technology vendor serving one (Business Associate)
2. If a Business Associate, execute a BAA with the healthcare organization you serve before handling any patient data
3. Implement security controls: encryption, access controls, audit logging
4. Establish a breach notification procedure
5. Consult with a healthcare attorney or compliance specialist to confirm your specific obligations

---

> **Note:** This response provides general information and does not constitute legal advice. Consult a qualified attorney or HIPAA compliance specialist for guidance specific to your situation.
