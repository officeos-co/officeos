# FedRAMP Impact Levels: Low, Moderate, and High

## Overview

FedRAMP uses three primary impact levels — Low, Moderate, and High — to categorize cloud systems based on the potential impact of a security breach. These levels are derived from FIPS 199 (Federal Information Processing Standards Publication 199), which evaluates the potential impact to confidentiality, integrity, and availability of federal information.

---

## FedRAMP Low

- **Definition**: A security incident would have a **limited adverse effect** on agency operations, assets, or individuals
- **Control count**: Approximately 125 security controls
- **Typical use cases**: Public-facing websites, general productivity tools, systems handling only publicly available information
- **Assessment**: Less rigorous; shorter timeline and lower cost
- **Not appropriate for**: Any system handling sensitive, controlled unclassified information (CUI), or personal data

---

## FedRAMP Moderate

- **Definition**: A security incident would have a **serious adverse effect** on agency operations, assets, or individuals
- **Control count**: Approximately 325 security controls
- **Typical use cases**: Most government cloud services — HR systems, financial management, CRM, collaboration platforms handling non-public government data
- **Assessment**: Full 3PAO assessment required; typical timeline of 12–18 months
- **Most common level**: The majority of FedRAMP authorizations are at the Moderate level

---

## FedRAMP High

- **Definition**: A security incident would have a **severe or catastrophic effect** on agency operations, assets, or individuals
- **Control count**: Approximately 420 security controls
- **Typical use cases**: Systems handling law enforcement data, financial data with national security implications, health information, emergency response systems, classified-adjacent systems
- **Assessment**: Most rigorous; stricter parameter requirements, more extensive 3PAO testing, 18–24+ months typical timeline
- **Hosting requirements**: Must be hosted on FedRAMP High authorized infrastructure (e.g., AWS GovCloud, Azure Government)

---

## What Impact Level Applies to Law Enforcement Sensitive Data?

**Answer: FedRAMP High**

Law enforcement sensitive data is specifically in the High impact category. Here is why:

### Confidentiality Impact
Unauthorized disclosure of law enforcement data could:
- Compromise ongoing criminal investigations
- Endanger undercover officers or confidential informants
- Allow criminal suspects to evade capture
- Violate privacy rights and civil liberties

This represents a **severe** potential impact — High categorization.

### Integrity Impact
If law enforcement records are tampered with, the consequences could include:
- Wrongful arrests based on incorrect warrants or records
- Compromised evidence and failed prosecutions
- Corrupted criminal history records

This also represents a **severe** potential impact — High categorization.

### Availability Impact
If law enforcement systems go down during active operations, the result could be:
- Inability to respond to emergencies
- Officers unable to verify warrants or identity in the field

Depending on the specific use case, this may be High or Moderate impact.

### Conclusion

Using the high-water mark principle (the final categorization equals the highest impact across all three objectives), your system would be categorized as **FedRAMP High**.

---

## Practical Implications for Your Product

1. **Infrastructure**: Must be hosted on FedRAMP High authorized cloud infrastructure (AWS GovCloud US, Azure Government)
2. **Controls**: Approximately 420 security controls to implement and document
3. **Timeline**: Plan for 18–24+ months to achieve authorization
4. **Cost**: FedRAMP High authorizations are significantly more expensive than Moderate (typically $1M–$3M+ for initial authorization including 3PAO costs)
5. **Agency sponsor**: Your likely customers (DOJ, DHS, FBI, DEA, ATF, state/local law enforcement via federal grants) are accustomed to High requirements

**Recommendation**: Confirm the impact level with your target federal agency customer early in the process. They will validate the categorization during their security review.
