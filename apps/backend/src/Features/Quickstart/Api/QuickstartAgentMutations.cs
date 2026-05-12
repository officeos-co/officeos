namespace OffceOs.Api.Features.Quickstart;

[ExtendObjectType(typeof(GraphQLMutations))]
public class QuickstartAgentMutations
{
    [GraphQLDescription("Generates or edits a declarative YAML agent definition from quickstart chat input.")]
    public async Task<QuickstartAgentChatPayload> QuickstartAgentChat(
        QuickstartAgentChatInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IQuickstartAgentService quickstartAgents,
        CancellationToken ct)
    {
        try
        {
            var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
            var result = await quickstartAgents.ChatAsync(
                new QuickstartAgentChatRequest(
                    input.Message,
                    input.CurrentYaml,
                    input.Messages?.Select(message => new QuickstartAgentMessageRequest(message.Role, message.Content)).ToList(),
                    input.Provider,
                    input.Model),
                user.Id,
                workspace.Id,
                ct);

            return new QuickstartAgentChatPayload(
                result.Message,
                result.ConfigYaml,
                result.ConfigJson,
                result.Provider,
                result.Model);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(ex.Message)
                    .SetCode("VALIDATION")
                    .Build());
        }
    }
}
