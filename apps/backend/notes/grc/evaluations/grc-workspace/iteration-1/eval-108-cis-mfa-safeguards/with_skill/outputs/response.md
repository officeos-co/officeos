# CIS Controls v8: Multi-Factor Authentication (MFA) Safeguards

## MFA Safeguards in CIS Controls v8

CIS Controls v8 contains three explicit MFA safeguards, all residing in **CIS Control 6: Access Control Management**. All three are assigned to **Implementation Group 2 (IG2)**, meaning they are required for organizations with dedicated IT staff, sensitive data, and moderate risk tolerance. They are not part of the IG1 (essential hygiene) baseline.

---

## The Three MFA Safeguards

| Safeguard ID | Title | IG Level | Asset Type | Security Function |
|-------------|-------|----------|-----------|------------------|
| **6.3** | Require MFA for Externally-Exposed Applications | IG2 | Users | Protect |
| **6.4** | Require MFA for Remote Network Access | IG2 | Users | Protect |
| **6.5** | Require MFA for Administrative Access | IG2 | Users | Protect |

### CIS Control 6, Safeguard 6.3 — Require MFA for Externally-Exposed Applications

**IG Level:** IG2

**Scope:** All SaaS platforms, web portals, cloud management consoles, webmail, and any application reachable from the public internet.

**What it requires:** Every user authenticating to an externally-exposed application must use a second factor beyond their password. This includes corporate SaaS tools (Microsoft 365, Google Workspace, Salesforce, etc.), customer-facing portals, and any web application accessible without a VPN.

**Implementation note from CIS:** Phishing-resistant MFA (FIDO2/passkeys) is the preferred approach. SMS OTP is considered an acceptable but weaker fallback.

---

### CIS Control 6, Safeguard 6.4 — Require MFA for Remote Network Access

**IG Level:** IG2

**Scope:** VPN connections, Remote Desktop Protocol (RDP) gateways, SSH access to internal systems, and any mechanism that extends network-layer access to remote workers or third parties.

**What it requires:** Remote connectivity into the enterprise network must require MFA at the authentication step — not just at the VPN client but enforced by the authentication server (AAA infrastructure). Safeguard 12.7 reinforces this by requiring that remote devices connect through VPN with AAA.

---

### CIS Control 6, Safeguard 6.5 — Require MFA for Administrative Access

**IG Level:** IG2

**Scope:** All administrative console access — Active Directory, Azure/Entra ID, cloud management planes (AWS Console, Azure Portal, GCP Console), network device management, security tools, and any privileged management interface.

**What it requires:** Administrative accounts must authenticate with MFA for every session. This is the highest-priority MFA safeguard because compromised admin accounts enable lateral movement, ransomware deployment, and full domain takeover. CIS explicitly calls for phishing-resistant MFA (FIDO2/hardware keys) for privileged users.

---

## Implementation Group Context

MFA safeguards first appear at **IG2**. This does not mean IG1 organizations can ignore MFA entirely — Safeguard 5.2 (IG1) notes that passwords of at least 14 characters are required "or MFA required," and Safeguard 5.4 (IG1) restricts admin privileges to dedicated admin accounts, which pairs with MFA at IG2. However, the formal MFA mandate begins at IG2.

| IG | MFA Requirement |
|----|----------------|
| IG1 (56 safeguards) | No explicit MFA mandate; strong passwords (14+ chars) required; admin account separation required |
| IG2 (130 total safeguards) | MFA required for external apps (6.3), remote access (6.4), and admin access (6.5) |
| IG3 (153 total safeguards) | All IG2 MFA requirements apply; phishing-resistant MFA strongly emphasized for privileged access |

---

## SMS OTP vs. Phishing-Resistant MFA in the CIS Controls Context

CIS Controls v8 does not prohibit SMS OTP, but it explicitly recommends phishing-resistant MFA as the preferred approach. The distinction is critical for understanding implementation quality:

### SMS OTP (One-Time Password via Text Message)

**How it works:** A six- or eight-digit code is sent to the user's registered phone number. The user enters this code as the second factor.

**Weaknesses acknowledged by CIS:**
- **SIM swapping:** Attackers socially engineer mobile carriers to transfer a victim's phone number to an attacker-controlled SIM, intercepting all SMS codes.
- **Real-time phishing:** Adversary-in-the-middle (AiTM) proxy attacks (e.g., Evilginx, Modlishka) capture the OTP code in real time and replay it to the legitimate service before it expires.
- **SS7 vulnerabilities:** Telecommunications protocol weaknesses can allow interception of SMS messages at the carrier level.
- **Malware:** Mobile malware can intercept SMS messages on the device before the user reads them.

**CIS position:** SMS OTP satisfies the letter of Safeguards 6.3, 6.4, and 6.5 but is identified as a common implementation pitfall when used for privileged access. The implementation guidance explicitly flags "SMS OTP used for privileged access" as a gap requiring remediation.

---

### Phishing-Resistant MFA (CIS Preferred Approach)

**What it means:** Authentication mechanisms that are cryptographically bound to the specific origin (domain) being accessed, making it impossible for an attacker to use a captured credential against a different site or at a different time.

**CIS-recommended technologies:**

| Technology | Description | Use Case |
|-----------|-------------|----------|
| **FIDO2 / WebAuthn** | Hardware or platform authenticator; cryptographic challenge/response bound to the registered origin | Web applications, cloud consoles |
| **Passkeys** | FIDO2 credential stored on device (biometric-protected); synced across devices via platform (Apple, Google, Microsoft) | Consumer-friendly phishing-resistant MFA |
| **Hardware Security Keys** | Physical devices (YubiKey, Google Titan, Token2); FIDO2/U2F; require physical presence | Privileged users, admin accounts |
| **Smart Card / PIV** | Certificate-based authentication; common in government and regulated industries | Enterprise, federal environments |

**Why phishing-resistant MFA defeats AiTM attacks:** The authentication response is cryptographically signed with the exact origin URL. A phishing site at `microsoft-login.attacker.com` cannot use a credential registered at `login.microsoft.com` — the origin binding check fails. The attacker intercepts nothing reusable.

---

## Recommended MFA Deployment Tiers (Aligned to CIS Safeguards)

| Access Type | Safeguard | Minimum Acceptable | CIS Preferred |
|-------------|----------|-------------------|---------------|
| External SaaS / Web apps | 6.3 | Authenticator app TOTP (e.g., Microsoft Authenticator, Google Authenticator) | FIDO2 / Passkeys |
| Remote network / VPN | 6.4 | TOTP or push notification | FIDO2 hardware key |
| Administrative / Privileged | 6.5 | Authenticator app TOTP | FIDO2 hardware key (YubiKey, Titan) |
| General workforce (IG1 recommendation) | 5.2 (password quality) | Strong password (14+ chars) | Passkeys to eliminate password |

**Note on push notification MFA (e.g., Microsoft Authenticator "Approve/Deny"):** This is more resistant than SMS OTP because it does not involve a code that can be intercepted. However, it is vulnerable to "MFA fatigue" attacks where attackers send repeated push requests hoping the user approves one. Number-matching and additional context features (location, app name) in modern authenticator apps substantially mitigate this risk. Push MFA is acceptable for Safeguard 6.3 and 6.4 but phishing-resistant MFA remains preferred for 6.5 (admin access).

---

## Common Pitfall: MFA Deployment Gaps

The CIS implementation guidance specifically identifies MFA deployment gaps as a top pitfall:

> **Problem:** MFA enabled on some systems but not others; SMS OTP used for privileged access.
> **Solution:** Comprehensive MFA inventory; phishing-resistant MFA for privileged and external access.

A complete MFA deployment covering all three IG2 safeguards requires:
1. Inventory of all externally-exposed applications (for 6.3)
2. Inventory of all remote access methods (for 6.4)
3. Inventory of all administrative accounts and consoles (for 6.5)
4. Enforcement via identity provider policy (Azure AD/Entra ID Conditional Access, Okta Policies, Google Workspace security policies) — not just user awareness

---

## Related Safeguards That Support MFA

| Safeguard | IG | Relevance to MFA |
|----------|-----|-----------------|
| 5.4 — Restrict Administrator Privileges to Dedicated Admin Accounts | IG1 | Ensures admin accounts exist as separate identities to apply MFA to |
| 5.6 — Centralize Account Management | IG2 | Single IdP (Azure AD, Okta) enables consistent MFA policy enforcement |
| 6.6 — Maintain an Inventory of Authentication/Authorization Systems | IG2 | Identifies all systems where MFA must be evaluated and enforced |
| 6.7 — Centralize Access Control | IG2 | SSO with MFA enforcement prevents MFA bypass through direct application access |
| 14.3 — Train Workforce on Authentication Best Practices | IG1 | Trains users to recognize MFA fatigue attacks and phishing attempts targeting MFA |

---

## Summary

- **Three safeguards require MFA in CIS Controls v8:** 6.3, 6.4, and 6.5 — all at IG2.
- **Scope of coverage:** Externally-exposed applications, remote network access, and administrative access respectively.
- **SMS OTP:** Technically satisfies the safeguard requirements but is explicitly flagged as insufficient for privileged access due to SIM swapping and AiTM phishing vulnerabilities.
- **Phishing-resistant MFA (FIDO2/hardware keys/passkeys):** The CIS-preferred implementation, cryptographically bound to the authentication origin and immune to real-time phishing interception. Required for IG3 privileged users; strongly recommended at IG2.
- **IG1 organizations** have no explicit MFA mandate but should implement MFA voluntarily, particularly for any admin accounts or externally-facing systems, as it is the single highest-impact control for preventing account compromise.
