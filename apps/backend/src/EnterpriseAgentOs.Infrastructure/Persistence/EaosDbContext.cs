namespace EnterpriseAgentOs.Infrastructure.Persistence;

public sealed class EaosDbContext : DbContext
{
    public EaosDbContext(DbContextOptions<EaosDbContext> options) : base(options)
    {
    }

    public DbSet<EnterpriseAgentOs.Domain.Models.AgentRecord> Agents => Set<EnterpriseAgentOs.Domain.Models.AgentRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.ProviderRecord> Providers => Set<EnterpriseAgentOs.Domain.Models.ProviderRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.SkillCredentialRecord> SkillCredentials => Set<EnterpriseAgentOs.Domain.Models.SkillCredentialRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.UserRecord> Users => Set<EnterpriseAgentOs.Domain.Models.UserRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.SessionRecord> Sessions => Set<EnterpriseAgentOs.Domain.Models.SessionRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.DeviceCodeRecord> DeviceCodes => Set<EnterpriseAgentOs.Domain.Models.DeviceCodeRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.BrowserSessionRecord> BrowserSessions => Set<EnterpriseAgentOs.Domain.Models.BrowserSessionRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.SkillRecord> Skills => Set<EnterpriseAgentOs.Domain.Models.SkillRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.AgentSkillRecord> AgentSkills => Set<EnterpriseAgentOs.Domain.Models.AgentSkillRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.UserSubscription> UserSubscriptions { get; set; } = null!;
    public DbSet<EnterpriseAgentOs.Domain.Models.OrgSubscription> OrgSubscriptions { get; set; } = null!;
    public DbSet<EnterpriseAgentOs.Domain.Models.ChannelConnectionRecord> ChannelConnections => Set<EnterpriseAgentOs.Domain.Models.ChannelConnectionRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.AgentChannelBindingRecord> AgentChannelBindings => Set<EnterpriseAgentOs.Domain.Models.AgentChannelBindingRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.SystemEventRecord> SystemEvents => Set<EnterpriseAgentOs.Domain.Models.SystemEventRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.AgentRateLimitRecord> AgentRateLimits => Set<EnterpriseAgentOs.Domain.Models.AgentRateLimitRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.SkillLikeRecord> SkillLikes => Set<EnterpriseAgentOs.Domain.Models.SkillLikeRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.SkillCommentRecord> SkillComments => Set<EnterpriseAgentOs.Domain.Models.SkillCommentRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.AgentLogRecord> AgentLogs => Set<EnterpriseAgentOs.Domain.Models.AgentLogRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.AgentToolPermissionRecord> AgentToolPermissions => Set<EnterpriseAgentOs.Domain.Models.AgentToolPermissionRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.AgentTemplateRecord> AgentTemplates => Set<EnterpriseAgentOs.Domain.Models.AgentTemplateRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.OrganizationRecord> Organizations => Set<EnterpriseAgentOs.Domain.Models.OrganizationRecord>();
    public DbSet<EnterpriseAgentOs.Domain.Models.OrgMemberRecord> OrgMembers => Set<EnterpriseAgentOs.Domain.Models.OrgMemberRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.AgentRecord>(e =>
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

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.ProviderRecord>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Name).IsUnique();
            e.Property(p => p.Name).IsRequired().HasMaxLength(64);
            e.Property(p => p.DisplayName).IsRequired().HasMaxLength(128);
            e.Property(p => p.EncryptedApiKey).HasMaxLength(4096);
            e.Ignore(p => p.Configured);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.SkillCredentialRecord>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.SkillName).IsUnique();
            e.Property(s => s.SkillName).IsRequired().HasMaxLength(64);
            e.Property(s => s.EncryptedCredentials).HasMaxLength(16384);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.UserRecord>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.GoogleSubjectId).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.SessionRecord>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.TokenHash).IsUnique();
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.DeviceCodeRecord>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.DeviceCode).IsUnique();
            e.HasIndex(d => d.UserCode);
            e.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.BrowserSessionRecord>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => b.AgentId).IsUnique();
            e.Property(b => b.CookiesJson).HasColumnType("text");
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.AgentSkillRecord>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.AgentId, a.SkillName }).IsUnique();
            e.HasIndex(a => a.AgentId);
            e.Property(a => a.SkillName).IsRequired().HasMaxLength(64);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.SkillRecord>(e =>
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

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.UserSubscription>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.UserId).IsUnique();
            e.Property(u => u.Plan).IsRequired().HasMaxLength(32);
            e.Property(u => u.BillingCycle).IsRequired().HasMaxLength(16);
            e.Property(u => u.StripeCustomerId).HasMaxLength(256);
            e.Property(u => u.StripeSubscriptionId).HasMaxLength(256);
            e.Property(u => u.StripeOverageItemId).HasMaxLength(256);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.OrgSubscription>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.OrganizationId).IsUnique();
            e.Property(o => o.OrganizationId).IsRequired().HasMaxLength(256);
            e.Property(o => o.Plan).IsRequired().HasMaxLength(32);
            e.Property(o => o.StripeCustomerId).HasMaxLength(256);
            e.Property(o => o.StripeSubscriptionId).HasMaxLength(256);
            e.Property(o => o.StripeOverageItemId).HasMaxLength(256);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.ChannelConnectionRecord>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.ChannelType).IsRequired().HasMaxLength(32);
            e.Property(c => c.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(c => c.EncryptedConfig).HasColumnType("text");
            e.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.AgentChannelBindingRecord>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => new { b.AgentId, b.ChannelConnectionId }).IsUnique();
            e.Property(b => b.Config).HasColumnType("text");
            e.HasOne(b => b.Agent).WithMany().HasForeignKey(b => b.AgentId);
            e.HasOne(b => b.ChannelConnection).WithMany().HasForeignKey(b => b.ChannelConnectionId);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.SystemEventRecord>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.CreatedAt);
            e.HasIndex(s => s.Severity);
            e.HasIndex(s => s.Category);
            e.HasIndex(s => s.SkillName);
            e.HasIndex(s => s.AgentId);
            e.Property(s => s.DetailJson).HasColumnType("text");
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.AgentRateLimitRecord>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.AgentId, r.BucketKey, r.WindowStart }).IsUnique();
            e.HasIndex(r => r.AgentId);
            e.Property(r => r.BucketKey).IsRequired().HasMaxLength(64);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.SkillLikeRecord>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => new { l.UserId, l.SkillId }).IsUnique();
            e.HasIndex(l => l.SkillId);
            e.HasOne(l => l.Skill).WithMany().HasForeignKey(l => l.SkillId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.SkillCommentRecord>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.SkillId);
            e.HasIndex(c => new { c.SkillId, c.CreatedAt });
            e.Property(c => c.Body).HasColumnType("text");
            e.HasOne(c => c.Skill).WithMany().HasForeignKey(c => c.SkillId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.AgentLogRecord>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => l.AgentId);
            e.HasIndex(l => new { l.AgentId, l.Time });
            e.HasIndex(l => l.CorrelationId);
            e.Property(l => l.Content).HasColumnType("text");
            e.Property(l => l.Type).HasConversion<string>().HasMaxLength(32);
            e.HasOne(l => l.Agent).WithMany().HasForeignKey(l => l.AgentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.AgentToolPermissionRecord>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.AgentId, p.SkillName, p.ToolName }).IsUnique();
            e.HasIndex(p => p.AgentId);
            e.Property(p => p.Permission).HasConversion<string>().HasMaxLength(16);
            e.HasOne(p => p.Agent).WithMany().HasForeignKey(p => p.AgentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.AgentTemplateRecord>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Name).IsUnique();
            e.Property(t => t.Prompt).HasColumnType("text");
            e.Property(t => t.IntegrationsJson).HasColumnType("text");
            e.Property(t => t.ChannelsJson).HasColumnType("text");
            e.HasOne(t => t.Owner).WithMany().HasForeignKey(t => t.OwnerId);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.OrganizationRecord>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.OrgMemberRecord>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.OrganizationId, m.Email }).IsUnique();
            e.HasIndex(m => m.UserId);
            e.Property(m => m.Email).IsRequired().HasMaxLength(256);
            e.Property(m => m.Role).IsRequired().HasMaxLength(16);
            e.Property(m => m.Status).IsRequired().HasMaxLength(16);
        });
    }
}
