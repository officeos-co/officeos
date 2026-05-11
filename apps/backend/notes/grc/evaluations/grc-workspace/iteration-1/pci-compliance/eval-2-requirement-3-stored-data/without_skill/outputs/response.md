# PCI DSS Requirements for Protecting Stored Cardholder Data (Requirement 3)

## Overview of Requirement 3

PCI DSS Requirement 3 focuses on protecting stored cardholder data. The core principle is simple: **if you don't need the data, don't store it**. If you must store it, protect it properly.

## Types of Card Data and Storage Rules

PCI DSS distinguishes between two categories of account data:

### Cardholder Data (CHD) — Can be stored if protected:
- **Primary Account Number (PAN)** — the card number; the most sensitive element; must be rendered unreadable if stored
- **Cardholder name** — can be stored
- **Expiration date** — can be stored
- **Service code** — can be stored

### Sensitive Authentication Data (SAD) — Must NEVER be stored after authorization:
- **Full magnetic stripe or chip data** (track data)
- **CAV2/CVC2/CVV2/CID** (the 3-4 digit security code on the card)
- **PINs and PIN blocks**

Even if encrypted, SAD cannot be retained after a transaction is authorized. This is an absolute prohibition.

## Key Requirements Under Requirement 3

### Data Retention (3.1/3.2)
- Implement a data retention policy specifying the minimum data needed
- Define retention periods for each data element
- Implement processes to securely delete data when no longer needed
- Review stored data at least quarterly to identify and delete unnecessary cardholder data

### SAD Not Retained After Authorization (3.3)
- All SAD must be unrecoverable after the authorization process completes
- No exceptions — even encrypted SAD cannot be stored post-authorization

### Masking PAN on Displays (3.4)
- When displaying PAN, mask it so that only the first six and last four digits are visible (e.g., 4111 11** **** 1111)
- Personnel with a legitimate business need can see more digits

### Rendering PAN Unreadable (3.5)
Stored PAN must be rendered unreadable using one or more of:
- **Strong one-way hash functions** (e.g., SHA-256) — the whole PAN is hashed
- **Truncation** — storing only a portion of the PAN (first 6/last 4 digits)
- **Index tokens** — a token replaces the PAN, with the mapping stored securely
- **Strong cryptography** (e.g., AES-256) with proper key management

### Cryptographic Key Management (3.6/3.7)
- Use strong cryptographic algorithms and key lengths
- Implement full key lifecycle management: key generation, distribution, storage, use, retirement, and destruction
- Split knowledge and dual control for manual key operations
- Change encryption keys when they reach their cryptoperiod
- Formal key custodian acknowledgment

## What Changed from PCI DSS v3.2.1 to v4.0?

PCI DSS v4.0 introduced several enhancements to Requirement 3:

### Stronger Hashing Requirements
In v3.2.1, strong one-way hashes were acceptable for protecting PAN. In v4.0, there's an added emphasis that hashes should be **keyed cryptographic hashes** (like HMAC). Using a plain SHA-256 hash of just the PAN is considered insufficient because it could theoretically be rainbow-table attacked given the limited PAN space.

### Documented Cryptographic Architecture
v4.0 introduced a new requirement to maintain a documented inventory and description of your cryptographic architecture, including the algorithms, key usage, and cryptographic devices (like HSMs) in use. This did not exist explicitly in v3.2.1.

### Key Protection Requirements Enhanced
v4.0 strengthened key protection requirements, including requirements around how keys that encrypt cardholder data must themselves be protected — typically requiring encryption with a key-encrypting key (KEK) or storage in a hardware security module (HSM).

### Remote Access Controls
A new control was added addressing the risk of cardholder data being copied or relocated via remote access technologies. Organizations must implement technical controls to prevent unauthorized copying of PAN through remote access tools.

### Roles and Responsibilities
v4.0 added a requirement to formally document and assign roles and responsibilities for each PCI DSS requirement area, including Requirement 3.

### SAD Definition Clarification
v4.0 provided clearer guidance around SAD protection during pre-authorization storage, which applies specifically to issuers and processors who need to temporarily store SAD during the authorization process.

## Practical Implications

For most merchants, the key takeaways are:
1. **Stop storing CVV/CVC** — this is the most common violation; you cannot store it for recurring payments
2. **Truncate or hash stored PANs** — if you need to store card references, use truncated numbers or tokens
3. **Review your logs** — transaction logs, error logs, and debug logs often inadvertently capture full card numbers
4. **Key management matters** — if you encrypt PANs, your key management practices must be rigorous
5. **Update hashing practices** — if using SHA hashes, ensure they are keyed hashes (HMAC) under v4.0

The best approach for most merchants is tokenization — replacing card data with tokens so that you never need to store actual PANs at all, which largely removes you from the scope of Requirement 3.
