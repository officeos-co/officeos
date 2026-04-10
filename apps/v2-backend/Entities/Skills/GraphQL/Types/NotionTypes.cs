namespace EnterpriseAgentOs.Api.Entities.Skills.GraphQL.Types;

public class NotionPage
{
    public string? Id { get; init; }
    public string Title { get; init; } = "";
    public string? Url { get; init; }
    public string? ObjectType { get; init; }
}

public class NotionSearchResult
{
    public List<NotionPage> Results { get; init; } = new();
}

public class NotionPageContent
{
    public string PageId { get; init; } = "";
    public string Text { get; init; } = "";
}
