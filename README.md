# OfficeOS

## Backend

We manage agents at scale.
Agents consist of tools in the backend being called by the agent loop. Agents live in sessions. Agents have tool permissions.
Integrations provide several tools in a grapql schema. Integrations then are saved in the database. Exectuion lives in a separate pod the integration executor. Integrations contain a skill.md this is injected into agent context. Integrations are graphql but are mapped to skill exec tool which acts like a cli. Graphql mapping is done deterministically.
Channels are kinda separated they each have an onboarding defined in the backend forwarded to the frontend trough onboardingsteps. If connected it should act like a webhook.and just in bot ways forwarding events.
Agents have a central logging system genralized as agent logs which all events should go trough e.g. the response of the agent and so on.
We use stripe for payment and have subscription yearly, monthly for enterprise and casuals. After certain limit token are metered if enabled.
We use clean architecture separating codebase into api, domain, application and infrastructure. This separation is pretty good but still gets really complicated. We really work with interface abstraction. And with rich domain models this is really good.

## Dashboard

Dashboard is a simple next.js project it follows bulletproof react patterns. Basically it just uses apollo to call graphql stuff sounds simple but isnt too simple. It consists of 3 important sections in the sidebbar which also are the domain troughout the whole repo. 1. agents with quickstart, agents, integrations and channels. 2. analytics with central logging, billing and usage 3. manage with profile, team and billing.

## Landing

Landing is not complicated at all its basically pretty standalone

# Blocker

Whats blocking me is the channel integrations and generally development speed. I literally dont know all about the application and dont understand everything like all the middlewares /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend/src/EnterpriseAgentOs.Api/Common/Middleware/AgentTokenAuthAttribute.cs. Also the api is kind of a mess a mix between graphql and rest. Most operations go trough several layers of abstraction. We kinda tried to implement a global error system but idk to what extend its done yet. Also we would need to implement a global event system in order to decouple services i imagine that would be especially important with logging and maybe channels.

Sometime we work with scope factory and then do something like using var scope = \_scopeFactory.CreateScope();
var cronRepo = scope.ServiceProvider.GetRequiredService<IAgentCronJobRepository>();
var logService = scope.ServiceProvider.GetRequiredService<IAgentLogService>(); which i dont like. Id want this not in the body.

Generally maybe we should consider going into microservices. And whats bugging me the most right now are the channel integrations i feel like everything else is pretty clear but the channel integrations are just really hard and there is no perfect open source solution.

Generally the complexity id say is really big. Although its handled pretty well. Its definitley an enterprise level software.

https://github.com/42wim/matterbridge
