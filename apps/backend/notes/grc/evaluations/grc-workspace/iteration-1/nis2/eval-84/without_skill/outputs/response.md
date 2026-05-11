# ISO 27001:2022 and NIS2 Compliance: Gap Analysis

## Short Answer

No. Holding ISO 27001:2022 certification does **not** mean you are NIS2 compliant. ISO 27001 provides a strong foundation and significantly reduces the compliance effort, but NIS2 (the EU Network and Information Security Directive 2, transposed into national law across EU Member States by October 2024) imposes legally binding obligations that go beyond what ISO 27001 requires or covers.

---

## Why ISO 27001 Helps — But Is Not Sufficient

ISO 27001 is a voluntary international standard for an Information Security Management System (ISMS). It is certification-based and process-oriented, demonstrating that your organisation manages information security systematically. NIS2, by contrast, is EU law. It imposes direct legal obligations on essential and important entities in critical sectors, with mandatory incident reporting timelines, personal liability for management, and national supervisory authority oversight.

The two frameworks overlap substantially in their security control objectives, but NIS2 adds requirements that ISO 27001 does not address at all — or addresses only partially.

---

## Key Gaps Between ISO 27001:2022 and NIS2

### 1. Mandatory Incident Reporting Obligations

**NIS2 requirement:** Article 23 requires notification of significant incidents:
- Within **24 hours**: early warning to the national CSIRT or competent authority.
- Within **72 hours**: incident notification with an initial assessment.
- Within **1 month**: final report with root cause, impact, and remediation.

**ISO 27001 gap:** ISO 27001 (Annex A 6.8, 5.24–5.28) requires you to have an incident management process, but it does not mandate reporting to external authorities, and it sets no specific timeframes tied to regulatory deadlines. Your ISMS may handle incidents internally without fulfilling NIS2's external notification chain.

**Action needed:** Establish formal procedures for notifying the national competent authority/CSIRT, with documented escalation paths and owners responsible for each reporting window.

---

### 2. Supply Chain and Third-Party Security

**NIS2 requirement:** Article 21(2)(d) explicitly requires organisations to address security in supply chains, including the security practices of direct suppliers and service providers and their sub-suppliers.

**ISO 27001 gap:** ISO 27001 Annex A 5.19–5.22 covers supplier relationships, but the depth of NIS2's supply chain security obligation — including assessing the vulnerability of specific products and services, and evaluating suppliers' own security posture — often exceeds what a typical ISO 27001 supplier management programme achieves. NIS2 expects a risk-based view of the entire supply chain, not just contractual clauses.

**Action needed:** Conduct supply chain risk assessments at a deeper level, including criticality classification of suppliers, security assessments of key third parties, and documented due diligence on sub-processors.

---

### 3. Management Accountability and Governance

**NIS2 requirement:** Article 20 requires that the management body (board-level or equivalent) approves, oversees, and is personally liable for cybersecurity risk management measures. Management members can be held personally liable for negligence and may be temporarily barred from management roles following serious incidents.

**ISO 27001 gap:** ISO 27001 clause 5 requires top management commitment and leadership, but it does not create personal legal liability for individual executives, nor does it require the same level of board-level direct accountability that NIS2 mandates. Many ISO 27001 implementations delegate ISMS oversight to an CISO or IT function rather than embedding it at the board level.

**Action needed:** Formally assign NIS2 cybersecurity responsibilities to the management body, ensure board-level sign-off on risk management decisions, and implement board training on cybersecurity obligations.

---

### 4. Specific Minimum Security Measures

**NIS2 requirement:** Article 21(2) sets out a minimum baseline of security measures that covered entities must implement, including:
- Policies on risk analysis and information system security.
- Business continuity and crisis management (including backup management and disaster recovery).
- Basic cyber hygiene practices and cybersecurity training.
- Policies and procedures on the use of cryptography and, where appropriate, encryption.
- Human resources security, access control policies, and asset management.
- Multi-factor authentication (MFA) or continuous authentication.
- Secured voice, video, and text communications; secured emergency communications.

**ISO 27001 gap:** ISO 27001 Annex A covers most of these areas, but implementation and certification do not guarantee that every measure is in place to the level NIS2 expects. In particular:
- **MFA**: ISO 27001 does not mandate MFA; NIS2 does.
- **Encrypted communications**: ISO 27001 recommends cryptography policies (A.8.24) but does not require secured emergency communication systems.
- **Cyber hygiene training**: ISO 27001 requires awareness (A.6.3) but NIS2 expects structured, regular cybersecurity training for all staff.

**Action needed:** Perform a control-by-control mapping of your existing Annex A controls against NIS2 Article 21 requirements, identify any gaps in implementation (not just policy), and address MFA deployment across all critical systems.

---

### 5. Scope and Entity Classification

**NIS2 requirement:** NIS2 applies to entities in 18 sectors (11 highly critical, 7 other critical), classified as "essential" or "important" based on size and sector. Some entities are automatically in scope regardless of size (e.g., DNS providers, TLD registries, critical infrastructure operators).

**ISO 27001 gap:** ISO 27001 scope is self-defined. Your ISMS scope may exclude parts of your organisation, subsidiaries, or specific services that fall within NIS2's mandatory scope. NIS2 applies to the entire legal entity (and potentially group entities) operating in scope sectors.

**Action needed:** Review your NIS2 entity classification, confirm your legal status as essential or important, and ensure your ISMS scope covers all in-scope operations. Self-register with the national competent authority as required.

---

### 6. Supervisory and Enforcement Regime

**NIS2 requirement:** NIS2 establishes a supervisory regime including on-site inspections, targeted security audits, ad hoc audits, security scans, and requests for evidence. Essential entities face ex-ante supervision; important entities face ex-post supervision. Fines reach up to **€10 million or 2% of global annual turnover** for essential entities, and **€7 million or 1.4% of global turnover** for important entities.

**ISO 27001 gap:** ISO 27001 certification is assessed by an accredited third-party certification body on a voluntary basis. There is no regulatory authority involved, no statutory power to impose fines for non-compliance with the standard, and no obligation to respond to government requests for evidence.

**Action needed:** Understand your national transposition of NIS2, identify your competent authority, and ensure you can respond to supervisory requests. Maintain audit-ready evidence files that meet regulatory expectations, not just certification body expectations.

---

### 7. Business Continuity and Crisis Management

**NIS2 requirement:** Article 21(2)(c) requires documented business continuity plans, backup management, disaster recovery, and crisis management specifically designed to maintain or rapidly restore services following a cyber incident.

**ISO 27001 gap:** ISO 27001 (A.5.29, A.5.30, A.8.13) addresses BCM and backup, but NIS2 requires this to be specifically oriented toward cyber incident scenarios and service continuity for critical services. Many ISMS implementations have BCM plans that are generic or oriented toward IT recovery rather than sector-specific essential service continuity.

**Action needed:** Review and update BCP/DRP documentation to explicitly address cyber incident scenarios, define recovery time and recovery point objectives for critical services, and test plans regularly.

---

### 8. National Transposition Variations

NIS2 is a directive, meaning each EU Member State transposes it into national law with possible additions or variations. Your obligations depend on the specific national legislation of each country in which you operate — which may go beyond the directive's minimum requirements.

**Action needed:** Identify all Member States where you are in scope, review the relevant national legislation in each jurisdiction, and assess whether additional national requirements apply.

---

## Summary Gap Table

| Area | ISO 27001:2022 | NIS2 Requirement | Gap |
|---|---|---|---|
| Incident reporting to authorities | Not required | 24h / 72h / 1-month reporting | Yes — significant |
| Management personal liability | Not addressed | Board accountability, personal liability | Yes |
| MFA requirement | Not mandated | Explicitly required | Yes |
| Supply chain depth | Supplier clauses | Full supply chain risk assessment | Partial |
| Regulatory supervision | Certification body only | National competent authority | Yes |
| Fines / enforcement | None | Up to €10M or 2% global turnover | Yes |
| Scope alignment | Self-defined | Legal entity / sector-based | Potential gap |
| BCM for cyber incidents | General BCM | Cyber-specific service continuity | Partial |
| Cyber hygiene training | Awareness required | Structured training required | Partial |
| Encrypted communications | Policy required | Secured comms systems required | Partial |

---

## Recommended Next Steps

1. **Perform a formal NIS2 gap assessment** mapped against Article 21 and Article 20 requirements, using your existing ISO 27001 controls as the baseline.
2. **Determine your entity classification** (essential vs. important) and register with your national competent authority.
3. **Implement an external incident reporting procedure** with documented owners and timelines aligned to NIS2 Article 23.
4. **Engage your board** on NIS2 obligations, personal liability, and required governance structures.
5. **Extend supply chain security assessments** beyond contractual clauses to include active risk evaluation of critical suppliers.
6. **Deploy MFA** across all critical systems and document compliance.
7. **Align BCM/DRP** documentation and testing to cyber-specific scenarios for essential services.
8. **Review national transpositions** in every EU Member State where you operate.

---

## Conclusion

ISO 27001:2022 certification demonstrates a mature, well-structured security programme and will significantly accelerate NIS2 compliance. However, NIS2 is a legal obligation with a different purpose — ensuring resilience of critical infrastructure across the EU — and it imposes requirements around governance, incident reporting, supply chain security, and regulatory accountability that ISO 27001 does not cover. Treat ISO 27001 as your compliance foundation, then close the specific gaps identified above to achieve full NIS2 compliance.
