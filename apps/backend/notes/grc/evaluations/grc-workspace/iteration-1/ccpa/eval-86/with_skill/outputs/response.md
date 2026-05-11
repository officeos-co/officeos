# CCPA/CPRA Applicability and Core Obligations Analysis

## Are You Subject to CCPA/CPRA?

**Yes — your business is subject to CCPA/CPRA.**

Under California law (Cal. Civ. Code §1798.100 et seq.), a for-profit business that does business in California must comply with CCPA/CPRA if it meets **at least one** of three thresholds. Your company meets the first threshold:

| Threshold | Requirement | Your Status |
|---|---|---|
| Revenue | Annual gross revenues exceeding **$25 million** | **MET** — $30M annual revenue exceeds the $25M threshold |
| Data Volume | Annually buys, sells, receives, or shares PI of **100,000+ consumers or households** | Not met — 80,000 California customers is below 100,000 |
| Revenue from PI | Derives **50%+ of annual revenues** from selling/sharing PI | Presumed not met for a standard e-commerce business |

Your $30 million annual revenue alone triggers full CCPA/CPRA compliance obligations. Even though your 80,000 California customers per year falls below the 100,000-consumer data volume threshold, the revenue threshold is satisfied independently.

---

## Core Obligations

### 1. Privacy Notice at Collection
You must inform consumers **at or before** collecting their personal information (PI) about:
- The categories of PI being collected
- The purposes for which PI will be used
- Whether PI is sold or shared with third parties
- Retention periods for each category of PI (CPRA requirement)
- A link to your full privacy policy

For an e-commerce site, this means clear disclosures at account creation, checkout, and any point where additional data is collected (e.g., email signups, reviews).

### 2. Comprehensive Privacy Policy
Your privacy policy must include:
- All categories of PI collected in the last 12 months
- The business or commercial purposes for each category
- Categories of third parties to whom PI is disclosed
- A description of all consumer rights and how to exercise them
- Contact information for submitting consumer requests
- A **"Do Not Sell or Share My Personal Information"** link (if you sell/share PI)
- A **"Limit the Use of My Sensitive Personal Information"** link (if applicable)

### 3. Consumer Rights Fulfillment
You must establish processes to honor the following rights with a **45-day response deadline** (extendable by another 45 days with notice):

| Right | Description |
|---|---|
| **Right to Know** | Consumers can request the specific PI you've collected about them, its sources, purposes, and who it's been shared with |
| **Right to Delete** | Consumers can request deletion of their PI (subject to limited exceptions, e.g., completing a transaction, legal obligations) |
| **Right to Correct** | Consumers can request correction of inaccurate PI (CPRA addition) |
| **Right to Data Portability** | PI must be delivered in a portable, usable format upon request |
| **Right to Non-Discrimination** | You cannot deny service, charge higher prices, or penalize consumers for exercising their privacy rights |

Additionally:
- **Right to Opt-Out of Sale/Sharing**: Must be honored **immediately** upon request
- **Right to Limit SPI Use**: Must be honored within **15 business days** (if you collect sensitive personal information)
- **Minors**: If any customers are under 16, you need opt-in consent before selling/sharing their PI

### 4. Opt-Out Mechanisms
- Place a **"Do Not Sell or Share My Personal Information"** link on your homepage if you sell or share PI (e.g., through advertising pixels, affiliate data sharing, data brokers)
- Honor **Global Privacy Control (GPC)** signals — under CPPA guidance and California court decisions, GPC must be treated as a valid opt-out from sale and sharing
- Implement a **"Limit the Use of My Sensitive Personal Information"** link if you process SPI (e.g., precise geolocation, health data, account credentials) beyond necessary purposes

### 5. Data Minimization and Purpose Limitation (CPRA)
- Collect only PI that is **adequate, relevant, and limited** to what is necessary for the disclosed purpose
- Do not use PI for purposes not disclosed to consumers at collection
- This is particularly relevant for e-commerce analytics, behavioral advertising, and personalization use cases

### 6. Retention Limits (CPRA)
- Disclose retention periods or the criteria used to determine them for each category of PI
- Do not retain PI longer than reasonably necessary for the disclosed purpose
- This applies to customer purchase history, account data, browsing data, and marketing lists

### 7. Service Provider Contracts
Any vendor that processes customer PI on your behalf (payment processors, fulfillment services, email platforms, analytics tools, CDPs) must have written contracts that include:
- Limitations on how the vendor may use your customers' PI
- Prohibition on selling or sharing PI without your authorization
- Obligations to comply with consumer deletion/correction requests
- Your right to audit the vendor's compliance
- Data deletion obligations upon contract termination

Vendors meeting these requirements qualify as **Service Providers** — their receipt of PI is not a "sale" under CCPA/CPRA.

### 8. Sensitive Personal Information (SPI) — If Applicable
As an e-commerce company, you may collect SPI such as:
- Precise geolocation (for delivery tracking or targeted offers)
- Account log-in credentials (username + password combinations)
- Health/medical data (if selling health-related products with health-related personalization)

If you collect SPI, consumers have the right to **limit its use and disclosure** to only what is necessary to provide the requested service.

### 9. Cybersecurity Audits and Risk Assessments (CPRA — Pending Rulemaking)
Businesses processing PI that presents significant risk must conduct **annual cybersecurity audits** and submit **risk assessments** to the California Privacy Protection Agency (CPPA). The exact scope for e-commerce businesses at your scale is subject to final CPPA rulemaking, but you should monitor this requirement.

---

## Penalty Exposure

Non-compliance carries significant financial risk:
- **Unintentional violations**: Up to **$2,500 per violation**
- **Intentional violations**: Up to **$7,500 per violation**
- **Violations involving minors' PI**: Always treated as intentional — up to **$7,500 per violation**
- **Data breach private right of action**: Statutory damages of **$100–$750 per consumer per incident** (class action eligible) if a breach results from failure to implement reasonable security measures

With 80,000 California customers, even a modest percentage of consumers affected by a systemic violation or data breach creates substantial aggregate liability.

---

## Immediate Next Steps

1. **Conduct a data inventory**: Map what PI you collect, from whom, for what purposes, how long you retain it, and who you share it with
2. **Audit your privacy policy**: Update it to include all CPRA-required disclosures (retention periods, right to correct, updated opt-out language)
3. **Implement a consumer request intake process**: Set up a webform or toll-free number to receive and track consumer rights requests within required timelines
4. **Review advertising and data sharing**: Determine whether any PI flows to ad networks, analytics platforms, or data brokers constitute a "sale" or "sharing" — if so, implement opt-out mechanisms and GPC signal handling
5. **Audit vendor contracts**: Ensure all service providers have CCPA/CPRA-compliant data processing agreements in place
6. **Train staff**: Ensure customer-facing and data-handling teams understand how to identify and escalate consumer rights requests

---

*This analysis is based on CCPA (Cal. Civ. Code §1798.100 et seq.) and CPRA (Proposition 24, effective January 1, 2023). Consult qualified legal counsel for advice specific to your business operations and data practices.*
