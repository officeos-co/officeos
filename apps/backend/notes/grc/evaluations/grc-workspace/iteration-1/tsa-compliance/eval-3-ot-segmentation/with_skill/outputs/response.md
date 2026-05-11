# Implementing Network Segmentation for Legacy OT/SCADA Environments — TSA Compliance Approach

## The TSA Requirement

Under TSA Security Directives (SD Pipeline-2021-02F for pipelines; SD 1580-21-01E for freight rail; SD 1582-21-01E for public transit/passenger rail), **Network Segmentation** is Domain 1 of the four required technical security domains. The requirement is:

> Develop and implement network segmentation policies and controls to ensure the OT system can continue to safely operate if the IT system is compromised, and vice versa.

Specific implementation requirements include:
- A formal network segmentation policy
- A documented and enforced IT/OT boundary (firewall rules, DMZ architecture, or physical separation)
- No direct routable connections between corporate IT and OT/ICS networks without security controls
- Remote access to OT must route through a demilitarised zone (DMZ) or jump server
- All segmentation exceptions documented with business justification

**The challenge you face is real and common**: Legacy SCADA and ICS systems were often designed in an era when network connectivity was minimal or non-existent, and they were never architected with firewall traversal or network segmentation in mind. Many use proprietary protocols, have no authentication capabilities, cannot be patched, and may fail unpredictably if network traffic is disrupted.

---

## Practical Implementation Strategy for Legacy OT Environments

The key principle: **TSA does not require a rip-and-replace of legacy OT systems**. It requires that effective segmentation controls exist at the boundary, and that compensating controls are in place for systems that cannot be fully hardened internally.

### Step 1: Establish Your Critical Cyber System (CCS) Inventory and Architecture Map

Before implementing segmentation, you must have an accurate picture of what you are segmenting.

- **Document all OT assets**: Every PLC, RTU, HMI, historian, SCADA server, engineering workstation — including legacy systems with unknown communication patterns
- **Map all network connections**: Including undocumented connections, dial-up connections, vendor remote access paths, wireless links, and any IT-to-OT data flows
- **Identify communication dependencies**: Which OT systems communicate with which? Which OT systems pull data from or push data to IT systems? What protocols are used?

This is the foundation of your Architecture Design Review (ADR) — one of the four required CRMP components. An accurate, current network diagram is mandatory evidence for TSA assessors.

**Common discovery finding in legacy environments**: Communication paths that nobody knew existed — old modems, undocumented cross-connections, historian servers that bridge IT and OT, or vendor jump hosts placed directly on OT networks years ago.

### Step 2: Classify Systems by Zone and Risk

Not all OT systems are equal. Use the Purdue Model (or IEC 62443 zone/conduit model) as a structuring framework:

| Zone | Examples | Segmentation Goal |
|------|---------|------------------|
| **Level 0-1** (Field devices) | PLCs, RTUs, sensors | Maximum isolation; no direct IT access |
| **Level 2** (Control) | HMIs, DCS workstations | Isolated OT zone; limited access via DMZ |
| **Level 3** (Operations) | SCADA servers, historians, engineering workstations | OT DMZ or semi-isolated zone; controlled data exchange with IT |
| **Level 3.5** (DMZ) | Data replication servers, jump hosts, proxy servers | Buffer zone between IT and OT |
| **Level 4** (IT/Business) | Corporate network, ERP, email | Standard IT environment; no direct OT access |

Legacy SCADA systems typically sit at Level 2-3. The segmentation controls are implemented at the boundary (Level 3.5 / DMZ), not necessarily within the legacy systems themselves.

### Step 3: Implement Boundary Controls at the IT/OT Interface

This is where most of the segmentation work happens with legacy OT — controlling what crosses the boundary rather than hardening legacy systems internally.

#### Option A: Firewall-Based Segmentation with DMZ
The most common approach. Deploy an industrial-grade firewall between IT and OT networks, with a DMZ in between.

- **Firewall placement**: Dedicated OT firewall (e.g., Palo Alto, Fortinet, Cisco) at the IT/OT boundary
- **DMZ zone**: Create an intermediate zone where data exchange servers, historian replicas, and jump hosts reside
- **Allow-list approach**: Firewall rules permit only explicitly required traffic; deny all by default
- **Protocol filtering**: Use deep packet inspection (DPI) capable of understanding industrial protocols (Modbus, DNP3, PROFINET, OPC-UA) to inspect and filter OT traffic

**Evidence for TSA**: Current firewall ruleset; network topology diagram showing DMZ; rule review records.

#### Option B: Data Diodes / Unidirectional Security Gateways
For the most sensitive legacy OT environments where even firewall-based connectivity is too risky, **data diodes** enforce one-way data flow at the hardware level.

- Data flows from OT to IT (e.g., historian data, operational data), but it is **physically impossible** for data to flow from IT to OT
- Eliminates the risk of IT-side compromise reaching OT entirely
- Vendors: Waterfall Security, Owl Cyber Defense (Perle), Forcepoint
- **Best for**: Legacy systems at Level 0-2 where no IT-to-OT commands are needed; read-only data replication use cases

**TSA perspective**: Data diodes are explicitly recognised as valid segmentation mechanisms. They provide the strongest segmentation evidence.

#### Option C: Application-Level Proxies and OPC-UA Bridging
For legacy SCADA using OPC-DA (old COM/DCOM-based OPC) or proprietary protocols that need to share data with IT:

- Deploy an **OPC-UA aggregation server** in the DMZ that translates OPC-DA to OPC-UA
- IT systems consume OPC-UA data from the DMZ server; they never connect directly to OT
- Eliminates dangerous DCOM exposure across the IT/OT boundary

### Step 4: Control Remote Access Into OT

Legacy environments often have the most problematic remote access situation — vendor modem connections, VPNs that terminate directly in OT, or shared credentials used by multiple vendors.

**TSA requirement**: Remote access to OT must go through a DMZ or jump server. MFA is required for all remote access to Critical Cyber Systems.

**For legacy OT environments**:
- Deploy a **dedicated jump server** (bastion host) in the OT DMZ; all remote access terminates at the jump server, never directly at OT devices
- Implement **MFA on the jump server** — even if legacy OT devices cannot support MFA themselves, MFA is enforced at the access layer
- Replace vendor modem connections with managed remote access solutions (e.g., Claroty SRA, Bayshore Networks, Tosibox)
- Implement **time-limited, session-recorded vendor access** — vendors connect only when needed, sessions are monitored and logged
- Disable or physically remove modems and legacy dial-up connections that are no longer actively used

### Step 5: Address Compensating Controls for Systems That Cannot Be Segmented Cleanly

Some legacy OT systems will have legitimate business reasons why a clean segmentation cannot be achieved immediately (e.g., a SCADA master that currently communicates directly with both field devices and a corporate reporting system). TSA recognises this reality but requires:

1. **Document the exception**: What is the system? Why can segmentation not be achieved? What is the residual risk?
2. **Implement compensating controls**: Options include:
   - Network-level allow-listing (only specific IP/port combinations permitted)
   - OT-aware intrusion detection monitoring the exception traffic
   - Enhanced logging and alerting on the exception connection
   - Physical access controls limiting who can reach the system
   - Accelerated migration timeline to achieve full segmentation
3. **Include in your remediation plan**: Your ADR must document findings and a remediation action plan with timelines. Exceptions without a remediation path are a compliance risk.

---

## OT-Aware Monitoring as a Complement to Segmentation

Even with segmentation controls in place, TSA requires **continuous monitoring** (Domain 3) of OT environments. For legacy SCADA, passive OT monitoring tools are strongly preferred because active scanning can disrupt industrial protocols.

**Recommended tools for passive OT monitoring**:
- Claroty
- Dragos Platform
- Nozomi Networks
- Armis
- Microsoft Defender for IoT

These tools perform passive network traffic analysis, build an asset inventory automatically, establish a baseline of normal OT communications, and alert on anomalies — without sending any active queries that could disrupt legacy PLCs or RTUs.

---

## Documentation Required for TSA Compliance

| Document | What It Must Show |
|----------|------------------|
| **Network topology diagram** | Current and accurate; shows IT zone, OT zone, DMZ, all firewall positions, all connections between zones |
| **Firewall ruleset documentation** | All rules at the IT/OT boundary; evidence of allow-list approach; rule review date |
| **CCS inventory** | All legacy OT systems identified and classified as CCS or not |
| **Segmentation exceptions log** | Any exceptions to full segmentation, with business justification and compensating controls |
| **ADR findings and remediation plan** | Annual review findings; open items; remediation timelines |
| **Remote access policy** | How vendor and employee remote access to OT is managed |

---

## Implementation Prioritisation

If you are starting from a flat or poorly segmented network, prioritise in this order:

1. **Immediate**: Identify and document all IT-to-OT connections (you cannot segment what you have not mapped)
2. **Short-term (30-90 days)**: Deploy firewall at IT/OT boundary; terminate any direct routable connections between corporate IT and OT; establish jump server for remote access
3. **Medium-term (90-180 days)**: Implement MFA on jump server; deploy OT-aware monitoring; document and remediate or justify all segmentation exceptions
4. **Ongoing**: Annual ADR; IRP testing of IT/OT isolation procedure; update network diagrams when architecture changes

---

*Disclaimer: This guidance is based on publicly available information about TSA Security Directives and OT cybersecurity best practices. TSA Security Directives are Sensitive Security Information (SSI). Work directly with TSA, qualified legal counsel, and OT/ICS cybersecurity professionals for your specific compliance situation.*
