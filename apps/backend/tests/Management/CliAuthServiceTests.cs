using OffceOs.Application.Features.Management;
using OffceOs.Configuration;
using OffceOs.Domain.Features.Management;
using OffceOs.Infrastructure.Features.Management;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Management;

public sealed class CliAuthServiceTests
{
    [Fact]
    public async Task Device_code_flow_returns_session_backed_token_once_authorized()
    {
        var userId = Guid.NewGuid();
        await using var db = TestDbFactory.Create("cli-auth");
        var deviceCodes = new DeviceCodeRepository(db);
        var auth = new FakeAuthService("returned-token");
        var service = new CliAuthService(deviceCodes, auth, new FrontendConfig("https://dashboard.example.com"));

        var created = await service.CreateDeviceCodeAsync(new CliDeviceCodeRequest("test-runner"));
        var pending = await service.PollTokenAsync(created.DeviceCode);

        Assert.Equal("pending", pending.Status);
        Assert.Null(pending.AccessToken);

        await service.AuthorizeDeviceCodeAsync(created.UserCode, userId);
        var authorized = await service.PollTokenAsync(created.DeviceCode);

        Assert.Equal("authorized", authorized.Status);
        Assert.Equal("returned-token", authorized.AccessToken);
        Assert.Equal(userId, auth.UserId);
        Assert.Equal(TimeSpan.FromDays(30), auth.Lifetime);
    }

    private sealed class FakeAuthService : IAuthService
    {
        private readonly string _token;

        public FakeAuthService(string token) => _token = token;

        public Guid? UserId { get; private set; }
        public TimeSpan? Lifetime { get; private set; }

        public Task<string> CreateSessionTokenAsync(Guid userId, TimeSpan lifetime, CancellationToken ct = default)
        {
            UserId = userId;
            Lifetime = lifetime;
            return Task.FromResult(_token);
        }

        public GoogleLoginResult BuildGoogleLoginUrl(string? redirectUri = null) => throw new NotImplementedException();
        public Task<GoogleCallbackResult> HandleGoogleCallbackAsync(string code, string? redirectUri = null, CancellationToken ct = default) => throw new NotImplementedException();
        public GitHubLoginResult BuildGitHubLoginUrl(string? redirectUri = null) => throw new NotImplementedException();
        public Task<GitHubCallbackResult> HandleGitHubCallbackAsync(string code, string? redirectUri = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UserRecord> UpdateProfileAsync(Guid userId, string? name, string? displayName, string? timezone, string? notificationPrefsJson, string? preferences, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> LogoutAsync(string? sessionToken, CancellationToken ct = default) => throw new NotImplementedException();
    }
}
