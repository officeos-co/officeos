# PCI DSS Breach Notification Obligations Following a Card Processing System Compromise

**PCI DSS Version: v4.0.1 (current, June 2024)**

---

## Immediate Priority: Contain and Preserve Evidence

Before addressing notifications, take immediate containment steps — but **do not destroy evidence** (do not wipe or reimage compromised systems without forensic imaging first):

1. Isolate compromised systems from the network (segment, but do not power off if avoidable)
2. Preserve all logs and forensic artifacts
3. Engage your internal incident response team per your Incident Response Plan (Req 12.10)
4. Engage a PCI Forensic Investigator (PFI) — your acquirer will likely mandate this

---

## Who to Notify and When

PCI DSS does not itself specify exact notification timeframes in hours/days in the same way that GDPR or US state breach laws do. However, the card brand rules, acquirer agreements, and PCI DSS requirements together create a clear set of obligations:

### 1. Your Acquiring Bank (Acquirer) — **Immediately / Within 24 Hours**

**Your most urgent notification.** Your merchant agreement with your acquirer (the bank that processes your card payments) almost certainly requires immediate notification of a suspected breach. Most acquirer agreements specify notification within **24 hours** of discovering a potential compromise.

- Contact your acquirer's security/risk team by phone first, then follow up in writing
- Your acquirer will coordinate with the card brands (Visa, Mastercard, etc.) on your behalf
- Failing to notify your acquirer promptly can result in accelerated fines and penalties

**PCI DSS Reference:** Req 12.10.1 requires an incident response plan that includes notification procedures. Your acquirer is the first mandatory notification target.

---

### 2. Card Brands (Visa, Mastercard, Amex, Discover, JCB) — **Via Acquirer, Typically Within 24–72 Hours**

In most cases, your acquirer will notify the card brands on your behalf. However, if you are a **Level 1 service provider** or if your acquirer instructs you to contact card brands directly, you may need to reach out independently.

**Visa**: Requires notification within 24 hours of discovery (via acquirer for merchants)
**Mastercard**: Requires notification within 24 hours of discovery (via acquirer for merchants)

The card brands will:
- Determine whether an Account Data Compromise (ADC) event has occurred
- Identify the window of exposure and the range of potentially compromised account numbers (the "common point of purchase" or CPP analysis)
- Issue fines and assessments based on the number of compromised accounts
- Mandate specific remediation steps

---

### 3. PCI Forensic Investigator (PFI) — **Within 24–72 Hours of Discovery**

The card brands and your acquirer will require you to engage a **PCI Forensic Investigator (PFI)** — a QSA firm approved to conduct forensic investigations under card brand rules. The PFI will:

- Conduct a forensic investigation of the compromise
- Determine the scope of the breach (what data was accessed, for what time period)
- Produce a PFI Report for the card brands
- Confirm whether your systems were PCI DSS compliant at the time of the breach

You typically cannot choose to forego a PFI engagement once the card brands are involved. Costs are typically borne by the merchant.

---

### 4. Law Enforcement — **As Appropriate**

- Report to the **FBI Cyber Division** or local law enforcement if criminal activity is suspected (which it typically is in a payment card breach)
- In the US, the **Secret Service** (Electronic Crimes Task Force) also investigates payment card fraud
- Law enforcement engagement is not strictly required by PCI DSS but is strongly recommended and may be required by your legal counsel
- A law enforcement hold may affect your forensic preservation obligations

---

### 5. Affected Cardholders — **Typically Via the Card Brands**

Under PCI DSS, **you do not directly notify cardholders**. The card brands notify issuing banks, who in turn notify cardholders and reissue compromised cards. However:

- US state breach notification laws (all 50 states have them) may require direct consumer notification if personal information was exposed alongside card data
- Consult legal counsel for applicable state/federal breach notification obligations (e.g., CCPA in California, NY SHIELD Act, etc.)
- GDPR notification within **72 hours** to your supervisory authority if EU residents' data may be affected

---

### 6. Regulators — **Dependent on Jurisdiction and Data Types**

Depending on your industry and jurisdiction:
- **GDPR (EU/UK)**: 72-hour notification to the relevant Data Protection Authority if personal data was involved
- **US State AGs**: Some states require notification to the Attorney General
- **SEC**: If you are a publicly traded company, material breaches may trigger disclosure obligations under SEC cybersecurity rules (effective December 2023)
- **FTC**: Relevant if you are subject to the FTC Safeguards Rule (financial institutions/fintech)

---

## PCI DSS Incident Response Requirements (Requirement 12.10)

Your organisation must have a documented Incident Response Plan under **Requirement 12.10**. Key sub-requirements:

| Req | Requirement | Notes |
|-----|-------------|-------|
| **12.10.1** | IR plan exists and is ready to be activated immediately upon system breach | Must include card brand and acquirer notification procedures |
| **12.10.2** | IR plan is reviewed and tested at least once every 12 months | Testing must cover payment card incident scenarios |
| **12.10.3** | Specific personnel are designated, trained, and available 24/7 to respond | |
| **12.10.4** | IR personnel receive appropriate and ongoing training | |
| **12.10.4.1** (new v4.0) | Training for IR personnel at least every 12 months | |
| **12.10.5** | Alerts from security monitoring systems are included in the IR plan | |
| **12.10.6** | IR plan is modified and evolved based on lessons learned and industry developments | |
| **12.10.7** (new v4.0) | IR procedures exist for detection of and response to payment page (e-commerce) attacks | Specifically addresses web skimming / Magecart-style attacks |

---

## Financial Consequences of a Breach

Understanding the financial stakes:

- **Card brand fines**: Visa and Mastercard can impose fines ranging from **$5,000 to $100,000 per month** during a non-compliance period; after a confirmed breach, assessments are tied to the number of compromised accounts
- **Fraud reimbursement**: You may be liable for fraudulent charges made on compromised cards
- **Card reissuance fees**: Issuing banks may seek reimbursement for card reissuance costs (typically $3–15 per card)
- **PFI investigation costs**: PFI investigations typically cost **$20,000–$100,000+**
- **Level 1 uplift**: After a confirmed breach, you may be elevated to **Merchant Level 1** regardless of transaction volume, requiring an annual QSA ROC going forward

---

## Summary: Notification Checklist

| Notify | Timeframe | Method |
|--------|-----------|--------|
| **Acquiring bank** | Immediately / within 24 hours | Phone + written |
| **Card brands** (via acquirer) | Within 24–72 hours | Via acquirer |
| **PFI** (engage forensic investigator) | Within 24–72 hours | Direct engagement |
| **Law enforcement (FBI/Secret Service)** | As soon as practical | Report online or via local field office |
| **Data Protection Authority (GDPR)** | Within 72 hours if EU personal data involved | Via national DPA portal |
| **State AGs / consumers (US breach laws)** | Per applicable state law (typically 30–72 days) | Via legal counsel |
| **SEC (if public company)** | 4 business days after materiality determination | Form 8-K |

---

> **Disclaimer:** This guidance is based on PCI DSS v4.0.1 (PCI SSC, June 2024) and general card brand rules as publicly known. Notification obligations are also governed by your acquirer agreement and applicable law. Engage legal counsel and a PCI Forensic Investigator immediately upon discovering a potential breach. This does not constitute legal advice.
