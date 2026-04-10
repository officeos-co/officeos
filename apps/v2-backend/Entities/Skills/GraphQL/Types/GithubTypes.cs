namespace EnterpriseAgentOs.Api.Entities.Skills.GraphQL.Types;

public class GithubRepo
{
    public string? FullName { get; init; }
    public bool? Private { get; init; }
    public string? Description { get; init; }
    public string? HtmlUrl { get; init; }
    public string? DefaultBranch { get; init; }
}

public class GithubIssue
{
    public long? Number { get; init; }
    public string? Title { get; init; }
    public string? State { get; init; }
    public string? Author { get; init; }
    public string? HtmlUrl { get; init; }
}

public class GithubPr
{
    public long? Number { get; init; }
    public string? Title { get; init; }
    public string? State { get; init; }
    public string? Author { get; init; }
    public string? HtmlUrl { get; init; }
    public bool? Draft { get; init; }
}
