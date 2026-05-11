# LGPD Compliance Analysis: Using Health Insurance Data and Credit Scores for Marketing Personalised Financial Products

## Executive Summary

Processing health insurance data and credit scores for marketing personalised financial products in Brazil raises significant LGPD compliance challenges. Health data is classified as **sensitive personal data** under LGPD and is subject to strict, narrowly defined legal bases. Credit scores, while not inherently sensitive, carry additional regulatory obligations. This use case — secondary use of both data types for marketing — is **highly restricted** and will require careful legal structuring, robust safeguards, and in some scenarios may not be permissible at all without explicit consent.

---

## 1. Applicable Law and Regulatory Framework

**Lei Geral de Proteção de Dados Pessoais (LGPD)** — Lei nº 13.709/2018, as amended by Lei nº 13.853/2019 — is the primary statute governing personal data processing in Brazil. The **Autoridade Nacional de Proteção de Dados (ANPD)** is the supervisory authority responsible for enforcement, interpretation, and guidance.

For credit data specifically, **Lei nº 12.414/2011** (Positive Credit Register Law) and complementary BACEN (Banco Central do Brasil) regulations also apply to fintech entities operating in the Brazilian financial sector.

---

## 2. Classification of the Data Types

### 2.1 Health Insurance Data — Sensitive Personal Data

Under **LGPD Art. 5, II**, health data is expressly listed as **sensitive personal data** (*dado pessoal sensível*). Health insurance data — which inherently reveals a person's health status, conditions covered, medical history, or insurance risk profile — falls squarely within this category.

Sensitive personal data is subject to **heightened legal requirements** under LGPD Art. 11, which provides a **closed and exhaustive list** of legal bases for processing. These are materially more restrictive than the general legal bases applicable to ordinary personal data.

### 2.2 Credit Scores — Ordinary Personal Data with Sector-Specific Rules

Credit scores are treated as **ordinary personal data** under LGPD (not sensitive), but they are subject to:
- LGPD's general legal bases (Art. 7)
- Lei nº 12.414/2011 requirements regarding permissible uses and access rights
- BACEN regulatory requirements for licensed fintech entities

The secondary use of credit scores for marketing purposes requires a valid LGPD legal basis and must be consistent with the purpose for which the data was originally collected.

---

## 3. Legal Bases Analysis

### 3.1 Legal Bases for Sensitive Data (Health Insurance Data) — LGPD Art. 11

LGPD Art. 11 provides the following exhaustive legal bases for sensitive data processing:

| Legal Basis | Applicability to Marketing Use Case |
|---|---|
| **Explicit consent** (Art. 11, I) | Potentially available, but subject to strict requirements (see below) |
| Compliance with legal or regulatory obligation | Not applicable to marketing |
| Shared processing by public bodies | Not applicable (private fintech) |
| Medical research, health studies (with anonymisation) | Not applicable to marketing |
| Exercise of rights in judicial, administrative, or arbitration proceedings | Not applicable |
| Health protection by health professionals or bodies | Not applicable |
| Fraud prevention and data subject security | Not applicable to marketing |
| Regulatory compliance for financial activities (Art. 11, II, g) | Narrow — generally limited to credit risk assessment, not marketing |

**Practical conclusion for health data:** The only realistically available legal basis for using health insurance data for marketing is **explicit consent (Art. 11, I)**. All other bases do not cover marketing activities.

### 3.2 Requirements for Explicit Consent — LGPD Art. 8 and Art. 11, I

For consent to be valid under LGPD for sensitive data, it must be:
- **Free** (*livre*): not conditioned on the provision of services or products; coerced or bundled consent is invalid
- **Informed** (*informado*): the data subject must be clearly told what data is being processed, for what purpose, and by whom
- **Unambiguous** (*inequívoco*): for ordinary data; for sensitive data, the standard is **explicit** (*destacado*)
- **Specific** (*para finalidades determinadas*): consent must be tied to a specific, clearly articulated purpose — in this case, marketing personalised financial products
- **Granular**: separate consent must be obtained for each distinct processing purpose; bundling consent for health data processing into general terms and conditions is not permitted
- **Revocable at any time** (Art. 8, §5): data subjects must be able to withdraw consent easily and without penalty

**Risk:** If customers originally consented to health data processing for insurance-related purposes (e.g., underwriting, claims), using that data for marketing financial products is a **new, secondary purpose** and requires **fresh, explicit consent** for that specific use. Repurposing previously collected health data without new consent would violate the **purpose limitation principle** (Art. 6, I).

### 3.3 Legal Bases for Credit Score Data — LGPD Art. 7

Credit scores may be processed under a broader set of legal bases, including:

- **Consent** (Art. 7, I)
- **Legitimate interests** (Art. 7, IX) — subject to the balancing test (controller's legitimate interest must not override the data subject's fundamental rights and freedoms)
- **Compliance with legal obligation** (Art. 7, II)
- **Execution of a contract** (Art. 7, V) — only where marketing is genuinely incidental to and necessary for contract performance

**Legitimate interests for marketing:** LGPD's legitimate interests basis (Art. 7, IX) is potentially available for credit score-based marketing but requires a documented **Legitimate Interest Assessment (LIA)** demonstrating that:
1. There is a genuine legitimate interest
2. The processing is necessary for that purpose
3. The data subject's interests, rights, and freedoms do not override the controller's interest

Regulators and privacy authorities across jurisdictions are increasingly skeptical of legitimate interests as a basis for direct marketing using financial data. Given that ANPD guidance is still developing, reliance on this basis carries legal risk without a robust and documented LIA.

---

## 4. Purpose Limitation and Data Minimisation

**LGPD Art. 6, I (purpose):** Personal data must be processed only for legitimate, specific, explicit, and informed purposes. Secondary use for marketing must be either:
- Covered by the original consent/purpose, or
- Supported by a new valid legal basis

**LGPD Art. 6, III (necessity / data minimisation):** Processing must be limited to what is necessary for the stated purpose. For marketing, the controller must demonstrate that using health insurance data specifically is necessary and proportionate — which is a very high bar to clear given the sensitivity of health data.

**Compatibility analysis:** If health data was originally collected to administer a health insurance product, using it to market unrelated financial products (e.g., loans, investment products) would typically fail a purpose compatibility assessment, as these are substantively different purposes not reasonably anticipated by the data subject at the time of collection.

---

## 5. Additional LGPD Principles and Obligations

### 5.1 Transparency (Art. 6, VI)
The fintech must provide clear, accessible privacy notices disclosing:
- What data is being processed
- The specific purpose of marketing personalisation
- The legal basis relied upon
- Data retention periods
- Data subject rights and how to exercise them

### 5.2 Non-Discrimination (Art. 6, IX and Art. 21)
LGPD expressly prohibits processing sensitive data (including health data) in ways that give rise to **unlawful discrimination**. Using health insurance data to target or exclude customers from financial product offers based on inferred health status could constitute discriminatory profiling and violates Art. 6, IX.

### 5.3 Data Subject Rights (Arts. 17–22)
Customers have rights to:
- **Access** their data (Art. 18, I–II)
- **Correct** inaccurate data (Art. 18, III)
- **Delete** data processed on the basis of consent (Art. 18, VI)
- **Opt out of automated decision-making** that produces significant effects (Art. 20)
- **Revoke consent** at any time (Art. 8, §5)

Marketing based on automated profiling using health and credit data must provide a mechanism to contest automated decisions (Art. 20).

### 5.4 Data Protection Officer (DPO) — Art. 41
Entities processing sensitive data at scale are expected to appoint a **Data Protection Officer (Encarregado)**. For a São Paulo fintech processing health and credit data, DPO appointment is effectively mandatory.

### 5.5 Data Protection Impact Assessment (DPIA)
While LGPD does not explicitly mandate DPIAs by name in all cases, **ANPD guidance and Art. 38** contemplate that controllers processing sensitive data or engaging in high-risk processing (such as large-scale profiling for marketing) should conduct **impact assessments** (*relatório de impacto à proteção de dados pessoais — RIPD*). ANPD has indicated in its published resolutions that processing sensitive data for marketing is likely to require a RIPD.

---

## 6. Sector-Specific Considerations for Fintechs

### 6.1 BACEN and CMN Regulations
Fintech entities regulated by the Banco Central do Brasil (e.g., as a Sociedade de Crédito Direto, Instituição de Pagamento, or other licensed entity) are subject to:
- **BACEN Resolution 4.658/2018** (cybersecurity requirements for financial institutions)
- **BACEN Resolution 4.893/2021** (updated cybersecurity policy requirements)
- Complementary BACEN rules on credit data sharing and Open Finance (Open Banking) frameworks

These rules impose additional data governance, security, and audit trail requirements beyond LGPD.

### 6.2 Open Finance / Open Banking
If the fintech participates in Brazil's Open Finance ecosystem (regulated by BACEN), specific consent and data sharing rules apply to financial data, including credit scores. Repurposing Open Finance data for marketing purposes outside the permitted scope of that framework is prohibited.

---

## 7. Recommended Compliance Pathway

If the fintech wishes to proceed with using health insurance data and credit scores for marketing personalised financial products, the following steps are required:

1. **Obtain explicit, specific, granular consent** from each data subject for the specific purpose of using health insurance data and credit scores for marketing. This consent must be:
   - Separate from any consent for the primary insurance/financial product purpose
   - Clearly worded and not buried in general terms
   - Easy to withdraw at any point

2. **Conduct a RIPD (DPIA)** documenting the processing, risks, and mitigations — particularly for the use of sensitive health data in a marketing context.

3. **Perform a Legitimate Interest Assessment** if relying on that basis for credit score processing, and document it formally.

4. **Review purpose limitation** — ensure that any data originally collected for insurance underwriting or credit assessment is not repurposed without fresh consent.

5. **Implement non-discrimination controls** to prevent health data from being used in ways that could discriminate against customers in access to financial products.

6. **Update privacy notices** to clearly disclose the marketing use of health and credit data.

7. **Appoint a DPO** and ensure they are accessible to data subjects.

8. **Establish data subject rights mechanisms** including easy consent withdrawal, data access requests, and opt-out of profiling.

9. **Apply data minimisation and retention controls** — collect and retain only what is strictly necessary for the marketing purpose.

10. **Engage legal counsel** with LGPD and BACEN expertise to review the specific business model and consent mechanisms before launch.

---

## 8. Summary Assessment

| Data Type | Use for Marketing | Primary Legal Basis | Risk Level |
|---|---|---|---|
| Health insurance data | Heavily restricted | Explicit consent only (Art. 11, I) | Very High |
| Credit scores | Possible with controls | Consent or Legitimate Interests (Art. 7) | Medium–High |
| Combined profiling (health + credit) | Requires full RIPD and explicit consent | Explicit consent | Very High |

**Overall Assessment:** Using health insurance data for marketing personalised financial products is **legally possible under LGPD only on the basis of valid, explicit, specific consent**. No other legal basis in Art. 11 supports this use. Absent genuine, freely given, informed, and specific consent, this processing would be **unlawful under LGPD**.

Credit score use for marketing is less restrictive but still requires a valid legal basis, purpose compatibility analysis, and documented LIA if relying on legitimate interests.

The combination of both sensitive and financial data for automated marketing profiling places this processing in a **high-risk category** requiring a RIPD, strong governance, and careful implementation. Enforcement by ANPD, while still developing, has been increasingly active. Administrative sanctions under LGPD Art. 52 include warnings, fines of up to 2% of Brazilian revenue (capped at R$50 million per violation), and public disclosure of the infraction.

---

*This analysis is based on LGPD (Lei nº 13.709/2018), associated ANPD guidance, and applicable Brazilian financial sector regulations. It does not constitute legal advice. The fintech should engage qualified Brazilian legal counsel before implementing any data processing activities described herein.*
