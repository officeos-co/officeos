# CPRA Classification and Obligations for Mobile App Data Collection

## Overview

Your mobile app collects three distinct categories of data: precise geolocation, health metrics from wearable integration, and biometric facial recognition for authentication. Under the California Privacy Rights Act (CPRA), all three categories qualify as **Sensitive Personal Information (SPI)** — the highest-protection tier of personal information under California law.

---

## 1. Data Classification Under CPRA

### All Three Categories Are Sensitive Personal Information (SPI)

The CPRA (Cal. Civ. Code §1798.140(ae)) defines SPI as a subset of personal information that warrants heightened protection. Each data type in your app maps directly to a statutory SPI category:

| Data Type | SPI Category | Statutory Basis |
|---|---|---|
| Precise Geolocation | "Precise geolocation" — defined as location within a radius of 1,850 feet | §1798.140(ae)(1)(H) |
| Health Metrics (wearable) | "Personal information collected and analyzed concerning a consumer's health" | §1798.140(ae)(1)(B) |
| Biometric Facial Recognition | "Biometric information" — including physiological characteristics used for identification | §1798.140(ae)(1)(B) |

These are not borderline classifications. All three are explicitly enumerated SPI categories, meaning they carry the full weight of CPRA's SPI protections regardless of how the data is processed or used.

All three categories also constitute standard **Personal Information (PI)** under §1798.140(v), which means baseline CCPA/CPRA obligations apply in addition to the SPI-specific obligations.

---

## 2. Special Obligations for SPI

### A. Right to Limit SPI Use (§1798.121) — CPRA Addition

Consumers have the right to direct your business to limit the use and disclosure of their SPI to only what is necessary to perform the services they requested (or as permitted by regulation).

**Permitted purposes without limitation** (i.e., SPI can be used without triggering the opt-out right) include:
- Performing the services or providing goods reasonably expected by the consumer
- Helping to ensure security and integrity of the business's systems
- Short-term transient use not used for profiling
- Activities that qualify under certain legal exemptions

**Trigger for the limitation right**: If your app uses SPI for purposes beyond what is strictly necessary to deliver the core service — for example, using geolocation data for targeted advertising, sharing health metrics with third-party analytics partners, or retaining biometric data beyond the authentication session — consumers can invoke this right.

**Response deadline**: You must honor limitation requests within **15 business days**.

### B. "Limit the Use of My Sensitive Personal Information" Link

If SPI is used for purposes beyond the narrowly permitted set, you must provide a clear and conspicuous link — titled exactly "Limit the Use of My Sensitive Personal Information" — on your homepage and in your app. This is a separate and additional link from the "Do Not Sell or Share My Personal Information" link.

Both links can be combined into a single "Your Privacy Choices" link under CPPA regulations, provided it is equally prominent.

### C. Data Minimization and Purpose Limitation (CPRA §1798.100(c))

SPI collection must be **adequate, relevant, and limited to what is necessary** for the disclosed purpose. Applied to your app:

- **Precise geolocation**: Collect only when necessary for app functionality (e.g., navigation or location-based features). Avoid continuous background collection if the service does not require it.
- **Health metrics**: Collect only the metrics necessary for the features offered. Avoid aggregating or retaining data beyond what the wearable integration feature requires.
- **Biometric data**: For authentication purposes, best practice is to process biometrics locally on-device and store only a derived cryptographic template (not the raw facial scan), and only for as long as the authentication session requires.

### D. Retention Limits (CPRA Addition)

You must disclose — and adhere to — specific retention periods or the criteria used to determine retention for each SPI category. You cannot retain SPI longer than reasonably necessary for the disclosed purpose.

**Recommended retention posture**:
- Biometric data: Retained only for the duration of the authentication session; derived templates purged upon account deletion or user request.
- Health metrics: Retain for the period necessary to deliver the wearable integration feature; disclose specific retention window.
- Precise geolocation: If collected in real-time for navigation, do not persist beyond the active session unless the user explicitly opts in to history storage.

### E. Cybersecurity Audits and Risk Assessments (CPRA §1798.185)

Businesses that process SPI that presents **significant risk to consumer privacy or security** are required to conduct annual cybersecurity audits and submit risk assessments to the CPPA. Biometric data and health data are the categories most likely to trigger this requirement. You should:

1. Conduct a Privacy Risk Assessment covering all three SPI categories.
2. Evaluate whether processing presents "significant risk" (biometrics almost certainly does).
3. Prepare to submit assessments to the CPPA once final rulemaking is completed.

---

## 3. Required Disclosures

### A. Privacy Notice at Collection

At or before collecting SPI, you must notify consumers of:

- The **specific categories of SPI** collected (list all three: precise geolocation, health data, biometric/facial recognition data)
- The **purposes** for which each category is collected and used
- Whether SPI is **sold or shared** with third parties (and to which categories of third parties)
- The **retention period** for each SPI category, or the criteria used to determine it
- A link to your full privacy policy
- The consumer's **right to limit** use of SPI (if applicable)

This notice must appear in the app at the point of data collection — ideally at first launch (for geolocation and account creation), at wearable integration setup (for health metrics), and before biometric enrollment (for facial recognition).

### B. Full Privacy Policy

Your privacy policy must include the following SPI-specific disclosures:

1. **Categories of SPI collected**: Precise geolocation, health/medical information, biometric information (facial recognition)
2. **Purposes of collection** for each SPI category
3. **Whether SPI is sold or shared**: Explicitly state yes or no for each category, and if yes, to what categories of third parties
4. **Consumer rights** with respect to SPI, including the right to limit use under §1798.121
5. **How to exercise the Right to Limit SPI**: Instructions, contact method, and the 15-business-day response timeline
6. **Retention periods** for each SPI category
7. **"Limit the Use of My Sensitive Personal Information" link** or equivalent "Your Privacy Choices" combined link

### C. At-Collection Notices for Each Data Type (Recommended Approach)

**Precise Geolocation** (at first location permission request):
> "We collect your precise location to [purpose]. This is sensitive personal information under California law. You may limit our use of your location data. [Link to SPI limitation]. We retain location data for [period/criteria]. See our Privacy Policy."

**Health Metrics** (at wearable integration setup):
> "By connecting your wearable device, you allow us to collect health metrics including [list]. This is sensitive personal information. We use it to [purpose] and retain it for [period]. You have the right to limit how we use this data. [Link]."

**Biometric Facial Recognition** (before enrollment):
> "We collect your facial recognition data to authenticate your identity. This is biometric sensitive personal information under California law. We [store/do not store] raw facial scans. Derived templates are retained for [period or until account deletion]. You may [opt out of biometric authentication / use an alternative]. [Link to SPI limitation and privacy policy]."

---

## 4. Standard CCPA/CPRA Obligations That Also Apply

In addition to SPI-specific requirements, all standard CPRA obligations apply to this data:

| Obligation | What Is Required |
|---|---|
| Right to Know | Respond within 45 days to requests for categories of SPI collected, sources, purposes, and third parties |
| Right to Delete | Delete SPI upon verified consumer request (with applicable exceptions) within 45 days |
| Right to Correct | Correct inaccurate SPI within 45 days |
| Right to Opt-Out of Sale/Sharing | Honor opt-out immediately if SPI is sold or shared |
| "Do Not Sell or Share" link | Required on homepage if PI is sold or shared |
| Global Privacy Control (GPC) | Must honor GPC signals as a valid opt-out of sale/sharing |
| Non-Discrimination | Cannot deny app features or charge more because a consumer exercises privacy rights |
| Service Provider Contracts | Any vendor receiving SPI (wearable platform API provider, cloud storage, analytics) must have a compliant service provider or contractor agreement |

---

## 5. Practical Compliance Action Plan

**Immediate (before or at launch)**:
- [ ] Update privacy policy to enumerate all three SPI categories with purposes, third-party disclosure, and retention periods
- [ ] Add at-collection notices at the point each SPI type is collected in the app
- [ ] Implement "Limit the Use of My Sensitive Personal Information" link/mechanism
- [ ] Verify all wearable integration and analytics vendors are under compliant service provider agreements

**Near-term (within 30-60 days)**:
- [ ] Audit data flows for each SPI category to ensure data minimization compliance
- [ ] Establish and document retention schedules for geolocation, health, and biometric data
- [ ] Build or configure a consumer request intake system capable of handling Right to Limit SPI (15-business-day response) alongside standard rights requests
- [ ] Implement GPC signal detection and opt-out in the app

**Ongoing**:
- [ ] Conduct annual cybersecurity audit given SPI processing (especially biometrics)
- [ ] Prepare Privacy Risk Assessments for CPPA submission when final rules are issued
- [ ] Review SPI processing purposes annually to confirm they remain within disclosed and permitted uses

---

## 6. Penalty Exposure Summary

Failure to comply with SPI obligations carries significant exposure under CPRA:

- **Unintentional violations**: Up to $2,500 per violation
- **Intentional violations**: Up to $7,500 per violation
- **Biometric data breaches**: Private right of action for $100–$750 per consumer per incident (class action eligible)

Given that biometric facial recognition data is involved, any security incident involving unauthorized access to facial recognition data could trigger both CPPA enforcement and class action litigation. This makes the cybersecurity audit and risk assessment obligations particularly important for your app.

---

*This analysis is based on the California Privacy Rights Act (CPRA) as amended, Cal. Civ. Code §1798.100 et seq., effective January 1, 2023, and CPPA guidance and rulemaking as of April 2026. Consult qualified California privacy counsel for advice specific to your business circumstances.*
