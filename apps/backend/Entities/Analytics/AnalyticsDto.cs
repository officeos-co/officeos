namespace EnterpriseAgentOs.Api.Entities.Analytics;

public record CaptureEventInput(string Name, Dictionary<string, object?>? Properties);
