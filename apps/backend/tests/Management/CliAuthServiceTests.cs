using OffceOs.Application.Features.Management;
using OffceOs.Configuration;
using OffceOs.Database.Models;
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
        db.Users.Add(new UserEntity
        {
            Id = userId,
            Email = "cli@example.com",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var deviceCodes = new DeviceCodeRepository(db);
        var sessions = new SessionRepository(db);
        var service = new CliAuthService(deviceCodes, sessions, new FrontendConfig("https://dashboard.example.com"));

        var created = await service.CreateDeviceCodeAsync(new CliDeviceCodeRequest("test-runner"));
        var pending = await service.PollTokenAsync(created.DeviceCode);

        Assert.Equal("pending", pending.Status);
        Assert.Null(pending.AccessToken);

        await service.AuthorizeDeviceCodeAsync(created.UserCode, userId);
        var authorized = await service.PollTokenAsync(created.DeviceCode);

        Assert.Equal("authorized", authorized.Status);
        Assert.NotNull(authorized.AccessToken);

        var tokenHash = SessionTokenHasher.Hash(authorized.AccessToken!);
        var session = await sessions.GetByAsync(new SessionFilter { TokenHash = tokenHash });
        Assert.NotNull(session);
        Assert.Equal(userId, session.UserId);
        Assert.True(session.ExpiresAt > DateTime.UtcNow.AddDays(29));
    }
}
