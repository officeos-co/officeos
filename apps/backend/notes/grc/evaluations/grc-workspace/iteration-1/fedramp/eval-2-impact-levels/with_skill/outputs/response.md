# FedRAMP Impact Levels: Low, Moderate, and High

*Use official FedRAMP templates from fedramp.gov — this content should be inserted into the appropriate template section.*

---

## Impact Level Framework

FedRAMP impact levels are determined using **FIPS 199** categorization, which assesses the potential impact to an organization if federal information is compromised across three security objectives:

- **Confidentiality** — Unauthorized disclosure of information
- **Integrity** — Unauthorized modification or destruction of information
- **Availability** — Disruption of access to or use of information

The impact level is determined by the **high-water mark** across these three objectives. The applicable NIST SP 800-53 Rev 5 baseline is then selected based on that categorization.

---

## The Four FedRAMP Tiers

### LI-SaaS (Low-Impact SaaS)

| Attribute | Details |
|---|---|
| **Data sensitivity** | No PII, no sensitive federal data, very limited scope |
| **Control count** | Reduced subset — uses a combined SSP + assessment template |
| **Best for** | Collaboration tools, productivity apps with no federal data storage |
| **Your situation** | Does NOT apply — law enforcement data is highly sensitive |

### Low Impact

| Attribute | Details |
|---|---|
| **Definition** | Loss of confidentiality, integrity, or availability has a **limited adverse effect** on agency operations, assets, or individuals |
| **Control count** | ~156 controls (NIST SP 800-53 Rev 5) |
| **Examples** | Publicly available information, general training systems |
| **Your situation** | Does NOT apply — law enforcement data warrants higher protection |

### Moderate Impact

| Attribute | Details |
|---|---|
| **Definition** | Loss of CIA has a **serious adverse effect** on agency operations, assets, or individuals |
| **Control count** | 323 controls (NIST SP 800-53 Rev 5) |
| **Coverage** | Most common baseline — covers the majority of CSPs handling non-classified government data |
| **Examples** | CRM systems, HR data, financial management, general government operations |
| **Your situation** | May apply as a floor, but law enforcement data likely requires High |

### High Impact

| Attribute | Details |
|---|---|
| **Definition** | Loss of CIA has a **severe or catastrophic effect** on agency operations, assets, or individuals |
| **Control count** | 421 controls (NIST SP 800-53 Rev 5) |
| **Examples** | Law enforcement data, financial systems, health records, critical infrastructure |
| **Your situation** | **Likely required** — see analysis below |

---

## Impact Level Comparison

| Attribute | Low | Moderate | High |
|---|---|---|---|
| NIST 800-53 Rev 5 Controls | ~156 | 323 | 421 |
| Typical assessment timeline | 6–9 months | 12–18 months | 18–24+ months |
| Cost (rough estimate) | Lower | Moderate | Highest |
| Required for law enforcement data | No | Unlikely | Yes |
| FedRAMP Marketplace listing | Yes | Yes | Yes |

---

## What Applies to Your Product: Law Enforcement Sensitive Data

**Recommendation: FedRAMP High**

The SKILL guidance and FedRAMP documentation are explicit: **law enforcement data is specifically cited as an example of High impact data**. Here is the analysis:

### FIPS 199 Categorization Analysis

**Confidentiality**: The unauthorized disclosure of law enforcement sensitive data could:
- Compromise active investigations
- Put officers, witnesses, or confidential informants at risk
- Enable criminal elements to evade law enforcement
- Violate individuals' civil liberties and privacy rights

**Impact assessment**: **HIGH** — severe or catastrophic effect

**Integrity**: Unauthorized modification of law enforcement records could:
- Result in wrongful arrests or releases
- Corrupt evidentiary chains of custody
- Undermine prosecutions

**Impact assessment**: **HIGH**

**Availability**: Loss of access to law enforcement systems during active operations could:
- Endanger officers and the public
- Disrupt critical public safety operations

**Impact assessment**: **HIGH**

**High-water mark result**: **HIGH** — FedRAMP High baseline applies

### What FedRAMP High Means Operationally

1. **421 NIST SP 800-53 Rev 5 controls** must be implemented and documented in your SSP
2. **Stricter parameter values** — FedRAMP sets more restrictive parameter values for High than base NIST (e.g., shorter session timeouts, stricter audit requirements)
3. **Additional technical requirements**:
   - FIPS 140-2 or 140-3 validated encryption required for all data at rest and in transit
   - Mandatory MFA for all privileged and non-privileged accounts
   - More frequent vulnerability scanning cadence
   - Stricter separation of duties requirements
4. **Hosting requirements** — Your system must be hosted on a FedRAMP High authorized infrastructure. AWS GovCloud (US) is FedRAMP High authorized and is a common choice.
5. **Assessment requirements** — High-impact assessments require more extensive 3PAO testing, including penetration testing and a broader set of tested controls.

### Authorization Path Implications

- **Agency ATO**: An agency with law enforcement missions (e.g., DOJ, FBI, DHS, DEA, ATF, state/local law enforcement federally funded programs) can sponsor your High authorization
- **JAB P-ATO**: Currently suspended as of 2024–2026; not a viable near-term path

### Recommended Next Steps

1. Conduct a formal **FIPS 199 categorization** exercise with your security team and document the results in SSP Section 2
2. Confirm FedRAMP High with your target agency sponsor — they will validate the impact level during their review
3. Select a **FedRAMP High authorized IaaS/PaaS** (AWS GovCloud US, Azure Government) to maximize inherited controls
4. Begin gap assessment against the **421-control High baseline** — this is a significant undertaking and will require 18–24+ months for most organizations
5. Engage an accredited 3PAO with FedRAMP High experience early
