using EnterpriseAgentOs.Api.Entities.Skills.GraphQL.Types;
using EnterpriseAgentOs.Api.Entities.Skills.Implementations;

namespace EnterpriseAgentOs.Api.Entities.Skills.GraphQL;

[ExtendObjectType("Query")]
public class NotionQueries
{
    [GraphQLDescription("Search the Notion workspace for pages matching a query")]
    public async Task<NotionSearchResult> NotionSearch(
        [Service] NotionSkill notion,
        [Service] ISkillService skills,
        [GraphQLDescription("Free-text search query")] string query,
        [GraphQLDescription("Max results (1-100)")] int pageSize = 10,
        CancellationToken ct = default)
    {
        var creds = await skills.GetDecryptedCredentialsAsync("notion", ct)
            ?? throw new GraphQLException("Notion skill not configured. Install and configure it on the Skills page.");
        return await notion.SearchAsync(new(query, pageSize), creds, ct);
    }

    [GraphQLDescription("Read a Notion page's content as plain text")]
    public async Task<NotionPageContent> NotionReadPage(
        [Service] NotionSkill notion,
        [Service] ISkillService skills,
        [GraphQLDescription("Page UUID from search results")] string pageId,
        CancellationToken ct = default)
    {
        var creds = await skills.GetDecryptedCredentialsAsync("notion", ct)
            ?? throw new GraphQLException("Notion skill not configured. Install and configure it on the Skills page.");
        return await notion.ReadPageAsync(new(pageId), creds, ct);
    }
}
