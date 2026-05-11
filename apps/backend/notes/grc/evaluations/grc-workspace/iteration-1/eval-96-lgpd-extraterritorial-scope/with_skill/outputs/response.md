# LGPD Compliance Analysis: US SaaS Company with Brazilian Customers

**Date:** 2026-05-05  
**Prepared by:** LGPD Compliance Advisor  
**Subject:** Extraterritorial scope of LGPD and applicable legal bases for processing name, email, CPF, and purchase history of Brazilian customers

---

## 1. Does LGPD Apply to Your Company?

**Yes. LGPD applies to your company, even though you are incorporated and based in the United States.**

### Legal Basis for Extraterritorial Application (Art. 3)

LGPD's territorial scope is defined in **Art. 3**, which expressly extends the law beyond Brazil's borders. The law applies to any processing of personal data when **at least one** of the following conditions is met:

| Condition | Art. 3 Reference | Applicability to Your Company |
|-----------|-----------------|-------------------------------|
| Processing occurs in Brazil | Art. 3, I | Possibly (if data is processed on Brazilian infrastructure) |
| **Purpose is to offer or provide goods or services to individuals located in Brazil** | **Art. 3, II** | **Yes — you actively serve Brazilian customers** |
| Personal data was collected in Brazil | Art. 3, III | Yes — your Brazilian customers are in Brazil when they sign up and use your SaaS |

Your company clearly meets **Art. 3, II** (and likely Art. 3, III as well): you are deliberately offering a SaaS product to individuals located in Brazil and collecting their personal data in that context. This is directly analogous to the GDPR "targeting" criterion under Art. 3(2) GDPR, and LGPD was deliberately drafted with similar extraterritorial reach.

### The "Location in Brazil" Standard

LGPD uses the phrase "individuals located in Brazil" (pessoas que se encontrem no território nacional) — meaning physical presence in Brazil at the time of data collection, not citizenship or domicile. Your Brazilian customers are located in Brazil when they use your app, which satisfies this criterion.

### No Exemption Applies (Art. 4)

The Art. 4 exemptions do not apply to your situation. Those exemptions cover:
- Personal or household use only
- Journalistic, artistic, or academic purposes
- National security, public safety, or criminal investigation by public entities
- Data originating exclusively outside Brazil with no communication to Brazilian recipients

A US SaaS company providing commercial services to Brazilian customers falls squarely outside all exemptions.

**Conclusion:** LGPD applies to your company in full. You are a **controller (controlador)** as defined in Art. 5, XIV — you determine the purposes and means of processing your Brazilian customers' personal data.

---

## 2. Data Classification Analysis

Before identifying legal bases, it is essential to classify the data you collect:

| Data Element | LGPD Classification | Notes |
|-------------|---------------------|-------|
| **Name** | Regular personal data (Art. 5, I) | Identifies the individual |
| **Email address** | Regular personal data (Art. 5, I) | Identifies or is linked to the individual |
| **CPF (Cadastro de Pessoas Físicas)** | Regular personal data (Art. 5, I) | Brazil's national taxpayer identification number; highly sensitive in practice due to identity fraud risk — ANPD has flagged CPF exposure as high-risk (see large-scale breach investigations) |
| **Purchase history** | Regular personal data (Art. 5, I) | Financial/transactional data linked to the individual |

**Important note on CPF:** While CPF is classified as regular (not sensitive) personal data under LGPD's Art. 11 definitions, the ANPD has treated CPF data breaches with heightened concern in enforcement actions, given the severe harm that CPF exposure causes to Brazilian individuals (identity theft, credit fraud). Your security and legal basis choices for CPF should reflect this elevated practical risk.

None of the data you collect constitutes **sensitive personal data** under Art. 11 (which covers racial/ethnic origin, religion, political opinion, trade union membership, health or sex life data, genetic data, or biometric data) — provided you do not profile customers in ways that reveal such attributes from their purchase history.

---

## 3. Legal Bases for Each Processing Activity

Under LGPD **Art. 7**, processing of regular personal data requires one of ten enumerated legal bases. Unlike a general balancing test, LGPD requires a specific positive basis for each processing activity. Below is an analysis of the bases most relevant to your SaaS context.

### 3.1 Legal Bases Applicable to Your Processing

#### A. Contract — Art. 7, V (Primary Basis for Core Service Delivery)

**"Processing necessary for the execution of a contract or preliminary procedures related to a contract to which the data subject is a party, at the data subject's request"**

**Applies to:** Collection and use of name, email, CPF (where required for account/billing), and purchase history for the purpose of:
- Creating and managing customer accounts
- Delivering the subscribed SaaS service
- Billing and payment processing
- Order confirmation and transaction records

**Key requirements:**
- Processing must be genuinely necessary for contract performance — not merely convenient
- The data subject must be a party to the contract (your Terms of Service / subscription agreement)
- Applies to pre-contractual steps (e.g., account setup, onboarding) as well as ongoing service delivery

**Practical guidance:** Name and email are clearly necessary for account creation and service delivery. CPF may be necessary where required for invoicing (Brazilian fiscal rules often require CPF on NF-e electronic invoices). Purchase history is directly generated by and necessary to the contractual relationship. This is your strongest and most appropriate primary basis for core service functions.

---

#### B. Consent — Art. 7, I (For Non-Essential Processing)

**"With the consent of the data subject"**

Consent under LGPD must be (Art. 8):
- **Free** — not coerced; cannot be bundled as a condition of service unless necessary
- **Informed** — purpose clearly stated
- **Unambiguous** — affirmative action required; pre-ticked boxes are invalid
- **Specific** — separate consent per distinct purpose; generic consent is invalid
- **Documented** — burden of proof rests on the controller
- **Revocable** — at any time, at no cost, without prejudice to prior lawful processing

**Applies to:** Processing activities that go beyond contract performance, such as:
- Marketing communications and promotional emails
- Sharing data with third-party marketing platforms
- Behavioural analytics beyond service improvement
- Personalised advertising based on purchase history

**Key limitation:** You cannot condition access to your SaaS service on consent for non-essential processing (Art. 8, §3º). Consent for marketing must be separate from and independent of the service agreement.

---

#### C. Legitimate Interest — Art. 7, IX (For Proportionate Business Purposes)

**"When necessary to serve the legitimate interests of the controller or a third party, except where the data subject's fundamental rights and freedoms prevail"**

Unlike GDPR, LGPD's legitimate interest basis is more circumscribed — Art. 10 requires that:
1. The interest be legitimate and specific
2. Processing not prevail over the data subject's fundamental rights
3. Data subjects have a reasonable expectation of the processing
4. A balancing test (Legitimate Interest Assessment / LIA) be documented

**Sensitive data cannot use legitimate interest** (Art. 11 does not include it as a basis).

**May apply to:**
- Internal analytics and product improvement (where proportionate)
- Fraud detection and security monitoring
- IT security and system integrity logging
- Customer support and service quality monitoring
- Internal business reporting aggregating anonymised data

**Required action:** For each activity relying on legitimate interest, conduct and document a balancing test addressing: (a) the specific legitimate interest pursued; (b) necessity of the processing; (c) potential impact on data subjects; (d) whether data subjects would have reasonable expectations; (e) mitigating measures (e.g., pseudonymisation, limited access).

---

#### D. Legal Obligation — Art. 7, II (For Regulatory Compliance)

**"When the processing is necessary for the controller to comply with a legal obligation or regulatory duty"**

**May apply to:**
- Retaining financial records as required by Brazilian law (e.g., Lei 9.613/1998 for anti-money laundering, if applicable)
- Responding to court orders or regulatory demands
- Tax and fiscal record-keeping obligations applicable to your Brazilian customers or Brazilian operations

**Note:** This basis applies where a specific Brazilian (or applicable) legal norm mandates the processing. It cannot be used as a general justification for convenience.

---

#### E. Credit Protection — Art. 7, X (Specific to Credit-Related Processing)

**"For credit protection purposes, including credit analysis"**

If your SaaS business model involves any form of credit analysis (e.g., payment terms, instalment options, assessing creditworthiness of Brazilian customers), this specific basis exists in LGPD and is not present in GDPR. CPF is commonly used in Brazilian credit analysis contexts, and this basis would be relevant where you perform credit checks using CPF.

---

### 3.2 Legal Basis Mapping Table

| Processing Activity | Recommended Primary Basis | Alternative Basis | Notes |
|--------------------|--------------------------|--------------------|-------|
| Account creation (name, email) | Art. 7, V — Contract | Art. 7, I — Consent | Contract is stronger; necessary for service |
| CPF collection for invoicing | Art. 7, V — Contract | Art. 7, II — Legal obligation | Required for Brazilian fiscal compliance |
| CPF collection for identity verification | Art. 7, V — Contract | Art. 7, IX — Legitimate interest | Document balancing test for LI |
| Purchase history — service delivery | Art. 7, V — Contract | — | Core to contractual performance |
| Purchase history — personalisation | Art. 7, I — Consent | Art. 7, IX — LI (with LIA) | Consent preferred for direct personalisation |
| Marketing emails | Art. 7, I — Consent | — | Consent mandatory; must be freely given |
| Fraud detection / security | Art. 7, IX — Legitimate interest | Art. 7, II — Legal obligation | Document LI balancing test |
| Analytics / product improvement | Art. 7, IX — Legitimate interest | Art. 7, I — Consent | Pseudonymise data where possible |
| Sharing with processors (cloud, payments) | Same basis as underlying activity | — | Governed by Art. 39 processor agreements |
| Retention for legal/fiscal purposes | Art. 7, II — Legal obligation | — | Specify applicable Brazilian/US law |

---

## 4. Key Compliance Obligations That Follow

Now that LGPD applies, the following obligations arise for your company as a controller:

### 4.1 Transparency — Privacy Notice (Art. 9)

You must provide Brazilian customers with a privacy notice containing:
- Identity and contact of your company (controller)
- DPO (Encarregado) identity and contact
- Purposes of processing for each data category
- Legal basis for each processing activity
- Whether data will be shared and with whom
- Information about international transfers (data leaving Brazil to US servers)
- Retention periods
- How data subjects may exercise their rights
- Any automated decisions or profiling

### 4.2 DPO Appointment — Art. 41

You must appoint a **Encarregado (DPO)**. Unlike GDPR (which has SME exemptions), LGPD's Art. 41 broadly requires all controllers to appoint a DPO. The DPO's identity and contact must be published (typically on your website's privacy policy). ANPD Resolution No. 2/2022 provides limited exemptions for micro/small enterprises with low-risk processing — your company's profile (CPF, purchase history at scale) is unlikely to qualify.

The DPO may be an employee, a contracted individual, or a service provider (external DPO). They do not need to be Brazilian or located in Brazil, but must be reachable by Brazilian data subjects and ANPD.

### 4.3 Records of Processing Activities — Art. 37

Maintain a **RoPA** documenting each processing activity, including the data elements, legal basis, retention period, third-party recipients, and international transfer mechanism. ANPD Resolution No. 2/2022 makes RoPA mandatory where processing occurs at large scale, involves sensitive data, or involves a public authority — if your Brazilian customer base is material in size, this applies.

### 4.4 International Data Transfers — Arts. 33–36

You are transferring Brazilian customers' personal data to the United States (and potentially to third-party cloud infrastructure in other countries). This constitutes an **international data transfer** under LGPD Art. 5, XV, and requires a compliant transfer mechanism under Art. 33:

| Available Mechanism | Current Status |
|--------------------|----------------|
| **ANPD adequacy decision** | No adequacy decision for the US has been issued by ANPD as of 2026 |
| **Standard contractual clauses** | ANPD has not yet published standard clauses; specific contractual clauses between parties may be used (Art. 33, II) |
| **Binding Corporate Rules (BCRs)** | Available for multinational groups (Art. 33, III) |
| **Specific consent** | Data subject consented specifically to the international transfer — must be informed of destination country and risks (Art. 33, VIII) |
| **ANPD authorisation** | Case-by-case; not scalable (Art. 33, IX) |

**Recommended approach:** Use specific contractual clauses in your Terms of Service or a dedicated data processing agreement that provides LGPD-equivalent protections, and disclose the international transfer in your privacy notice. Alternatively, obtain specific informed consent for the transfer to the US.

### 4.5 Data Subject Rights — Arts. 17–22

Brazilian customers have the following rights, which you must operationally support:

| Right | Article | Timeframe |
|-------|---------|-----------|
| Confirmation of processing | Art. 18, I | Immediately (simplified) |
| Access to their data | Art. 18, II | Within 15 days (full report) |
| Correction of inaccurate/outdated data | Art. 18, III | Without undue delay |
| Anonymisation, blocking, or deletion | Art. 18, IV | Without undue delay |
| Portability | Art. 18, V | ANPD to define format |
| Deletion of consent-based data | Art. 18, VI | Without undue delay |
| Information about data sharing | Art. 18, VII | Without undue delay |
| Information about consequences of not consenting | Art. 18, VIII | Without undue delay |
| Revocation of consent | Art. 18, IX | Without undue delay |
| Review of automated decisions | Art. 20 | Upon request |

You must provide a mechanism for Brazilian customers to submit these requests (e.g., a dedicated privacy request form or email) in accessible language.

### 4.6 Security — Art. 46

Implement technical and administrative security measures proportionate to the risk of your processing. Given that you hold CPF numbers (which carry high fraud risk in Brazil) and purchase history, your measures should include:

- Encryption of CPF data at rest and in transit
- Access controls limiting internal access to CPF data on a need-to-know basis
- Pseudonymisation of CPF where full identification is not required for the processing step
- Logging and monitoring of access to CPF and financial data
- Documented information security policies
- Employee training on data handling

### 4.7 Breach Notification — Art. 48 + ANPD Resolution No. 15/2024

If a security incident occurs that may cause relevant risk or harm to your Brazilian customers (e.g., exposure of CPF or purchase history):
- **Preliminary notification to ANPD: within 3 working days** of becoming aware
- **Full report to ANPD: within 20 working days**
- **Notification to affected data subjects** where there is high risk of harm

Portal: https://www.gov.br/anpd/pt-br/assuntos/incidentes-de-seguranca

---

## 5. Penalty Exposure If Non-Compliant

Under **Art. 52**, ANPD may impose:

| Sanction | Details |
|----------|---------|
| Warning | With remediation period |
| Fine | Up to **2% of Brazilian revenue** (prior fiscal year); capped at **R$50 million per violation** |
| Daily fine | To compel compliance; same cap |
| Public disclosure | Of the violation |
| Data blocking | Temporary suspension of processing |
| Data deletion | Of data related to the violation |
| Processing suspension | Up to 6 months (renewable) |
| Processing prohibition | Total ban on processing activities |

ANPD enforcement priorities specifically include: large-scale data breaches, failure to respond to data subject rights requests, and lack of DPO appointment — all areas directly relevant to a SaaS company with a large Brazilian customer base.

---

## 6. Immediate Action Priorities

Based on your described data processing, here are the highest-priority actions:

| Priority | Action | LGPD Reference |
|----------|--------|----------------|
| 1 — Critical | Appoint and publish a DPO (Encarregado) | Art. 41 |
| 2 — Critical | Draft and publish a LGPD-compliant privacy notice covering all four data types | Art. 9 |
| 3 — Critical | Implement a mechanism for Brazilian data subjects to exercise their rights | Art. 18 |
| 4 — Critical | Establish an international data transfer mechanism for US-hosted data | Arts. 33–36 |
| 5 — High | Map legal basis to each processing activity and document in RoPA | Arts. 7, 37 |
| 6 — High | Implement consent mechanism for non-essential processing (marketing, analytics) | Art. 8 |
| 7 — High | Implement enhanced security for CPF data | Art. 46 |
| 8 — High | Establish breach detection and ANPD notification procedure (3-day requirement) | Art. 48 |
| 9 — Medium | Conduct Data Protection Impact Assessment (RIPD) for CPF processing | Art. 38 |
| 10 — Medium | Review processor agreements with sub-contractors to include LGPD terms | Art. 39 |

---

## 7. LGPD vs. GDPR: Relevance If You Are Already GDPR-Compliant

If your company already complies with GDPR for EU customers, much of your framework will transfer — but note these key differences:

| Topic | LGPD | GDPR |
|-------|------|------|
| Legal bases | 10 bases (includes credit protection — Art. 7, X) | 6 bases |
| DPO requirement | Required for all controllers (limited exemptions) | Only in specific cases (large-scale, systematic, sensitive) |
| Breach notification | 3 working days preliminary; 20 working days full | 72 hours to supervisory authority |
| Fines | Up to 2% of Brazilian revenue; max R$50M per violation | Up to 4% global turnover; max €20M |
| Children | Parental consent required (Art. 14); controller must verify | Parental consent for under-16 (member states may lower to 13) |
| Adequacy for US | No adequacy decision; ANPD has not listed US as adequate | No adequacy for US under Schrems II; EU-US Data Privacy Framework exists |
| Sensitive data | Cannot use legitimate interest for sensitive data (Art. 11) | Cannot use legitimate interest for special categories (Art. 9 GDPR) |

The main gaps GDPR-compliant companies typically face under LGPD are: the DPO publication requirement (name and contact must be publicly listed), the 3-working-day breach preliminary notification (shorter than 72 hours in calendar terms for working days, but operationally similar), and the lack of an established international transfer mechanism for Brazil-to-US flows equivalent to the EU-US DPF.

---

## 8. Summary

**Yes, LGPD applies to your US SaaS company** by virtue of Art. 3, II — you are offering services to individuals located in Brazil and processing their personal data. There is no exemption that shields you.

**The legal bases most applicable to your processing are:**

1. **Art. 7, V — Contract**: Primary basis for collecting name, email, CPF (for billing/invoicing), and generating/retaining purchase history as part of your SaaS service delivery.
2. **Art. 7, I — Consent**: Required for marketing communications, sharing data with third-party marketing tools, and non-essential analytics or personalisation based on purchase history.
3. **Art. 7, IX — Legitimate Interest**: May apply to fraud prevention, security monitoring, and internal product analytics — but requires a documented balancing test (LIA) for each activity.
4. **Art. 7, II — Legal Obligation**: Where Brazilian law mandates retention of specific records (e.g., fiscal, tax).
5. **Art. 7, X — Credit Protection**: If any credit assessment of customers is performed using CPF.

Your most urgent compliance steps are: appointing a DPO, publishing an LGPD-compliant privacy notice, establishing a data subject rights request process, implementing an international data transfer mechanism, and creating a breach notification procedure meeting ANPD's 3-working-day preliminary notification requirement.

---

*References: LGPD — Law No. 13,709/2018 as amended by Law No. 13,853/2019; ANPD Resolution CD/ANPD No. 2/2022 (RoPA/DPO); ANPD Resolution CD/ANPD No. 15/2024 (breach notification); ANPD Resolution CD/ANPD No. 4/2023 (micro/small enterprise simplified rules).*
