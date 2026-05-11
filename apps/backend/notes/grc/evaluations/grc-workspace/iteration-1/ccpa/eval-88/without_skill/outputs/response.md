# CCPA/CPRA Analysis: Ad Tech Platform, Cookie IDs, and Browsing Data

## Short Answer

Yes — sharing website visitors' cookie IDs and browsing data with an ad tech platform for targeted advertising almost certainly constitutes both a **"sale"** and **"sharing"** of personal information under CCPA/CPRA. Both categories trigger opt-out rights and specific compliance obligations.

---

## 1. Does This Qualify as a "Sale" Under CCPA/CPRA?

### Statutory Definition of "Sale"

Under Cal. Civ. Code § 1798.140(ad)(1), a **"sale"** means:

> Selling, renting, releasing, disclosing, disseminating, making available, transferring, or otherwise communicating orally, in writing, or by electronic or other means, a consumer's personal information by the business to a third party **for monetary or other valuable consideration**.

### Cookie IDs Are Personal Information

Cookie IDs (persistent identifiers linked to a browser or device) are explicitly listed as **personal information** under CCPA/CPRA (§ 1798.140(v)(1)) because they can be used to identify or profile an individual over time, even without a name attached.

Browsing data (pages visited, products viewed, time on site, etc.) also qualifies as personal information — it constitutes **internet or other electronic network activity information** under § 1798.140(v)(1)(F).

### The "Valuable Consideration" Threshold

You likely do not pay the ad tech platform — they pay you (or provide ad services in exchange for data access). This reciprocal exchange — data for advertising services, ad revenue, or reduced platform costs — satisfies the **"other valuable consideration"** element. California regulators and the CPPA have confirmed that non-monetary consideration (such as free or subsidized ad services) is sufficient.

**Conclusion: This is likely a "sale."**

---

## 2. Does This Qualify as "Sharing" Under CPRA?

CPRA (effective January 1, 2023) added a distinct category — **"sharing"** — specifically to capture cross-context behavioral advertising:

> Cal. Civ. Code § 1798.140(ah): **"Sharing"** means communicating orally, in writing, or by electronic or other means, a consumer's personal information by the business to a third party **for cross-context behavioral advertising**, whether or not for monetary or other valuable consideration...

### Key Points About "Sharing"

- **No monetary exchange required.** Unlike "sale," sharing applies even when there is zero compensation — closing the "free services" loophole.
- **Cross-context behavioral advertising** means targeting ads based on personal information obtained from activity across businesses, websites, apps, or services other than the one the consumer is intentionally interacting with. Ad tech networks that build profiles across sites clearly fall within this definition.
- Sending cookie IDs and browsing behavior to an ad tech platform to enable targeted advertising across the web is the textbook example of cross-context behavioral advertising.

**Conclusion: This is definitively "sharing."**

---

## 3. Who Is Covered? Applicability Threshold

CCPA/CPRA applies to for-profit businesses that:

1. Have annual gross revenues exceeding $25 million (as of January 1 of the preceding calendar year), OR
2. Buy, sell, receive, or share the personal information of 100,000 or more consumers or households annually, OR
3. Derive 50% or more of annual revenues from selling or sharing consumers' personal information.

If your business meets any of these thresholds and serves California consumers, full compliance obligations apply. Even if you are below the revenue threshold, meeting the 100,000 consumer/household threshold — easily triggered by a website with moderate traffic — brings you into scope.

---

## 4. Required Compliance Actions

### A. Update Your Privacy Notice (Required)

Your privacy policy must clearly disclose, at or before the point of data collection:

- That you sell or share personal information (including cookie IDs and browsing data).
- The **categories** of personal information sold or shared.
- The **categories of third parties** to whom data is disclosed (e.g., "advertising networks," "data analytics partners").
- A description of consumers' rights to opt out of sale/sharing.
- How consumers can exercise those rights.

**Citation:** Cal. Civ. Code §§ 1798.100(b), 1798.110, 1798.115, 1798.130.

### B. Provide an Opt-Out Mechanism (Required)

You must give California consumers a clear and conspicuous way to opt out of the sale/sharing of their personal information:

- **"Do Not Sell or Share My Personal Information" link** — must be prominently placed on your homepage (or every page where data collection occurs). This is the standard required link text under § 1798.135(a)(1).
- **Global Privacy Control (GPC) compliance** — you must honor GPC signals sent by consumers' browsers as a valid opt-out. The CPPA has issued enforcement guidance making GPC compliance mandatory, not optional.
- Optionally, you may provide a **preference center** allowing granular control, but this does not replace the GPC obligation.

### C. Honor Opt-Out Requests (Required)

Upon receiving a valid opt-out (including GPC):

- **Stop selling/sharing within 15 business days** of the request.
- **Notify all third parties** (including the ad tech platform) that the consumer has opted out and they must stop using that consumer's data — Cal. Civ. Code § 1798.120(d).
- Do not re-engage the consumer for sale/sharing without their explicit **opt-in consent**, and not before 12 months have passed (§ 1798.135(d)).

### D. Review Your Ad Tech Contract (Required)

Execute or update a written contract with the ad tech platform. The contract must:

- Specify the **limited and specified purpose** for which the ad tech platform may use the data.
- Prohibit the ad tech platform from selling or sharing the personal information unless the consumer has received notice and opportunity to opt out.
- Prohibit the ad tech platform from retaining, using, or disclosing the data outside of the contract's scope.
- Require the ad tech platform to notify you if it can no longer comply.

**Citation:** Cal. Civ. Code § 1798.100(d); CPPA Regulations § 7050.

If no compliant contract exists, the ad tech platform cannot qualify as a "service provider" or "contractor" — meaning every disclosure is automatically a sale/sharing, and the platform bears its own obligations as a third party.

### E. Update Your Cookie Banner / Consent Management Platform (Recommended, Practically Required)

- Implement or reconfigure your **Consent Management Platform (CMP)** to:
  - Disclose ad tech data sharing to users **before** cookies are set.
  - Capture and propagate opt-out signals.
  - Honor GPC signals at the technical layer (prevent the ad tech pixel/tag from firing for opted-out users).
- Consider category-level consent for "targeting/advertising" cookies separate from "functional" cookies.

### F. Consumer Rights Infrastructure (Required)

Ensure your rights request process supports:

- **Right to Know** — what data was sold/shared, to whom, and for what purpose.
- **Right to Delete** — delete personal information and instruct the ad tech platform to delete its copy.
- **Right to Correct** — correct inaccurate personal information.
- **Right to Opt Out** — as described above.
- **Right of No Retaliation** — consumers who opt out must receive the same quality of service.

Response timelines: 45 calendar days for most requests, extendable by 45 days with notice (§ 1798.145(b)).

### G. Data Minimization and Purpose Limitation (CPRA Addition)

CPRA requires that personal information be:

- Collected for **disclosed purposes** only.
- Limited to what is **reasonably necessary** for those purposes.
- Not **retained longer than necessary**.

Review whether the scope of data shared with the ad tech platform (e.g., granular browsing paths, session recordings, inferred attributes) is proportionate to the stated advertising purpose.

---

## 5. Sensitive Personal Information Consideration

If the browsing data reveals information about health conditions, precise geolocation, financial status, or other **sensitive personal information** (SPI) categories under § 1798.140(ae), additional obligations apply:

- Consumers have the right to **limit the use and disclosure** of SPI.
- A separate **"Limit the Use of My Sensitive Personal Information" link** may be required.
- SPI shared with ad tech platforms for targeted advertising purposes is particularly high-risk.

---

## 6. Enforcement Risk

The **California Privacy Protection Agency (CPPA)** has made ad tech data flows a priority enforcement area. The CPPA's 2023–2024 enforcement sweep explicitly targeted businesses that:

- Failed to honor GPC signals.
- Did not provide a compliant opt-out link.
- Continued sharing data with ad tech partners without proper contracts.

Penalties: Up to **$2,500 per unintentional violation** and **$7,500 per intentional violation**, with each consumer's record potentially constituting a separate violation (§ 1798.155).

---

## 7. Summary Checklist

| Action | Priority | Owner |
|---|---|---|
| Add "Do Not Sell or Share My Personal Information" link to website | Critical | Web/Legal |
| Honor Global Privacy Control (GPC) browser signals | Critical | Engineering |
| Update privacy policy with sale/sharing disclosures | Critical | Legal |
| Execute CPRA-compliant contract with ad tech platform | Critical | Legal/Procurement |
| Configure CMP to block ad tech tags for opted-out users | Critical | Engineering |
| Establish consumer rights request workflow | High | Legal/Ops |
| Audit data minimization — scope of data shared | High | Privacy/Engineering |
| Check for sensitive personal information in browsing data | High | Privacy |
| Train marketing/ad ops team on opt-out propagation | Medium | HR/Legal |

---

## References

- Cal. Civ. Code § 1798.100 et seq. (CCPA as amended by CPRA)
- Cal. Civ. Code §§ 1798.120, 1798.135, 1798.140(ad), 1798.140(ah)
- CPPA Final Regulations (effective March 29, 2023), Cal. Code Regs. tit. 11, §§ 7000–7304
- CPPA Enforcement Advisory No. 2023-03 (Global Privacy Control)
- FTC Enforcement Guidance on Digital Advertising and Data Brokers (complementary federal context)
