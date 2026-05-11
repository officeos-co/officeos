# PCI DSS Compliance for Stripe-Based E-Commerce: SAQ Selection

**PCI DSS Version: v4.0.1 (current as of June 2024)**

---

## Do You Need PCI DSS Compliance?

**Yes.** Even though Stripe processes the payment card data on your behalf, your organisation is still a payment card merchant and is subject to PCI DSS. The requirement applies to any entity that stores, processes, or transmits cardholder data — or whose systems could impact the security of cardholder data. Using a payment service provider like Stripe reduces your scope but does not eliminate your PCI DSS obligations.

---

## SAQ Selection Decision Tree

To determine the correct SAQ, we need to understand how your website integrates with Stripe:

### Key Question: How does payment data flow through your site?

**Option A — Stripe-hosted payment page (Stripe Checkout / Payment Links)**
- Customers are fully redirected to Stripe's hosted payment page to enter card data
- Your website never loads, processes, or transmits card numbers
- All cardholder data functions are fully outsourced to Stripe (a PCI-compliant third party)

→ **Recommended SAQ: SAQ A** (~22 controls)

**Option B — Stripe.js / Stripe Elements embedded on your page**
- Your payment page loads JavaScript from Stripe's servers (e.g., Stripe.js or Stripe Elements)
- Card data is entered directly into Stripe-hosted iframes embedded on your page
- Your servers never receive raw card data — the PAN goes directly to Stripe
- However, your e-commerce page controls how customers access the payment form

→ **Recommended SAQ: SAQ A-EP** (~191 controls)

**Option C — Custom integration where card data touches your servers**
- Your servers receive raw card numbers before passing them to Stripe
→ **SAQ D (Merchant)** (~340 controls) — and this is strongly inadvisable; restructure the integration.

---

## Most Likely Scenario for Stripe Users

Most e-commerce merchants using Stripe fall into one of two categories:

| Integration Type | SAQ Type | Controls | Notes |
|-----------------|----------|----------|-------|
| Stripe Checkout / Payment Links (full redirect) | **SAQ A** | ~22 | Simplest compliance path |
| Stripe Elements / Stripe.js (embedded iframe) | **SAQ A-EP** | ~191 | More controls; your web server is in partial scope |

### Why SAQ A-EP Instead of SAQ A for Stripe Elements?

The PCI SSC distinguishes between merchants who fully redirect to a third-party page (SAQ A) versus those whose e-commerce page controls the payment flow but uses JavaScript/iframe to capture card data (SAQ A-EP). With Stripe Elements:

- Your web server serves the checkout page that contains the Stripe iframe
- A compromise of your web server could potentially inject malicious scripts that intercept card data before it enters Stripe's iframe
- Therefore, your web server and hosting environment are partially in scope

Under **PCI DSS v4.0.1 Requirement 6.4.3 and 11.6.1**, SAQ A-EP merchants must maintain an inventory of all scripts on payment pages and implement integrity checks (e.g., Subresource Integrity hashes or a Content Security Policy). This is a key new requirement that became mandatory on March 31, 2025.

---

## Merchant Level

Your merchant level determines the rigour of validation required:

| Level | Annual Transactions | Validation Required |
|-------|--------------------|--------------------|
| **Level 1** | >6 million Visa/MC | Annual ROC by QSA + quarterly ASV scans |
| **Level 2** | 1–6 million | Annual SAQ + quarterly ASV scans |
| **Level 3** | 20,000–1 million e-commerce | Annual SAQ + quarterly ASV scans |
| **Level 4** | <20,000 e-commerce | Annual SAQ recommended + quarterly ASV scans |

Most early-to-mid stage e-commerce companies fall at **Level 3 or Level 4**. If you process more than 6 million transactions annually, you are a Level 1 merchant and must engage a QSA for a full Report on Compliance (ROC) — an SAQ is not sufficient.

---

## Practical Compliance Steps (SAQ A or A-EP)

1. **Confirm your Stripe integration type** — check whether you use Stripe Checkout (redirect) or Stripe Elements (embedded iframe)
2. **Complete the applicable SAQ** — obtain from pcisecuritystandards.org
3. **Run quarterly ASV scans** — use a PCI SSC-approved ASV vendor to scan your external-facing IP addresses and domains
4. **Verify Stripe's compliance status** — obtain Stripe's current Attestation of Compliance (AOC); Stripe maintains Level 1 Service Provider compliance
5. **Execute a Service Provider Agreement** — ensure your contract with Stripe addresses data security responsibilities
6. **Implement script integrity controls (SAQ A-EP)** — under Req 6.4.3/11.6.1, maintain a script inventory and integrity verification mechanism for your payment page
7. **Submit your SAQ and AOC** — to your acquiring bank annually

---

## Important v4.0.1 Notes

- **PCI DSS v3.2.1 was retired March 31, 2024** — all assessments must now reference v4.0 or v4.0.1
- **All "future-dated" requirements in v4.0 became mandatory March 31, 2025**, including Req 6.4.3 (payment page script inventory) and Req 11.6.1 (payment page integrity monitoring) — highly relevant to SAQ A-EP merchants
- The Customised Approach (new in v4.0) is available to SAQ A-EP merchants with mature security programs who wish to implement alternative controls with a Targeted Risk Analysis (TRA)

---

> **Disclaimer:** This guidance is based on PCI DSS v4.0.1 (PCI SSC, June 2024). Formal compliance validation must be conducted by a Qualified Security Assessor (QSA) or Internal Security Assessor (ISA). Verify against the official standard at pcisecuritystandards.org.
