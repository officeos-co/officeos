# System Boundary Description — FedRAMP Moderate SSP

*Use official FedRAMP templates from fedramp.gov — this content should be inserted into SSP Section 9 (System Boundary) and associated architecture appendices. All diagrams referenced below must be included as figures in the SSP.*

---

## SSP Section 9: System Boundary Description

### 9.1 Authorization Boundary Overview

The [System Name] authorization boundary encompasses all components that process, store, or transmit federal information in support of the Cloud Service Offering (CSO). The boundary is hosted entirely within the **AWS GovCloud (US-West) and AWS GovCloud (US-East) regions**, which hold a FedRAMP High Provisional Authorization to Operate (P-ATO) issued by the JAB. All physical infrastructure, including data centers, networking hardware, and physical security controls, is inherited from AWS GovCloud.

The boundary includes the following component categories:

1. **Web Application Layer** — Internet-accessible application servers and associated load balancers
2. **Database Layer** — PostgreSQL relational database instances storing federal data
3. **Caching Layer** — Redis in-memory cache instances
4. **Integration Layer** — API connections to external services (Okta, SendGrid)
5. **Supporting Infrastructure** — Network components, security monitoring tools, and management plane elements within AWS GovCloud

**Note on External Services**: Two external SaaS services — Okta (identity provider) and SendGrid (email delivery) — connect to the in-boundary system. These services are addressed in Section 9.3 (External Services) below.

---

### 9.2 System Components Within the Authorization Boundary

#### 9.2.1 Web Application Tier

The web application is deployed on **Amazon EC2 instances** (or Amazon ECS/EKS containers, if applicable) within a private subnet in the AWS GovCloud VPC. Application traffic is received through an **AWS Application Load Balancer (ALB)** in a public subnet. The ALB enforces TLS 1.2 minimum for all inbound connections.

| Component | Type | Data Classification | FedRAMP Role |
|---|---|---|---|
| AWS ALB | Load Balancer | Transit only | In boundary |
| Web Application Servers (EC2) | Compute | Process and transit federal data | In boundary |
| AWS WAF | Web Application Firewall | Metadata only | In boundary |
| Amazon CloudFront (if applicable) | CDN | Transit only | In boundary |

**FIPS compliance**: All TLS termination uses FIPS 140-2 validated algorithms via AWS GovCloud FIPS endpoints.

#### 9.2.2 Database Tier

Federal data at rest is stored in an **Amazon RDS for PostgreSQL** instance deployed in a private subnet with no public internet connectivity. Multi-AZ deployment is enabled for availability. Database encryption is enabled using **AWS KMS with FIPS 140-2 validated keys**.

| Component | Type | Data Classification | FedRAMP Role |
|---|---|---|---|
| Amazon RDS (PostgreSQL) — Primary | Database | Stores federal data | In boundary |
| Amazon RDS (PostgreSQL) — Read Replica | Database | Stores federal data (replica) | In boundary |
| AWS KMS (GovCloud) | Key Management | Encryption keys | In boundary (inherited from AWS) |
| RDS Automated Backups (S3 — GovCloud) | Storage | Federal data backups | In boundary |

**Data classification note**: All federal data stored in PostgreSQL must be identified in SSP Appendix J (Information Types) and Appendix K (Data Flow Diagrams).

#### 9.2.3 Caching Tier

A **Redis cache cluster** (Amazon ElastiCache for Redis) is deployed in the private subnet and used exclusively for session data and application-level caching of non-sensitive intermediate results. Redis does not serve as a persistent data store for federal information.

| Component | Type | Data Classification | FedRAMP Role |
|---|---|---|---|
| Amazon ElastiCache (Redis) | Cache | Session data; transient federal data | In boundary |

**Important**: If session data includes authentication tokens or any federal information — even transiently — the Redis cluster is in-scope and must be inventoried and secured accordingly (encryption in transit via TLS, encryption at rest enabled, no public exposure).

#### 9.2.4 Security and Monitoring Components

All security telemetry stays within the AWS GovCloud boundary.

| Component | Type | FedRAMP Role | Controls Addressed |
|---|---|---|---|
| AWS CloudTrail | Audit Logging | In boundary | AU-2, AU-3, AU-12 |
| Amazon GuardDuty | Threat Detection | In boundary | SI-3, IR-5 |
| AWS Security Hub | SIEM Aggregation | In boundary | AU-6, SI-4 |
| AWS Config | Configuration Monitoring | In boundary | CM-6, CM-7 |
| Amazon VPC Flow Logs | Network Logging | In boundary | AU-12, SC-7 |

---

### 9.3 External Services — Outside the Authorization Boundary

Two external SaaS services are connected to the [System Name] boundary via API integrations. These services are **outside the authorization boundary** and must be addressed through one of the following mechanisms per FedRAMP requirements:

1. The external service is FedRAMP-authorized (listed on the FedRAMP Marketplace), OR
2. Compensating controls are documented and approved by the AO

#### 9.3.1 Okta (Identity and Authentication Provider)

| Attribute | Details |
|---|---|
| **Service** | Okta Workforce Identity |
| **Purpose** | Federated authentication and SSO; MFA enforcement |
| **Data exchanged** | User identity assertions (SAML/OIDC tokens); no federal payload data |
| **FedRAMP status** | Okta is listed on the FedRAMP Marketplace — verify current authorization status and impact level at marketplace.fedramp.gov |
| **Boundary relationship** | External — outside the authorization boundary |
| **Documentation required** | CIS/CRM workbook entry; SSP Appendix Q (Integrated Inventory) |

**Important**: If Okta's FedRAMP authorization level (Moderate or High) matches or exceeds your system's baseline, you may document Okta as a leveraged authorization. If Okta is not authorized or authorized at a lower level, additional compensating controls and AO approval are required.

#### 9.3.2 SendGrid (Email Delivery Service)

| Attribute | Details |
|---|---|
| **Service** | Twilio SendGrid Email API |
| **Purpose** | Transactional email delivery (notifications, alerts) |
| **Data exchanged** | Email metadata and content (may include PII or federal data in notification payloads) |
| **FedRAMP status** | Verify at marketplace.fedramp.gov — SendGrid's FedRAMP status may be limited or at a lower impact level |
| **Boundary relationship** | External — outside the authorization boundary |
| **Risk consideration** | If emails contain federal data (e.g., user PII, case identifiers), this is a high-risk external connection requiring AO attention |

**Recommended mitigations for SendGrid**:
- Minimize federal data in email payloads (use reference links rather than embedding data)
- Document the data types transmitted in SSP Section 10 (Information Types) and Appendix K (Data Flow)
- Obtain AO approval for this external connection
- Consider whether a FedRAMP-authorized alternative (e.g., AWS SES GovCloud) should replace SendGrid to keep email within the boundary

---

### 9.4 What Is Explicitly Outside the Boundary

The following are **not** within the [System Name] authorization boundary:

- Customer/agency-managed identity systems (unless Okta above is used)
- End-user devices (laptops, workstations) of agency personnel
- Agency network infrastructure
- SendGrid's internal infrastructure
- AWS commercial (non-GovCloud) services — no commercial region endpoints shall be used
- Developer workstations and CI/CD pipelines (these should be documented separately as they may require boundary inclusion if they access or deploy to in-boundary systems)

---

### 9.5 Data Flow Summary

| Data Flow | Source | Destination | Protocol | Encryption | In/Out of Boundary |
|---|---|---|---|---|---|
| User authentication | Browser | Okta (external) | HTTPS/OIDC | TLS 1.2+ (FIPS) | Exits boundary |
| Auth token return | Okta | Web App (ALB) | HTTPS/OIDC | TLS 1.2+ (FIPS) | Re-enters boundary |
| Application requests | Browser | ALB → Web App | HTTPS | TLS 1.2+ (FIPS) | In boundary |
| DB queries | Web App | RDS PostgreSQL | TCP/TLS | TLS 1.2+ (FIPS) | In boundary |
| Cache reads/writes | Web App | ElastiCache Redis | TCP/TLS | TLS 1.2+ | In boundary |
| Email notifications | Web App | SendGrid (external) | HTTPS/SMTP | TLS 1.2+ | Exits boundary |
| Audit logs | All components | CloudTrail/Security Hub | Internal | Encrypted at rest | In boundary |

---

### 9.6 Boundary Diagram Reference

*[INSERT NETWORK DIAGRAM — SSP Figure 9-1: Authorization Boundary Diagram]*
*[INSERT DATA FLOW DIAGRAM — SSP Figure 9-2: Data Flow Diagram]*

The authorization boundary diagram must depict:
- All in-boundary components grouped within a clearly labeled boundary line
- External services (Okta, SendGrid) clearly shown outside the boundary
- All data flows between components, including protocol and port
- AWS GovCloud region designation
- Internet-facing components and ingress/egress points
- Trust zones (public subnet, private subnet, management subnet)

---

*This content should be placed in SSP Section 9 using the official FedRAMP SSP template. Data flow diagrams are required in SSP Appendix K. Component inventory must be maintained in SSP Appendix Q. External service documentation belongs in the CIS/CRM workbook and SSP Appendix Q.*
