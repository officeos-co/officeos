using OffceOs.Database;

namespace OffceOs.Tests.Shared;

public static class TestDbFactory
{
    public static EaosDbContext Create(string namePrefix)
    {
        var options = new DbContextOptionsBuilder<EaosDbContext>()
            .UseInMemoryDatabase($"{namePrefix}-{Guid.NewGuid():N}")
            .Options;
        return new EaosDbContext(options);
    }

    public static EaosDbContext CreateNamed(string dbName)
    {
        var options = new DbContextOptionsBuilder<EaosDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new EaosDbContext(options);
    }
}
