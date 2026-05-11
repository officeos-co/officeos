# GDPR to CCPA/CPRA Gap Analysis: Additional Steps Required

## Overview

Good news: a mature GDPR programme gives you a substantial head start on CCPA/CPRA compliance. The GDPR is generally the more demanding of the two frameworks, and most of its core obligations map directly onto California law. However, CCPA/CPRA contains several concepts and mechanisms with no GDPR equivalent that you will need to implement from scratch.

---

## What Your GDPR Programme Already Covers

The following CCPA/CPRA obligations are satisfied (or substantially satisfied) by your existing GDPR controls:

| CCPA/CPRA Obligation | Covered by GDPR Programme |
|---|---|
| Privacy notice at collection | Art. 13/14 notices |
| Comprehensive privacy policy | Privacy notice |
| Right to access/know | DSAR process |
| Right to delete | Right to erasure process |
| Right to correct | Right to rectification |
| Right to data portability | Art. 20 portability |
| Data minimisation and purpose limitation | Art. 5 principles |
| Retention schedules | Art. 5(1)(e) storage limitation |
| Processor/service provider agreements | Art. 28 DPAs |
| Security measures | Art. 32 technical/organisational measures |

---

## Additional Steps Needed for CCPA/CPRA

### 1. "Do Not Sell or Share My Personal Information" Link and Opt-Out Workflow

**No GDPR equivalent.** CCPA/CPRA defines "sale" broadly (monetary or other valuable consideration) and adds "sharing" (disclosure for cross-context behavioural advertising, even without payment). You must:

- Add a conspicuous "Do Not Sell or Share My Personal Information" link to your homepage and privacy policy
- Build a consumer-facing opt-out form or mechanism
- Update your consent/preference management platform to process opt-outs within 15 business days
- Propagate opt-outs to all downstream service providers and contractors
- Implement a 12-month moratorium on re-asking opted-out consumers to reconsider

**Key implication for ad tech:** If you use third-party ad exchanges, DMPs, or retargeting partners who receive cookie IDs or device identifiers, those disclosures will likely constitute a "sale" or "sharing" under CCPA/CPRA and must be stopped for opted-out California consumers.

### 2. Global Privacy Control (GPC) Signal Honoring

**No GDPR equivalent.** The CPPA has confirmed that businesses must recognise the GPC browser signal as a valid opt-out of sale/sharing — it must be processed automatically, not just as a preference signal. This requires:

- Technical implementation to detect GPC headers in browser requests
- Automatic suppression of data sharing/selling for consumers sending GPC signals
- This must work even if the consumer has not submitted a manual opt-out request

### 3. "Limit the Use of My Sensitive Personal Information" Link and Workflow

**Partial GDPR overlap, but different mechanism.** GDPR requires explicit consent (opt-in) for special category data. CCPA/CPRA uses an opt-out model for most SPI categories — meaning you can use SPI by default but must stop on request.

CPRA's SPI categories include some items not in GDPR special categories:
- Precise geolocation (within 1/4 mile)
- Account credentials (login + security code)
- Contents of consumer communications (mail, email, texts)
- Social Security numbers and government IDs

You must:
- Add a "Limit the Use of My Sensitive Personal Information" link (can be combined with the Do Not Sell link)
- Process SPI limitation requests within **15 business days** (faster than the 45-day standard deadline)
- Restrict SPI use to only the permitted purposes (service delivery, safety/security, transient use, quality verification)
- Propagate limitations to service providers and contractors

### 4. Vendor Contract Review and Reclassification

**GDPR Art. 28 DPAs are necessary but not sufficient.** CCPA/CPRA introduces a three-tier vendor taxonomy — Service Providers, Contractors (CPRA addition), and Third Parties — each with different rules:

- **Service Providers:** Process PI on your behalf; contract must prohibit further use or sale; not a "sale"
- **Contractors:** Receive PI under contract; must certify compliance; must not sell or share
- **Third Parties:** Any other recipient — disclosure is likely a "sale" unless an exception applies

Action required:
- Audit all current processor/vendor relationships and reclassify under CCPA/CPRA taxonomy
- Update contracts with service providers and contractors to include CCPA/CPRA-specific clauses: purpose limitation, prohibition on resale, obligation to honour consumer deletion requests, audit rights, certification of compliance
- Note: your GDPR DPAs will not automatically satisfy these requirements — specific CCPA/CPRA language is needed

### 5. Minors' Opt-In Consent for Sale/Sharing

**Different age thresholds from GDPR.** CCPA/CPRA requires affirmative opt-in before selling or sharing PI of consumers aged 13–15, and parental consent for under-13s. This operates independently of your GDPR age-of-consent controls (which vary by Member State from 13–16 for consent-based processing).

Action required:
- Implement age-screening for consumer-facing products
- Build opt-in workflows for 13–15 year-olds and parental consent mechanisms for under-13s
- Ensure ad tech and data sharing is suppressed for minors absent opt-in

### 6. Financial Incentive and Loyalty Programme Disclosures

If you operate loyalty programmes, premium tiers, or offer any benefit in exchange for consumers' PI:

- You must disclose the financial incentive in your privacy policy
- Provide a "reasonably related" justification (the incentive must be proportionate to the value of the PI)
- Obtain affirmative opt-in consent from consumers with a clear description of material terms
- Allow consumers to withdraw at any time without penalty

This has no direct GDPR equivalent.

### 7. Annual Business Threshold Verification

CCPA/CPRA applicability is re-evaluated each year based on:
- Annual gross revenues exceeding $25 million, OR
- Buying, selling, receiving, or sharing PI of 100,000+ consumers or households, OR
- 50%+ of annual revenues from selling or sharing PI

You should build an annual process to confirm threshold status and update your compliance posture if thresholds change.

### 8. CPPA Rulemaking — Automated Decision-Making and Cybersecurity Audits

**Pending but plan now.** The California Privacy Protection Agency (CPPA) is developing regulations on:

- **Automated decision-making (ADM):** Expected to require opt-out rights and access to decision logic for significant automated decisions. Map this to your existing Art. 22 GDPR controls — you will likely need US-specific opt-out mechanisms rather than relying on the GDPR's exemption framework
- **Annual cybersecurity audits:** Businesses processing PI that presents "significant risk" will need to conduct and potentially submit annual cybersecurity audits to the CPPA
- **Data broker registration:** If you sell PI without a direct relationship with the consumer, California data broker registration may be required

---

## Where the Two Laws Differ Most Significantly

### 1. Lawful Basis vs. No Lawful Basis

**This is the most fundamental structural difference.** GDPR prohibits processing unless you have a lawful basis (consent, contract, legitimate interests, etc.). CCPA/CPRA imposes no such requirement — businesses may collect and use PI without any legal justification, provided they give notice. This means:

- Your GDPR legitimate interests assessments (LIAs) and consent records have no direct CCPA/CPRA equivalent
- CCPA/CPRA compliance does not require you to identify or document a basis for processing
- However, you still cannot use PI for undisclosed purposes (purpose limitation applies)

### 2. Opt-In (GDPR) vs. Opt-Out (CCPA/CPRA) Model

GDPR is fundamentally opt-in — consent must be freely given, specific, informed, and unambiguous before processing. CCPA/CPRA is fundamentally opt-out — consumers can use their PI unless and until they say stop. The only opt-in requirements under CCPA/CPRA are for minors' data and financial incentive programmes.

This creates an asymmetry: for California consumers, you may be doing more processing by default than for EU/EEA consumers, but you face a different enforcement risk (action after the fact rather than lack of prior consent).

### 3. "Sale" and "Sharing" — Concepts with No GDPR Equivalent

GDPR treats disclosure to a third party as a separate processing activity governed by lawful basis and purpose limitation. CCPA/CPRA creates the specific concept of "sale" (for any valuable consideration, not just money) and "sharing" (for cross-context behavioural advertising) with dedicated opt-out rights. Many data flows that are routine under GDPR may constitute a "sale" or "sharing" under CCPA/CPRA.

### 4. Sensitive Personal Information — Broader and Different Categories

CPRA's SPI category is wider than GDPR's special categories in specific ways:
- Precise geolocation is SPI under CPRA but not a special category under GDPR (though it may require legitimate interests assessment)
- Account credentials and communication contents are SPI under CPRA with no GDPR special category equivalent
- The treatment mechanism is also different: GDPR requires explicit consent; CPRA provides an opt-out right (businesses can use SPI unless the consumer limits it)

### 5. Response Timelines

| Action | CCPA/CPRA | GDPR |
|---|---|---|
| Standard rights requests | 45 days + 45-day extension | 1 month + 2-month extension |
| SPI limitation requests | 15 business days | N/A |
| Opt-out of sale/sharing | Immediate (update within 15 business days) | N/A |

CCPA/CPRA timelines for standard requests are somewhat shorter than GDPR (45 days vs. ~30 calendar days, but with a longer extension available). The SPI limitation 15-business-day deadline has no GDPR analogue.

### 6. Private Right of Action — Scope

GDPR allows data subjects to bring claims for any infringement of their rights, and compensation for material or non-material damage. CCPA/CPRA's private right of action is narrowly scoped to **data breaches only** — consumers cannot sue for general CCPA/CPRA non-compliance. However, class actions for data breaches with statutory damages of $100–$750 per consumer per incident create very significant financial exposure.

### 7. Enforcement Architecture

| Aspect | CCPA/CPRA | GDPR |
|---|---|---|
| Enforcement body | California Privacy Protection Agency (CPPA) | 27+ national DPAs |
| Penalty calculation | Per violation (not global turnover) | % of global annual turnover |
| Maximum penalty | $7,500 per intentional violation | €20M or 4% global turnover |
| Cure period | 30 days (for AG actions) | No formal cure period |

GDPR's turnover-based penalties create larger maximum exposure for large organisations. CCPA/CPRA's per-violation model can add up quickly in a mass data incident (especially involving minors, where $7,500 per record applies automatically).

### 8. International Data Transfers

GDPR imposes strict transfer restriction rules for data leaving the EEA (SCCs, BCRs, adequacy decisions). CCPA/CPRA has **no equivalent** — there are no restrictions on cross-border data transfers under California law. Your GDPR transfer mechanisms are relevant only for EU/EEA data.

---

## Prioritised Remediation Roadmap

| Priority | Action | Rationale |
|---|---|---|
| **Immediate** | Add "Do Not Sell or Share" link and opt-out workflow | Core CCPA right; enforcement focus |
| **Immediate** | Implement GPC signal detection and processing | CPPA enforcement confirmed; court decisions support |
| **Short-term** | Audit and update vendor contracts for CCPA/CPRA requirements | Liability risk from non-compliant service providers |
| **Short-term** | Add SPI limitation link and 15-day response workflow | CPRA right; penalty exposure |
| **Short-term** | Review ad tech stack for "sale/sharing" data flows | Likely highest-risk area for most organisations |
| **Medium-term** | Implement minors' opt-in controls if applicable | $7,500/violation for minors; always intentional |
| **Medium-term** | Add financial incentive disclosures if applicable | Required for loyalty/incentive programmes |
| **Ongoing** | Monitor CPPA rulemaking on ADM and cybersecurity audits | Significant new obligations pending |
| **Annual** | Verify business thresholds | Applicability can change year to year |

---

*This analysis is based on CCPA (Cal. Civ. Code §1798.100 et seq.) and CPRA (effective January 1, 2023). CPPA rulemaking is ongoing and requirements in areas such as automated decision-making and cybersecurity audits may be finalised after this date. Consult qualified US privacy counsel before implementing compliance measures.*
