# Implementing IT/OT Network Segmentation for Legacy SCADA Systems

## The Challenge

You are correct that TSA Security Directives require network segmentation between IT and OT networks. The challenge you face is common across critical infrastructure: legacy SCADA and industrial control systems (ICS) were often designed and deployed before network security was a primary concern. Many of these systems:

- Use proprietary or older protocols (Modbus, DNP3, legacy OPC-DA)
- Run outdated operating systems that cannot be patched
- Were not designed with authentication or encryption in mind
- May behave unpredictably or fail if network conditions change
- Have vendor support requirements that constrain what changes can be made

The good news is that TSA's segmentation requirement focuses on the boundary between IT and OT — it does not require you to rebuild your SCADA systems from scratch. The practical approach is to implement segmentation controls at the boundary layer while leaving legacy systems largely intact.

---

## Core Principle: Protect the Boundary, Not Just the Asset

For legacy OT environments, the primary strategy is to:

1. **Establish a clear IT/OT boundary** with security controls at that boundary
2. **Control all traffic crossing the boundary** (ideally using an allow-list approach)
3. **Implement compensating controls** for systems that cannot be hardened internally
4. **Monitor the OT environment passively** for anomalies without disrupting legacy systems

---

## Practical Implementation Approaches

### 1. Firewall-Based IT/OT Segmentation with a DMZ

The most practical starting point for most organisations is deploying a dedicated industrial firewall at the IT/OT boundary, creating a demilitarised zone (DMZ) as a buffer between the two environments.

**How it works**:
- Corporate IT network connects to the DMZ
- OT/SCADA network connects to the DMZ from the other side
- No direct routable connections exist between IT and OT
- Only explicitly approved traffic flows through firewall rules (allow-list approach)
- Data exchange between IT and OT happens via proxy servers or data aggregation servers placed in the DMZ

**For legacy SCADA**: Your SCADA systems remain on the OT side of the firewall. They don't need to change — the segmentation is enforced at the boundary, not on the SCADA system itself.

### 2. Data Diodes for One-Way Data Flows

If your primary need is getting data out of the OT environment (e.g., sending operational data to a corporate historian or reporting system) without allowing anything to flow back in, **data diodes** provide the strongest possible segmentation.

Data diodes are hardware devices that physically enforce one-way data flow — it is impossible at the hardware level for data to travel from the IT side to the OT side. This eliminates the risk of an IT compromise affecting OT systems.

Vendors include Waterfall Security Solutions and Owl Cyber Defense.

**Best for**: Read-only data replication use cases; environments where the OT/SCADA systems require maximum isolation.

### 3. Jump Servers / Bastion Hosts for Remote Access

Legacy SCADA environments often have ad hoc remote access paths — vendor modems, VPN tunnels that terminate directly in OT, or shared credentials. TSA's directives require that all remote access to OT go through a controlled pathway.

**The solution**: Deploy a dedicated jump server (bastion host) in your OT DMZ. All remote access — whether by employees or vendors — terminates at the jump server. Nobody connects directly to OT devices.

This also allows you to:
- Enforce multi-factor authentication (MFA) at the jump server, even if legacy OT devices cannot support MFA themselves
- Record and monitor all remote sessions
- Implement time-limited access for vendors
- Centralise access control management

### 4. OPC Protocol Bridging

Many older SCADA systems use OPC-DA (the older, COM/DCOM-based version of OPC), which has significant security problems and was not designed for cross-network use. The modern replacement is OPC-UA, which includes authentication and encryption.

**For legacy systems**: Deploy an OPC-UA aggregation server in the DMZ that connects to the OPC-DA environment on one side and presents OPC-UA data to IT systems on the other. IT consumers only see the OPC-UA interface in the DMZ — they never have direct network access to the legacy SCADA systems.

---

## Handling Systems That Cannot Be Cleanly Segmented

Some legacy systems will have legitimate reasons why a clean segmentation cannot be immediately achieved:
- A SCADA system that currently communicates with both field devices and corporate reporting on the same network
- A historian server that has network connections in both IT and OT zones
- A vendor that requires direct connectivity to OT systems for support

For these situations:
1. **Document the exception and the business reason** for why clean segmentation is not yet possible
2. **Implement compensating controls**: Enhanced monitoring, network-level allow-lists restricting traffic to only what is absolutely required, time-limited vendor access windows
3. **Create a remediation plan with a target date** for achieving full segmentation — regulators generally accept that legacy environments take time to remediate, but expect a documented plan

---

## Passive OT Monitoring

A key consideration for legacy SCADA: **do not use active network scanning** in your OT environment. Active scanning (like the kind used in IT security tools) can send unexpected packets to PLCs and RTUs that may cause them to hang, crash, or behave erratically.

Use **passive OT monitoring tools** instead. These tools listen to network traffic without sending any queries to OT devices. They build asset inventories and detect anomalies from what they observe. Examples include tools from vendors like Claroty, Dragos, and Nozomi Networks, which are designed specifically for industrial environments.

---

## Documentation and Compliance Evidence

For TSA compliance, you need to be able to demonstrate that segmentation is in place and effective. Key documentation includes:
- Current network topology diagrams showing IT zone, OT zone, and DMZ
- Firewall ruleset documentation showing the allow-list approach
- Inventory of all OT/SCADA systems and their network connections
- Documentation of any exceptions with compensating controls
- Records of annual architecture reviews

---

## Summary

The practical approach for legacy SCADA environments is: **segment at the boundary, monitor passively, control remote access tightly, and document compensating controls for gaps.** You do not need to replace legacy systems to achieve TSA compliance — you need to ensure that robust controls exist at the interface between IT and OT, and that you have visibility into what is happening in your OT environment.
