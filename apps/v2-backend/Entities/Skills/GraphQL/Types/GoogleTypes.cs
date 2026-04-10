namespace EnterpriseAgentOs.Api.Entities.Skills.GraphQL.Types;

public class DriveFile
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? MimeType { get; init; }
    public string? WebViewLink { get; init; }
    public string? ModifiedTime { get; init; }
}

public class CalendarEvent
{
    public string? Id { get; init; }
    public string? Summary { get; init; }
    public string? Start { get; init; }
    public string? End { get; init; }
    public string? HtmlLink { get; init; }
}
