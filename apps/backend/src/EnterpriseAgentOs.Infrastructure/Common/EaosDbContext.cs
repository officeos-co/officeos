using EnterpriseAgentOs.Infrastructure.Common.Entities;

namespace EnterpriseAgentOs.Infrastructure.Common;

public sealed class EaosDbContext : DbContext
{
    public EaosDbContext(DbContextOptions<EaosDbContext> options) : base(options)
    {
    }

    public DbSet<AgentEntity> Agents => Set<AgentEntity>();

    public DbSet<OAuthTokenEntity> OAuthTokens => Set<OAuthTokenEntity>();
    public DbSet<OAuthGrantedScopeEntity> OAuthGrantedScopes => Set<OAuthGrantedScopeEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<DeviceCodeEntity> DeviceCodes => Set<DeviceCodeEntity>();
    public DbSet<BrowserSessionEntity> BrowserSessions => Set<BrowserSessionEntity>();
    public DbSet<UserSubscriptionEntity> UserSubscriptions { get; set; } = null!;
    public DbSet<OrgSubscriptionEntity> OrgSubscriptions { get; set; } = null!;
    public DbSet<ChannelConnectionEntity> ChannelConnections => Set<ChannelConnectionEntity>();
    public DbSet<AgentChannelBindingEntity> AgentChannelBindings => Set<AgentChannelBindingEntity>();
    public DbSet<SystemEventEntity> SystemEvents => Set<SystemEventEntity>();
    public DbSet<AgentRateLimitEntity> AgentRateLimits => Set<AgentRateLimitEntity>();
    public DbSet<AgentLogEntity> AgentLogs => Set<AgentLogEntity>();
    public DbSet<AgentToolPermissionEntity> AgentToolPermissions => Set<AgentToolPermissionEntity>();
    public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();
    public DbSet<OrgMemberEntity> OrgMembers => Set<OrgMemberEntity>();
    public DbSet<AgentMemoryEntity> AgentMemories => Set<AgentMemoryEntity>();
    public DbSet<AgentPersonalityEntity> AgentPersonalities => Set<AgentPersonalityEntity>();
    public DbSet<AgentCronJobEntity> AgentCronJobs => Set<AgentCronJobEntity>();
    public DbSet<AgentSessionEntity> AgentSessions => Set<AgentSessionEntity>();
    public DbSet<AgentSessionContextEntity> AgentSessionContexts => Set<AgentSessionContextEntity>();
    public DbSet<AgentRunEntity> AgentRuns => Set<AgentRunEntity>();

    public DbSet<McpServerEntity> McpServers => Set<McpServerEntity>();
    public DbSet<AgentMcpServerEntity> AgentMcpServers => Set<AgentMcpServerEntity>();
    public DbSet<McpCredentialEntity> McpCredentials => Set<McpCredentialEntity>();
    public DbSet<AtlasConnectorConnectionEntity> AtlasConnectorConnections => Set<AtlasConnectorConnectionEntity>();
    public DbSet<AtlasEntityStatusEntity> AtlasEntityStatuses => Set<AtlasEntityStatusEntity>();
    public DbSet<AtlasIndexJobEntity> AtlasIndexJobs => Set<AtlasIndexJobEntity>();
    public DbSet<AtlasIndexedRecordEntity> AtlasIndexedRecords => Set<AtlasIndexedRecordEntity>();
    public DbSet<AtlasActivityEntity> AtlasActivity => Set<AtlasActivityEntity>();
    public DbSet<AtlasRequestHistoryEntity> AtlasRequestHistory => Set<AtlasRequestHistoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentEntity>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Name).IsRequired().HasMaxLength(200);
            e.Property(a => a.Provider).IsRequired().HasMaxLength(64);
            e.Property(a => a.Status).IsRequired().HasMaxLength(32);
            e.Property(a => a.PodName).HasMaxLength(128);
            e.Property(a => a.ServiceUrl).HasMaxLength(256);
            e.Property(a => a.EncryptedBackendToken).HasMaxLength(4096);
            e.Property(a => a.Prompt).HasColumnType("text");
        });

        modelBuilder.Entity<OAuthTokenEntity>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.Provider).IsUnique();
            e.Property(o => o.Provider).IsRequired().HasMaxLength(32);
            e.Property(o => o.EncryptedAccessToken).HasMaxLength(16384);
            e.Property(o => o.EncryptedRefreshToken).HasMaxLength(16384);
            e.Property(o => o.Email).HasMaxLength(256);
            e.HasMany(o => o.GrantedScopes).WithOne(s => s.OAuthToken).HasForeignKey(s => s.OAuthTokenId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OAuthGrantedScopeEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.OAuthTokenId, s.Scope }).IsUnique();
            e.Property(s => s.Scope).IsRequired().HasMaxLength(512);
        });

        modelBuilder.Entity<UserEntity>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.GoogleSubjectId).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<SessionEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.TokenHash).IsUnique();
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
        });

        modelBuilder.Entity<DeviceCodeEntity>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.DeviceCode).IsUnique();
            e.HasIndex(d => d.UserCode);
            e.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<BrowserSessionEntity>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => b.AgentId).IsUnique();
            e.Property(b => b.CookiesJson).HasColumnType("text");
        });

        modelBuilder.Entity<UserSubscriptionEntity>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.UserId).IsUnique();
            e.Property(u => u.Plan).IsRequired().HasMaxLength(32);
            e.Property(u => u.BillingCycle).IsRequired().HasMaxLength(16);
            e.Property(u => u.StripeCustomerId).HasMaxLength(256);
            e.Property(u => u.StripeSubscriptionId).HasMaxLength(256);
            e.Property(u => u.StripeOverageItemId).HasMaxLength(256);
        });

        modelBuilder.Entity<OrgSubscriptionEntity>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.OrganizationId).IsUnique();
            e.Property(o => o.OrganizationId).IsRequired().HasMaxLength(256);
            e.Property(o => o.Plan).IsRequired().HasMaxLength(32);
            e.Property(o => o.StripeCustomerId).HasMaxLength(256);
            e.Property(o => o.StripeSubscriptionId).HasMaxLength(256);
            e.Property(o => o.StripeOverageItemId).HasMaxLength(256);
        });

        modelBuilder.Entity<ChannelConnectionEntity>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.ChannelType).IsRequired().HasMaxLength(32);
            e.Property(c => c.DisplayName).IsRequired().HasMaxLength(200);
            e.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
        });

        modelBuilder.Entity<AgentChannelBindingEntity>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => new { b.AgentId, b.ChannelConnectionId }).IsUnique();
            e.Property(b => b.Config).HasColumnType("text");
            e.HasOne(b => b.Agent).WithMany().HasForeignKey(b => b.AgentId);
            e.HasOne(b => b.ChannelConnection).WithMany().HasForeignKey(b => b.ChannelConnectionId);
        });

        modelBuilder.Entity<SystemEventEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.CreatedAt);
            e.HasIndex(s => s.Severity);
            e.HasIndex(s => s.Category);
            e.HasIndex(s => s.SkillName);
            e.HasIndex(s => s.AgentId);
            e.Property(s => s.DetailJson).HasColumnType("text");
        });

        modelBuilder.Entity<AgentRateLimitEntity>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.AgentId, r.BucketKey, r.WindowStart }).IsUnique();
            e.HasIndex(r => r.AgentId);
            e.Property(r => r.BucketKey).IsRequired().HasMaxLength(64);
        });

        modelBuilder.Entity<AgentLogEntity>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => l.AgentId);
            e.HasIndex(l => new { l.AgentId, l.Time });
            e.HasIndex(l => l.CorrelationId);
            e.Property(l => l.Content).HasColumnType("text");
            e.Property(l => l.Type).HasConversion<string>().HasMaxLength(32);
            e.HasOne(l => l.Agent).WithMany().HasForeignKey(l => l.AgentId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentToolPermissionEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.AgentId, p.SkillName, p.ToolName }).IsUnique();
            e.HasIndex(p => p.AgentId);
            e.Property(p => p.Permission).HasConversion<string>().HasMaxLength(16);
            e.HasOne(p => p.Agent).WithMany().HasForeignKey(p => p.AgentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganizationEntity>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<OrgMemberEntity>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.OrganizationId, m.Email }).IsUnique();
            e.HasIndex(m => m.UserId);
            e.Property(m => m.Email).IsRequired().HasMaxLength(256);
            e.Property(m => m.Role).IsRequired().HasMaxLength(16);
            e.Property(m => m.Status).IsRequired().HasMaxLength(16);
        });

        modelBuilder.Entity<AgentMemoryEntity>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Key).HasMaxLength(512).IsRequired();
            e.Property(m => m.Content).HasColumnType("text").IsRequired();
            e.HasIndex(m => new { m.AgentId, m.Key }).IsUnique();
            e.HasOne(m => m.Agent).WithMany()
                .HasForeignKey(m => m.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentPersonalityEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.FileName).HasMaxLength(128).IsRequired();
            e.Property(p => p.Content).HasColumnType("text").IsRequired();
            e.HasIndex(p => new { p.AgentId, p.FileName }).IsUnique();
            e.HasOne(p => p.Agent).WithMany()
                .HasForeignKey(p => p.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentCronJobEntity>(e =>
        {
            e.HasKey(j => j.Id);
            e.HasIndex(j => j.AgentId);
            e.Property(j => j.Name).IsRequired().HasMaxLength(200);
            e.Property(j => j.Expression).IsRequired().HasMaxLength(64);
            e.Property(j => j.Prompt).HasColumnType("text").IsRequired();
        });

        modelBuilder.Entity<AgentSessionEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.AgentId, s.Status });
            e.Property(s => s.Status).IsRequired().HasMaxLength(16);
            e.HasOne(s => s.Agent).WithMany()
                .HasForeignKey(s => s.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentSessionContextEntity>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.AgentId).IsUnique();
            e.Property(c => c.Summary).HasColumnType("text");
            e.HasOne(c => c.Agent).WithMany()
                .HasForeignKey(c => c.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentRunEntity>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.AgentId);
            e.HasIndex(r => r.ParentRunId);
            e.HasIndex(r => r.Status);
            e.Property(r => r.Kind).IsRequired().HasMaxLength(16);
            e.Property(r => r.Status).IsRequired().HasMaxLength(32);
            e.Property(r => r.Name).IsRequired().HasMaxLength(128);
            e.Property(r => r.Description).HasColumnType("text");
            e.Property(r => r.Prompt).HasColumnType("text");
            e.Property(r => r.Result).HasColumnType("text");
            e.Property(r => r.Error).HasColumnType("text");
            e.HasOne(r => r.Agent).WithMany()
                .HasForeignKey(r => r.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<McpServerEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Name).IsUnique();
            e.Property(s => s.Name).IsRequired().HasMaxLength(64);
            e.Property(s => s.Title).IsRequired().HasMaxLength(128);
            e.Property(s => s.TransportType).IsRequired().HasMaxLength(32);
            e.Property(s => s.Command).HasMaxLength(256);
            e.Property(s => s.Args).HasMaxLength(2048);
            e.Property(s => s.Url).HasMaxLength(512);
            e.Property(s => s.Logo).HasColumnType("text");
            e.Property(s => s.Category).HasMaxLength(64);
            e.Property(s => s.CredentialFieldsJson).HasColumnType("jsonb");
            e.Property(s => s.Subtitle).HasMaxLength(256);
            e.Property(s => s.AuthorName).HasMaxLength(128);
            e.Property(s => s.AuthorUrl).HasMaxLength(512);
            e.Property(s => s.DocumentationUrl).HasMaxLength(512);
            e.Property(s => s.RepositoryUrl).HasMaxLength(512);
            e.Property(s => s.ToolsJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<AgentMcpServerEntity>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.AgentId, a.McpServerName }).IsUnique();
            e.HasIndex(a => a.AgentId);
            e.Property(a => a.McpServerName).IsRequired().HasMaxLength(64);
        });

        modelBuilder.Entity<McpCredentialEntity>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.McpServerName).IsUnique();
            e.Property(c => c.McpServerName).IsRequired().HasMaxLength(64);
            e.Property(c => c.EncryptedCredentials).HasMaxLength(16384);
        });

        modelBuilder.Entity<AtlasConnectorConnectionEntity>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Provider);
            e.Property(c => c.Provider).IsRequired().HasMaxLength(32);
            e.Property(c => c.WorkspaceName).IsRequired().HasMaxLength(128);
            e.Property(c => c.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(c => c.RepositoriesJson).HasColumnType("jsonb");
            e.Property(c => c.EntitiesJson).HasColumnType("jsonb");
            e.Property(c => c.Status).IsRequired().HasMaxLength(32);
            e.Property(c => c.Error).HasColumnType("text");
            e.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AtlasEntityStatusEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.ConnectionId, s.Entity }).IsUnique();
            e.Property(s => s.Entity).IsRequired().HasMaxLength(64);
            e.Property(s => s.Status).IsRequired().HasMaxLength(32);
            e.Property(s => s.Error).HasColumnType("text");
            e.HasOne(s => s.Connection).WithMany().HasForeignKey(s => s.ConnectionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AtlasIndexJobEntity>(e =>
        {
            e.HasKey(j => j.Id);
            e.HasIndex(j => j.Status);
            e.HasIndex(j => j.ConnectionId);
            e.Property(j => j.Status).IsRequired().HasMaxLength(32);
            e.Property(j => j.Error).HasColumnType("text");
            e.HasOne(j => j.Connection).WithMany().HasForeignKey(j => j.ConnectionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AtlasIndexedRecordEntity>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.ConnectionId, r.Entity, r.ExternalId }).IsUnique();
            e.HasIndex(r => new { r.ConnectionId, r.Entity });
            e.Property(r => r.Entity).IsRequired().HasMaxLength(64);
            e.Property(r => r.ExternalId).IsRequired().HasMaxLength(512);
            e.Property(r => r.Title).HasMaxLength(512);
            e.Property(r => r.SearchText).HasColumnType("text");
            e.Property(r => r.RawJson).HasColumnType("jsonb");
            e.HasOne(r => r.Connection).WithMany().HasForeignKey(r => r.ConnectionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AtlasActivityEntity>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.ConnectionId);
            e.HasIndex(a => a.CreatedAt);
            e.Property(a => a.Type).IsRequired().HasMaxLength(64);
            e.Property(a => a.Entity).HasMaxLength(64);
            e.Property(a => a.Message).IsRequired().HasMaxLength(512);
            e.Property(a => a.DetailsJson).HasColumnType("jsonb");
            e.HasOne(a => a.Connection).WithMany().HasForeignKey(a => a.ConnectionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AtlasRequestHistoryEntity>(e =>
        {
            e.HasKey(h => h.Id);
            e.HasIndex(h => h.ConnectionId);
            e.HasIndex(h => h.CreatedAt);
            e.Property(h => h.Type).IsRequired().HasMaxLength(16);
            e.Property(h => h.Entity).IsRequired().HasMaxLength(64);
            e.Property(h => h.Action).IsRequired().HasMaxLength(64);
            e.Property(h => h.ParamsJson).HasColumnType("jsonb");
            e.Property(h => h.Error).HasColumnType("text");
            e.HasOne(h => h.Connection).WithMany().HasForeignKey(h => h.ConnectionId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
