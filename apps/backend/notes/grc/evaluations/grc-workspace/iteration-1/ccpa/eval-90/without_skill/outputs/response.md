# CPRA Classification and Compliance Obligations for Mobile App Data Collection

## Overview

Your mobile application collects three categories of data that trigger heightened obligations under the California Privacy Rights Act (CPRA), which amended and significantly expanded the California Consumer Privacy Act (CCPA). All three data types — precise geolocation, health metrics from wearable integration, and biometric facial recognition — qualify as **Sensitive Personal Information (SPI)** under CPRA, and biometric data also qualifies as a distinct **special category** subject to the most restrictive protections.

---

## 1. Data Classification Under CPRA

### 1.1 Precise Geolocation Data

**Classification: Sensitive Personal Information (SPI)**

Under Cal. Civ. Code § 1798.140(ah)(1)(G), "precise geolocation" is explicitly defined as information that reveals the physical location of a consumer within a radius of 1,850 feet (approximately 1/3 mile) or less. This includes GPS coordinates, cell tower triangulation data, and Wi-Fi positioning data.

- Mobile apps that collect GPS or fine-grained location signals fall squarely within this definition.
- Even periodic or on-demand location collection qualifies — there is no minimum frequency threshold.
- Location data derived from wearable devices is also covered.

### 1.2 Health Metrics from Wearable Integration

**Classification: Sensitive Personal Information (SPI) — potentially also "Personal Information" under CPRA's general framework**

Under Cal. Civ. Code § 1798.140(ah)(1)(C), "personal information collected and analyzed concerning a consumer's health" is classified as SPI. Health metrics from wearables — including heart rate, blood oxygen levels (SpO2), sleep patterns, step counts, stress indicators, menstrual cycle data, ECG readings, and similar physiological measurements — all fall within this definition.

Key points:
- The CPRA does not require data to be medically diagnostic to qualify as health SPI; fitness and wellness metrics are included.
- If the wearable integration involves a third-party SDK or API, data sharing with that third party may constitute a "sale" or "sharing" of SPI requiring disclosure and opt-out rights.
- Health data that also qualifies as Protected Health Information (PHI) under HIPAA must comply with both HIPAA and CPRA simultaneously where applicable.

### 1.3 Biometric Facial Recognition for Authentication

**Classification: Sensitive Personal Information (SPI) AND Biometric Information — the highest-risk category under CPRA**

Under Cal. Civ. Code § 1798.140(ah)(1)(B), SPI includes "a consumer's racial or ethnic origin, religious or philosophical beliefs, or union membership" — but more directly, § 1798.140(c) defines **"biometric information"** as an individual's physiological, biological, or behavioral characteristics that can be used to establish identity, including "imagery of the iris, retina, fingerprint, face, hand, palm, vein patterns, and voice recordings," as well as "keystroke patterns or rhythms, gait patterns or rhythms, and sleep, health, or exercise data that contain identifying information."

Facial recognition data used for authentication involves:
- Capture and processing of facial geometry
- Creation of a facial template or biometric identifier
- Ongoing comparison of facial scans against stored templates

This is SPI under § 1798.140(ah)(1)(E) ("biometric information processed for the purpose of uniquely identifying a consumer") AND constitutes biometric data under the general CPRA personal information definition. The **combination** of collecting, processing, and storing facial recognition data for authentication is among the most regulated activities under CPRA.

---

## 2. Special Obligations Triggered by SPI Collection

### 2.1 Right to Limit Use and Disclosure of SPI

This is the most significant new right introduced by CPRA that did not exist under CCPA.

**Obligation:** Businesses must provide consumers with the right to direct the business to **limit the use and disclosure of their SPI** to only what is necessary to perform the services requested, or as otherwise permitted by CPRA regulations (Cal. Civ. Code § 1798.121).

**Implementation requirements:**
- You must provide a clear and conspicuous link titled **"Limit the Use of My Sensitive Personal Information"** on your homepage (or app equivalent, such as your app's settings page or privacy center).
- Alternatively, if you do not use SPI for purposes beyond providing the requested service, you may include a statement to that effect in your privacy policy to avoid the obligation to post the opt-out link — but only if use is genuinely limited.
- The link can be combined with the "Do Not Sell or Share My Personal Information" link into a single combined opt-out page.

### 2.2 Right to Opt-Out of Sale or Sharing

**Obligation:** If any of the three data types are sold to or shared with third parties for cross-context behavioral advertising, consumers must be given the right to opt out (Cal. Civ. Code § 1798.120).

- This applies to sharing of precise geolocation with advertising networks, analytics platforms, or data brokers.
- Sharing health metrics with third-party wellness platforms or analytics services may qualify.
- Facial recognition templates shared with authentication vendors or identity providers must be disclosed.

### 2.3 Data Minimization

Under CPRA regulations (Cal. Code Regs. tit. 11, § 7002), businesses must adhere to **data minimization principles** for SPI:

- Collection must be **reasonably necessary and proportionate** to the stated purpose.
- You may not collect more SPI than needed for the disclosed purpose.
- You may not retain SPI longer than reasonably necessary.
- **Practical implication:** If facial recognition is used only for authentication, you cannot also use the biometric template for marketing personalization or user behavioral analysis without separate disclosure and legal basis.

### 2.4 Purpose Limitation

SPI collected for one purpose (e.g., authentication via facial recognition) cannot be repurposed for other uses (e.g., emotion detection, demographic inference) without obtaining additional consumer consent or providing a new notice.

### 2.5 Storage Limitation and Retention Schedules

CPRA regulations (Cal. Code Regs. tit. 11, § 7051) require businesses to establish and disclose:
- Retention periods for each category of personal information and SPI collected.
- The criteria used to determine retention periods.
- This applies specifically to biometric data, geolocation history, and health records.

### 2.6 Security Requirements

Given the sensitivity of all three data types, CPRA mandates implementation of **reasonable security measures** (Cal. Civ. Code § 1798.150). The California Attorney General and California Privacy Protection Agency (CPPA) have signaled that SPI requires enhanced security controls. Recommended measures include:
- End-to-end encryption for biometric templates and health data in transit and at rest.
- Strict access controls and role-based permissions for SPI.
- Biometric templates should be stored as irreversible cryptographic hashes rather than raw facial images where technically feasible.
- Geolocation data should be anonymized or aggregated when precise coordinates are no longer necessary.

### 2.7 Automated Decision-Making and Profiling (Pending Regulations)

The CPPA is finalizing **automated decision-making technology (ADMT)** regulations (expected 2025–2026) that will apply to systems that use profiling, including facial recognition for authentication. Once finalized, these may require:
- Pre-use notices to consumers before automated decisions are made.
- Opt-out rights for certain automated decision-making.
- Access rights to understand the logic of automated decisions.

You should monitor CPPA rulemaking and prepare to comply when these regulations take effect.

### 2.8 Risk Assessments (Mandatory for High-Risk Processing)

Under CPRA § 1798.185(a)(15) and CPPA regulations, businesses that process personal information presenting **significant risk** to consumers' privacy must conduct and submit a **Privacy Risk Assessment** to the CPPA. This is likely mandatory for your app given:
- Use of biometric facial recognition (explicitly high-risk).
- Collection of precise geolocation.
- Processing of health metrics.

The CPPA is still finalizing the risk assessment submission requirements, but internal assessments should begin immediately.

---

## 3. Required Disclosures

### 3.1 Privacy Policy Disclosures

Your privacy policy must include all of the following, updated to reflect SPI (Cal. Civ. Code § 1798.100(b), § 1798.130):

**Categories of SPI collected:**
- Precise geolocation (GPS-derived location to within 1,850 feet)
- Health and wellness data (heart rate, sleep, fitness metrics from wearable integration)
- Biometric information (facial geometry/template used for authentication)

**For each SPI category, disclose:**
1. The specific SPI categories collected.
2. The **purposes** for which each SPI category is used (e.g., "biometric data is collected solely to authenticate your identity and is not shared with third parties or used for marketing").
3. Whether SPI is **sold or shared** with third parties, and if so, which categories of third parties.
4. The **retention period** for each SPI category, or if that is not possible, the criteria used to determine how long it is retained.
5. Whether SPI is used to make **inferences** about consumers.

**Third-party disclosures:** If you share any SPI with third parties (including wearable SDKs, analytics vendors, cloud authentication providers), disclose:
- The categories of third parties.
- The purpose of the disclosure.
- Whether the disclosure constitutes a "sale" or "sharing."

### 3.2 At-Collection Notice (Notice at Collection)

Under Cal. Civ. Code § 1798.100(b) and Cal. Code Regs. tit. 11, § 7012, you must provide a **Notice at Collection** at or before the time you collect SPI. For a mobile app, this means:

- **Before enabling location services:** Display a notice specifying that precise geolocation is collected, the purpose, and the categories of third parties with whom it may be shared.
- **Before wearable integration is activated:** Inform users that health metrics will be collected and for what purposes.
- **Before facial recognition enrollment:** Provide a clear disclosure that biometric data will be captured, how the template will be stored, whether it will be shared, and how long it will be retained. This notice must be presented **before** the facial scan is taken.

The Notice at Collection must include:
- Categories of SPI collected.
- Purposes for collection.
- A link to the full privacy policy.
- Whether SPI is sold or shared (or a statement that it is not).

### 3.3 "Limit the Use of My Sensitive Personal Information" Link

As noted above, you must provide a mechanism (typically a link in your app's privacy/settings section and on your website) through which consumers can direct you to limit use of their SPI. This link:
- Must be **clear and conspicuous**.
- Can be combined with "Do Not Sell or Share My Personal Information" into one opt-out interface.
- Must be honored within **15 business days** of receiving the consumer's request.

### 3.4 "Do Not Sell or Share My Personal Information" Link

If any of the three SPI categories are sold or shared with third parties for cross-context behavioral advertising, you must:
- Post this link prominently.
- Honor opt-out requests within 15 business days.
- Maintain records of opt-out requests.

### 3.5 Consumer Rights Notice

Inform users of their full CPRA rights, including:
- **Right to Know:** What SPI is collected and how it is used.
- **Right to Delete:** Request deletion of SPI (subject to exceptions).
- **Right to Correct:** Request correction of inaccurate SPI.
- **Right to Opt-Out:** Of sale or sharing.
- **Right to Limit:** Use of SPI beyond essential purposes.
- **Right to Non-Discrimination:** For exercising privacy rights.

---

## 4. Summary Table

| Data Type | CPRA Classification | SPI Category | Key Obligations |
|---|---|---|---|
| Precise Geolocation | SPI | § 1798.140(ah)(1)(G) | Limit-use right, data minimization, retention disclosure, opt-out if shared |
| Health Metrics (Wearables) | SPI | § 1798.140(ah)(1)(C) | Limit-use right, data minimization, purpose limitation, third-party sharing disclosure |
| Biometric Facial Recognition | SPI + Biometric Information | § 1798.140(ah)(1)(E) | Highest-risk; pre-collection notice mandatory, limit-use right, security requirements, privacy risk assessment likely required |

---

## 5. Recommended Immediate Actions

1. **Audit data flows** for all three SPI categories — map where data is collected, stored, processed, and shared.
2. **Update your privacy policy** to include all required SPI disclosures, retention schedules, and third-party sharing details.
3. **Implement at-collection notices** within the app before any SPI is captured.
4. **Deploy the opt-out and limit-use mechanisms** — either as separate links or combined, accessible in the app and on your website.
5. **Conduct a Privacy Risk Assessment** covering biometric data processing, geolocation tracking, and health data collection.
6. **Review third-party contracts** (wearable SDKs, authentication vendors) to ensure they function as service providers under CPRA rather than third parties to whom you are "selling" or "sharing" data.
7. **Establish retention and deletion schedules** for biometric templates, location history, and health records.
8. **Monitor CPPA rulemaking** on automated decision-making technology regulations, which will affect your facial recognition authentication system.

---

*This analysis is based on the California Privacy Rights Act (Cal. Civ. Code §§ 1798.100–1798.199.100) and California Privacy Protection Agency regulations (Cal. Code Regs. tit. 11, §§ 7000–7304) as of April 2026. Consult qualified legal counsel for advice specific to your business operations.*
