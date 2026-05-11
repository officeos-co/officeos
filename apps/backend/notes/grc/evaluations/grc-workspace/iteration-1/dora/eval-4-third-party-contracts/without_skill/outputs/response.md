# DORA Third-Party Risk: Contracts and Due Diligence for Cloud and SaaS Providers

## Overview

DORA's ICT third-party risk management requirements are among its most complex and practically significant obligations, particularly for financial institutions that rely heavily on cloud and SaaS providers. With 15 different providers, you have substantial work to do to achieve DORA compliance.

---

## Classifying Your Providers: Critical vs. Non-Critical

The first step is to classify each of your 15 providers based on whether they support **critical or important functions** of your institution. This distinction determines the level of contractual and due diligence requirements.

**Critical or important functions** are those whose disruption, failure, or inadequate performance would materially impact:
- The continuity of your banking operations
- Your financial soundness
- Your customers' interests
- Financial stability

Cloud infrastructure providers (IaaS like AWS, Azure, GCP) supporting core banking systems, payment processing, or business continuity are likely to be classified as critical. Peripheral SaaS tools (e.g., HR software, minor productivity tools) may be non-critical.

---

## Required Contractual Provisions

### For Critical/Important Function Providers

DORA requires that contracts with providers supporting critical or important functions include specific provisions covering:

1. **Service description**: A clear, complete description of the ICT services provided
2. **Data locations**: Where services are provided and where data is stored and processed — important for data sovereignty and regulatory access
3. **Data protection**: Provisions on data security, confidentiality, and compliance with applicable data protection laws (GDPR)
4. **Availability and integrity**: SLAs and provisions on service availability, data integrity, and security standards
5. **Audit rights**: Rights for your institution, your competent authority, and resolution authorities to audit the provider and access relevant information — this is a significant requirement that many standard cloud contracts do not adequately address
6. **Termination rights**: Defined rights to terminate and minimum notice periods; termination for cause and for convenience
7. **Incident reporting**: Obligations for the provider to notify you of incidents affecting your services
8. **Data portability and migration**: Rights to retrieve your data and receive migration assistance upon termination
9. **Subcontracting**: Requirements for consent before the provider subcontracts key elements; visibility of the subcontracting chain

### For Non-Critical Providers

Lighter contractual requirements apply for non-critical arrangements, focused on basic service description and data protection provisions.

---

## Due Diligence Requirements

DORA requires pre-contractual due diligence before entering new ICT service arrangements. For critical providers, this should cover:

- **Security posture assessment**: Review of certifications (ISO 27001, SOC 2 Type II), security policies, and incident history
- **Financial stability**: Assessment of the provider's financial health and business continuity
- **Data practices**: Understanding of data location, sub-processing chains, and data handling
- **Substitutability**: Can you replace this provider if needed? How quickly?
- **Concentration risk**: Are multiple critical functions dependent on the same provider?
- **Regulatory compliance**: Does the provider comply with relevant regulations?

Due diligence should be repeated periodically throughout the relationship — typically annually for critical providers.

---

## Register of Information

DORA requires financial entities to maintain a **Register of Information** covering all ICT service arrangements. This is a detailed inventory — not merely a vendor list — that must capture:

- Provider identity and legal entity details
- Description of services provided
- Whether the arrangement supports critical or important functions
- Data storage locations
- Sub-processor information
- Substitutability assessment
- Contract dates and terms

This Register must be submitted to your competent authority (at least annually or on request).

---

## Concentration Risk Assessment

With 15 providers, concentration risk is a key concern. DORA requires you to assess:

- Whether any single provider supports multiple critical functions (entity-level concentration)
- The feasibility of substituting a provider if needed
- Your overall dependency profile across providers

If significant concentration risk exists (e.g., AWS supports five critical functions), you must document this and consider mitigating strategies such as multi-cloud arrangements.

---

## Exit Strategies

For critical ICT service arrangements, DORA requires documented exit strategies covering:
- Conditions that would trigger exit
- Steps for transitioning to an alternative provider
- Data migration and portability arrangements
- Estimated timelines and resources required

---

## Practical Steps for Your 15 Providers

1. Map each provider to the business functions they support and classify as critical/important or non-critical
2. Review all existing contracts against DORA's required provisions and identify gaps
3. Negotiate contract amendments or addenda as needed — this is often the most time-consuming step with large cloud providers
4. Build your Register of Information with the required detail
5. Document concentration risk assessments
6. Develop exit strategies for critical provider arrangements
7. Implement ongoing monitoring and annual review processes
