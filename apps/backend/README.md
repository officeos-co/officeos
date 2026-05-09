```bash
dotnet build src/OffceOs.csproj --no-restore
```

```bash
dotnet test tests/OffceOs.Tests.csproj --no-restore
```

```bash
dotnet test analyzers/OffceOs.Architecture.Analyzers.csproj --no-restore
```

```bash
dotnet build OffceOs.sln --no-restore
```

## Migrations

The backend applies pending migrations automatically on startup via `Database.MigrateAsync()`. Do not run `dotnet ef database update` manually for normal development.

Add a new migration:

```bash
dotnet ef migrations add MigrationName --project src/OffceOs.csproj --output-dir Database/Migrations
```

Remove the last unapplied migration:

```bash
dotnet ef migrations remove --project src/OffceOs.csproj
```
