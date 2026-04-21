namespace EnterpriseAgentOs.Api.Tests.Domain;


/// <summary>
/// Tests for OAuth2 scope management on OAuthTokenRecord.
/// Scenario: user connects Gmail (google provider, gmail scopes), then later connects
/// Google Sheets (same google provider, spreadsheets scope). The token must accumulate
/// scopes and correctly report coverage.
/// </summary>
public sealed class OAuthTokenScopeTests
{
    private static readonly string[] GmailScopes =
    [
        "https://www.googleapis.com/auth/gmail.modify",
        "https://www.googleapis.com/auth/gmail.send",
        "https://www.googleapis.com/auth/gmail.labels",
    ];

    private static readonly string[] SheetsScopes =
    [
        "https://www.googleapis.com/auth/spreadsheets",
    ];

    private static readonly string[] CalendarScopes =
    [
        "https://www.googleapis.com/auth/calendar",
        "https://www.googleapis.com/auth/calendar.events",
    ];

    private static OAuthTokenRecord CreateToken(params string[] scopes)
    {
        var token = new OAuthTokenRecord { Provider = "google" };
        token.ReplaceScopes(scopes);
        return token;
    }

    // =========================================================================
    // ReplaceScopes
    // =========================================================================

    [Fact]
    public void ReplaceScopes_StoresAllScopes()
    {
        var token = CreateToken(GmailScopes);
        Assert.Equal(3, token.GrantedScopes.Count);
        Assert.All(GmailScopes, s => Assert.Contains(token.GrantedScopes, gs => gs.Scope == s));
    }

    [Fact]
    public void ReplaceScopes_ClearsPreviousScopes()
    {
        var token = CreateToken(GmailScopes);
        token.ReplaceScopes(SheetsScopes);

        Assert.Single(token.GrantedScopes);
        Assert.Equal(SheetsScopes[0], token.GrantedScopes.Single().Scope);
    }

    [Fact]
    public void ReplaceScopes_DeduplicatesDuplicateScopes()
    {
        var token = new OAuthTokenRecord { Provider = "google" };
        token.ReplaceScopes(["https://www.googleapis.com/auth/gmail.send", "https://www.googleapis.com/auth/gmail.send"]);
        Assert.Single(token.GrantedScopes);
    }

    [Fact]
    public void ReplaceScopes_EmptyArray_ClearsAll()
    {
        var token = CreateToken(GmailScopes);
        token.ReplaceScopes([]);
        Assert.Empty(token.GrantedScopes);
    }

    [Fact]
    public void ReplaceScopes_SetsCorrectForeignKey()
    {
        var token = new OAuthTokenRecord { Provider = "google" };
        token.ReplaceScopes(GmailScopes);
        Assert.All(token.GrantedScopes, gs => Assert.Equal(token.Id, gs.OAuthTokenId));
    }

    // =========================================================================
    // CoversScopes
    // =========================================================================

    [Fact]
    public void CoversScopes_AllPresent_ReturnsTrue()
    {
        var token = CreateToken(GmailScopes);
        Assert.True(token.CoversScopes(GmailScopes));
    }

    [Fact]
    public void CoversScopes_SubsetPresent_ReturnsTrue()
    {
        var token = CreateToken(GmailScopes);
        Assert.True(token.CoversScopes(["https://www.googleapis.com/auth/gmail.send"]));
    }

    [Fact]
    public void CoversScopes_EmptyRequired_ReturnsTrue()
    {
        var token = CreateToken(GmailScopes);
        Assert.True(token.CoversScopes([]));
    }

    [Fact]
    public void CoversScopes_NoGrantedScopes_ReturnsFalse()
    {
        var token = CreateToken();
        Assert.False(token.CoversScopes(GmailScopes));
    }

    [Fact]
    public void CoversScopes_MissingSheetsScope_ReturnsFalse()
    {
        // User authenticated Gmail but not Sheets — Sheets should not be "configured"
        var token = CreateToken(GmailScopes);
        Assert.False(token.CoversScopes(SheetsScopes));
    }

    [Fact]
    public void CoversScopes_PartialOverlap_ReturnsFalse()
    {
        var token = CreateToken(GmailScopes);
        var mixed = new[] { "https://www.googleapis.com/auth/gmail.send", "https://www.googleapis.com/auth/spreadsheets" };
        Assert.False(token.CoversScopes(mixed));
    }

    [Fact]
    public void CoversScopes_CaseInsensitive()
    {
        var token = CreateToken(["https://www.googleapis.com/auth/gmail.send"]);
        Assert.True(token.CoversScopes(["HTTPS://WWW.GOOGLEAPIS.COM/AUTH/GMAIL.SEND"]));
    }

    // =========================================================================
    // MissingScopes
    // =========================================================================

    [Fact]
    public void MissingScopes_AllPresent_ReturnsEmpty()
    {
        var token = CreateToken(GmailScopes);
        Assert.Empty(token.MissingScopes(GmailScopes));
    }

    [Fact]
    public void MissingScopes_NonePresent_ReturnsAll()
    {
        var token = CreateToken(GmailScopes);
        var missing = token.MissingScopes(SheetsScopes);
        Assert.Equal(SheetsScopes, missing);
    }

    [Fact]
    public void MissingScopes_PartialOverlap_ReturnsOnlyMissing()
    {
        var token = CreateToken(GmailScopes);
        var required = new[] { "https://www.googleapis.com/auth/gmail.send", "https://www.googleapis.com/auth/spreadsheets" };
        var missing = token.MissingScopes(required);
        Assert.Single(missing);
        Assert.Equal("https://www.googleapis.com/auth/spreadsheets", missing[0]);
    }

    [Fact]
    public void MissingScopes_EmptyRequired_ReturnsEmpty()
    {
        var token = CreateToken(GmailScopes);
        Assert.Empty(token.MissingScopes([]));
    }

    // =========================================================================
    // MergedScopesWith — used by Start endpoint to build full auth URL
    // =========================================================================

    [Fact]
    public void MergedScopesWith_UnionsExistingAndNew()
    {
        // User has Gmail scopes, now requesting Sheets → auth URL should include both
        var token = CreateToken(GmailScopes);
        var merged = token.MergedScopesWith(SheetsScopes);

        Assert.Equal(4, merged.Count);
        Assert.All(GmailScopes, s => Assert.Contains(s, merged));
        Assert.Contains(SheetsScopes[0], merged);
    }

    [Fact]
    public void MergedScopesWith_DeduplicatesOverlappingScopes()
    {
        var token = CreateToken(GmailScopes);
        var merged = token.MergedScopesWith(GmailScopes);
        Assert.Equal(3, merged.Count);
    }

    [Fact]
    public void MergedScopesWith_EmptyExisting_ReturnsNew()
    {
        var token = CreateToken();
        var merged = token.MergedScopesWith(SheetsScopes);
        Assert.Single(merged);
        Assert.Contains(SheetsScopes[0], merged);
    }

    [Fact]
    public void MergedScopesWith_EmptyNew_ReturnsExisting()
    {
        var token = CreateToken(GmailScopes);
        var merged = token.MergedScopesWith([]);
        Assert.Equal(3, merged.Count);
    }

    // =========================================================================
    // Full scenario: Gmail → Sheets → Calendar incremental auth
    // =========================================================================

    [Fact]
    public void Scenario_GmailThenSheets_IncrementalAuth()
    {
        // Step 1: User connects Gmail
        var token = CreateToken(GmailScopes);
        Assert.True(token.CoversScopes(GmailScopes));
        Assert.False(token.CoversScopes(SheetsScopes));

        // Step 2: User clicks "Upgrade permissions" for Sheets.
        // Start endpoint merges existing + requested scopes for the auth URL.
        var authUrlScopes = token.MergedScopesWith(SheetsScopes);
        Assert.Equal(4, authUrlScopes.Count);

        // Step 3: Google returns all 4 scopes on the new token.
        // Callback calls ReplaceScopes with what Google returned.
        token.ReplaceScopes(authUrlScopes);

        // Step 4: Both Gmail and Sheets should now be covered.
        Assert.True(token.CoversScopes(GmailScopes));
        Assert.True(token.CoversScopes(SheetsScopes));
        Assert.Equal(4, token.GrantedScopes.Count);
    }

    [Fact]
    public void Scenario_GmailThenSheetsThenCalendar_ThreeStepAuth()
    {
        // Step 1: Gmail
        var token = CreateToken(GmailScopes);

        // Step 2: Add Sheets
        var merged = token.MergedScopesWith(SheetsScopes);
        token.ReplaceScopes(merged);
        Assert.Equal(4, token.GrantedScopes.Count);

        // Step 3: Add Calendar
        merged = token.MergedScopesWith(CalendarScopes);
        token.ReplaceScopes(merged);

        Assert.Equal(6, token.GrantedScopes.Count);
        Assert.True(token.CoversScopes(GmailScopes));
        Assert.True(token.CoversScopes(SheetsScopes));
        Assert.True(token.CoversScopes(CalendarScopes));
    }

    [Fact]
    public void Scenario_GoogleReturnsFewerScopesThanRequested_UserDeniedSome()
    {
        // User had Gmail scopes. Requested Gmail + Sheets but denied Sheets in Google consent.
        var token = CreateToken(GmailScopes);
        var authUrlScopes = token.MergedScopesWith(SheetsScopes);
        Assert.Equal(4, authUrlScopes.Count);

        // Google only returns Gmail scopes (user unchecked Sheets)
        token.ReplaceScopes(GmailScopes);

        Assert.True(token.CoversScopes(GmailScopes));
        Assert.False(token.CoversScopes(SheetsScopes));
        Assert.Equal(["https://www.googleapis.com/auth/spreadsheets"], token.MissingScopes(SheetsScopes));
    }

    [Fact]
    public void Scenario_ReauthSameSkill_NoScopeLoss()
    {
        // User re-authenticates Gmail (e.g., token expired). Scopes should stay the same.
        var token = CreateToken([.. GmailScopes, .. SheetsScopes]);
        Assert.Equal(4, token.GrantedScopes.Count);

        // Re-auth only requests Gmail scopes, but Start merges with existing
        var authUrlScopes = token.MergedScopesWith(GmailScopes);
        Assert.Equal(4, authUrlScopes.Count);

        // Google returns the full set
        token.ReplaceScopes(authUrlScopes);
        Assert.True(token.CoversScopes(GmailScopes));
        Assert.True(token.CoversScopes(SheetsScopes));
    }

    [Fact]
    public void Scenario_DisconnectAndReconnect_StartsClean()
    {
        // User had Gmail + Sheets, then disconnects (token deleted) and reconnects with only Sheets.
        var token = CreateToken([.. GmailScopes, .. SheetsScopes]);
        Assert.Equal(4, token.GrantedScopes.Count);

        // Simulate disconnect + reconnect: new token with only Sheets
        var newToken = CreateToken(SheetsScopes);
        Assert.False(newToken.CoversScopes(GmailScopes));
        Assert.True(newToken.CoversScopes(SheetsScopes));
    }

    // =========================================================================
    // GetScopeSet
    // =========================================================================

    [Fact]
    public void GetScopeSet_ReturnsAllScopesAsCaseInsensitiveSet()
    {
        var token = CreateToken(GmailScopes);
        var set = token.GetScopeSet();
        Assert.Equal(3, set.Count);
        // Case insensitive
        Assert.Contains("HTTPS://WWW.GOOGLEAPIS.COM/AUTH/GMAIL.MODIFY", set);
    }

    [Fact]
    public void GetScopeSet_EmptyWhenNoScopes()
    {
        var token = CreateToken();
        Assert.Empty(token.GetScopeSet());
    }
}
