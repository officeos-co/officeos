This is supposed to be a microservice which handels all the channel logic. The files are copied straight up from nanoclaw which is a lightweight rebuild of openclaw. It uses scripts/ and setup/ via cli to setup the stuff. We want to keep all the logic and refactor it to remove the cli entirelt. It should expose onboarding to our backend which will then be forwarded to the frontend.

I pasted quite a it of files which are non useful.

on the exqample of whatsapp i see several files /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/channels/setup/install-whatsapp.sh, /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/channels/setup/install-whatsapp-cloud.sh, /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/channels/setup/channels/whatsapp.ts

The same goes for all other popular channels.

The backend has pretty complex api for channels with mutation, queries, types, internal controller /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend/src/EnterpriseAgentOs.Api/Channels

# Consideration

Should channel get its own table in our database with migrations and so on. Or should that stay in C#?

# Implementation

1. Think trough the final architecture excatly how we want it to work.
2. delete files which we 100% know of are not needed
3. keep all redundant channel stuff and make graphql work like we want it so at the end the channel microservice should be fully working.
4. connect it to the /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend
