# PCI DSS v4.0.1 Requirement 3: Protect Stored Account Data

**PCI DSS Version: v4.0.1 (current, June 2024)**

---

## Overview

**Requirement 3: Protect Stored Account Data** falls under the second PCI DSS goal: *Protect Account Data*. It governs what cardholder data and sensitive authentication data (SAD) organisations may store, how long it may be retained, and what cryptographic protections must be applied to stored Primary Account Numbers (PANs).

Requirement 3 works in conjunction with **Requirement 4** (which covers data in transit). Together they protect account data throughout its lifecycle.

---

## Key Terminology

| Term | Definition | Storage Rules |
|------|-----------|---------------|
| **PAN** (Primary Account Number) | The card number | Can be stored if rendered unreadable (Req 3.5) |
| **Cardholder Name** | Name on card | Can be stored in plaintext |
| **Expiry Date** | Card expiry | Can be stored in plaintext |
| **Service Code** | 3–4 digit value on magnetic stripe | Can be stored in plaintext |
| **Full Track Data** (magnetic stripe or chip equivalent) | All data on the magnetic stripe | **Must NEVER be stored after authorisation** |
| **CVV/CVC** (Card Verification Value) | 3–4 digit security code | **Must NEVER be stored after authorisation** |
| **PIN / PIN Block** | Personal identification number | **Must NEVER be stored after authorisation** |

**Critical rule**: SAD (Full track data, CVV/CVC, PINs) must never be stored after the authorisation of a transaction — even in encrypted form.

---

## Requirement 3 Sub-Controls (v4.0.1)

### 3.1 — Processes and Mechanisms for Protecting Stored Account Data

**3.1.1**: All security policies and procedures for protecting stored account data are documented, in use, and known to all affected parties.

**3.1.2** (new in v4.0): Roles and responsibilities for performing activities in Requirement 3 are documented, assigned, and understood.

---

### 3.2 — Storage of Account Data is Kept to a Minimum

**3.2.1**: Account data storage is limited to the minimum data elements necessary, consistent with legal, regulatory, and/or business requirements. A data retention policy exists and includes:
- Storage amount and retention time for each data element
- Processes for secure deletion when data is no longer needed
- Quarterly process to identify and securely delete stored data that exceeds retention requirements

**Key point**: If you don't need the data, don't store it. The best protection for stored data is not storing it in the first place.

---

### 3.3 — Sensitive Authentication Data (SAD) is Not Retained After Authorisation

**3.3.1**: SAD is not retained after authorisation, even if encrypted. All sensitive authentication data received is rendered unrecoverable upon completion of the authorisation process.

**3.3.2**: SAD that is stored electronically prior to completion of authorisation is encrypted using strong cryptography.

**3.3.3** (applies to issuers only): Any SAD stored by issuers prior to authorisation is protected and a business justification exists for such storage.

---

### 3.4 — Access to Displays of Full PAN and Ability to Copy PAN Data is Restricted

**3.4.1**: If disk-level or partition-level encryption is used (rather than database/file-level encryption), it is managed separately and independently of OS authentication/access control mechanisms, and decryption keys are not associated with user accounts.

**3.4.2** (new in v4.0): When using remote-access technologies, technical controls prevent copy and/or relocation of PAN for all personnel, except those with documented, explicit authorisation and a legitimate business need.

---

### 3.5 — Primary Account Number (PAN) is Secured Wherever it is Stored

**3.5.1**: PAN is secured with any of the following:
- **One-way hashes** based on strong cryptography (hash of the entire PAN)
- **Truncation** — only a segment of the PAN is stored (no more than the first six and last four digits)
- **Index tokens** — a token replaces the PAN; the mapping table is stored securely
- **Strong cryptography** (e.g., AES-256) with associated key management processes

**3.5.1.1**: Hashes used to render PAN unreadable shall be keyed cryptographic hashes (e.g., HMAC) using strong cryptography, with associated key management processes. (Added in v4.0.1)

**3.5.1.2**: If disk-level or partition-level encryption is used to render PAN unreadable, it must meet specific criteria (not just relying on OS-level encryption).

**3.5.1.3**: If disk-level or partition-level encryption is used, logical access must be managed independently of OS.

---

### 3.6 — Cryptographic Keys Used to Protect Stored Account Data are Secured

**3.6.1**: Procedures and processes for protecting cryptographic keys used to protect stored account data are defined and implemented, including:
- Generation of strong keys
- Secure key distribution
- Secure key storage
- Cryptographic key changes when keys reach end of cryptoperiod
- Retirement or replacement of keys that have weakened or are suspected of compromise
- Splitting knowledge and dual control of keys (to avoid single persons having full key access)
- Prevention of unauthorised substitution of keys

**3.6.1.1** (new in v4.0): A documented description of the cryptographic architecture is maintained, including:
- Algorithms, protocols, and key lengths in use
- Description of key usage for each key
- Inventory of all hardware security modules (HSMs) and other SCDs used

**3.6.1.2** (new in v4.0): Secret and private keys used to encrypt/decrypt account data are stored in one or both of the following forms: encrypted with a key-encrypting key; within a secure cryptographic device such as an HSM.

**3.6.1.3** (new in v4.0): Access to cleartext cryptographic key-encrypting keys is restricted to the fewest number of custodians necessary.

**3.6.1.4** (new in v4.0): Cryptographic keys are stored in the fewest possible locations.

---

### 3.7 — Where Cryptography is Used to Protect Stored Account Data, Key Management Processes and Procedures Covering All Aspects of the Key Lifecycle are Defined and Implemented

**3.7.1**: Key management policies include procedures for generation of strong cryptographic keys.

**3.7.2**: Key management policies include procedures for secure distribution of cryptographic keys.

**3.7.3**: Key management policies include procedures for secure storage of cryptographic keys.

**3.7.4**: Key management policies include procedures for cryptographic key changes for keys that have reached the end of their cryptoperiod.

**3.7.5**: Key management policies include procedures for retirement, replacement, or destruction of keys.

**3.7.6**: Where manual cleartext cryptographic key-management operations are performed, these operations are managed using split knowledge and dual control.

**3.7.7**: Unauthorised substitution of cryptographic keys is prevented.

**3.7.8**: Cryptographic key custodians formally acknowledge (in writing or electronically) that they understand and accept their key-custodian responsibilities.

**3.7.9** (new in v4.0): Where an entity shares cryptographic keys with a service provider for transmission or processing of account data, guidance on secure transmission, storage, and changing of such keys exists.

---

## What Changed from PCI DSS v3.2.1 to v4.0/v4.0.1?

| Change Area | v3.2.1 | v4.0 / v4.0.1 |
|------------|--------|--------------|
| **Roles and responsibilities** | Not explicitly required in Req 3 | **New 3.1.2**: Roles and responsibilities must be documented, assigned, and understood |
| **Remote access PAN copy controls** | Not explicitly addressed | **New 3.4.2**: Technical controls must prevent copy/relocation of PAN via remote access |
| **Keyed hashing** | Strong one-way hash acceptable | **New 3.5.1.1**: Hashes must be *keyed* cryptographic hashes (e.g., HMAC); plain SHA hashes of PAN are no longer sufficient |
| **Cryptographic architecture documentation** | Not required | **New 3.6.1.1**: Must maintain documented cryptographic architecture including algorithms, key usage, and HSM inventory |
| **Key-encrypting key protection** | General key protection required | **New 3.6.1.2**: Secret/private keys must be stored encrypted with a KEK or within an HSM/SCD |
| **KEK custodian access** | Not specifically restricted | **New 3.6.1.3**: Cleartext KEK access restricted to minimum number of custodians |
| **Key storage minimisation** | Not addressed | **New 3.6.1.4**: Keys must be stored in fewest possible locations |
| **Service provider key guidance** | Not addressed | **New 3.7.9**: Guidance required when sharing keys with service providers |

### Summary of v4.0 Themes in Requirement 3

1. **Stronger hashing requirements**: Unkeyed SHA hashes of PAN (e.g., SHA-256 of the raw PAN) are no longer acceptable under 3.5.1.1. You must use HMAC or another keyed hash.
2. **Formalised cryptographic inventory**: For the first time, organisations must document their entire cryptographic architecture (3.6.1.1).
3. **Key protection elevated**: Keys encrypting cardholder data must themselves be stored in HSMs or encrypted by a key-encrypting key (3.6.1.2).
4. **Remote access controls**: A new control (3.4.2) addresses the risk of remote access tools being used to exfiltrate PAN data.
5. **Roles formalised**: Explicit ownership of Requirement 3 activities must be assigned (3.1.2).

---

## Evidence Required for QSA Assessment

| Sub-Control | Evidence a QSA Will Request |
|------------|----------------------------|
| 3.2.1 | Data retention policy, evidence of quarterly data purge process, data flow diagrams |
| 3.3.1 | Confirmation SAD is not stored; log/database reviews showing no SAD in storage |
| 3.5.1 | Database/file inspection showing PAN is hashed, truncated, or encrypted |
| 3.5.1.1 | Proof that hashing algorithm is a keyed hash (HMAC); key management docs |
| 3.6.1.1 | Cryptographic architecture document; HSM inventory |
| 3.6.1.2 | Evidence that encryption keys are stored in HSMs or encrypted with KEKs |
| 3.7.6 | Key ceremony documentation showing split knowledge and dual control |
| 3.7.8 | Signed key custodian acknowledgement forms |

---

## Common Gaps

- Storing CVV/CVC in databases "just in case" for recurring billing — this is **never permitted**
- Using unkeyed SHA-256 hashes of PAN (now non-compliant under v4.0.1, Req 3.5.1.1)
- Retaining full track data in transaction logs, error logs, or debug logs
- Inadequate key management: symmetric keys stored in the same location as encrypted data
- No formal key custodian acknowledgement process
- Undocumented cryptographic architecture — now explicitly required under 3.6.1.1

---

> **Disclaimer:** This guidance is based on PCI DSS v4.0.1 (PCI SSC, June 2024). Formal compliance validation must be conducted by a Qualified Security Assessor (QSA) or Internal Security Assessor (ISA). Verify against the official standard at pcisecuritystandards.org.
