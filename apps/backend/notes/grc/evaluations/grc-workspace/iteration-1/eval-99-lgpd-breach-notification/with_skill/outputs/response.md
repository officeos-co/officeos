# LGPD Breach Notification Obligations — Database Breach Affecting 50,000 Brazilian Customers

**Incident Summary:** Database breach exposing names, email addresses, and encrypted passwords of 50,000 Brazilian customers.
**Date of Response:** 2026-05-05

---

## 1. Immediate Legal Assessment

### Does LGPD Apply?
Yes. The affected individuals are Brazilian customers, meaning LGPD (Law No. 13,709/2018, as amended by Law No. 13,853/2019) applies in full under **Art. 3**. The data controller has obligations under **Art. 48** and ANPD Resolution CD/ANPD No. 15/2024.

### Data Types Affected

| Data Element | Classification | Notes |
|---|---|---|
| Names | Personal data (Art. 5, I) | Directly identifies individuals |
| Email addresses | Personal data (Art. 5, I) | Contact identifier |
| Encrypted passwords | Personal data (Art. 5, I) | Still personal data; encryption is a mitigating factor but does not eliminate the obligation to notify |

**Important note on encrypted passwords:** Although passwords are encrypted, they remain personal data under LGPD. Depending on encryption strength and whether the keys were also compromised, there is risk of credential-stuffing attacks and account takeover. This constitutes "relevant risk or harm" triggering notification obligations.

This breach does **not** appear to involve sensitive personal data (Art. 11) — health, biometric, racial/ethnic, political, or religious data — which would trigger stricter obligations. However, 50,000 affected data subjects constitutes large-scale processing.

---

## 2. LGPD Notification Obligations

### Primary Legal Basis: Art. 48, LGPD + ANPD Resolution CD/ANPD No. 15/2024

**Art. 48 requires** that controllers notify both the ANPD and affected data subjects of security incidents that **may cause relevant risk or harm** to data subjects.

A breach of 50,000 customers' personal data — including contact information and credentials — clearly meets the threshold of "relevant risk or harm." This triggers **two mandatory notifications**:

1. **Preliminary notification to ANPD** — within 3 working days of awareness
2. **Full incident report to ANPD** — within 20 working days of confirmation
3. **Notification to affected data subjects** — where high risk of harm exists (concurrent with or following ANPD notification)

---

## 3. The 72-Hour Action Plan

*Note: LGPD uses "3 working days" not 72 calendar hours (unlike GDPR). However, treating this as approximately 72 hours is the safest approach to ensure compliance.*

### Hour 0–4: Detect and Contain

- [ ] **Activate incident response team** — include legal/DPO, IT security, communications, and executive leadership
- [ ] **Isolate affected systems** — take the compromised database offline or restrict access immediately to prevent further exfiltration
- [ ] **Preserve evidence** — capture logs, access records, and forensic data before any remediation that could destroy evidence
- [ ] **Change all database credentials** — rotate passwords, API keys, and access tokens for all affected systems
- [ ] **Identify the attack vector** — SQL injection, compromised credentials, misconfigured access controls, or third-party breach?
- [ ] **Engage forensic specialists** — internal or external cybersecurity experts to conduct investigation
- [ ] **Notify DPO (Encarregado)** — under Art. 41, the DPO must be involved immediately and will be the point of contact with ANPD

### Hour 4–24: Assess and Scope

- [ ] **Confirm the scope of breach** — verify that exactly 50,000 (or more/fewer) records were exposed; identify which fields were accessed
- [ ] **Assess encryption strength** — determine the hashing/encryption algorithm used for passwords (bcrypt, SHA-1, MD5, etc.); weaker algorithms (MD5, SHA-1) dramatically increase risk
- [ ] **Determine if encryption keys were also compromised** — if so, passwords are effectively exposed in plaintext
- [ ] **Assess whether exfiltration occurred** — was data copied, or only accessed?
- [ ] **Identify timeline of breach** — when did it begin, when was it discovered?
- [ ] **Assess risk to data subjects** — consider: likelihood of password cracking given encryption method; risk of account takeover on other platforms (password reuse); phishing risk using exposed names and emails
- [ ] **Document all findings** — maintain a written incident log for accountability (Art. 6, X — Accountability principle)

### Hour 24–48: Preliminary ANPD Notification (Working Day 1–3)

**You must file a preliminary notification with ANPD within 3 working days** of becoming aware of the incident (ANPD Resolution CD/ANPD No. 15/2024; Art. 48, §1º).

#### ANPD Preliminary Notification — Required Content

The preliminary notification must include all of the following:

| Required Element | Description |
|---|---|
| **Nature of affected data** | Names, email addresses, encrypted passwords |
| **Number of affected data subjects** | 50,000 Brazilian customers (confirmed or estimated) |
| **Technical and security measures adopted** | Encryption in place; containment measures taken (system isolation, credential rotation, forensic investigation commenced) |
| **Risks of the incident** | Potential credential-stuffing attacks, account takeover, phishing using exposed contact data |
| **Measures taken or planned** | Containment actions, password reset enforcement, user notification plan, forensic review |
| **Controller identity and contact** | Full legal name, CNPJ, DPO name and contact details (Art. 41) |
| **Reason for any delay** | If notification is delayed beyond 3 working days, reason must be stated |

**How to file:** Submit via the ANPD's official portal at [https://www.gov.br/anpd/pt-br](https://www.gov.br/anpd/pt-br) or by email to anpd@anpd.gov.br. Check ANPD's current portal for the most up-to-date submission form.

### Hour 48–72: Notify Affected Data Subjects

**Art. 48 requires notification to affected data subjects** where the incident poses high risk of harm. A breach of login credentials (even encrypted) and contact data for 50,000 individuals meets this threshold.

#### Data Subject Notification Must Include:

- Description of the incident in plain language
- Types of data exposed (name, email, encrypted password)
- Measures taken to protect them
- Practical steps data subjects should take (password reset, watch for phishing)
- Contact information for the DPO / support team
- Data subjects' rights under LGPD (Art. 18) and how to exercise them

**Recommended notification channels:** Direct email to each affected customer (you have their email addresses); supplementary notice on your website/app.

**Tone and content guidance:** Be clear, honest, and actionable. Avoid minimising the incident. Customers should be told:
- Their password on your platform has been reset or should be reset immediately
- If they use the same password elsewhere, they should change it on all other platforms
- To be vigilant for phishing emails

---

## 4. Full Incident Report — Within 20 Working Days

Within 20 working days of confirming the incident, submit a **comprehensive report to ANPD** (Art. 48). This must include:

- Complete forensic findings — how the breach occurred, timeline, full scope
- Complete list of data categories and fields affected
- Exact (or refined) count of affected data subjects
- Root cause analysis
- Full description of corrective and preventive measures implemented
- Any third-party processor involvement and their liability assessment (Art. 42)
- Evidence of data subject notifications sent
- Impact assessment on data subjects' rights
- Long-term remediation plan

---

## 5. Parallel Actions — Beyond 72 Hours

### Force a Password Reset
- Immediately invalidate all sessions and force password resets for affected accounts
- Implement multi-factor authentication (MFA) if not already in place

### Assess Third-Party Processors
Under **Art. 39 and Art. 42**, if the database was managed or accessed by a processor (third-party cloud provider, database vendor, etc.), you must:
- Notify them of the incident
- Assess whether they bear joint liability
- Review your Data Processing Agreement (DPA) with them

### Review Legal Bases and Records
- Confirm your legal basis for processing the affected customer data (Art. 7) — most likely **Art. 7, V (contract)** or **Art. 7, I (consent)**
- Ensure your Records of Processing Activities (RoPA — Art. 37) is updated to reflect the incident

### Engage Legal Counsel
- Assess civil liability exposure under **Art. 42–44** — affected data subjects may seek damages
- Consider whether the Consumer Defence Code (CDC) creates additional obligations
- Review cyber insurance policy for coverage and notification requirements

### Conduct a DPIA / RIPD
Under **Art. 38**, conduct or update a Data Protection Impact Assessment (RIPD) for the affected processing activity to document risks and mitigation measures. ANPD may request this during their investigation.

---

## 6. Penalty Exposure

If LGPD obligations are not met, the following sanctions may apply under **Art. 52**:

| Sanction | Applicable Scenario |
|---|---|
| **Warning** | First violation; minor procedural breach |
| **Simple fine** | Up to 2% of Brazilian revenue (prior FY); maximum **R$50 million per violation** |
| **Daily fine** | To compel compliance with ANPD orders |
| **Public disclosure** | Publication of the violation after investigation |
| **Data blocking or deletion** | Affecting the processing activities involved |
| **Suspension or prohibition** | For serious or recurrent violations |

**Key mitigating factors (Art. 52, §1º)** that reduce penalty exposure:
- Promptly notifying ANPD within the 3-working-day window
- Cooperating fully with the ANPD investigation
- Demonstrating good faith and existing security measures (encrypted passwords)
- Promptly notifying and protecting affected data subjects
- Implementing corrective measures before ANPD action

**Key aggravating factors to avoid:**
- Late or incomplete ANPD notification
- Failure to notify data subjects
- Evidence of inadequate security practices (e.g., weak hashing algorithms)
- Prior LGPD violations

**Large-scale breach context:** ANPD has identified large-scale data breaches as an enforcement priority. A 50,000-person breach will attract regulatory attention. Prompt, transparent action is essential.

---

## 7. 72-Hour Checklist Summary

| Priority | Action | Deadline |
|---|---|---|
| **CRITICAL** | Contain the breach — isolate systems, preserve evidence | Hour 0–4 |
| **CRITICAL** | Notify DPO; activate incident response team | Hour 0–4 |
| **CRITICAL** | Change all compromised credentials and access keys | Hour 0–8 |
| **HIGH** | Scope and assess the breach — data types, volume, vector | Hour 4–24 |
| **HIGH** | Force password reset for all 50,000 affected accounts | Hour 4–24 |
| **CRITICAL** | File **preliminary ANPD notification** (3 working days) | Working Day 1–3 |
| **HIGH** | Notify affected data subjects by email | Hour 48–72 |
| **HIGH** | Engage forensic experts for full investigation | Hour 24–48 |
| **MEDIUM** | Notify third-party processors if involved | Hour 24–72 |
| **MEDIUM** | Brief legal counsel; assess civil liability | Hour 24–72 |

---

## 8. Key LGPD References

| Article / Resolution | Topic |
|---|---|
| **Art. 3** | Scope — applies to Brazilian customers' data |
| **Art. 5, I** | Definition of personal data |
| **Art. 6** | Principles — Security, Prevention, Accountability |
| **Art. 41** | DPO (Encarregado) — must be involved immediately |
| **Art. 46** | Security measures obligation |
| **Art. 48** | Incident notification obligation — ANPD and data subjects |
| **Art. 48, §1º** | Content requirements for notification |
| **Art. 52** | Administrative sanctions and penalty methodology |
| **Art. 42–44** | Civil liability for controllers and processors |
| **ANPD Resolution CD/ANPD No. 15/2024** | 3-working-day preliminary notification requirement |

---

## 9. LGPD vs. GDPR — Key Difference on Breach Notification

If your organisation also has GDPR obligations (e.g., EU customers or EU-based operations), note:

| Aspect | LGPD | GDPR |
|---|---|---|
| Supervisory authority notification | **3 working days** (preliminary) | **72 calendar hours** |
| Full report | **20 working days** | No separate full-report deadline (Art. 33 GDPR) |
| Data subject notification | Required where high risk | Required where high risk (Art. 34 GDPR) |

Under GDPR, 72 calendar hours is a hard deadline; under LGPD, 3 working days is slightly more generous but should still be treated with urgency. If you have dual obligations, you should comply with the stricter 72-hour GDPR clock.

---

*This analysis is based on LGPD (Law No. 13,709/2018, as amended), ANPD Resolution CD/ANPD No. 15/2024, and ANPD enforcement guidance current as of May 2026. This document is for compliance planning purposes and does not constitute legal advice. Engage qualified Brazilian data protection counsel for legal representation.*
