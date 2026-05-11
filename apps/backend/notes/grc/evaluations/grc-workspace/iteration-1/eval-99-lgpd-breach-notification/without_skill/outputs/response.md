# LGPD Breach Notification: 50,000 Brazilian Customers Affected

## Incident Summary

- **Scope:** 50,000 Brazilian data subjects
- **Data Exposed:** Names, email addresses, encrypted passwords
- **Applicable Law:** Lei Geral de Proteção de Dados (LGPD) — Law No. 13,709/2018
- **Regulatory Authority:** Autoridade Nacional de Proteção de Dados (ANPD)

---

## LGPD Notification Obligations

### Legal Basis

Article 48 of the LGPD requires that the data controller communicate to the ANPD and to affected data subjects the occurrence of a security incident that may create risk or relevant harm to the data subjects.

### Notification Threshold

Notification is required when the incident **may cause relevant risk or harm** to the data subjects. For 50,000 individuals with exposed names and emails, this threshold is almost certainly met, even with encrypted passwords, because:

- The volume (50,000 records) is significant.
- Names and emails alone can enable phishing, spam, identity fraud, and credential-stuffing attacks.
- Even encrypted passwords represent a risk if encryption is weak or passwords are reused.

### Notification Deadline

The LGPD states notification must occur **within a reasonable timeframe** after the controller becomes aware of the incident. ANPD Resolution CD/ANPD No. 15/2024 (the ANPD's incident reporting regulation) clarified this as:

- **72 hours** for preliminary notification to the ANPD (same standard as GDPR, and explicitly aligned by ANPD guidance).
- A more **complete follow-up report** is expected within **30 days**.

### Who Must Be Notified

1. **ANPD (Autoridade Nacional de Proteção de Dados)** — Mandatory preliminary report within 72 hours.
2. **Affected Data Subjects** — Must be notified when the incident may cause significant harm, within a reasonable timeframe.

### Content of the ANPD Notification

The preliminary notification must include, at minimum:

- Description of the nature of the affected personal data
- Categories and approximate number of affected data subjects
- Contact details of the Data Protection Officer (DPO/Encarregado)
- Description of the likely consequences of the incident
- Measures taken or proposed to mitigate the effects
- Date and time of discovery of the incident
- Whether the notification to data subjects has been or will be made

---

## 72-Hour Action Plan

### Hour 0–6: Containment and Assessment

- [ ] **Activate the Incident Response Plan.** Convene the incident response team immediately.
- [ ] **Isolate the compromised system(s).** Take affected database(s) or services offline or restrict access to prevent further exfiltration.
- [ ] **Preserve evidence.** Capture logs, system snapshots, and access records before remediation alters them — these will be needed for the ANPD report.
- [ ] **Determine the attack vector.** Understand how the breach occurred (SQL injection, compromised credentials, misconfigured storage, etc.).
- [ ] **Confirm scope.** Verify exactly which records were accessed or exfiltrated: confirm the 50,000 figure and the fields involved.
- [ ] **Notify internal leadership.** Inform the CEO, Legal, and the DPO (Encarregado de Dados) immediately.

### Hour 6–24: Legal and Regulatory Engagement

- [ ] **Engage legal counsel** with Brazilian data protection expertise.
- [ ] **Contact the DPO (Encarregado).** The DPO is the designated point of contact with the ANPD and must lead the regulatory response.
- [ ] **Assess harm risk formally.** Determine whether the incident crosses the "relevant risk or harm" threshold (it almost certainly does given the scale).
- [ ] **Begin drafting the ANPD preliminary notification.** Use the ANPD's official incident reporting form (available on the ANPD portal at gov.br/anpd).
- [ ] **Document everything.** Maintain a written incident log with timestamped actions — this is required for the 30-day follow-up report and may be inspected.

### Hour 24–48: ANPD Preliminary Notification

- [ ] **Submit the preliminary notification to the ANPD** via the official ANPD portal. Include:
  - Nature of the breach and data categories exposed
  - Approximate number of affected individuals (50,000)
  - DPO contact information
  - Likely consequences for data subjects
  - Mitigation measures taken so far
- [ ] **Assess encryption strength.** If passwords were encrypted with a strong, salted algorithm (e.g., bcrypt, Argon2), document this as a mitigating factor. Weak or unsalted hashing (MD5, SHA-1) elevates risk and must be disclosed.
- [ ] **Evaluate cyber insurance coverage.** Notify your insurer if you have a cyber liability policy — late notification can void coverage.
- [ ] **Engage a forensics firm** if not already done, to conduct an independent investigation and produce a report for the ANPD.

### Hour 48–72: Data Subject Notification and Remediation

- [ ] **Draft data subject notification.** Notifications must be:
  - Written in clear, plain Portuguese
  - Explain what happened, what data was affected, and potential risks
  - Provide steps data subjects can take to protect themselves (e.g., change passwords on other sites, watch for phishing)
  - Include DPO contact information for inquiries
- [ ] **Choose notification channel.** Email is appropriate given you have email addresses. Consider also a prominent notice on your website and/or app.
- [ ] **Send notifications to all 50,000 affected individuals.**
- [ ] **Force password resets** for all affected accounts, even though passwords were encrypted.
- [ ] **Consider offering credit monitoring or fraud alerts** if feasible — not legally required but demonstrates good faith to the ANPD.
- [ ] **Patch the vulnerability** that allowed the breach before bringing systems back online.

---

## Risk Assessment for This Specific Incident

| Factor | Assessment |
|---|---|
| Data sensitivity | Moderate — names and emails are personal data; no financial or health data reported |
| Encryption of passwords | Mitigating — reduces immediate credential misuse risk |
| Volume | High — 50,000 individuals is significant scale |
| Risk of harm | High — phishing, credential stuffing, identity fraud are likely downstream risks |
| Notification required? | **Yes** — both ANPD and data subjects |

---

## Potential Penalties for Non-Compliance

Under LGPD Article 52, failure to notify the ANPD of a breach can result in:

- **Warnings** with a mandatory corrective action deadline
- **Fines** up to 2% of the company's revenue in Brazil in the prior fiscal year, capped at BRL 50 million per infraction
- **Public disclosure** of the violation
- **Suspension** of data processing activities

Failure to notify affected data subjects can result in similar sanctions and significantly increases reputational damage.

---

## 30-Day Follow-Up Obligations

After the 72-hour window, the ANPD expects a complete incident report within 30 days (per ANPD Resolution CD/ANPD No. 15/2024), covering:

- Root cause analysis
- Full forensics findings
- Complete list of affected data categories and record counts
- All remediation measures implemented
- Evidence that data subjects were notified
- Measures adopted to prevent recurrence

---

## Key Contacts and Resources

- **ANPD Portal:** [https://www.gov.br/anpd](https://www.gov.br/anpd)
- **ANPD Incident Reporting Form:** Available in the "Peticionamento Eletrônico" section of the ANPD portal
- **LGPD Text:** Lei No. 13,709/2018
- **ANPD Incident Resolution:** Resolução CD/ANPD No. 15, de 24 de abril de 2024

---

## Summary Checklist

**Within 72 hours:**
- [ ] Contain the breach
- [ ] Preserve forensic evidence
- [ ] Notify internal leadership and DPO
- [ ] Engage legal counsel
- [ ] Submit preliminary notification to ANPD
- [ ] Notify all 50,000 affected data subjects
- [ ] Force password resets for affected accounts
- [ ] Patch the vulnerability

**Within 30 days:**
- [ ] Submit complete incident report to ANPD
- [ ] Complete forensic investigation
- [ ] Implement and document remediation measures
- [ ] Review and update incident response and data security policies

---

*This analysis is based on LGPD (Lei No. 13,709/2018) and ANPD regulatory guidance as of 2025. Organizations should consult qualified Brazilian data protection counsel for advice specific to their circumstances.*
