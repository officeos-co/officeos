# DPDPA Cross-Border Data Transfer Requirements

## Overview

India's Digital Personal Data Protection Act (DPDPA) includes provisions governing the transfer of personal data outside India. Understanding these requirements is essential for any organization that processes Indian user data on foreign servers.

---

## DPDPA Transfer Framework

The DPDPA takes a somewhat different approach to cross-border transfers compared to GDPR. Rather than a positive "whitelist" approach requiring adequacy decisions or specific mechanisms, the DPDPA uses a framework where the Central Government can restrict transfers to specific countries.

### Key Provisions

- The default position under DPDPA allows transfers of personal data outside India, subject to any restrictions the Central Government may impose
- The Central Government has the power to **restrict or prohibit transfers** to specific countries or territories
- Transfers to countries that have not been restricted are generally permissible
- The implementing rules and government notifications will specify which countries (if any) face restrictions

### Current State of the Law

The DPDPA's transfer provisions are still being fully implemented through subsidiary regulations and government notifications. As of the time of this writing:
- The framework for identifying restricted countries is being established
- Many specific operational requirements remain to be notified by the government
- Organisations should monitor developments from MeitY (Ministry of Electronics and Information Technology)

---

## Are Transfers to US Servers Permissible?

The answer depends on current government notifications, which continue to evolve. Under the current framework:

- If the United States has **not been designated as a restricted country**, transfers to US servers should generally be permissible
- However, you should verify the current list of restricted countries with MeitY or through legal counsel
- Future restrictions could affect your ability to transfer data to the US

**Recommendation:** Assume transfers may face future restrictions and proactively implement safeguards.

---

## Comparison with GDPR

| Aspect | GDPR | DPDPA |
|--------|------|-------|
| Default position | Restrictive — transfers require a positive mechanism (adequacy, SCCs, BCRs, etc.) | More permissive — transfers generally allowed unless restricted |
| Transfer mechanism | Adequacy decisions, Standard Contractual Clauses (SCCs), Binding Corporate Rules, etc. | No equivalent prescribed mechanism yet; government can restrict specific countries |
| Current US transfers | Require SCCs or EU-US Data Privacy Framework | Currently likely permissible (no known restriction on US) |
| Documentation | Detailed transfer records, SCCs required | Less prescriptive currently |
| Uncertainty | Lower (well-established framework) | Higher (implementing rules still developing) |

### Key Differences

**DPDPA is less complex than GDPR for transfers today.** Under GDPR, every cross-border transfer requires a positive transfer mechanism — adequacy decision, SCCs, or other Article 46 safeguards. Under DPDPA, the burden shifts: you can transfer unless specifically restricted.

**However, DPDPA future restrictions could be significant.** India may restrict transfers to countries it considers to have inadequate data protection, potentially including countries like the US. This is an evolving area.

---

## Data Localization Concerns

Beyond general transfer restrictions, some categories of data processed by organizations designated as "Significant Data Fiduciaries" may face **data localization requirements** — meaning certain data must be stored and processed within India. This is a separate concern from general transfer restrictions and depends on:

- Whether your organization is designated as a Significant Data Fiduciary
- Which categories of data are subject to localization requirements (to be specified in notifications)

---

## Recommended Steps

1. **Monitor MeitY notifications** — keep track of any countries added to the restricted list

2. **Document your data flows** — map exactly what Indian personal data is transferred to your US servers, for what purposes, and under what security conditions

3. **Implement contractual safeguards** — even if not currently required, having data processing agreements with your US-side infrastructure and vendors is good practice and may become required

4. **Assess data localization** — determine whether you may be designated as a Significant Data Fiduciary; if so, budget for potential data localization infrastructure in India

5. **Build flexibility into your architecture** — design your systems so that Indian user data could be kept in India if required

6. **Consult legal counsel** — given the evolving nature of DPDPA transfer rules, work with Indian data protection lawyers to stay current

---

## Bottom Line

Currently, transferring Indian user data to your US servers is likely permissible under DPDPA, but this may change. Unlike GDPR (which requires SCCs or equivalents), the DPDPA's transfer framework is currently more permissive, but organisations should not be complacent — restrictions could be imposed at any time, and data localization requirements for certain organizations could require significant infrastructure changes.
