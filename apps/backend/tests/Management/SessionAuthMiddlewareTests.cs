using OffceOs.Api.Common.Middleware;
using OffceOs.Configuration;
using OffceOs.Database.Models;
using OffceOs.Domain.Features.Management;
using OffceOs.Infrastructure.Features.Management;
using OffceOs.Tests.Shared;

namespace OffceOs.Tests.Management;

public sealed class SessionAuthMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_authenticates_bearer_token_from_session_table()
    {
        const string token = "cli-token";
        var userId = Guid.NewGuid();

        await using var db = TestDbFactory.Create("session-auth");
        db.Users.Add(new UserEntity
        {
            Id = userId,
            Email = "cli@example.com",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        });
        db.Sessions.Add(new SessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = SessionTokenHasher.Hash(token),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var services = new ServiceCollection()
            .AddSingleton(db)
            .AddSingleton<Microsoft.Extensions.Caching.Distributed.IDistributedCache, InMemoryDistributedCache>()
            .AddScoped<ISessionRepository, SessionRepository>()
            .BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Headers.Authorization = $"Bearer {token}";

        var middleware = new SessionAuthMiddleware(
            _ => Task.CompletedTask,
            new SessionAuthConfig());

        await middleware.InvokeAsync(context);

        var user = Assert.IsType<UserRecord>(context.Items["User"]);
        Assert.Equal(userId, user.Id);
        Assert.Equal("cli@example.com", user.Email);
    }
}
