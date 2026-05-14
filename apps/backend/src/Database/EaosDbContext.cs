namespace OffceOs.Database;

public sealed class EaosDbContext : DbContext
{
    public EaosDbContext(DbContextOptions<EaosDbContext> options) : base(options)
    {
    }

    public DbSet<AgentEntity> Agents => Set<AgentEntity>();
    public DbSet<AgentDefinitionEntity> AgentDefinitions => Set<AgentDefinitionEntity>();

    public DbSet<OAuthTokenEntity> OAuthTokens => Set<OAuthTokenEntity>();
    public DbSet<OAuthGrantedScopeEntity> OAuthGrantedScopes => Set<OAuthGrantedScopeEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<WorkspaceEntity> Workspaces => Set<WorkspaceEntity>();
    public DbSet<WorkspaceMemberEntity> WorkspaceMembers => Set<WorkspaceMemberEntity>();
    public DbSet<WorkspaceOrganizationGrantEntity> WorkspaceOrganizationGrants => Set<WorkspaceOrganizationGrantEntity>();
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
    public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();
    public DbSet<OrgMemberEntity> OrgMembers => Set<OrgMemberEntity>();
    public DbSet<AccessGroupEntity> AccessGroups => Set<AccessGroupEntity>();
    public DbSet<AccessGroupMemberEntity> AccessGroupMembers => Set<AccessGroupMemberEntity>();
    public DbSet<AccessGroupWorkspaceGrantEntity> AccessGroupWorkspaceGrants => Set<AccessGroupWorkspaceGrantEntity>();
    public DbSet<OrganizationPolicyProfileEntity> OrganizationPolicyProfiles => Set<OrganizationPolicyProfileEntity>();
    public DbSet<ProviderResourceEntity> ProviderResources => Set<ProviderResourceEntity>();
    public DbSet<OrganizationAuditLogEntity> OrganizationAuditLogs => Set<OrganizationAuditLogEntity>();
    public DbSet<AgentMemoryEntity> AgentMemories => Set<AgentMemoryEntity>();
    public DbSet<AgentPersonalityEntity> AgentPersonalities => Set<AgentPersonalityEntity>();
    public DbSet<AgentRoutineEntity> AgentRoutines => Set<AgentRoutineEntity>();
    public DbSet<AgentRoutineTriggerEntity> AgentRoutineTriggers => Set<AgentRoutineTriggerEntity>();
    public DbSet<AgentSessionEntity> AgentSessions => Set<AgentSessionEntity>();
    public DbSet<AgentSessionContextEntity> AgentSessionContexts => Set<AgentSessionContextEntity>();
    public DbSet<AgentRunEntity> AgentRuns => Set<AgentRunEntity>();
    public DbSet<AgentUsageCallEntity> AgentUsageCalls => Set<AgentUsageCallEntity>();
    public DbSet<AgentUsageContextPartEntity> AgentUsageContextParts => Set<AgentUsageContextPartEntity>();
    public DbSet<BrowserResourceEntity> BrowserResources => Set<BrowserResourceEntity>();
    public DbSet<MemoryStoreEntity> MemoryStores => Set<MemoryStoreEntity>();
    public DbSet<MemoryStoreEntryEntity> MemoryStoreEntries => Set<MemoryStoreEntryEntity>();
    public DbSet<AgentSessionResourceAttachmentEntity> AgentSessionResourceAttachments => Set<AgentSessionResourceAttachmentEntity>();

    public DbSet<IntegrationDefinitionEntity> Integrations => Set<IntegrationDefinitionEntity>();
    public DbSet<AgentIntegrationEntity> AgentIntegrations => Set<AgentIntegrationEntity>();
    public DbSet<IntegrationCredentialEntity> IntegrationCredentials => Set<IntegrationCredentialEntity>();
    public DbSet<IntegrationDeploymentEntity> IntegrationDeployments => Set<IntegrationDeploymentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentEntity>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.OwnerId, a.WorkspaceId });
            e.Property(a => a.Name).IsRequired().HasMaxLength(200);
            e.Property(a => a.Provider).IsRequired().HasMaxLength(64);
            e.Property(a => a.Status).IsRequired().HasMaxLength(32);
            e.Property(a => a.PodName).HasMaxLength(128);
            e.Property(a => a.ServiceUrl).HasMaxLength(256);
            e.Property(a => a.EncryptedBackendToken).HasMaxLength(4096);
            e.Property(a => a.Prompt).HasColumnType("text");
            e.HasOne(a => a.Workspace).WithMany().HasForeignKey(a => a.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentDefinitionEntity>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => new { d.AgentId, d.Version }).IsUnique();
            e.HasIndex(d => d.AgentId);
            e.Property(d => d.Name).IsRequired().HasMaxLength(200);
            e.Property(d => d.Description).HasMaxLength(1000);
            e.Property(d => d.Provider).IsRequired().HasMaxLength(64);
            e.Property(d => d.Model).HasMaxLength(128);
            e.Property(d => d.SystemPrompt).HasColumnType("text");
            e.Property(d => d.ConfigJson).IsRequired().HasColumnType("jsonb");
            e.Property(d => d.ConfigHash).IsRequired().HasMaxLength(128);
            e.HasOne(d => d.Agent).WithMany().HasForeignKey(d => d.AgentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OAuthTokenEntity>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.UserId);
            e.HasIndex(o => new { o.UserId, o.Provider }).IsUnique();
            e.Property(o => o.Provider).IsRequired().HasMaxLength(32);
            e.Property(o => o.EncryptedAccessToken);
            e.Property(o => o.EncryptedRefreshToken).HasMaxLength(16384);
            e.Property(o => o.Email).HasMaxLength(256);
            e.HasOne<UserEntity>().WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
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
            e.HasOne(u => u.CurrentWorkspace).WithMany().HasForeignKey(u => u.CurrentWorkspaceId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(u => u.CurrentOrganization).WithMany().HasForeignKey(u => u.CurrentOrganizationId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkspaceEntity>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasIndex(w => new { w.OwnerUserId, w.Name });
            e.HasIndex(w => new { w.OrganizationId, w.Name });
            e.HasIndex(w => w.OwnerKind);
            e.Property(w => w.Name).IsRequired().HasMaxLength(200);
            e.Property(w => w.OwnerKind).IsRequired().HasMaxLength(32);
            e.HasOne(w => w.OwnerUser).WithMany().HasForeignKey(w => w.OwnerUserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(w => w.Organization).WithMany().HasForeignKey(w => w.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkspaceMemberEntity>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.WorkspaceId, m.UserId }).IsUnique();
            e.HasIndex(m => m.UserId);
            e.Property(m => m.Role).IsRequired().HasMaxLength(16);
            e.HasOne(m => m.Workspace).WithMany().HasForeignKey(m => m.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkspaceOrganizationGrantEntity>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => new { g.WorkspaceId, g.OrganizationId }).IsUnique();
            e.Property(g => g.MaxRole).IsRequired().HasMaxLength(16);
            e.HasOne(g => g.Workspace).WithMany().HasForeignKey(g => g.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(g => g.Organization).WithMany().HasForeignKey(g => g.OrganizationId).OnDelete(DeleteBehavior.Cascade);
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
            e.HasIndex(c => new { c.CreatedById, c.WorkspaceId });
            e.Property(c => c.ChannelType).IsRequired().HasMaxLength(32);
            e.Property(c => c.DisplayName).IsRequired().HasMaxLength(200);
            e.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            e.HasOne(c => c.Workspace).WithMany().HasForeignKey(c => c.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
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
            e.HasIndex(l => l.WorkspaceId);
            e.HasIndex(l => l.ChannelConnectionId);
            e.HasIndex(l => new { l.AgentId, l.Time });
            e.HasIndex(l => l.CorrelationId);
            e.Property(l => l.Content).HasColumnType("text");
            e.Property(l => l.Type).HasConversion<string>().HasMaxLength(32);
            e.HasOne(l => l.Agent).WithMany().HasForeignKey(l => l.AgentId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Workspace).WithMany().HasForeignKey(l => l.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganizationEntity>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Name).IsRequired().HasMaxLength(200);
            e.Property(o => o.Kind).IsRequired().HasMaxLength(32).HasDefaultValue("individual");
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

        modelBuilder.Entity<AccessGroupEntity>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => new { g.OrganizationId, g.Name }).IsUnique();
            e.Property(g => g.Name).IsRequired().HasMaxLength(200);
            e.HasOne(g => g.Organization).WithMany().HasForeignKey(g => g.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccessGroupMemberEntity>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.AccessGroupId, m.UserId }).IsUnique();
            e.HasIndex(m => m.UserId);
            e.HasOne(m => m.AccessGroup).WithMany().HasForeignKey(m => m.AccessGroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccessGroupWorkspaceGrantEntity>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => new { g.AccessGroupId, g.WorkspaceId }).IsUnique();
            e.HasIndex(g => g.WorkspaceId);
            e.Property(g => g.Role).IsRequired().HasMaxLength(16);
            e.HasOne(g => g.AccessGroup).WithMany().HasForeignKey(g => g.AccessGroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(g => g.Workspace).WithMany().HasForeignKey(g => g.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganizationPolicyProfileEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.OrganizationId).IsUnique();
            e.Property(p => p.AllowedToolsJson).HasColumnType("jsonb");
            e.Property(p => p.DeniedToolsJson).HasColumnType("jsonb");
            e.Property(p => p.AllowedIntegrationsJson).HasColumnType("jsonb");
            e.Property(p => p.DeniedIntegrationsJson).HasColumnType("jsonb");
            e.HasOne(p => p.Organization).WithMany().HasForeignKey(p => p.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProviderResourceEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.WorkspaceId, p.Name }).IsUnique();
            e.Property(p => p.Name).IsRequired().HasMaxLength(64);
            e.Property(p => p.Type).IsRequired().HasMaxLength(64);
            e.Property(p => p.DisplayName).IsRequired().HasMaxLength(128);
            e.Property(p => p.DefaultModel).HasMaxLength(128);
            e.Property(p => p.AllowedModelsJson).HasColumnType("jsonb");
            e.Property(p => p.AuthKind).IsRequired().HasMaxLength(64);
            e.Property(p => p.EncryptedCredentialsJson).HasColumnType("text");
            e.HasOne(p => p.Workspace).WithMany().HasForeignKey(p => p.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganizationAuditLogEntity>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.OrganizationId, a.OccurredAt });
            e.HasIndex(a => a.Action);
            e.HasIndex(a => a.ActorUserId);
            e.HasIndex(a => a.WorkspaceId);
            e.HasIndex(a => a.AgentId);
            e.HasIndex(a => a.Outcome);
            e.Property(a => a.Action).IsRequired().HasMaxLength(128);
            e.Property(a => a.ResourceType).IsRequired().HasMaxLength(128);
            e.Property(a => a.ResourceId).HasMaxLength(128);
            e.Property(a => a.Outcome).IsRequired().HasMaxLength(32);
            e.Property(a => a.CorrelationId).HasMaxLength(128);
            e.Property(a => a.MetadataJson).HasColumnType("jsonb");
            e.HasOne(a => a.Organization).WithMany().HasForeignKey(a => a.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Actor).WithMany().HasForeignKey(a => a.ActorUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.Workspace).WithMany().HasForeignKey(a => a.WorkspaceId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.Agent).WithMany().HasForeignKey(a => a.AgentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AgentMemoryEntity>(e =>
        {
            e.ToTable("AgentMemories");
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

        modelBuilder.Entity<AgentRoutineEntity>(e =>
        {
            e.ToTable("AgentRoutines");
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.AgentId);
            e.Property(r => r.Name).IsRequired().HasMaxLength(200);
            e.Property(r => r.Prompt).HasColumnType("text").IsRequired();
            e.HasOne(r => r.Agent).WithMany()
                .HasForeignKey(r => r.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentRoutineTriggerEntity>(e =>
        {
            e.ToTable("AgentRoutineTriggers");
            e.HasKey(t => t.Id);
            e.HasIndex(t => new { t.RoutineId, t.Kind });
            e.Property(t => t.Kind).IsRequired().HasMaxLength(32);
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
            e.Property(t => t.ConfigJson).HasColumnType("jsonb").IsRequired();
            e.Property(t => t.SecretHash).HasMaxLength(128);
            e.Property(t => t.EncryptedSecret).HasColumnType("text");
            e.HasOne(t => t.Routine).WithMany(r => r.Triggers)
                .HasForeignKey(t => t.RoutineId)
                .OnDelete(DeleteBehavior.Cascade);
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
            e.HasIndex(r => r.WorkspaceId);
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
            e.HasOne(r => r.Workspace).WithMany()
                .HasForeignKey(r => r.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentUsageCallEntity>(e =>
        {
            e.ToTable("AgentUsageCalls");
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.AgentId);
            e.HasIndex(c => c.WorkspaceId);
            e.HasIndex(c => c.OwnerId);
            e.HasIndex(c => c.RunId);
            e.HasIndex(c => c.CorrelationId);
            e.HasIndex(c => new { c.OwnerId, c.Time });
            e.HasIndex(c => new { c.OwnerId, c.Model, c.Time });
            e.Property(c => c.CorrelationId).IsRequired().HasMaxLength(128);
            e.Property(c => c.Provider).IsRequired().HasMaxLength(64);
            e.Property(c => c.Model).IsRequired().HasMaxLength(128);
            e.Property(c => c.Activity).IsRequired().HasMaxLength(64);
            e.Property(c => c.Outcome).IsRequired().HasMaxLength(32);
            e.HasOne(c => c.Agent).WithMany().HasForeignKey(c => c.AgentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Workspace).WithMany().HasForeignKey(c => c.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Owner).WithMany().HasForeignKey(c => c.OwnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Run).WithMany().HasForeignKey(c => c.RunId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AgentUsageContextPartEntity>(e =>
        {
            e.ToTable("AgentUsageContextParts");
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.CallId);
            e.HasIndex(p => p.Kind);
            e.Property(p => p.Kind).IsRequired().HasMaxLength(64);
            e.Property(p => p.Label).IsRequired().HasMaxLength(256);
            e.Property(p => p.Role).HasMaxLength(32);
            e.Property(p => p.Tool).HasMaxLength(128);
            e.Property(p => p.Integration).HasMaxLength(128);
            e.HasOne(p => p.Call).WithMany(c => c.ContextParts).HasForeignKey(p => p.CallId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BrowserResourceEntity>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.OwnerId);
            e.HasIndex(r => new { r.OwnerId, r.WorkspaceId });
            e.HasIndex(r => r.CurrentAgentId);
            e.Property(r => r.DisplayName).IsRequired().HasMaxLength(200);
            e.HasOne(r => r.Owner).WithMany().HasForeignKey(r => r.OwnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Workspace).WithMany().HasForeignKey(r => r.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.CurrentAgent).WithMany().HasForeignKey(r => r.CurrentAgentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MemoryStoreEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.OwnerId);
            e.HasIndex(s => new { s.OwnerId, s.WorkspaceId });
            e.Property(s => s.DisplayName).IsRequired().HasMaxLength(200);
            e.HasOne(s => s.Owner).WithMany().HasForeignKey(s => s.OwnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Workspace).WithMany().HasForeignKey(s => s.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MemoryStoreEntryEntity>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.MemoryStoreId, m.Key }).IsUnique();
            e.Property(m => m.Key).HasMaxLength(512).IsRequired();
            e.Property(m => m.Content).HasColumnType("text").IsRequired();
            e.HasOne(m => m.MemoryStore).WithMany().HasForeignKey(m => m.MemoryStoreId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentSessionResourceAttachmentEntity>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.SessionId, a.ResourceType, a.ResourceId }).IsUnique();
            e.HasIndex(a => new { a.AgentId, a.ResourceType });
            e.Property(a => a.ResourceType).IsRequired().HasMaxLength(32);
            e.Property(a => a.AccessMode).IsRequired().HasMaxLength(32);
            e.Property(a => a.Instructions).HasColumnType("text");
            e.HasOne(a => a.Agent).WithMany().HasForeignKey(a => a.AgentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Session).WithMany().HasForeignKey(a => a.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IntegrationDefinitionEntity>(e =>
        {
            e.ToTable("Integrations");
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.OwnerId, s.WorkspaceId });
            e.HasIndex(s => new { s.WorkspaceId, s.Name }).IsUnique();
            e.Property(s => s.Name).IsRequired().HasMaxLength(64);
            e.Property(s => s.Provider).HasMaxLength(64);
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
            e.Property(s => s.CapabilitiesJson).HasColumnType("jsonb");
            e.Property(s => s.EntitiesJson).HasColumnType("jsonb");
            e.HasOne<WorkspaceEntity>().WithMany().HasForeignKey(s => s.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentIntegrationEntity>(e =>
        {
            e.ToTable("AgentIntegrations");
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.AgentId, a.IntegrationName }).IsUnique();
            e.HasIndex(a => a.AgentId);
            e.Property(a => a.IntegrationName).IsRequired().HasMaxLength(64);
        });

        modelBuilder.Entity<IntegrationCredentialEntity>(e =>
        {
            e.ToTable("IntegrationCredentials");
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.OwnerId);
            e.HasIndex(c => new { c.WorkspaceId, c.IntegrationName }).IsUnique();
            e.Property(c => c.IntegrationName).IsRequired().HasMaxLength(64);
            e.Property(c => c.AuthKind).IsRequired().HasMaxLength(32);
            e.Property(c => c.State).IsRequired().HasMaxLength(32);
            e.Property(c => c.EncryptedSecretEnvelope).HasMaxLength(32768);
            e.Property(c => c.PublicAuthMetadataJson).HasColumnType("jsonb");
            e.Property(c => c.ScopesJson).HasColumnType("jsonb");
            e.HasOne<UserEntity>().WithMany().HasForeignKey(c => c.OwnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Workspace).WithMany().HasForeignKey(c => c.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IntegrationDeploymentEntity>(e =>
        {
            e.ToTable("IntegrationDeployments");
            e.HasKey(d => d.Id);
            e.HasIndex(d => new { d.WorkspaceId, d.IntegrationName }).IsUnique();
            e.HasIndex(d => d.OrganizationId);
            e.Property(d => d.IntegrationName).IsRequired().HasMaxLength(64);
            e.HasOne(d => d.Organization).WithMany().HasForeignKey(d => d.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.Workspace).WithMany().HasForeignKey(d => d.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.CreatedBy).WithMany().HasForeignKey(d => d.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

    }
}
