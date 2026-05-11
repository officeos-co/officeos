# PCI DSS Compliance for E-Commerce with Stripe

## Do You Need PCI DSS Compliance?

Yes, as an e-commerce company that accepts credit card payments, you are subject to PCI DSS (Payment Card Industry Data Security Standard) requirements. This applies even when you use a payment processor like Stripe. PCI DSS is a set of security standards established by the major card brands (Visa, Mastercard, American Express, Discover, and JCB) through the PCI Security Standards Council.

Using Stripe significantly reduces your compliance burden because Stripe handles the actual card data processing, but it does not eliminate your obligations entirely.

## What SAQ Level Applies?

The Self-Assessment Questionnaire (SAQ) you need depends on how you've integrated Stripe into your website:

### SAQ A — Fully Outsourced (Simplest)
If you use **Stripe Checkout** or **Payment Links** where customers are completely redirected to Stripe's hosted payment page, you likely qualify for **SAQ A**. This is the simplest SAQ with the fewest requirements (around 20 controls). Your website never touches card data in this scenario.

**Requirements for SAQ A:**
- All payment processing is outsourced to a PCI-compliant third party
- Your company does not electronically store, process, or transmit cardholder data
- Your website only redirects customers to the payment processor

### SAQ A-EP — E-Commerce with Embedded Payment
If you use **Stripe Elements** or **Stripe.js** with an embedded payment form on your own website pages, you likely need **SAQ A-EP**. This has more controls (around 190) because your web server is partially in scope.

**This applies when:**
- Customers enter card data into a form hosted on your domain (even if it's in an iframe from Stripe)
- Your web servers serve the payment page
- A compromise of your web server could potentially affect payment data security

### SAQ D — Full Scope
If your servers ever receive, transmit, or store raw card numbers, you would need the much more comprehensive **SAQ D** with hundreds of controls. This is rare with Stripe integrations and should be avoided by using Stripe's recommended integration patterns.

## Merchant Levels

Your merchant level determines how you validate compliance:

- **Level 1**: Over 6 million transactions/year — requires a formal audit by a Qualified Security Assessor (QSA)
- **Level 2**: 1-6 million transactions/year — annual SAQ plus quarterly vulnerability scans
- **Level 3**: 20,000 to 1 million e-commerce transactions — annual SAQ plus quarterly scans
- **Level 4**: Under 20,000 e-commerce transactions — annual SAQ recommended plus quarterly scans

Most small to mid-size e-commerce companies fall into Level 3 or Level 4.

## Practical Recommendations

1. **Identify your Stripe integration type** — check if you use Stripe Checkout (redirect) or Stripe Elements (embedded)
2. **Complete the appropriate SAQ** — available free from the PCI Security Standards Council website
3. **Run quarterly external vulnerability scans** — use an Approved Scanning Vendor (ASV)
4. **Obtain Stripe's compliance documentation** — Stripe publishes their compliance certificates; download their Attestation of Compliance (AOC)
5. **Report to your acquiring bank** — submit your SAQ annually

## Bottom Line

- Using Stripe Checkout/redirect → likely **SAQ A** (easiest)
- Using Stripe Elements/embedded forms → likely **SAQ A-EP** (moderate)
- Most e-commerce companies with Stripe do NOT need a full QSA audit unless processing very high transaction volumes

The good news is that using a reputable payment processor like Stripe places the heaviest compliance burden on them, not you. Your main obligations are completing the right SAQ, running quarterly scans, and ensuring your website itself is secure.
