# CCPA/CPRA Analysis: Ad Tech Platform — Sale and Sharing of Personal Information

## Short Answer

Yes. Disclosing your website visitors' cookie IDs and browsing data to an ad tech platform almost certainly constitutes both a **"sale"** and **"sharing"** under CCPA/CPRA. Both triggers carry distinct compliance obligations, and you need to act on both.

---

## 1. Why This Is a "Sale" Under CCPA/CPRA

**Definition (Cal. Civ. Code §1798.140(ad)):** A "sale" is the disclosure of personal information to a third party for **monetary or other valuable consideration**.

Cookie IDs are personal information — they identify or can be linked to a specific consumer or household. Browsing data (pages visited, time on page, interactions) is also personal information, and can qualify as sensitive personal information (SPI) if it reveals health, financial, or other SPI-adjacent categories.

The ad tech platform is a **third party** (not a service provider) because:
- It receives the data and uses it for its **own purposes** (building audience segments, profiling, selling ad inventory);
- It is not processing the data solely on your behalf under a contract prohibiting further use.

The "valuable consideration" you receive is the ability to serve targeted ads, which has clear commercial value — even if no cash directly changes hands. CCPA's definition of "sale" is deliberately broad and captures non-monetary exchanges.

**Conclusion:** This arrangement meets the definition of a sale.

---

## 2. Why This Is Also "Sharing" Under CPRA

**Definition (Cal. Civ. Code §1798.140(ah)):** "Sharing" is the disclosure of personal information to a third party for **cross-context behavioral advertising**, even if no consideration is exchanged.

Cross-context behavioral advertising is advertising based on personal information obtained from a consumer's activity across different businesses, websites, or apps. This is exactly what ad tech platforms do: they combine your visitors' browsing data with data from other sources to build profiles and serve targeted ads.

**Conclusion:** Even if you argued there was no "sale," the disclosure would still constitute "sharing" — a CPRA-specific concept added precisely to close this loophole.

---

## 3. What You Must Do

### A. Verify CCPA/CPRA Applicability to Your Business

You are subject to CCPA/CPRA if you are a for-profit business doing business in California and meet at least one of:
- Annual gross revenues exceeding $25 million;
- Annually buy, sell, receive, or share PI of 100,000+ consumers or households;
- Derive 50%+ of annual revenues from selling or sharing consumers' PI.

Website visitor data at scale will likely trigger the second threshold. Confirm which thresholds apply.

### B. Update Your Privacy Policy

Your privacy policy must disclose:
- That you sell and share personal information (cookie IDs, browsing/behavioral data);
- The categories of PI sold or shared (identifiers, internet/network activity);
- The categories of third parties to whom PI is sold or shared (ad networks, demand-side platforms, data brokers);
- A prominent **"Do Not Sell or Share My Personal Information"** link.

### C. Provide a "Do Not Sell or Share My Personal Information" Link

- Place this link prominently on your homepage (and ideally in your site footer and privacy policy).
- The link must lead to a mechanism that stops data from being passed to the ad tech platform for consumers who opt out.
- Honor opt-out requests **immediately** upon receipt.

### D. Honor Global Privacy Control (GPC) Signals

GPC is a browser-level opt-out signal. Per CPPA guidance and enforcement actions:
- You **must** detect and honor GPC signals as a valid opt-out of sale and sharing.
- This requires technical implementation: when a GPC signal is detected, do not fire the ad tech platform's tags/pixels for that user.
- This applies even if the user has not clicked your "Do Not Sell or Share" link.

### E. Implement a Consent Management Platform (CMP) or Tag Manager Logic

Your technical implementation should:
1. Detect GPC signals server-side or via your tag manager before any data is sent to the ad tech platform.
2. Block or suppress ad tech tags for opted-out users.
3. Maintain a record of opt-out signals and honor them for future sessions (do not re-engage after opt-out without re-consent).

### F. Update Your At-Collection Privacy Notice

At or before collecting cookie/browsing data, inform visitors:
- What categories of PI are collected (identifiers, browsing/internet activity);
- That this information may be sold or shared with third parties for targeted advertising;
- How to exercise their opt-out right.

This can be done via a cookie banner or layered notice, but it must be clear and not buried.

### G. Review or Re-Classify the Ad Tech Vendor Relationship

Evaluate whether the ad tech platform could be restructured as a **service provider** rather than a third party:
- This requires a written contract that: (a) limits the platform's use of data strictly to providing the ad service to you; (b) prohibits the platform from selling or sharing data further; (c) prohibits retaining, using, or disclosing data for its own commercial purposes.
- Many ad tech platforms will not agree to these terms, or their business model depends on further use — in which case they cannot be classified as a service provider and the sale/sharing characterization stands.
- If you can restructure the relationship with appropriate contractual controls, the disclosure would no longer constitute a sale or sharing.

### H. Assess Sensitive Personal Information (SPI) Implications

If browsing data reveals health conditions, financial status, political views, or other SPI categories, additional obligations apply:
- Provide a **"Limit the Use of My Sensitive Personal Information"** link;
- Limit use and disclosure of SPI to what is necessary for the disclosed purpose.

### I. Minors

If any of your website visitors are under 16:
- You **cannot** sell or share their PI without opt-in consent (under 13: parent/guardian consent; 13–15: consumer's own consent).
- Violations involving minors' PI are automatically treated as intentional, carrying fines of up to **$7,500 per violation**.

---

## 4. Penalty Exposure

| Violation Type | Penalty |
|---|---|
| Unintentional violation | Up to $2,500 per violation |
| Intentional violation | Up to $7,500 per violation |
| Violation involving minor's PI | Up to $7,500 per violation (always intentional) |

"Per violation" can mean per consumer record or per transaction. Given the volume typical of ad tech operations, aggregate exposure can be substantial.

---

## 5. Priority Action List

| Priority | Action |
|---|---|
| Immediate | Implement GPC signal detection and suppression |
| Immediate | Add "Do Not Sell or Share My Personal Information" link to homepage |
| Short-term | Update privacy policy with sale/sharing disclosures |
| Short-term | Update at-collection cookie/browsing notice |
| Short-term | Assess ad tech vendor contracts — service provider feasibility |
| Short-term | Build opt-out request intake and fulfillment workflow |
| Medium-term | Audit for minor visitors; implement age-gating or consent flows if needed |
| Medium-term | Assess SPI exposure in browsing data categories |

---

## Summary

Your ad tech arrangement almost certainly constitutes both a **sale** (valuable consideration) and **sharing** (cross-context behavioral advertising) under CCPA/CPRA. The key obligations are: (1) disclose it in your privacy policy and at-collection notice, (2) provide a "Do Not Sell or Share" opt-out mechanism, (3) honor GPC signals technically, and (4) explore whether the vendor relationship can be restructured to qualify as a service provider. If restructuring is not feasible, ensure your opt-out infrastructure is in place before continued data flows to the platform.
