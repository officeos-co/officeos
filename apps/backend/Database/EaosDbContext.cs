using EnterpriseAgentOs.Api.Database.Models;

namespace EnterpriseAgentOs.Api.Database;

public sealed class EaosDbContext : DbContext
{
    public EaosDbContext(DbContextOptions<EaosDbContext> options) : base(options)
    {
    }

    public DbSet<AgentRecord> Agents => Set<AgentRecord>();
    public DbSet<ProviderRecord> Providers => Set<ProviderRecord>();
    public DbSet<SkillCredentialRecord> SkillCredentials => Set<SkillCredentialRecord>();
    public DbSet<UserRecord> Users => Set<UserRecord>();
    public DbSet<SessionRecord> Sessions => Set<SessionRecord>();
    public DbSet<RunnerRecord> Runners => Set<RunnerRecord>();
    public DbSet<RunnerJobRecord> RunnerJobs => Set<RunnerJobRecord>();
    public DbSet<CustomSkillRecord> CustomSkills => Set<CustomSkillRecord>();
    public DbSet<DeviceCodeRecord> DeviceCodes => Set<DeviceCodeRecord>();
    public DbSet<AgentMemoryRecord> AgentMemories => Set<AgentMemoryRecord>();
    public DbSet<AgentCacheRecord> AgentCacheEntries => Set<AgentCacheRecord>();
    public DbSet<AgentConversationRecord> AgentConversations => Set<AgentConversationRecord>();
    public DbSet<BrowserSessionRecord> BrowserSessions => Set<BrowserSessionRecord>();
    public DbSet<SkillRegistryRecord> SkillRegistry => Set<SkillRegistryRecord>();
    public DbSet<SkillRecord> Skills => Set<SkillRecord>();
    public DbSet<AgentSkillRecord> AgentSkills => Set<AgentSkillRecord>();
    public DbSet<UserSubscription> UserSubscriptions { get; set; } = null!;
    public DbSet<OrgSubscription> OrgSubscriptions { get; set; } = null!;
    public DbSet<ChannelConnectionRecord> ChannelConnections => Set<ChannelConnectionRecord>();
    public DbSet<AgentChannelBindingRecord> AgentChannelBindings => Set<AgentChannelBindingRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentRecord>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Name).IsRequired().HasMaxLength(200);
            e.Property(a => a.Provider).IsRequired().HasMaxLength(64);
            e.Property(a => a.Status).IsRequired().HasMaxLength(32);
            e.Property(a => a.PodName).HasMaxLength(128);
            e.Property(a => a.ServiceUrl).HasMaxLength(256);
            e.Property(a => a.EncryptedBackendToken).HasMaxLength(4096);
        });

        modelBuilder.Entity<ProviderRecord>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Name).IsUnique();
            e.Property(p => p.Name).IsRequired().HasMaxLength(64);
            e.Property(p => p.DisplayName).IsRequired().HasMaxLength(128);
            e.Property(p => p.EncryptedApiKey).HasMaxLength(4096);
            e.Ignore(p => p.Configured);
        });

        modelBuilder.Entity<SkillCredentialRecord>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.SkillName).IsUnique();
            e.Property(s => s.SkillName).IsRequired().HasMaxLength(64);
            e.Property(s => s.EncryptedCredentials).HasMaxLength(16384);
        });

        modelBuilder.Entity<UserRecord>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.GoogleSubjectId).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<SessionRecord>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.TokenHash).IsUnique();
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
        });

        modelBuilder.Entity<RunnerRecord>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasOne(r => r.Owner).WithMany().HasForeignKey(r => r.OwnerId);
        });

        modelBuilder.Entity<RunnerJobRecord>(e =>
        {
            e.HasKey(j => j.Id);
            e.HasIndex(j => new { j.RunnerId, j.Status });
            e.HasOne(j => j.Runner).WithMany().HasForeignKey(j => j.RunnerId);
        });

        modelBuilder.Entity<CustomSkillRecord>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Name).IsUnique();
            e.HasOne(c => c.Owner).WithMany().HasForeignKey(c => c.OwnerId);
        });

        modelBuilder.Entity<DeviceCodeRecord>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.DeviceCode).IsUnique();
            e.HasIndex(d => d.UserCode);
            e.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AgentMemoryRecord>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => m.AgentId);
            e.HasIndex(m => new { m.AgentId, m.Key }).IsUnique();
            e.HasIndex(m => new { m.AgentId, m.Category });
            e.HasIndex(m => new { m.AgentId, m.SessionId });
            e.Property(m => m.Key).IsRequired().HasMaxLength(512);
            e.Property(m => m.Content).IsRequired();
            e.Property(m => m.Category).IsRequired().HasMaxLength(64);
            e.Property(m => m.Namespace).IsRequired().HasMaxLength(128).HasDefaultValue("default");
            e.Property(m => m.SessionId).HasMaxLength(256);
            e.Property(m => m.SupersededBy).HasMaxLength(512);
        });

        modelBuilder.Entity<AgentCacheRecord>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => new { c.AgentId, c.CacheKey }).IsUnique();
            e.HasIndex(c => c.AccessedAt);
            e.Property(c => c.CacheKey).IsRequired().HasMaxLength(128);
            e.Property(c => c.Model).IsRequired().HasMaxLength(128);
            e.Property(c => c.Response).IsRequired();
        });

        modelBuilder.Entity<AgentConversationRecord>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.AgentId);
            e.HasIndex(c => new { c.AgentId, c.SessionId });
            e.Property(c => c.Role).IsRequired().HasMaxLength(32);
            e.Property(c => c.Content).IsRequired();
            e.Property(c => c.SessionId).HasMaxLength(256);
        });

        modelBuilder.Entity<BrowserSessionRecord>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => b.AgentId).IsUnique();
            e.Property(b => b.CookiesJson).HasColumnType("text");
        });

        modelBuilder.Entity<SkillRegistryRecord>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Name).IsUnique();
            e.Property(r => r.Name).IsRequired().HasMaxLength(64);
            e.Property(r => r.Version).HasMaxLength(32);
            e.Property(r => r.NpmPackage).HasMaxLength(256);
            e.Property(r => r.BundleUrl).HasMaxLength(512);
            e.Property(r => r.ManifestJson).HasColumnType("text");
            e.Property(r => r.Status).IsRequired().HasMaxLength(16);
            e.HasOne(r => r.PublishedBy).WithMany().HasForeignKey(r => r.PublishedById);
        });

        modelBuilder.Entity<AgentSkillRecord>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.AgentId, a.SkillName }).IsUnique();
            e.HasIndex(a => a.AgentId);
            e.Property(a => a.SkillName).IsRequired().HasMaxLength(64);
        });

        modelBuilder.Entity<SkillRecord>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Name).IsUnique();
            e.Property(s => s.Name).IsRequired().HasMaxLength(64);
            e.Property(s => s.Title).IsRequired().HasMaxLength(128);
            e.Property(s => s.Emoji).HasMaxLength(8);
            e.Property(s => s.ManifestJson).HasColumnType("text");
            e.Property(s => s.Doc).HasColumnType("text");
            e.Property(s => s.BundleS3Key).HasMaxLength(512);
            e.Property(s => s.Version).HasMaxLength(32);
            e.Property(s => s.Status).IsRequired().HasMaxLength(16);
            e.Property(s => s.BuildError).HasColumnType("text");
            e.Property(s => s.GitHubRepoUrl).HasMaxLength(512);
            e.Property(s => s.GitHubBranch).HasMaxLength(128);
            e.HasOne(s => s.Owner).WithMany().HasForeignKey(s => s.OwnerId);
        });

        modelBuilder.Entity<UserSubscription>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.UserId).IsUnique();
            e.Property(u => u.Plan).IsRequired().HasMaxLength(32);
            e.Property(u => u.BillingCycle).IsRequired().HasMaxLength(16);
            e.Property(u => u.StripeCustomerId).HasMaxLength(256);
            e.Property(u => u.StripeSubscriptionId).HasMaxLength(256);
            e.Property(u => u.StripeOverageItemId).HasMaxLength(256);
        });

        modelBuilder.Entity<OrgSubscription>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.OrganizationId).IsUnique();
            e.Property(o => o.OrganizationId).IsRequired().HasMaxLength(256);
            e.Property(o => o.Plan).IsRequired().HasMaxLength(32);
            e.Property(o => o.StripeCustomerId).HasMaxLength(256);
            e.Property(o => o.StripeSubscriptionId).HasMaxLength(256);
            e.Property(o => o.StripeOverageItemId).HasMaxLength(256);
        });

        modelBuilder.Entity<ChannelConnectionRecord>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.ChannelType).IsRequired().HasMaxLength(32);
            e.Property(c => c.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(c => c.EncryptedConfig).HasColumnType("text");
            e.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
        });

        modelBuilder.Entity<AgentChannelBindingRecord>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => new { b.AgentId, b.ChannelConnectionId }).IsUnique();
            e.Property(b => b.Config).HasColumnType("text");
            e.HasOne(b => b.Agent).WithMany().HasForeignKey(b => b.AgentId);
            e.HasOne(b => b.ChannelConnection).WithMany().HasForeignKey(b => b.ChannelConnectionId);
        });
    }
}
