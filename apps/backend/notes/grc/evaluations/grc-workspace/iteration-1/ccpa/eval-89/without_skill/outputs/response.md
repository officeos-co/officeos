# GDPR to CCPA/CPRA: Additional Compliance Steps and Key Differences

## Executive Summary

If you already have a mature GDPR compliance programme, you have a strong foundation for CCPA/CPRA compliance. Many core privacy principles overlap — transparency, data subject rights, security safeguards, and vendor management. However, the two laws differ significantly in scope, mechanics, opt-out architecture, and enforcement model. This document outlines the gaps you need to close and where the laws diverge most sharply.

---

## 1. Scope and Applicability — Understand Whether CCPA/CPRA Applies

Before closing gaps, confirm you are in scope. CCPA/CPRA (California Consumer Privacy Act, as amended by the California Privacy Rights Act) applies to for-profit businesses that collect personal information of California residents and meet **at least one** of:

- Annual gross revenues exceeding **$25 million**
- Annually buying, selling, receiving, or sharing the personal information of **100,000 or more** consumers or households
- Deriving **50% or more** of annual revenues from selling or sharing consumers' personal information

GDPR applies to any organisation processing EU/EEA residents' personal data regardless of revenue. If your business is small, GDPR may apply while CCPA/CPRA may not — and vice versa for large US-only operations.

---

## 2. Key Additional Steps Required Beyond Your GDPR Programme

### 2.1 Data Mapping and Inventory — CCPA-Specific Categories

**What GDPR already gives you:** A data inventory (Article 30 Record of Processing Activities) covering categories of data, purposes, retention periods, and recipients.

**What you still need for CCPA/CPRA:**

- Map data to CCPA's defined categories of "personal information," which are broader in some respects (e.g., commercial information, inferences drawn from consumer data, olfactory/visual/thermal data).
- Identify **"sensitive personal information" (SPI)** under CPRA — this is a distinct sub-category with additional rights (right to limit use and disclosure) not present in the original CCPA. SPI includes: Social Security numbers, financial account credentials, precise geolocation, racial/ethnic origin, religious beliefs, union membership, contents of communications, genetic/biometric data, and health information.
- Identify all instances of **"selling" or "sharing"** personal information as defined under CCPA — these terms are broader than common understanding and include data exchanged for cross-context behavioural advertising even without monetary consideration.

### 2.2 Implement "Do Not Sell or Share My Personal Information"

**GDPR equivalent:** GDPR requires a legal basis for processing; if based on legitimate interests, individuals can object. There is no exact analogue.

**CCPA/CPRA requirement:** Businesses that sell or share personal information must:

- Post a clear and conspicuous **"Do Not Sell or Share My Personal Information"** link on the homepage and in the privacy notice.
- Establish a mechanism to receive and honour opt-out requests within **15 business days**.
- Respect opt-out signals from recognised **Global Privacy Control (GPC)** browser settings — this is mandatory, not optional.
- Cease selling/sharing the opted-out consumer's data and notify downstream third parties.

**Action:** Implement GPC signal detection in your web infrastructure. Deploy the opt-out link. Build a backend workflow to flag opted-out consumers and suppress their data from being shared or sold.

### 2.3 Right to Limit Use of Sensitive Personal Information

**GDPR equivalent:** Sensitive data requires explicit consent or another specific lawful basis under Article 9. This is a processing restriction at collection, not a post-collection opt-out right.

**CPRA addition:** Consumers have the right to direct a business to **limit the use and disclosure of SPI** to purposes necessary to perform the service. Businesses that use SPI beyond necessary purposes must offer a "Limit the Use of My Sensitive Personal Information" opt-out link (or a combined link).

**Action:** Audit all uses of SPI. If you use SPI for secondary purposes (advertising, profiling), implement the limitation right mechanism.

### 2.4 Data Retention — Explicit Disclosure Requirements

**GDPR:** Requires disclosure of retention periods or criteria in the privacy notice (Articles 13/14).

**CPRA:** Requires businesses to disclose **at or before collection** the retention period for each category of personal information, or if that is not possible, the criteria used. The CPRA also adds an explicit prohibition on retaining personal information longer than reasonably necessary for the disclosed purpose.

**Action:** Update your privacy notice to include category-level retention disclosures aligned with CCPA categories, not just GDPR-style criteria statements.

### 2.5 Contracts with Service Providers, Contractors, and Third Parties

**GDPR equivalent:** Data Processing Agreements (DPAs) with processors under Article 28.

**CCPA/CPRA framework:** The terminology and obligations differ:

| CCPA/CPRA Term | Closest GDPR Analogue | Key Difference |
|---|---|---|
| Service Provider | Processor | Contract must specify permitted purposes; service provider cannot sell/share data or use it for its own purposes |
| Contractor | Processor | Similar restrictions; must certify compliance; applies to data "shared" not just processed for payment |
| Third Party | Controller receiving data | Business must enter a contract restricting use; third party is liable for downstream violations |

**Action:** Review all existing vendor DPAs. They may not contain CCPA-required clauses (e.g., prohibition on selling data received from you, contractual certification of compliance, audit rights framed in CCPA terms). Create a CCPA addendum template. Note that GDPR DPAs and CCPA service provider agreements can sometimes be combined into a single privacy addendum.

### 2.6 Consumer Rights — Gaps vs. GDPR

GDPR rights provide a good baseline, but there are procedural and substantive differences:

| Right | GDPR | CCPA/CPRA | Gap to Close |
|---|---|---|---|
| Right to Know / Access | Art. 15 — access to data and information | Right to know categories and specific pieces of personal information collected in the past 12 months | CCPA response must include specific pieces (not just categories); 45-day response window (same) |
| Right to Delete | Art. 17 — right to erasure | Right to delete with fewer exceptions | CCPA exceptions are different from GDPR; must also instruct service providers and contractors to delete |
| Right to Correct | Art. 16 — rectification | CPRA added right to correct (not in original CCPA) | Already have this capability from GDPR; check your workflow routes corrections to service providers too |
| Right to Portability | Art. 20 — structured, machine-readable format | Right to obtain data in portable format | Similar; CCPA requires delivery within 45 days, twice per 12-month period free of charge |
| Right to Opt-Out of Sale/Sharing | No direct equivalent | Core CCPA right | New — see section 2.2 above |
| Right to Limit SPI Use | No direct equivalent | CPRA right | New — see section 2.3 above |
| Right to Non-Discrimination | No explicit equivalent | Explicit prohibition on discriminating against consumers who exercise rights | Ensure loyalty programmes, pricing, and service tiers do not penalise opt-outs |
| Automated Decision-Making | Art. 22 — right not to be subject to solely automated decisions | CPRA gives CPPA rulemaking authority; regulations are evolving | Monitor CPPA rulemaking; implement opt-out of automated decision-making affecting consumers significantly |

**Action:** Update your DSR (Data Subject Request) platform to handle CCPA-specific request types (e.g., "Do Not Sell," "Limit SPI"). Train your response team on CCPA exceptions (which differ from GDPR exceptions). Ensure deletion requests cascade to service providers.

### 2.7 Privacy Notice Updates

**GDPR:** Article 13/14 notices are required at point of collection.

**CCPA/CPRA:** Requires a privacy notice "at or before" collection for each category, including:

- Categories of personal information collected and the purposes for each
- Categories of third parties to whom information is disclosed
- Retention periods per category
- Whether personal information is sold or shared (and categories of recipients)
- The consumer rights available and how to exercise them
- The right to submit requests via a designated toll-free number (for businesses with a physical presence or required by regulation)
- Whether SPI is collected and used beyond necessary purposes

**Action:** Create a California-specific privacy notice (or a layered notice with California addendum). Ensure it is accessible from every page where personal information is collected.

### 2.8 Establish a Toll-Free Number or Designated Request Method

**GDPR:** No specific requirement for a toll-free telephone number.

**CCPA/CPRA:** Businesses must provide at least two designated methods for submitting requests to know and delete, including a toll-free telephone number (unless the business operates solely online, in which case an email address suffices).

**Action:** Set up a CCPA request intake channel (toll-free number or email for online-only businesses). Log all requests for compliance records.

### 2.9 No Consent-as-Legal-Basis Architecture (Generally)

**GDPR:** Consent is one of six lawful bases; it is often the primary basis for marketing.

**CCPA/CPRA:** CCPA is largely an **opt-out** regime for adults (not opt-in). Consent (opt-in) is only required in specific situations:

- **Sale/sharing of data of consumers under 16** (opt-in required; under 13 requires parent/guardian opt-in)
- **Re-use of personal information** for a purpose materially different from the disclosed purpose

**Action:** If you serve consumers under 16 in California, implement age-detection and parental consent workflows separate from any GDPR age-gating mechanisms.

### 2.10 California Privacy Protection Agency (CPPA) Registration and Rulemaking

**GDPR:** Supervisory authority is your lead DPA (determined by establishment).

**CCPA/CPRA:** The **California Privacy Protection Agency (CPPA)** is the primary regulatory body with rulemaking authority. Regulations are actively evolving (cybersecurity audits, risk assessments, automated decision-making rules were still being finalised as of early 2026).

**Action:** Monitor CPPA rulemaking at cppa.ca.gov. Subscribe to regulatory updates. Be prepared to comply with cybersecurity audit requirements and privacy risk assessments once finalised — these will require operational processes similar to GDPR's Data Protection Impact Assessments (DPIAs) but with California-specific frameworks.

---

## 3. Where GDPR and CCPA/CPRA Differ Most Significantly

### 3.1 Opt-Out vs. Opt-In Philosophy

This is the most fundamental structural difference.

- **GDPR:** Processing personal data generally requires a **lawful basis established before or at the point of processing**. For consent-based processing, this means opt-in before data is used. For other bases (contract, legitimate interests), the data subject can object after the fact.
- **CCPA/CPRA:** For most processing, businesses can collect and use data freely; consumers have the right to **opt out after the fact** (except for the specific scenarios noted above). This reflects a different cultural and legal philosophy about data use.

**Implication:** Your GDPR consent management infrastructure (CMPs, consent banners) may create compliance friction in the CCPA context if misapplied. CCPA does not require consent banners or cookie consent; it requires opt-out mechanisms.

### 3.2 Household vs. Individual as the Unit of Protection

- **GDPR:** Protects **natural persons** (individuals).
- **CCPA/CPRA:** Protects **consumers** and **households** — household-level data can trigger CCPA obligations even if no individual is identified.

### 3.3 "Sale" and "Sharing" Are Defined Very Broadly

- **GDPR:** Transfers of data to third parties are governed by controller-to-controller or controller-to-processor frameworks; the concept of "sale" is not legally significant.
- **CCPA/CPRA:** "Selling" includes any disclosure for **valuable consideration**, which courts and the CPPA have interpreted broadly. "Sharing" specifically covers disclosure for **cross-context behavioural advertising** even without monetary exchange — this captures most adtech arrangements (Google Analytics, Meta Pixel, programmatic advertising) that GDPR handles through consent but CCPA handles through opt-out.

**Implication:** If you use any third-party tracking for advertising purposes, you are almost certainly "selling or sharing" under CCPA/CPRA, triggering opt-out obligations regardless of your GDPR consent model.

### 3.4 No Equivalent to GDPR's Lawful Basis Framework

- **GDPR:** Six lawful bases (consent, contract, legal obligation, vital interests, public task, legitimate interests). Processing without a basis is unlawful.
- **CCPA/CPRA:** No equivalent lawful basis requirement. The law focuses on **transparency and opt-out rights**, not on pre-authorising processing. Businesses disclose what they do and give consumers the right to opt out of sale/sharing and limit SPI use.

### 3.5 Private Right of Action is Narrow (But Significant)

- **GDPR:** Individuals can complain to supervisory authorities; Member State law may provide private rights of action varying by jurisdiction.
- **CCPA/CPRA:** Private right of action is **limited to data breaches** involving certain categories of unencrypted/unredacted personal information. However, statutory damages of **$100–$750 per consumer per incident** (or actual damages if greater) can aggregate to enormous class action exposure. Regulatory enforcement (up to **$2,500 per unintentional violation** and **$7,500 per intentional violation or violations involving minors**) is handled by the CPPA and California Attorney General.

### 3.6 DPIA / Risk Assessment Approach

- **GDPR:** Article 35 DPIAs are triggered by high-risk processing (large-scale processing of special categories, systematic monitoring, automated decisions with significant effects).
- **CPRA:** Privacy risk assessments (and cybersecurity audits) will be required by CPPA regulation for businesses whose processing presents significant risk. The triggers and scope are not yet identical to GDPR Art. 35. Automated decision-making assessments are required separately.

### 3.7 No Data Protection Officer (DPO) Requirement

- **GDPR:** Mandatory DPO appointment in certain circumstances (Articles 37–39).
- **CCPA/CPRA:** No mandatory DPO. However, designating a privacy lead or privacy officer is a best practice and may be required by industry frameworks.

### 3.8 Scope of Special/Sensitive Data

- **GDPR Special Categories (Art. 9):** Race/ethnicity, political opinions, religious/philosophical beliefs, trade union membership, genetic data, biometric data for unique identification, health data, sex life/orientation.
- **CPRA Sensitive Personal Information:** Overlaps substantially but adds: **Social Security/government ID numbers**, **financial account credentials** (account numbers + access codes), **precise geolocation**, **contents of messages** (email, text, mail). Notably, health data is SPI under CPRA but does not require the same lawful basis structure as GDPR Art. 9 — instead it triggers the right to limit use.

---

## 4. Prioritised Action Plan for GDPR-Ready Organisations

| Priority | Action Item | Effort |
|---|---|---|
| 1 | Determine if CCPA/CPRA applies (revenue/volume thresholds) | Low |
| 2 | Audit data flows for "selling" or "sharing" (adtech, data brokers) | Medium |
| 3 | Implement GPC signal detection and Do Not Sell/Share opt-out | High |
| 4 | Update privacy notice with CCPA-required disclosures | Medium |
| 5 | Update vendor contracts with CCPA service provider clauses | Medium |
| 6 | Implement right to limit SPI use if SPI used beyond necessary purposes | Medium |
| 7 | Extend DSR platform to handle CCPA request types and timelines | Medium |
| 8 | Set up designated request intake (toll-free number or email) | Low |
| 9 | Implement children's data opt-in if serving under-16s | High |
| 10 | Monitor CPPA rulemaking for cybersecurity audit and ADM requirements | Ongoing |

---

## 5. Summary: What GDPR Gives You for Free

Your GDPR programme already provides:
- Data inventory and mapping (extend to CCPA categories)
- Privacy notice drafting capability (update for CCPA requirements)
- DSR intake and response workflows (extend to CCPA right types)
- Vendor management (update contracts with CCPA clauses)
- Data retention policies (ensure category-level disclosure)
- Security safeguards (supports CCPA breach liability defence)
- DPIAs (foundation for CPPA risk assessment requirements)
- Training programmes (extend to CCPA-specific scenarios)

The primary **net-new** work is: the opt-out infrastructure (GPC, Do Not Sell link), the SPI limitation right, the California-specific privacy notice, CCPA-compliant vendor contracts, and ongoing monitoring of CPPA regulatory developments.
