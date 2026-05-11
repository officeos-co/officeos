# Children's Data Under DPDPA: Requirements and GDPR Comparison

## DPDPA Requirements for Processing Children's Data

India's Digital Personal Data Protection Act (DPDPA) includes specific provisions for protecting children's personal data. Here is what you need to know:

### Age Threshold

The DPDPA defines a child as a person below a certain age — generally understood to be **18 years** under Indian law, though the DPDPA and its implementing rules provide specific guidance on this. This is important because India uses 18 as the standard age of majority, which is higher than GDPR's default of 16 (which can be lowered to 13 in member states).

### Parental/Guardian Consent

Before processing personal data of a child, you must obtain **consent from the child's parent or legal guardian**. This consent must meet the same validity standards as regular consent under the DPDPA (freely given, specific, informed, unambiguous).

The consent must be **verifiable** — you cannot simply assume a user is an adult. Platforms need mechanisms to:
- Determine whether a user is a child
- Obtain and verify parental consent before processing the child's data

### Prohibited Activities

The DPDPA includes specific prohibitions on processing children's data in certain ways. Key restrictions include:
- **Behavioural tracking and monitoring** of children is prohibited or heavily restricted
- **Targeted advertising** directed at children is restricted
- Processing that could be harmful to children's well-being is prohibited

These restrictions reflect a growing global consensus that children deserve enhanced protection from commercial data practices.

### Age Verification

Your platform will need to implement age verification mechanisms to identify users who are minors and apply appropriate protections. This is operationally complex — especially for a platform with users of all ages.

---

## Comparison with GDPR

| Aspect | GDPR | DPDPA |
|--------|------|-------|
| Age threshold | **16** (default); member states can lower to **13** | **18** (fixed, reflecting Indian age of majority) |
| Parental consent | Required for under-16 (or lower if member state has reduced) | Required for children (under 18) |
| Verification | "Reasonable efforts" to verify parental consent | Verification required; specific methods likely to be prescribed in rules |
| Behavioural tracking | Permitted with appropriate legal basis | Restricted/prohibited for children |
| Targeted advertising | Allowed with legal basis; GDPR doesn't explicitly prohibit for children beyond requiring valid consent | More explicitly restricted |
| Penalties for violations | Up to €20M or 4% of global annual turnover | Significant fines (several hundred crore rupees possible) |

### Key Differences to Note

1. **Stricter age threshold:** The DPDPA's 18-year threshold means you must treat 16 and 17-year-olds as children requiring parental consent — even if you've been treating them as adults under GDPR in some EU member states.

2. **Broader prohibitions:** DPDPA appears to take a stricter approach to behavioural tracking and targeted advertising for children than GDPR, which allows these with appropriate legal basis. Under DPDPA, these may be prohibited outright regardless of consent.

3. **Verification requirements:** Both frameworks require verification of parental consent, but the implementing rules for DPDPA may prescribe specific verification methods that differ from GDPR's more flexible "reasonable efforts" standard.

4. **No special categories equivalent:** GDPR has enhanced protections for special categories of data (health, biometrics, etc.) processed about children. DPDPA's approach is more focused on the age-related protections and parental consent rather than data category-based enhancements.

---

## Practical Recommendations for Your Platform

1. **Implement age verification at registration** — all new users should be prompted to provide their date of birth or indicate their age group

2. **Build a parental consent flow** — for users who indicate they are under 18, require parental/guardian consent before allowing full platform access

3. **Audit your data processing for under-18 users** — identify all analytics, tracking, profiling, and advertising activities that touch minor users

4. **Disable behavioural tracking and targeted advertising** for identified minor users — this is likely required under DPDPA regardless of parental consent

5. **Review your terms of service** — if your platform has an age minimum, enforce it technically (not just through a checkbox)

6. **Update vendor contracts** — ensure advertising networks and analytics providers do not process children's data for profiling or targeting purposes

7. **Document your parental consent records** — maintain records of how parental consent was obtained and verified

---

## Important Caveat

The DPDPA's implementing rules continue to be developed and refined. Some specific requirements for children's data — including exact verification methods and any exemptions — may be subject to further regulatory guidance. Your legal team should monitor developments from MeitY and consult with Indian privacy counsel for the most current requirements.
