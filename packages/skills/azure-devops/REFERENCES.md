# Azure DevOps Skill — References

## Source CLI
- **Repository:** https://github.com/Azure/azure-devops-cli-extension
- **License:** MIT
- **Language:** Python
- **pip package:** `azure-devops`

## API Documentation
- **Azure DevOps REST API:** https://learn.microsoft.com/en-us/rest/api/azure/devops/
- **Projects:** https://learn.microsoft.com/en-us/rest/api/azure/devops/core/projects
- **Git Repositories:** https://learn.microsoft.com/en-us/rest/api/azure/devops/git/repositories
- **Pull Requests:** https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-requests
- **Work Items:** https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/work-items
- **Work Item Tracking WIQL:** https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/wiql
- **Builds:** https://learn.microsoft.com/en-us/rest/api/azure/devops/build/builds
- **Pipelines:** https://learn.microsoft.com/en-us/rest/api/azure/devops/pipelines/pipelines

## Authentication
- Basic auth: `Authorization: Basic base64(:token)` — username is empty, token is a PAT
- Base URL: `https://dev.azure.com/{organization}/`
- API version: `api-version=7.1`

## Notes
- Work item creation uses JSON Patch operations (RFC 6902).
- WIQL queries return a list of work item references; a second batch GET is needed to fetch full fields.
- Pipelines API is separate from the classic Builds API (`build/builds` vs `pipelines/pipelines`).
