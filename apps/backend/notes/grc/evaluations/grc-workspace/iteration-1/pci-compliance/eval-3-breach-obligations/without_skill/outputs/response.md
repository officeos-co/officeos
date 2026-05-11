# PCI DSS Breach Notification Obligations

## Overview

A potential compromise of your card processing system triggers multiple notification obligations across different parties. PCI DSS itself doesn't prescribe exact hour-by-hour notification timelines the way GDPR does, but your acquirer agreements and card brand rules do. Time is critical — act immediately.

## Step 1: Contain First, Then Notify

Before notifications, take immediate action to limit the damage:
- Isolate compromised systems (do not destroy evidence — preserve logs and system images)
- Do not power off systems without consulting a forensic investigator first
- Activate your incident response plan

## Who to Notify

### 1. Your Acquiring Bank (Most Urgent — Within 24 Hours)

Your acquiring bank (the bank that processes your card transactions) is your **most important first call**. Your merchant agreement almost certainly requires you to notify them within 24 hours of discovering a suspected breach.

- Call your acquirer's risk or security team immediately
- Follow up in writing
- Your acquirer will then communicate with the card networks on your behalf

Failure to notify your acquirer promptly can result in significantly increased fines and penalties.

### 2. Card Brands (Visa, Mastercard, Amex, etc. — via Acquirer)

In most cases, your acquirer notifies the card brands for you. Card brands typically expect notification within 24-72 hours of discovery. They will:
- Investigate whether a confirmed Account Data Compromise occurred
- Identify the potentially compromised card numbers and timeframe
- Assess fines and penalties
- Require you to engage a forensic investigator

### 3. PCI Forensic Investigator (PFI) — Engage Immediately

The card brands will require you to hire a **PCI Forensic Investigator (PFI)**, which is a specially qualified firm approved by the card brands. The PFI will:
- Conduct an independent forensic investigation
- Determine what data was compromised and for what period
- Prepare a report for the card brands
- Confirm your compliance status at the time of the incident

You typically cannot avoid this requirement once a potential breach is reported. Budget for significant costs.

### 4. Law Enforcement

While not strictly required by PCI DSS, you should:
- Report to the **FBI** (via the Internet Crime Complaint Center — IC3.gov) or local FBI field office
- Consider reporting to the **Secret Service Electronic Crimes Task Force** (they investigate payment card fraud)
- Consult your legal counsel about obligations to report to specific agencies

### 5. Affected Cardholders — Indirect

You typically do **not** directly notify affected cardholders under PCI DSS. Instead:
- The card brands notify the issuing banks
- Issuing banks notify cardholders and reissue cards
- However, US state breach notification laws may require direct consumer notification

### 6. Regulators and Legal Obligations

Depending on your situation:
- **US State Breach Notification Laws**: All 50 US states require breach notification. Timelines vary but are commonly 30-72 days. Many require notification to both affected individuals and the state Attorney General.
- **GDPR**: If any EU residents' card data may have been compromised, notify your supervisory Data Protection Authority within **72 hours** of becoming aware of the breach.
- **SEC Disclosure**: If you're a publicly traded company, material cybersecurity incidents must be disclosed within 4 business days of determining materiality.
- **FTC Safeguards Rule**: May apply if you are considered a financial institution.

## Timeframe Summary

| Party | When to Notify |
|-------|---------------|
| Acquiring bank | Immediately, within 24 hours |
| Card brands | Within 24-72 hours (usually via acquirer) |
| PFI | Engage within 24-72 hours |
| Law enforcement | As soon as practical |
| GDPR supervisory authority | Within 72 hours if EU data affected |
| Consumers (US state law) | Varies by state, typically 30-72 days |
| SEC (if public) | Within 4 business days of materiality determination |

## Financial Consequences

Understanding the stakes:
- **Card brand fines**: Substantial monthly fines during non-compliance
- **Fraud liability**: You may be liable for fraudulent transactions on compromised cards
- **Card reissuance fees**: Issuing banks may recover card replacement costs
- **Investigation costs**: PFI investigations can cost tens of thousands of dollars
- **Compliance escalation**: Following a breach, you may be required to undergo a full QSA audit annually going forward, regardless of your transaction volume

## Immediate Action Checklist

1. Activate incident response plan
2. Isolate compromised systems (preserve evidence)
3. Call acquiring bank within 24 hours
4. Engage a PCI Forensic Investigator
5. Contact legal counsel immediately
6. Document all response actions with timestamps
7. Do not make public statements without legal approval
8. Do not destroy any evidence
