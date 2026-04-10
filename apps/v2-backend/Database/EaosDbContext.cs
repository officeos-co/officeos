
namespace EnterpriseAgentOs.Api.Database;

public sealed class EaosDbContext : DbContext
{
    public EaosDbContext(DbContextOptions<EaosDbContext> options) : base(options)
    {
    }

    public DbSet<AgentRecord> Agents => Set<AgentRecord>();
    public DbSet<ProviderRecord> Providers => Set<ProviderRecord>();
    public DbSet<SkillCredentialRecord> SkillCredentials => Set<SkillCredentialRecord>();

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
    }
}
