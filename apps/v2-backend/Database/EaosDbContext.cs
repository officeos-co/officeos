using EnterpriseAgentOs.Api.Entities.Agents;
using EnterpriseAgentOs.Api.Entities.Providers;
using EnterpriseAgentOs.Api.Entities.Skills;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Api.Database;

public sealed class EaosDbContext : DbContext
{
    public EaosDbContext(DbContextOptions<EaosDbContext> options) : base(options)
    {
    }

    public DbSet<AgentRecord> Agents => Set<AgentRecord>();
    public DbSet<ProviderRecord> Providers => Set<ProviderRecord>();
    public DbSet<SkillRecord> Skills => Set<SkillRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentRecord>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Name).IsRequired().HasMaxLength(200);
            e.Property(a => a.Status).IsRequired().HasMaxLength(32);
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

        modelBuilder.Entity<SkillRecord>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Name).IsUnique();
            e.Property(s => s.Name).IsRequired().HasMaxLength(64);
            e.Property(s => s.DisplayName).IsRequired().HasMaxLength(128);
        });
    }
}
