# Azure Skill — References

## Proxy Pattern
This skill uses a proxy pattern: the skill-runtime sends `{ command: "az", args: [...] }` to the configured `proxy_url`. The proxy executes `az` (Azure CLI) with the appropriate service principal or managed identity credentials.

## Azure CLI
- **Source**: https://github.com/Azure/azure-cli
- **Docs**: https://learn.microsoft.com/en-us/cli/azure/reference-index
- **Install**: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli
- **Auth**: `az login --service-principal -u CLIENT_ID -p CLIENT_SECRET --tenant TENANT_ID`

## API References Used
| Service | Reference |
|---------|-----------|
| Resource Groups | https://learn.microsoft.com/en-us/cli/azure/group |
| Virtual Machines | https://learn.microsoft.com/en-us/cli/azure/vm |
| App Service / Web Apps | https://learn.microsoft.com/en-us/cli/azure/webapp |
| Storage | https://learn.microsoft.com/en-us/cli/azure/storage |
| Azure SQL | https://learn.microsoft.com/en-us/cli/azure/sql |
| AKS | https://learn.microsoft.com/en-us/cli/azure/aks |
| Azure Functions | https://learn.microsoft.com/en-us/cli/azure/functionapp |
| Container Instances | https://learn.microsoft.com/en-us/cli/azure/container |
| Key Vault | https://learn.microsoft.com/en-us/cli/azure/keyvault |

## License
MIT (Azure CLI)
