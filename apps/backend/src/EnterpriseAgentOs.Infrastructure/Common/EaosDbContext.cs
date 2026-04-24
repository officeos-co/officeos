using EnterpriseAgentOs.Infrastructure.Common.Entities;

namespace EnterpriseAgentOs.Infrastructure.Common;

public sealed class EaosDbContext : DbContext
{
    public EaosDbContext(DbContextOptions<EaosDbContext> options) : base(options)
    {
    }

    public DbSet<AgentEntity> Agents => Set<AgentEntity>();
    public DbSet<ProviderEntity> Providers => Set<ProviderEntity>();
    public DbSet<SkillCredentialEntity> SkillCredentials => Set<SkillCredentialEntity>();
    public DbSet<OAuthTokenEntity> OAuthTokens => Set<OAuthTokenEntity>();
    public DbSet<OAuthGrantedScopeEntity> OAuthGrantedScopes => Set<OAuthGrantedScopeEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<DeviceCodeEntity> DeviceCodes => Set<DeviceCodeEntity>();
    public DbSet<BrowserSessionEntity> BrowserSessions => Set<BrowserSessionEntity>();
    public DbSet<SkillEntity> Skills => Set<SkillEntity>();
    public DbSet<AgentSkillEntity> AgentSkills => Set<AgentSkillEntity>();
    public DbSet<UserSubscriptionEntity> UserSubscriptions { get; set; } = null!;
    public DbSet<OrgSubscriptionEntity> OrgSubscriptions { get; set; } = null!;
    public DbSet<ChannelConnectionEntity> ChannelConnections => Set<ChannelConnectionEntity>();
    public DbSet<AgentChannelBindingEntity> AgentChannelBindings => Set<AgentChannelBindingEntity>();
    public DbSet<SystemEventEntity> SystemEvents => Set<SystemEventEntity>();
    public DbSet<AgentRateLimitEntity> AgentRateLimits => Set<AgentRateLimitEntity>();
    public DbSet<SkillLikeEntity> SkillLikes => Set<SkillLikeEntity>();
    public DbSet<SkillCommentEntity> SkillComments => Set<SkillCommentEntity>();
    public DbSet<AgentLogEntity> AgentLogs => Set<AgentLogEntity>();
    public DbSet<AgentToolPermissionEntity> AgentToolPermissions => Set<AgentToolPermissionEntity>();
    public DbSet<AgentTemplateEntity> AgentTemplates => Set<AgentTemplateEntity>();
    public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();
    public DbSet<OrgMemberEntity> OrgMembers => Set<OrgMemberEntity>();
    public DbSet<AgentMemoryEntity> AgentMemories => Set<AgentMemoryEntity>();
    public DbSet<AgentPersonalityEntity> AgentPersonalities => Set<AgentPersonalityEntity>();
    public DbSet<AgentCronJobEntity> AgentCronJobs => Set<AgentCronJobEntity>();
    public DbSet<AgentSessionEntity> AgentSessions => Set<AgentSessionEntity>();

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

        modelBuilder.Entity<ProviderEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Name).IsUnique();
            e.Property(p => p.Name).IsRequired().HasMaxLength(64);
            e.Property(p => p.DisplayName).IsRequired().HasMaxLength(128);
            e.Property(p => p.EncryptedApiKey).HasMaxLength(4096);
        });

        modelBuilder.Entity<SkillCredentialEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.SkillName).IsUnique();
            e.Property(s => s.SkillName).IsRequired().HasMaxLength(64);
            e.Property(s => s.EncryptedCredentials).HasMaxLength(16384);
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

        modelBuilder.Entity<AgentSkillEntity>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.AgentId, a.SkillName }).IsUnique();
            e.HasIndex(a => a.AgentId);
            e.Property(a => a.SkillName).IsRequired().HasMaxLength(64);
        });

        modelBuilder.Entity<SkillEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Name).IsUnique();
            e.Property(s => s.Name).IsRequired().HasMaxLength(64);
            e.Property(s => s.Title).IsRequired().HasMaxLength(128);
            e.Property(s => s.Logo).HasColumnType("text");
            e.Property(s => s.Doc).HasColumnType("text");
            e.Property(s => s.Readme).HasColumnType("text");
            e.Property(s => s.Changelog).HasColumnType("text");
            e.Property(s => s.License).HasMaxLength(64);
            e.Property(s => s.Repository).HasMaxLength(512);
            e.Property(s => s.Category).HasMaxLength(64);
            e.Property(s => s.AuthorName).HasMaxLength(128);
            e.Property(s => s.AuthorUrl).HasMaxLength(512);
            e.Property(s => s.ActionsJson).HasColumnType("jsonb");
            e.Property(s => s.CredentialFieldsJson).HasColumnType("jsonb");
            e.Property(s => s.ContributorsJson).HasColumnType("jsonb");
            e.Property(s => s.BundleS3Key).HasMaxLength(512);
            e.Property(s => s.Version).HasMaxLength(32);
            e.Property(s => s.Status).IsRequired().HasMaxLength(16);
            e.Property(s => s.BuildError).HasColumnType("text");
            e.Property(s => s.GitHubRepoUrl).HasMaxLength(512);
            e.Property(s => s.GitHubBranch).HasMaxLength(128);
            e.HasOne(s => s.Owner).WithMany().HasForeignKey(s => s.OwnerId);
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

        modelBuilder.Entity<SkillLikeEntity>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => new { l.UserId, l.SkillId }).IsUnique();
            e.HasIndex(l => l.SkillId);
            e.HasOne(l => l.Skill).WithMany().HasForeignKey(l => l.SkillId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SkillCommentEntity>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.SkillId);
            e.HasIndex(c => new { c.SkillId, c.CreatedAt });
            e.Property(c => c.Body).HasColumnType("text");
            e.HasOne(c => c.Skill).WithMany().HasForeignKey(c => c.SkillId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentLogEntity>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => l.AgentId);
            e.HasIndex(l => new { l.AgentId, l.Time });
            e.HasIndex(l => l.CorrelationId);
            e.Property(l => l.Content).HasColumnType("text");
            e.Property(l => l.Type).HasConversion<string>().HasMaxLength(32);
            e.HasOne(l => l.Agent).WithMany().HasForeignKey(l => l.AgentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentToolPermissionEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.AgentId, p.SkillName, p.ToolName }).IsUnique();
            e.HasIndex(p => p.AgentId);
            e.Property(p => p.Permission).HasConversion<string>().HasMaxLength(16);
            e.HasOne(p => p.Agent).WithMany().HasForeignKey(p => p.AgentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentTemplateEntity>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Name).IsUnique();
            e.Property(t => t.Prompt).HasColumnType("text");
            e.Property(t => t.IntegrationsJson).HasColumnType("text");
            e.Property(t => t.ChannelsJson).HasColumnType("text");
            e.HasOne(t => t.Owner).WithMany().HasForeignKey(t => t.OwnerId);
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
    }
}
