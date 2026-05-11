# LGPD Compliance for US SaaS Companies Serving Brazilian Customers

## Question
"We are a US SaaS company with Brazilian customers. Our app collects name, email, CPF, and purchase history. Do we need to comply with LGPD? Which legal bases apply to our processing?"

---

## 1. Applicability: Does LGPD Apply to Your Company?

**Yes, LGPD applies to your company, even though you are based in the United States.**

Brazil's Lei Geral de Proteção de Dados (LGPD), Law No. 13,709/2018, has explicit extraterritorial reach. Article 3 establishes that the law applies to any data processing operation, regardless of where the controller or processor is headquartered, if **any one** of the following conditions is met:

1. **The processing operation is carried out in Brazil** — even if the company is foreign, if data is collected, stored, or processed on Brazilian territory (e.g., servers, agents, or data flows touching Brazil), LGPD applies.
2. **The processing activity aims to offer goods or services to individuals located in Brazil** — this is your primary trigger. Your SaaS platform targets Brazilian customers and offers them a service, which squarely brings you within LGPD's scope.
3. **The personal data being processed was collected in Brazil** — when Brazilian residents submit their name, email, CPF, or purchase history through your platform while located in Brazil, that data was collected in Brazil.

Your company meets **all three** of these conditions. Your US location is irrelevant for purposes of applicability. This mirrors the territorial logic of the EU's GDPR (Article 3), and Brazilian regulators (ANPD — Autoridade Nacional de Proteção de Dados) are expected to interpret the extraterritorial scope similarly.

### The Data You Collect

| Data Element     | Classification Under LGPD | Notes |
|------------------|---------------------------|-------|
| Name             | Personal data (Art. 5, I) | Identifies a natural person |
| Email address    | Personal data (Art. 5, I) | Identifies or can identify a person |
| CPF (Cadastro de Pessoas Físicas) | Personal data (Art. 5, I) | National taxpayer ID — highly sensitive identifier |
| Purchase history | Personal data (Art. 5, I) | Behavioral/transactional data linked to identifiable individuals |

None of these categories constitute **sensitive personal data** (dados pessoais sensíveis) under Article 5, II (which covers racial/ethnic origin, religion, political opinion, health, biometric, genetic, and sexual orientation data). However, the **CPF is a highly regulated identifier** in Brazil — its collection and use requires particular care, a clear legal basis, and robust security controls, as it is commonly used for identity verification and fraud.

---

## 2. Legal Bases for Processing

LGPD Article 7 enumerates **ten legal bases** for processing personal data of private individuals. Unlike GDPR's six legal bases, LGPD's list is broader. The most relevant bases for your scenario are:

### 2.1 Contract Performance (Art. 7, V)
**"Processing necessary for the performance of a contract or preliminary procedures related to a contract of which the data subject is a party, at the request of the data subject."**

- **Applies to:** Name, email, CPF (for account creation, billing, and identity verification), and purchase history.
- **Rationale:** When a Brazilian customer signs up for your SaaS, a contractual relationship is formed. Processing their name and email to create an account, deliver the service, send invoices, and record purchases is necessary to fulfill that contract. This is likely your **primary legal basis** for core service delivery.
- **Practical scope:** Account registration, authentication, service delivery, billing, and transaction records.

### 2.2 Legitimate Interest of the Controller (Art. 7, IX)
**"Processing based on the legitimate interests of the controller or a third party, except in the case where the data subject's fundamental rights and freedoms prevail."**

- **Applies to:** Purchase history (for fraud prevention, analytics, service improvement), email (for security notifications, product updates).
- **Rationale:** You have a legitimate interest in maintaining service integrity, preventing fraud, improving your product, and communicating relevant updates to customers. However, this basis requires a **three-part balancing test** (similar to GDPR): (a) the interest must be legitimate, (b) the processing must be necessary, and (c) the interest must not be overridden by the data subject's rights.
- **Limitation:** Legitimate interest cannot be used as a catch-all. It must be documented and balanced against data subject rights. ANPD is expected to scrutinize this basis carefully.

### 2.3 Consent (Art. 7, I)
**"Provision of free, informed, and unambiguous consent by the data subject."**

- **Applies to:** Marketing emails, behavioral analytics, sharing data with third-party advertising partners, profiling.
- **Rationale:** For processing that goes beyond direct service delivery — particularly marketing communications, behavioral tracking, or sharing data with partners — consent is the appropriate (and often required) legal basis.
- **LGPD consent requirements (Art. 8):**
  - Must be given for a specific purpose
  - Must be free, informed, unambiguous, and given in written or equivalent form
  - The burden of proof of consent lies with the controller
  - Consent may be revoked at any time (the controller must make revocation as easy as giving consent)
  - Bundled consent (consent buried in terms of service) is not valid
- **Important:** Consent is **not required** for contract performance or legitimate interest processing. Do not over-rely on consent — if another valid basis applies, use it, as over-reliance on consent creates risk when users withdraw it.

### 2.4 Compliance with a Legal Obligation (Art. 7, II)
**"Processing necessary for compliance with a legal obligation or regulatory requirement by the controller."**

- **Applies to:** Retention of transaction records and CPF data for Brazilian tax/accounting regulations, responding to lawful government requests.
- **Rationale:** Brazilian law (and potentially US law on cross-border transactions) may require you to retain certain records. If Brazilian fiscal or consumer protection regulations require you to keep invoices or purchase records with CPF for a defined period, this basis justifies that retention even after the contractual relationship ends.

### 2.5 Regular Exercise of Rights in Judicial, Administrative, or Arbitration Proceedings (Art. 7, VI)
**"Processing necessary for the regular exercise of rights in judicial, administrative, or arbitration proceedings."**

- **Applies to:** Retaining data to defend against chargebacks, legal disputes, or regulatory investigations.
- **Rationale:** If a customer disputes a transaction or files a complaint, you may need to retain their data beyond what the contract alone would justify. This basis covers that scenario.

---

## 3. Key Obligations Under LGPD

### 3.1 Appoint a Data Protection Officer (DPO) — Encarregado
Article 41 requires controllers to appoint a **Data Protection Officer (Encarregado)**. The ANPD has clarified that this requirement applies broadly, including to foreign companies processing Brazilian data. The DPO must:
- Be publicly identified (name and contact information disclosed in your privacy notice)
- Serve as the point of contact between your company, data subjects, and the ANPD
- Can be an individual or a legal entity (e.g., an outsourced DPO service)

### 3.2 Privacy Notice / Privacy Policy
You must provide data subjects with **clear, transparent information** (Art. 9) including:
- Identity and contact details of the controller (and your DPO/Encarregado)
- Purposes of processing for each data category
- Legal basis for each processing activity
- Data retention periods
- Whether data is shared with third parties and with whom
- Whether data is transferred internationally and the safeguards in place
- Data subject rights and how to exercise them

### 3.3 International Data Transfers
Since your company is US-based, **every time you process Brazilian customer data**, you are engaging in an international data transfer. Article 33 of LGPD restricts international transfers. Permissible mechanisms include:
- Transfer to countries that provide an adequate level of protection (as determined by ANPD — no US adequacy decision exists yet)
- Standard contractual clauses approved by ANPD
- Global corporate policies (binding corporate rules equivalent)
- Specific consent of the data subject for the transfer
- Necessity for contract performance (where the transfer is necessary to execute the contract with the data subject)

**Critical action item:** You must implement an international data transfer mechanism. The most practical option for a US SaaS company is likely **standard contractual clauses** (once ANPD finalizes them) or **reliance on contract necessity** for core service data flows.

### 3.4 Data Subject Rights
LGPD grants Brazilian data subjects the following rights (Arts. 17–22), which you must be able to fulfill:

| Right | Description |
|-------|-------------|
| Confirmation | Confirm whether processing of their data exists |
| Access | Receive a copy of their personal data |
| Correction | Correct inaccurate or outdated data |
| Anonymization/Blocking/Deletion | Request anonymization, blocking, or deletion of unnecessary or unlawful data |
| Data portability | Receive their data in a structured, interoperable format |
| Deletion of consent-based data | Delete data processed on the basis of consent, upon revocation |
| Information about sharing | Know which third parties their data has been shared with |
| Opt-out of consent/legitimate interest | Refuse or revoke consent; oppose processing based on legitimate interest |
| Review of automated decisions | Request human review of automated decisions affecting them |

You must have a **mechanism** (e.g., a web form, email process) for Brazilian users to submit these requests and a process to respond within a **reasonable timeframe** (ANPD guidance suggests 15 days as a benchmark, though the law does not specify an exact period).

### 3.5 Data Minimization and Purpose Limitation
Article 6 establishes principles including:
- **Finality (Finalidade):** Data may only be processed for stated, legitimate, specific, and explicit purposes.
- **Necessity (Necessidade):** Only collect data that is strictly necessary for the stated purpose. For example, if CPF is not technically required for your SaaS to function, carefully consider whether its collection is justified.
- **Adequacy (Adequação):** Processing must be compatible with the stated purposes.

### 3.6 Security Measures
Article 46 requires controllers to adopt **technical and administrative security measures** to protect personal data from unauthorized access, destruction, loss, alteration, or leakage. You should:
- Encrypt data at rest and in transit
- Implement access controls and least-privilege principles
- Conduct regular security assessments
- Maintain an incident response plan

### 3.7 Data Breach Notification
Article 48 requires notification of security incidents that may cause **risk or harm** to data subjects. Notification must be made to:
- The **ANPD** — within a reasonable timeframe (ANPD guidelines indicate 2 business days for initial notification of serious incidents)
- **Affected data subjects** — when the incident poses risk to them

The notification must include: nature of the data affected, data subjects involved, security measures in place, risks of the incident, and measures taken to remedy it.

### 3.8 Data Processing Records
While LGPD does not have an explicit equivalent to GDPR's Article 30 Records of Processing Activities (RoPA), the **accountability principle** (Art. 6, X) and ANPD's regulatory guidance effectively require controllers to maintain internal documentation of their processing activities. Maintain records of:
- What data you collect
- Why you collect it (legal basis and purpose)
- How long you retain it
- Who you share it with
- Security measures in place

### 3.9 Privacy by Design and by Default
Article 46, paragraph 2 and the general accountability principle require you to implement data protection from the outset of system design, not as an afterthought.

---

## 4. Recommended Compliance Roadmap

Given your situation as a US SaaS company with Brazilian customers, prioritize the following:

1. **Immediate:** Conduct a data mapping exercise — document all data collected (name, email, CPF, purchase history), where it is stored, how it flows, who has access, and with whom it is shared.

2. **Short-term (1–3 months):**
   - Update your privacy policy to meet LGPD transparency requirements (Art. 9), including identifying your Encarregado/DPO contact.
   - Appoint or designate a Data Protection Officer/Encarregado.
   - Implement a data subject rights request mechanism.
   - Review your consent flows — ensure marketing consent is granular, informed, and revocable.
   - Establish an international data transfer mechanism (evaluate contract necessity basis and monitor ANPD's standard contractual clauses).

3. **Medium-term (3–6 months):**
   - Conduct a legitimate interest assessment (LIA) for processing based on Art. 7, IX.
   - Implement or verify technical security controls (encryption, access controls, logging).
   - Develop a data breach response plan with ANPD notification procedures.
   - Review data retention schedules and implement deletion/anonymization workflows.

4. **Ongoing:**
   - Monitor ANPD regulatory guidance and resolutions (the ANPD continues to publish implementing regulations).
   - Conduct periodic privacy reviews as your product evolves.
   - Train staff on LGPD obligations.

---

## 5. Summary Table

| Topic | Answer |
|-------|--------|
| Does LGPD apply? | **Yes** — extraterritorial application under Art. 3 applies because you offer services to people in Brazil and collect data in Brazil |
| Primary legal basis | **Contract performance** (Art. 7, V) for core service data |
| Secondary legal bases | **Legitimate interest** (Art. 7, IX) for analytics/security; **Consent** (Art. 7, I) for marketing; **Legal obligation** (Art. 7, II) for mandatory retention |
| CPF sensitivity | Not "sensitive data" under LGPD but a highly regulated identifier requiring strong justification and security |
| DPO required? | **Yes** — Encarregado must be appointed and publicly identified |
| International transfers | **Must be addressed** — no US adequacy decision; use contractual mechanisms |
| Key risks of non-compliance | Fines up to 2% of Brazilian-market revenue (capped at R$50 million per violation), public warnings, data processing bans, reputational harm |

---

*This response reflects LGPD as enacted (Law No. 13,709/2018, as amended by Law No. 13,853/2019) and ANPD regulatory guidance as of the knowledge cutoff date. ANPD continues to issue implementing resolutions; consult current ANPD publications and qualified Brazilian legal counsel for jurisdiction-specific advice.*
