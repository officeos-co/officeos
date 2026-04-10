using EnterpriseAgentOs.Api.Entities.Skills.GraphQL.Types;
using EnterpriseAgentOs.Api.Entities.Skills.Implementations;

namespace EnterpriseAgentOs.Api.Entities.Skills.GraphQL;

[ExtendObjectType("Query")]
public class GoogleQueries
{
    [GraphQLDescription("Search Google Drive for files matching a query")]
    public async Task<List<DriveFile>> GoogleDriveSearch(
        [Service] GoogleSkill google,
        [Service] ISkillService skills,
        [GraphQLDescription("Search text (matches file names)")] string query,
        [GraphQLDescription("Max results (1-100)")] int pageSize = 10,
        CancellationToken ct = default)
    {
        var creds = await skills.GetDecryptedCredentialsAsync("google", ct)
            ?? throw new GraphQLException("Google skill not configured. Install and configure it on the Skills page.");
        return await google.DriveSearchAsync(new(query, pageSize), creds, ct);
    }

    [GraphQLDescription("List upcoming events from Google Calendar")]
    public async Task<List<CalendarEvent>> GoogleCalendarUpcoming(
        [Service] GoogleSkill google,
        [Service] ISkillService skills,
        [GraphQLDescription("Max events to return (1-50)")] int maxResults = 10,
        CancellationToken ct = default)
    {
        var creds = await skills.GetDecryptedCredentialsAsync("google", ct)
            ?? throw new GraphQLException("Google skill not configured. Install and configure it on the Skills page.");
        return await google.CalendarUpcomingAsync(new(maxResults), creds, ct);
    }
}
