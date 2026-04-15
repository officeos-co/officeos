# Jenkins Skill — References

## Source CLI
- **Repository:** https://github.com/jenkinsci/jenkins
- **License:** MIT
- **Language:** Java
- **Official CLI jar:** https://www.jenkins.io/doc/book/managing/cli/

## API Documentation
- **Remote Access API:** https://www.jenkins.io/doc/book/using/remote-access-api/
- **Jobs:** `{url}/job/{name}/api/json`
- **Builds:** `{url}/job/{name}/{number}/api/json`
- **Console log:** `{url}/job/{name}/{number}/consoleText`
- **Progressive log:** `{url}/job/{name}/{number}/logText/progressiveText?start={offset}`
- **Queue:** `{url}/queue/api/json`
- **Nodes:** `{url}/computer/api/json`
- **Views:** `{url}/api/json?tree=views[name,url]`
- **Pipeline stages:** `{url}/job/{name}/{number}/wfapi/describe`

## Authentication
- Basic auth: `Authorization: Basic base64(user:api_token)`
- API token created at: `{jenkins_url}/user/{username}/configure`

## Notes
- Most POST endpoints (build, stop) require a CSRF crumb (`{url}/crumbIssuer/api/json`).
  This skill fetches the crumb automatically before each mutating request.
- Parameterized builds use `/buildWithParameters` instead of `/build`.
