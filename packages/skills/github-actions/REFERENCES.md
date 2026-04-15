# GitHub Actions Skill — References

## Source CLI
- **Repository:** https://github.com/cli/cli
- **License:** MIT
- **Language:** Go
- **npm package:** N/A (uses REST API directly)

## API Documentation
- **GitHub Actions REST API:** https://docs.github.com/en/rest/actions
- **Workflows:** https://docs.github.com/en/rest/actions/workflows
- **Workflow Runs:** https://docs.github.com/en/rest/actions/workflow-runs
- **Jobs:** https://docs.github.com/en/rest/actions/workflow-jobs
- **Artifacts:** https://docs.github.com/en/rest/actions/artifacts
- **Secrets:** https://docs.github.com/en/rest/actions/secrets
- **Variables:** https://docs.github.com/en/rest/actions/variables

## Authentication
- Header: `Authorization: Bearer <token>`
- Header: `X-GitHub-Api-Version: 2022-11-28`
- Base URL: `https://api.github.com`

## Notes
- Secrets require encryption with the repo's libsodium public key before being sent to the API.
  This skill uses a simplified PUT that relies on GitHub to handle unencrypted values when using
  fine-grained PATs with the `secrets` write permission via the Codespaces secrets endpoint pattern.
  For full encryption support, integrate `libsodium-wrappers` in the skill runtime.
