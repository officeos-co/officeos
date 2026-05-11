# Significant Data Fiduciary (SDF) Under the DPDPA

## What Is a Significant Data Fiduciary?

A **Significant Data Fiduciary (SDF)** is a Data Fiduciary designated by the Central Government of India under **Section 10** of the DPDPA. The designation is based on an assessment of the potential risks and scale of data processing by the organisation.

The equivalent in GDPR would be a "high-risk controller" subject to heightened obligations — but unlike GDPR, which uses risk-based thresholds for DPO appointments and DPIAs, DPDPA's SDF designation is an explicit government notification, not a self-assessed threshold.

---

## Designation Criteria (Section 10)

The Central Government designates an organisation as an SDF based on any combination of the following factors:

| Criterion | Description |
|-----------|-------------|
| **Volume of personal data** | Large-scale processing of personal data of Indian Data Principals |
| **Sensitivity of personal data** | Processing of sensitive or high-risk categories of data |
| **Risk to Data Principals** | Potential risk of harm to rights and freedoms of Data Principals |
| **National security/sovereignty impact** | Potential impact on India's sovereignty, integrity, security, or electoral democracy |
| **Risk to public order** | Processing that could threaten public order |

---

## Current Designation Status

> **As of April 2026, no organisations have been publicly designated as SDFs.** The Central Government has not yet published the list of designated SDFs. This is flagged because several obligations depend on this notification, and until it is issued, SDF-specific obligations are not yet triggered for any specific company.

Organisations most likely to be designated as SDFs in the first wave include:
- Large social media platforms with significant Indian user bases
- Major e-commerce companies
- Fintech and digital payment platforms
- Large technology companies processing high volumes of Indian personal data
- Search engines and content aggregators

If your organisation processes personal data at significant scale in India, you should **self-assess and prepare** for potential SDF designation — even before the official notification.

---

## Additional Obligations for SDFs (Section 10 + Rule 13)

SDF designation triggers four additional obligations beyond the standard Data Fiduciary requirements:

### 1. Data Protection Officer (DPO) — India-Resident

| Requirement | Detail |
|------------|--------|
| Mandatory appointment | Yes — cannot outsource or use a foreign DPO |
| Residency | Must be **resident in India** (a critical difference from GDPR, which has no location requirement) |
| Role | Sole representative of the SDF before the **Data Protection Board of India (DPBI)** |
| Function | Primary contact for Data Principal grievances |
| Accountability | Senior individual; must have authority within the organisation |

**Note for GDPR-compliant organisations:** If your current DPO is based outside India, they cannot fulfil the DPDPA DPO role for India. A separate India-resident appointment is required.

### 2. Data Protection Impact Assessment (DPIA) — Annual

| Requirement | Detail |
|------------|--------|
| Frequency | **Annual** — not event-triggered |
| Scope | Must evaluate: (a) compliance with the Act and Rules; (b) Data Principals' ability to exercise their rights; (c) adequacy of technical and organisational safeguards; (d) risks from large-scale processing |
| Conducted by | Internal privacy/compliance team (Rule 13 does not require independent conduct for the DPIA, unlike the audit) |
| Output | Formal DPIA report |

This is significantly different from GDPR, where a DPIA is a one-time event-triggered assessment (required when processing is "likely to result in high risk"). DPDPA SDFs must conduct one every year.

### 3. Independent Data Audit — Annual

| Requirement | Detail |
|------------|--------|
| Frequency | **Annual** |
| Auditor | Must be **independent** — cannot be an employee or internal team |
| Qualification | Must be a qualified independent auditor (exact qualification criteria to be specified by Central Government) |
| Report submitted to | Data Protection Board of India (DPBI) |
| Content | Significant observations, material risks identified, and remediation recommendations |

This is an important governance mechanism. The auditor's report goes directly to the Board — not just to the SDF's management — creating external accountability.

### 4. Data Localisation (If Notified)

- The Central Government may require SDFs to store **specified categories of personal data within India** — i.e., prohibit their cross-border transfer
- This is the data localisation obligation, and it applies only to categories of data specifically notified
- **Current status (April 2026):** No data localisation categories have been notified for SDFs
- This could require significant infrastructure investment (India-based data centres) for organisations currently operating entirely from offshore

---

## How Would Your Company Know If It Is Designated as an SDF?

Designation occurs by **Central Government notification**, published in the **Official Gazette of India**. There is no self-assessment threshold that automatically applies.

### The Process:
1. The Ministry of Electronics and Information Technology (MeitY) evaluates organisations based on the Section 10 criteria
2. The Central Government issues a notification designating specific organisations as SDFs
3. The notification is published in the Official Gazette
4. Designated organisations are formally notified

### What You Should Do Now (Before Designation):

Since no SDFs have been designated yet, organisations in high-risk categories should take these proactive steps:

| Action | Why |
|--------|-----|
| **Monitor the Official Gazette and MeitY announcements** | You will be notified via official publication |
| **Self-assess against Section 10 criteria** | If you process large volumes of Indian data, sensitively-categorised data, or operate platforms with significant societal/electoral reach, you are at elevated risk of designation |
| **Identify a prospective India-resident DPO candidate** | Appointing and onboarding an India-resident DPO takes time; identify the candidate before designation |
| **Develop DPIA methodology** | A DPDPA-specific DPIA framework differs from GDPR DPIAs; develop your template and process now |
| **Identify qualified independent auditors** | Building relationships with qualified data audit firms in India takes time |
| **Map your data flows for potential localisation** | Understand what data could be subject to localisation requirements and what infrastructure changes that would require |

---

## SDF Obligations vs. Standard Data Fiduciary Obligations

| Obligation | All Data Fiduciaries | SDFs Only |
|-----------|---------------------|-----------|
| Section 5 Notice | Yes | Yes |
| Section 6 Consent | Yes | Yes |
| Section 7 Legitimate Uses | Yes | Yes |
| Section 8 General Obligations (security, processors, breach notification in 72h) | Yes | Yes |
| Section 9 Children's Data | Yes | Yes |
| India-resident DPO | No | **Yes (Rule 13)** |
| Annual DPIA | No | **Yes (Rule 13)** |
| Annual Independent Audit (reported to Board) | No | **Yes (Rule 13)** |
| Data Localisation (if notified categories) | No | **Yes (Section 10)** |

---

## Penalties for SDF Non-Compliance

Non-compliance with Section 10 (SDF additional obligations) carries a maximum penalty of **₹150 crore**, imposed by the Data Protection Board of India. This is separate from penalties for standard Data Fiduciary violations, which can reach ₹250 crore for security failures.
