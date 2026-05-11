# DPDPA Requirements for Processing Children's Data

## Definition of "Child" Under DPDPA

Under **Section 2(f)** of the DPDPA, a **"child"** means any individual who has not completed **18 years of age**. This is a uniform threshold — there is no regional or platform-level variation permitted. Any user under 18 is legally a child, triggering the full protections of **Section 9**.

---

## Section 9 Obligations: What Your Platform Must Do

### 1. Verifiable Parental/Guardian Consent (Section 9(1))

Before processing **any** personal data of a child, you must obtain **verifiable parental or lawful guardian consent**. This is a hard prerequisite — there is no processing of a child's data without it.

"Verifiable" means you cannot accept a parent's self-declaration at face value without a verification mechanism. **Rule 12** of the DPDP Rules 2025 prescribes the permitted verification methods:

| Method | Description |
|--------|-------------|
| **Existing user data** | If your platform already holds verified identity/age data for the parent, that data can support verification |
| **Voluntary self-declaration** | Parent/guardian self-declares identity and relationship (lowest assurance level) |
| **Token-based verification** | Via government or government-mandated entities, **DigiLocker** (India's official digital document wallet), or other notified token-issuing bodies |

DigiLocker integration is likely to become the standard approach for high-assurance parental consent verification in India.

### 2. Age Verification at Registration (Rule 10)

You must implement **age verification mechanisms** at the point of user registration/onboarding. This means:

- A functional age gate that prevents under-18 users from registering without triggering the parental consent flow
- The mechanism must be robust — self-declaration of age alone without any verification is insufficient for a platform that has reason to believe it serves minors

### 3. Absolute Prohibitions — Section 9(2)

These prohibitions apply to **all Data Fiduciaries** processing children's data, regardless of size or type. They cannot be overridden by parental consent:

| Prohibited Activity | Description |
|--------------------|-------------|
| **Tracking or behavioural monitoring** | GPS tracking, activity profiling, clickstream analysis, session recording specifically of children |
| **Targeted advertising** | Personalised ads, recommendation algorithms based on child profile, marketing communications directed at children |
| **Detrimental processing** | Any processing likely to cause detrimental effect on a child's well-being — physical, mental, emotional, or developmental harm |

**These are absolute bans.** They apply even if a parent has consented. A parent cannot consent on behalf of their child to behavioural tracking or targeted advertising — the Act prohibits these outright.

---

## Exemptions from Section 9

Processing without parental consent is only permitted in the following narrow circumstances:

1. **Health and safety emergencies** — medical treatment, child safety services
2. **Essential prescribed services** — age-appropriate educational platforms, child safety apps (as specifically prescribed by the Central Government)
3. **Law enforcement** — crime prevention or investigation involving the child

These exemptions do not unlock the prohibited activities in Section 9(2). They only permit processing without parental consent where urgency justifies it.

---

## Penalties for Children's Data Violations

Violations of Section 9 carry a maximum penalty of **₹200 crore** — one of the highest penalty tiers under the DPDPA. The **Data Protection Board of India (DPBI)** is the enforcement body.

---

## Practical Gap Analysis for Your Platform

| Obligation | What You Need | Common Gap |
|-----------|--------------|-----------|
| Age gate at registration | Mechanism that identifies under-18 users and triggers parental consent flow | No age verification; open registration to all |
| Verifiable parental consent | DigiLocker/token-based or data-backed verification | Accepting checkbox "I am over 18" or simple self-declaration |
| No behavioural tracking of children | Technical controls to suppress tracking for child accounts | Analytics/session tracking running uniformly on all accounts |
| No targeted advertising to children | Ad platform configuration excludes child accounts from targeting | Standard ad targeting applied to all users |
| No detrimental processing | Review all automated processing for potential well-being impact on children | No child-specific data processing impact review |
| Processor contracts | Contracts with ad networks, analytics vendors prohibit secondary use of children's data | Generic vendor contracts not updated |

---

## DPDPA vs. GDPR: Children's Data Comparison

| Dimension | GDPR | DPDPA Section 9 |
|-----------|------|----------------|
| **Age threshold** | **16 years** (member states may lower to 13) | **18 years** — uniform, no variation |
| **Parental consent requirement** | Required for under-16 (or under-13 in some member states) | Required for **all under-18** |
| **Verification mechanism** | Not specifically prescribed — reasonable measures | **Prescribed in Rule 12**: DigiLocker, government tokens, existing data |
| **Behavioural monitoring of children** | Permitted with appropriate legal basis | **Prohibited outright** — Section 9(2) |
| **Targeted advertising to children** | Permitted with appropriate consent/legal basis | **Prohibited outright** — Section 9(2) |
| **Detrimental processing prohibition** | General harm avoidance principles; DPIA required for high-risk processing | **Explicit prohibition** — Section 9(2)(c) |
| **Penalty for violation** | Up to €20M or 4% of global turnover | Up to **₹200 crore** |

### What This Means for Your Platform

1. **16/17-year-olds require full protection in India.** If your GDPR compliance handles 16–17 year olds as adults (permitted in most EU member states), you must apply full child protections for this age band in India.

2. **Behavioural monitoring must be switched off for all under-18 accounts.** If your analytics, recommendations, or ad systems process user behaviour, you need child-account detection and suppression — even where a parent has consented.

3. **Targeted advertising to any under-18 user in India is prohibited.** This is not a consent issue — it is an outright ban. No parental consent can unlock targeted advertising to a child.

4. **Verification is more prescriptive.** GDPR leaves verification methods to reasonable judgment. DPDPA Rule 12 prescribes specific mechanisms, with DigiLocker as the likely primary method.

---

## Recommended Action Plan

1. **Implement a robust age gate** — require date of birth at registration; route under-18 users to a parental consent flow before completing registration
2. **Integrate DigiLocker or token-based verification** for parental consent to meet Rule 12 requirements
3. **Audit your analytics and tracking systems** — create child-account flags that suppress behavioural tracking, profiling, and targeted advertising
4. **Review your ad network contracts** — ensure child accounts are excluded from targeting audiences and that this exclusion is contractually required of your ad partners
5. **Conduct a processing impact review** for all automated processing of children's data to identify any potentially detrimental effects
6. **Update Data Processor agreements** to prohibit secondary use of children's personal data

Full compliance is required by **13 May 2027**, but given the complexity of implementing age verification and ad suppression systems, your engineering teams should begin scoping this work immediately.
