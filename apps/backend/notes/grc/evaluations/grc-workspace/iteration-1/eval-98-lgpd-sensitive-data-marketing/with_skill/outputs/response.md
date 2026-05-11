# LGPD Analysis: Processing Health Insurance Data and Credit Scores for Marketing Personalised Financial Products

**Prepared for:** Fintech Startup, São Paulo, Brazil  
**Date:** 5 May 2026  
**Governing Law:** Lei Geral de Proteção de Dados Pessoais (LGPD) — Law No. 13,709/2018, as amended by Law No. 13,853/2019

---

## Executive Summary

Your proposed use case — processing customers' health insurance data and credit scores to market personalised financial products — triggers the most restrictive provisions of the LGPD. Health data is explicitly classified as **sensitive personal data** under Art. 5, X, and the legal bases available to justify its use for marketing are severely limited. Credit score data, while potentially regular personal data, also carries heightened obligations when used for profiling and automated marketing decisions. This analysis concludes that:

1. **Using health insurance data for marketing is legally precarious under LGPD** and cannot be justified by most standard legal bases — only express, specific consent (Art. 11, I) could potentially support it, subject to strict conditions.
2. **Using credit scores for marketing** may be possible under consent (Art. 7, I) or — with caution — legitimate interest (Art. 7, IX), subject to a documented balancing test.
3. **Multiple overlapping safeguards** are mandatory, including a Data Protection Impact Assessment (RIPD), DPO appointment, and robust consent management infrastructure.

---

## 1. Classification of the Data: Sensitive vs. Regular Personal Data

### 1.1 Health Insurance Data — Sensitive Personal Data (Art. 5, II; Art. 11)

Under Art. 5, II of the LGPD, **sensitive personal data** includes data concerning a person's **health** ("dados sobre a saúde"). Health insurance data falls squarely within this definition because it reveals details about a person's health status, medical history, conditions, treatments, or insured risks.

This classification is not merely definitional — it triggers an entirely separate and stricter legal regime under **Art. 11**, which exhaustively lists the bases on which sensitive data may be processed. The bases available under Art. 7 (regular data) do **not** automatically apply to sensitive data.

### 1.2 Credit Scores — Regular Personal Data (Art. 5, I; Art. 7)

Credit scores are personal data under Art. 5, I, as they relate to an identified or identifiable natural person and reflect their financial behaviour. Unless the credit score is derived from or combined with health data in a way that reveals health information, it is treated as **regular personal data** governed by Art. 7.

However, when credit scores are used for **profiling and automated marketing decisions**, additional obligations arise under **Art. 20** (right to review automated decisions).

---

## 2. Legal Bases for Processing

### 2.1 Health Insurance Data for Marketing (Art. 11 Analysis)

Art. 11 lists the only permissible bases for processing sensitive personal data. We assess each in the context of marketing:

| Art. 11 Basis | Applicable to Marketing? | Assessment |
|---|---|---|
| **Art. 11, I — Express and specific consent** | YES — with strict conditions | The only viable basis; see Section 3 below |
| **Art. 11, II, a — Legal obligation** | NO | Marketing is not required by any Brazilian law |
| **Art. 11, II, b — Public policy by public entities** | NO | Your startup is a private entity |
| **Art. 11, II, c — Research (anonymised)** | NO | Research results must be anonymised; cannot be used to target individuals |
| **Art. 11, II, d — Exercise of rights in proceedings** | NO | Not applicable to marketing |
| **Art. 11, II, e — Protection of life or physical safety** | NO | Marketing does not protect life |
| **Art. 11, II, f — Health care by professionals/entities** | NO | Marketing of financial products is not health care |
| **Art. 11, II, g — Prevention of fraud / security of data subject** | NO | Marketing is not fraud prevention |
| **Art. 11, II, h — Protection of credit** | NARROW | Art. 7, X credit protection basis does not extend to sensitive data; Art. 11 does not replicate this exception for sensitive data |

**Critical observation:** Art. 10 and Art. 11 expressly prohibit the use of **legitimate interest** as a basis for processing sensitive personal data. Unlike regular data, there is no balancing test available to justify health data processing for commercial marketing.

**Conclusion for health data:** The only plausible legal basis is **express and specific consent under Art. 11, I**. Any other approach would constitute unlawful processing.

### 2.2 Credit Score Data for Marketing (Art. 7 Analysis)

Credit scores benefit from more flexible legal bases under Art. 7:

| Art. 7 Basis | Applicable to Marketing? | Assessment |
|---|---|---|
| **Art. 7, I — Consent** | YES | Valid if freely given, specific, informed, and unambiguous (Art. 8) |
| **Art. 7, IX — Legitimate interest** | POSSIBLE | Requires documented balancing test; controller's marketing interest must not outweigh data subject's fundamental rights |
| **Art. 7, X — Credit protection** | NARROW | Permits credit analysis; does not extend broadly to marketing personalisation beyond credit risk assessment |
| **Art. 7, V — Contract** | LIMITED | Only if the marketing is strictly necessary for a pre-existing or contemplated contract with the data subject |

**Recommended basis for credit scores:** Consent (Art. 7, I) is the safest and most defensible basis for using credit scores for personalised marketing. Legitimate interest (Art. 7, IX) may also be available but requires a formal balancing test and carries greater regulatory risk — particularly given ANPD's stated enforcement focus on sensitive and financial data processing (see Section 6).

---

## 3. Consent Requirements in Detail

Since **express and specific consent** (Art. 11, I read with Art. 8) is the only viable basis for using health insurance data in marketing, the requirements for valid consent are critical:

### 3.1 Requirements for Valid Sensitive Data Consent (Art. 8; Art. 11, I)

| Requirement | LGPD Provision | Practical Implication |
|---|---|---|
| **Express** | Art. 11, I | Cannot be implied; must be an affirmative, positive action by the data subject |
| **Specific to purpose** | Art. 8, §4º | Consent for health data use in marketing must be separate from other consents; bundled consent is invalid |
| **Highlighted/differentiated** | Art. 11, I | Must be presented separately and prominently from consents for non-sensitive data processing |
| **Informed** | Art. 8 | Data subject must understand what health data will be used, for what marketing purposes, and by whom |
| **Free** | Art. 8, §3º | Cannot condition access to financial products or services on providing this consent (unless strictly necessary) |
| **Documented** | Art. 8, §2º | Burden of proof on controller to demonstrate valid consent was obtained |
| **Revocable at any time** | Art. 8, §5º | Data subject must be able to withdraw consent freely, at no cost, without prejudice |
| **No pre-ticked boxes** | Art. 8 | Consent must be an affirmative act; passive acceptance is invalid |

### 3.2 Consequence of Consent Withdrawal

Upon revocation of consent (Art. 18, IX), the controller must:
- Cease processing for the marketing purpose without undue delay
- Delete data processed solely on that consent basis (Art. 18, VI), unless another lawful basis applies (e.g., legal obligation, credit protection for the original credit assessment purpose)

---

## 4. Key Safeguards Required

### 4.1 Data Protection Impact Assessment — RIPD (Art. 38)

Processing health data (sensitive) at scale for marketing constitutes **high-risk processing** that will trigger the obligation to conduct a Relatório de Impacto à Proteção de Dados Pessoais (RIPD/DPIA). This is particularly acute because your processing involves:

- Sensitive personal data (health insurance data)
- Profiling and automated marketing decisions (credit scores)
- Large-scale financial customer data

The RIPD must be completed **before** the processing begins and must address:
- Description of the processing activity and legal basis
- Necessity and proportionality assessment (Art. 38; Art. 6, III)
- Identification of risks to data subjects (e.g., discrimination, financial harm, health data exposure)
- Mitigation measures for each identified risk
- DPO opinion on whether to proceed

ANPD may require disclosure of the RIPD upon request (Art. 38, §§1º–2º). Given ANPD's enforcement priorities (Section 6), a well-documented RIPD is a critical defence.

### 4.2 DPO Appointment (Art. 41)

As a fintech processing sensitive data (health) and credit data at scale, you are required to appoint a **Encarregado (DPO)**. The DPO's identity and contact must be **publicly published** (Art. 41, §1º) — typically on your website's privacy policy page and on any forms collecting data.

ANPD Resolution No. 2/2022 provides a limited DPO exemption for micro and small enterprises with low-risk processing, but processing sensitive data for marketing disqualifies you from this exemption.

### 4.3 Records of Processing Activities — RoPA (Art. 37; ANPD Resolution No. 2/2022)

You must maintain a RoPA for the marketing processing activities, documenting at minimum:
- Processing activity name and purpose
- Legal basis (Art. 11, I — express consent for health data; Art. 7 basis for credit data)
- Categories of sensitive and personal data
- Data subject categories (customers)
- Recipients and sharing (marketing technology vendors, analytics platforms)
- International transfers (if data leaves Brazil — e.g., cloud providers)
- Retention periods
- Security measures
- Reference to the RIPD

### 4.4 Non-Discrimination Principle (Art. 6, IX)

The LGPD expressly prohibits processing that enables **unlawful discriminatory** outcomes. Using health insurance data to target or exclude certain customers from financial products (e.g., denying credit or insurance based on health risk proxies) would violate this principle and potentially also Brazilian Consumer Protection Code obligations (Art. 45 LGPD). Your marketing logic and segmentation models must be audited for discriminatory outcomes.

### 4.5 Automated Decision Review Rights (Art. 20)

If credit scores and health data are combined in algorithms that automatically determine which marketing offers are presented to which customers, data subjects have the right to:
- Request review of decisions made solely by automated processing
- Request human review of the automated decision
- Be informed of the criteria and procedures used

You must implement a mechanism to provide meaningful human review and must document the logic of your personalisation algorithms.

### 4.6 Transparency and Privacy Notice (Art. 9)

Your privacy notice must include, specifically for this processing:
- The precise purpose of using health and credit data for marketing
- The legal basis for each data type (Art. 11, I for health; chosen Art. 7 basis for credit)
- The types of financial products marketed
- Whether data will be shared with third-party marketing platforms or data brokers
- Any international transfers of this data
- Retention periods for marketing profiles
- How to exercise rights, including consent withdrawal

### 4.7 Security Measures (Art. 46)

Given the sensitivity of health insurance data, technical and administrative security measures must be proportionate to the risk. Minimum recommended measures include:
- Encryption of health data at rest and in transit
- Strict access controls (need-to-know basis)
- Pseudonymisation where feasible for marketing analytics
- Regular security testing and vulnerability assessments
- Incident detection and response capability (Art. 48; 3 working-day preliminary ANPD notification per ANPD Resolution CD/ANPD No. 15/2024)

### 4.8 Data Minimisation and Purpose Limitation (Art. 6, I and III)

A critical compliance risk in your use case is the **secondary use** of health data originally collected for insurance purposes and now being repurposed for marketing. The LGPD's purpose limitation principle (Art. 6, I) and adequacy principle (Art. 6, II) require that:

- Health data collected for insurance administration purposes is not automatically available for marketing
- Repurposing health data for marketing requires a **new and specific legal basis** — i.e., fresh express consent (Art. 11, I) specifically for the marketing purpose
- The collection must be limited to the minimum necessary for the marketing purpose (Art. 6, III)

If your health data was originally collected under a different legal basis (e.g., contract for health insurance administration), you **cannot** rely on that original basis to now use the data for marketing. A fresh consent must be obtained.

---

## 5. Summary Legal Basis Table

| Data Type | Proposed Use | Permissible Legal Basis | Key Conditions |
|---|---|---|---|
| **Health insurance data** | Personalised marketing | **Art. 11, I — Express specific consent only** | Separate, highlighted, affirmative consent; purpose-specific; revocable; documented |
| **Health insurance data** | Fraud prevention in credit offer | Art. 11, II, g (narrow) | Only if genuinely for fraud/security prevention, not marketing |
| **Credit scores** | Personalised marketing | **Art. 7, I — Consent** (preferred) | Informed, unambiguous, specific, revocable |
| **Credit scores** | Personalised marketing | Art. 7, IX — Legitimate interest (possible) | Formal balancing test required; document that marketing interest does not override data subject rights; higher regulatory risk |
| **Credit scores** | Credit risk assessment | Art. 7, X — Credit protection | Limited to credit assessment; not a basis for general marketing |

---

## 6. Regulatory Risk Assessment

### ANPD Enforcement Priorities

ANPD has explicitly identified **sensitive data processing without legal basis** — particularly health and financial data — as a priority enforcement area. Relevant enforcement precedents include:

- **Health Sector Data Sharing (2022–2023):** ANPD investigated sharing of health data with third parties without express consent and issued compliance orders.
- **Telemarketing Sector (2023):** Multiple companies received warnings and remediation orders for processing personal data for marketing without adequate legal basis.

Given ANPD's joint oversight with:
- **Banco Central do Brasil** (financial data oversight)
- **ANS/ANVISA** (health sector data)
- **SENACON** (consumer protection)

a fintech startup using health insurance data for marketing could face **multi-regulator scrutiny**, not solely ANPD enforcement.

### Penalty Exposure (Art. 52)

| Scenario | Potential Sanctions |
|---|---|
| Using health data for marketing without valid consent | Simple fine up to 2% of Brazilian revenue or R$50,000,000 per violation; possible data blocking or deletion |
| Invalid/bundled consent for sensitive data | Warning + remediation order; escalation to fine on recurrence |
| Failure to conduct RIPD for high-risk processing | Warning; fine; suspension of processing |
| Unlawful discrimination via health data profiling | Fine + potential prohibition of processing |
| Failure to honour consent revocation | Daily fine to compel compliance |

---

## 7. Practical Recommendations

### Immediate Actions (Before Any Processing Begins)

1. **Do not use health insurance data for marketing without first obtaining fresh, express, specific, highlighted consent** from each data subject — separately from any consent for insurance services. Existing data collected for insurance administration cannot be repurposed without new consent.

2. **Conduct a RIPD** covering both the health data marketing processing and the credit score profiling processing before launching any campaign.

3. **Appoint a DPO (Encarregado)** and publish their contact details publicly.

4. **Create a RoPA entry** for each marketing processing activity, documenting legal bases, data categories, recipients, transfers, and security measures.

5. **Implement a consent management platform** capable of:
   - Recording granular, purpose-specific consents
   - Distinguishing sensitive data consents (health) with explicit highlighting
   - Enabling easy consent withdrawal
   - Maintaining timestamped consent audit logs

6. **Review your privacy notice** to add the specific elements required for marketing use of health and credit data (Art. 9).

### Structural Advice

- **Consider anonymisation/aggregation:** If health data can be used to create anonymised cohort-level marketing segments without targeting individuals based on their health status, this may reduce legal risk — but must be genuinely anonymised per Art. 12.

- **Separate the credit scoring use case from the health data use case:** They have different legal bases, different risk profiles, and should be managed separately in your consent infrastructure and RoPA.

- **Implement automated decision review processes** before launching credit-score-based personalisation, to honour Art. 20 rights.

- **Train sales, marketing, and product teams** on the restrictions on using health data for targeting — the prohibition on discriminatory processing (Art. 6, IX) must be embedded in campaign design.

---

## 8. Comparison with GDPR (for Reference)

| Topic | LGPD Position | GDPR Position |
|---|---|---|
| Health data legal bases for marketing | Art. 11, I — express specific consent only (no LI available) | Art. 9(2)(a) GDPR — explicit consent; LI also excluded for special category data |
| Credit data for marketing | Consent (Art. 7, I) or LI (Art. 7, IX) with balancing test | Consent (Art. 6(1)(a)) or LI (Art. 6(1)(f)) with balancing test |
| DPIA requirement | Art. 38 — ANPD may require for high-risk processing | Art. 35 GDPR — mandatory for high-risk processing |
| DPO | Art. 41 — always required for controllers (limited SME exemption per Resolution 2/2022) | Art. 37 GDPR — required only for specific cases (large-scale special category data processing qualifies) |
| Fines | Up to 2% Brazilian revenue; max R$50M per violation | Up to 4% global turnover; max €20M |

Both frameworks align on the fundamental principle that sensitive/special category data may not be used for commercial marketing without explicit, specific consent.

---

## 9. Conclusion

**Using health insurance data for personalised financial product marketing is legally permissible under LGPD only if you obtain express, specific, highlighted, and freely given consent from each data subject under Art. 11, I.** No other legal basis under Art. 11 supports this use case. Given the strict requirements for that consent and the high regulatory risk, you should carefully consider whether the marketing benefit justifies the compliance burden and operational risk.

**Using credit scores for personalised marketing** is more feasible, with consent (Art. 7, I) being the recommended legal basis, and legitimate interest (Art. 7, IX) potentially available subject to a documented balancing test.

Regardless of the legal bases chosen, the following safeguards are mandatory and must be implemented before any such processing begins:
- RIPD / DPIA (Art. 38)
- DPO appointment and publication (Art. 41)
- RoPA for each processing activity (Art. 37; Resolution No. 2/2022)
- Granular, auditable consent management (Art. 8; Art. 11, I)
- Updated and transparent privacy notice (Art. 9)
- Automated decision review mechanism (Art. 20)
- Non-discrimination review of marketing algorithms (Art. 6, IX)
- Proportionate security measures (Art. 46)

Engaging a qualified Brazilian data protection lawyer alongside your internal DPO is strongly recommended before operationalising this use case, given the intersection of financial sector regulation (Banco Central), health sector oversight (ANS/ANVISA), consumer protection (SENACON), and LGPD enforcement by ANPD.

---

*This analysis is based on LGPD (Law No. 13,709/2018 as amended), ANPD Resolutions CD/ANPD No. 2/2022 and CD/ANPD No. 15/2024, and ANPD published enforcement guidance as of May 2026. It does not constitute legal advice and should be reviewed by qualified Brazilian legal counsel.*
