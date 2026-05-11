# DPDPA vs. GDPR Consent Requirements: Key Differences for Compliance Teams

## Side-by-Side Comparison

| Dimension | GDPR (Art. 4(11) + Art. 7) | DPDPA (Section 6) | Implication |
|-----------|---------------------------|-------------------|-------------|
| **Consent standard** | Freely given, specific, informed, unambiguous | Free, specific, informed, **unconditional**, unambiguous | DPDPA adds "unconditional" — a stricter element |
| **Mechanism** | Clear affirmative action; no pre-ticked boxes | Clear affirmative action; no pre-ticked boxes | Same standard |
| **Bundled consent** | Problematic but not always fatal (contract performance may save it) | **Explicitly prohibited** — consent may not be bundled with service provision | Stricter under DPDPA |
| **Withdrawal** | Must be as easy to withdraw as to give; withdrawal does not affect prior lawfulness | Same: withdrawal as easy as giving; Section 6(4) | Equivalent |
| **Effect of withdrawal** | Data Fiduciary must stop processing; erasure unless another basis applies | Data Fiduciary must cease processing and erase data (unless retention required by law) | Broadly equivalent |
| **Granularity** | Separate consent per purpose (specificity requirement) | Separate consent per purpose (specificity requirement) | Equivalent |
| **Pre-ticked boxes** | Invalid | Invalid | Equivalent |
| **Silence/inaction** | Invalid | Invalid | Equivalent |
| **Parental consent (minors)** | Required under 16 (member states may lower to 13) | Required under **18** years; must be verifiable (Rule 12) | DPDPA is stricter — higher age threshold |
| **Consent records** | Must be demonstrable | Must be demonstrable | Equivalent |

---

## Critical Differences Your Compliance Team Must Know

### 1. "Unconditional" — A New Element Not in GDPR

GDPR requires consent to be "freely given, specific, informed, and unambiguous." The DPDPA (Section 6) adds a fifth element: **unconditional**. This means:

- Consent cannot be made conditional on accepting other terms or services
- Any condition attached to the act of giving consent invalidates it
- This goes further than GDPR's "freely given" standard

**Practical impact:** A GDPR "freely given" analysis might permit some conditions (e.g., access to a premium feature conditioned on consent to analytics). Under DPDPA's "unconditional" standard, such arrangements face greater legal risk.

### 2. Bundled Consent Is Explicitly Prohibited

Under GDPR, bundling consent with service access is disfavoured but may survive if the processing is genuinely necessary for the contract (lawful basis shifts to Art. 6(1)(b)). Under the DPDPA:

- Consent **cannot** be bundled with service provision
- The classic "by using this app, you agree to our privacy policy" model is **non-compliant**
- There is no equivalent contractual necessity basis to rescue bundled consent

**Practical impact:** If your current Indian consent mechanism says "by registering, you consent to X," you need to redesign this into a separate, standalone consent interaction.

### 3. Only Two Lawful Bases — No "Legitimate Interests"

This is the most operationally significant difference:

| GDPR Lawful Bases | DPDPA Equivalent |
|-------------------|-----------------|
| Consent (Art. 6(1)(a)) | Consent (Section 6) |
| Contract performance (Art. 6(1)(b)) | Partial equivalent only under Section 7(a) (voluntary data for a purpose) or Section 7(e) (employment) |
| Legal obligation (Art. 6(1)(c)) | Section 7(d) — legal obligations under Indian law |
| Vital interests (Art. 6(1)(d)) | Section 7(g) — medical emergencies/disasters |
| Public task (Art. 6(1)(e)) | Section 7(b)/(c) — State functions/subsidies |
| **Legitimate interests (Art. 6(1)(f))** | **No equivalent — does not exist** |

**What this means for consent:** Because "legitimate interests" does not exist under the DPDPA, processing activities that GDPR teams currently justify on that basis — analytics, fraud detection, marketing to existing customers, B2B data sharing — must either:
- Be mapped to one of the **8 enumerated Section 7 legitimate uses** (exhaustive), OR
- Be based on explicit **consent under Section 6**

Most commercial analytics and B2C marketing will require consent.

### 4. Notice Must Come First (Section 5)

DPDPA consent is only valid if given **after** receiving the Section 5/Rule 3 notice. The notice must:
- Be **standalone** — not embedded in terms and conditions
- Be in plain, comprehensible language
- Contain itemised list of data collected, purposes, recipients, retention period, rights, Board complaint pathway, and withdrawal mechanism

Consent obtained before providing a compliant notice is invalid. This is similar to GDPR's informed consent requirement but the DPDPA notice requirements are more specifically prescribed in Rule 3.

### 5. Withdrawal Must Be "As Easy As Giving Consent" — Technically Tested

GDPR states withdrawal must be as easy as giving consent — a standard frequently ignored in practice. DPDPA Section 6(4) codifies the same requirement, and compliance teams should expect the Data Protection Board of India (DPBI) to scrutinise this technically:

- If consent was given via a single checkbox: withdrawal must be achievable in a single action
- Multi-step withdrawal processes (e.g., emailing a support team) against one-click consent will likely be **non-compliant**
- In-app consent withdrawal toggles are the expected implementation

### 6. Consent Records

Both GDPR and DPDPA require organisations to be able to demonstrate valid consent. Under the DPDPA, this includes:
- A record of when consent was given
- The notice that was presented at the time
- The specific purpose consented to
- Records of any withdrawal and subsequent erasure actions

---

## Practical Compliance Checklist for Your Team

| Check | GDPR Status | DPDPA Status |
|-------|-------------|-------------|
| No pre-ticked boxes | Required | Required |
| Separate consent per purpose | Required | Required |
| Standalone notice before consent | Required (informed) | Required (Rule 3 standalone notice) |
| No bundled consent with service access | Discouraged (may survive if contract basis exists) | **Prohibited** |
| One-click withdrawal (if one-click consent given) | Standard | **Explicitly required (Section 6(4))** |
| Age gate at 18 for children | 16 (varies by member state) | **18 — fixed, no variation** |
| Verifiable parental consent for children | Required for under-16 | Required for under-18; verified via DigiLocker/government tokens (Rule 12) |
| Replace "legitimate interests" with consent or valid basis | N/A | **Action required** |

---

## Summary for Compliance Teams

The DPDPA consent standard is broadly similar to GDPR but with three key tightening elements:
1. The explicit **"unconditional"** requirement makes bundled consent clearly unlawful
2. The **absence of legitimate interests** means many currently non-consent-based processing activities will need redesign
3. The **18-year children's threshold** (vs. GDPR's 16) requires reconfigured age gates for India specifically

For organisations with a mature GDPR consent programme, the primary remediation work is: (a) unbundling consent from service access, (b) auditing all legitimate-interests-based processing, and (c) updating age verification for the Indian market.
