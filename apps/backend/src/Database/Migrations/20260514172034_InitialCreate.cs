using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OffceOs.Database.Migrations{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentIntegrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentRateLimits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BucketKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WindowStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRateLimits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BrowserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuntimeSessionId = table.Column<string>(type: "text", nullable: false),
                    CookiesJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrowserSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    DetailJson = table.Column<string>(type: "text", nullable: true),
                    SkillName = table.Column<string>(type: "text", nullable: true),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    Acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentChannelBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Config = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentChannelBindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SystemPrompt = table.Column<string>(type: "text", nullable: true),
                    ConfigJson = table.Column<string>(type: "jsonb", nullable: false),
                    ConfigHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentMemories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentMemories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentPersonalities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentPersonalities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentRoutines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastTriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRoutines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentRoutineTriggers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    ConfigJson = table.Column<string>(type: "jsonb", nullable: false),
                    SecretHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EncryptedSecret = table.Column<string>(type: "text", nullable: true),
                    LastTriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRoutineTriggers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentRoutineTriggers_AgentRoutines_RoutineId",
                        column: x => x.RoutineId,
                        principalTable: "AgentRoutines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentCorrelationId = table.Column<string>(type: "text", nullable: true),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<string>(type: "text", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Model = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PodName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ServiceUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Prompt = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    EncryptedBackendToken = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    ActiveDefinitionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentSessionContexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    LastCompactedLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastCompactedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreCompactTokens = table.Column<int>(type: "integer", nullable: false),
                    PostCompactTokens = table.Column<int>(type: "integer", nullable: false),
                    CompactionVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentSessionContexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentSessionContexts_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MessageCount = table.Column<int>(type: "integer", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentSessions_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentSessionResourceAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Instructions = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentSessionResourceAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentSessionResourceAttachments_AgentSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AgentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentSessionResourceAttachments_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BrowserResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrentAgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrowserResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrowserResources_Agents_CurrentAgentId",
                        column: x => x.CurrentAgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChannelConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    EncryptedCreds = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceCode = table.Column<string>(type: "text", nullable: false),
                    UserCode = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RunnerName = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastPolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AuthKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EncryptedSecretEnvelope = table.Column<string>(type: "character varying(32768)", maxLength: 32768, nullable: false),
                    PublicAuthMetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    ScopesJson = table.Column<string>(type: "jsonb", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfiguredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationDeployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationDeployments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TransportType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Command = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Args = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Logo = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CredentialFieldsJson = table.Column<string>(type: "jsonb", nullable: true),
                    Subtitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AuthorUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DocumentationUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    RepositoryUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ToolsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CapabilitiesJson = table.Column<string>(type: "jsonb", nullable: true),
                    EntitiesJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsBuiltin = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Integrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemoryStoreEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemoryStoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryStoreEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemoryStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryStores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProviderResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultModel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AllowedModelsJson = table.Column<string>(type: "jsonb", nullable: false),
                    AuthKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EncryptedCredentialsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderResources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ParentResourceKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ParentResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Tool = table.Column<string>(type: "text", nullable: true),
                    Integration = table.Column<string>(type: "text", nullable: true),
                    Channel = table.Column<string>(type: "text", nullable: true),
                    ChannelConnectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    InputTokens = table.Column<int>(type: "integer", nullable: true),
                    OutputTokens = table.Column<int>(type: "integer", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    RunId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentRunId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceLogs_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    GoogleSubjectId = table.Column<string>(type: "text", nullable: true),
                    GitHubSubjectId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    Timezone = table.Column<string>(type: "text", nullable: true),
                    NotificationPrefsJson = table.Column<string>(type: "text", nullable: true),
                    Preferences = table.Column<string>(type: "text", nullable: true),
                    CurrentWorkspaceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workspaces_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceMembers_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentChannelBindings_AgentId_ChannelConnectionId",
                table: "AgentChannelBindings",
                columns: new[] { "AgentId", "ChannelConnectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentChannelBindings_ChannelConnectionId",
                table: "AgentChannelBindings",
                column: "ChannelConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinitions_AgentId",
                table: "AgentDefinitions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinitions_AgentId_Version",
                table: "AgentDefinitions",
                columns: new[] { "AgentId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentIntegrations_AgentId",
                table: "AgentIntegrations",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentIntegrations_AgentId_IntegrationName",
                table: "AgentIntegrations",
                columns: new[] { "AgentId", "IntegrationName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentMemories_AgentId_Key",
                table: "AgentMemories",
                columns: new[] { "AgentId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentPersonalities_AgentId_FileName",
                table: "AgentPersonalities",
                columns: new[] { "AgentId", "FileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentRateLimits_AgentId",
                table: "AgentRateLimits",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRateLimits_AgentId_BucketKey_WindowStart",
                table: "AgentRateLimits",
                columns: new[] { "AgentId", "BucketKey", "WindowStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoutines_AgentId",
                table: "AgentRoutines",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoutineTriggers_RoutineId_Kind",
                table: "AgentRoutineTriggers",
                columns: new[] { "RoutineId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_AgentId",
                table: "AgentRuns",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_ParentRunId",
                table: "AgentRuns",
                column: "ParentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_Status",
                table: "AgentRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_WorkspaceId",
                table: "AgentRuns",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_OwnerId_WorkspaceId",
                table: "Agents",
                columns: new[] { "OwnerId", "WorkspaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_WorkspaceId",
                table: "Agents",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessionContexts_AgentId",
                table: "AgentSessionContexts",
                column: "AgentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessionResourceAttachments_AgentId_ResourceType",
                table: "AgentSessionResourceAttachments",
                columns: new[] { "AgentId", "ResourceType" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessionResourceAttachments_SessionId_ResourceType_Reso~",
                table: "AgentSessionResourceAttachments",
                columns: new[] { "SessionId", "ResourceType", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentSessions_AgentId_Status",
                table: "AgentSessions",
                columns: new[] { "AgentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BrowserResources_CurrentAgentId",
                table: "BrowserResources",
                column: "CurrentAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_BrowserResources_OwnerId",
                table: "BrowserResources",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BrowserResources_OwnerId_WorkspaceId",
                table: "BrowserResources",
                columns: new[] { "OwnerId", "WorkspaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_BrowserResources_WorkspaceId",
                table: "BrowserResources",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_BrowserSessions_AgentId",
                table: "BrowserSessions",
                column: "AgentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelConnections_CreatedById_WorkspaceId",
                table: "ChannelConnections",
                columns: new[] { "CreatedById", "WorkspaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelConnections_WorkspaceId",
                table: "ChannelConnections",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCodes_DeviceCode",
                table: "DeviceCodes",
                column: "DeviceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCodes_UserCode",
                table: "DeviceCodes",
                column: "UserCode");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCodes_UserId",
                table: "DeviceCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_OwnerId",
                table: "IntegrationCredentials",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_WorkspaceId_IntegrationName",
                table: "IntegrationCredentials",
                columns: new[] { "WorkspaceId", "IntegrationName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDeployments_CreatedById",
                table: "IntegrationDeployments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDeployments_WorkspaceId_IntegrationName",
                table: "IntegrationDeployments",
                columns: new[] { "WorkspaceId", "IntegrationName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_OwnerId_WorkspaceId",
                table: "Integrations",
                columns: new[] { "OwnerId", "WorkspaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_WorkspaceId_Name",
                table: "Integrations",
                columns: new[] { "WorkspaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemoryStoreEntries_MemoryStoreId_Key",
                table: "MemoryStoreEntries",
                columns: new[] { "MemoryStoreId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemoryStores_OwnerId",
                table: "MemoryStores",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryStores_OwnerId_WorkspaceId",
                table: "MemoryStores",
                columns: new[] { "OwnerId", "WorkspaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryStores_WorkspaceId",
                table: "MemoryStores",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderResources_WorkspaceId_Name",
                table: "ProviderResources",
                columns: new[] { "WorkspaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLogs_AgentId",
                table: "ResourceLogs",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLogs_AgentId_Time",
                table: "ResourceLogs",
                columns: new[] { "AgentId", "Time" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLogs_ChannelConnectionId",
                table: "ResourceLogs",
                column: "ChannelConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLogs_CorrelationId",
                table: "ResourceLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLogs_WorkspaceId",
                table: "ResourceLogs",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLogs_WorkspaceId_ResourceKind_ResourceId_Time",
                table: "ResourceLogs",
                columns: new[] { "WorkspaceId", "ResourceKind", "ResourceId", "Time" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLogs_WorkspaceId_ResourceKind_ResourceName_Time",
                table: "ResourceLogs",
                columns: new[] { "WorkspaceId", "ResourceKind", "ResourceName", "Time" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TokenHash",
                table: "Sessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId",
                table: "Sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemEvents_AgentId",
                table: "SystemEvents",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemEvents_Category",
                table: "SystemEvents",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SystemEvents_CreatedAt",
                table: "SystemEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemEvents_Severity",
                table: "SystemEvents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SystemEvents_SkillName",
                table: "SystemEvents",
                column: "SkillName");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CurrentWorkspaceId",
                table: "Users",
                column: "CurrentWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleSubjectId",
                table: "Users",
                column: "GoogleSubjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMembers_UserId",
                table: "WorkspaceMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMembers_WorkspaceId_UserId",
                table: "WorkspaceMembers",
                columns: new[] { "WorkspaceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_OwnerKind",
                table: "Workspaces",
                column: "OwnerKind");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_OwnerUserId_Name",
                table: "Workspaces",
                columns: new[] { "OwnerUserId", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_AgentChannelBindings_Agents_AgentId",
                table: "AgentChannelBindings",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentChannelBindings_ChannelConnections_ChannelConnectionId",
                table: "AgentChannelBindings",
                column: "ChannelConnectionId",
                principalTable: "ChannelConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentDefinitions_Agents_AgentId",
                table: "AgentDefinitions",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentMemories_Agents_AgentId",
                table: "AgentMemories",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentPersonalities_Agents_AgentId",
                table: "AgentPersonalities",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentRoutines_Agents_AgentId",
                table: "AgentRoutines",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentRuns_Agents_AgentId",
                table: "AgentRuns",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentRuns_Workspaces_WorkspaceId",
                table: "AgentRuns",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Users_OwnerId",
                table: "Agents",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Workspaces_WorkspaceId",
                table: "Agents",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BrowserResources_Users_OwnerId",
                table: "BrowserResources",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BrowserResources_Workspaces_WorkspaceId",
                table: "BrowserResources",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelConnections_Users_CreatedById",
                table: "ChannelConnections",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelConnections_Workspaces_WorkspaceId",
                table: "ChannelConnections",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceCodes_Users_UserId",
                table: "DeviceCodes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationCredentials_Users_OwnerId",
                table: "IntegrationCredentials",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationCredentials_Workspaces_WorkspaceId",
                table: "IntegrationCredentials",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationDeployments_Users_CreatedById",
                table: "IntegrationDeployments",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationDeployments_Workspaces_WorkspaceId",
                table: "IntegrationDeployments",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Integrations_Workspaces_WorkspaceId",
                table: "Integrations",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemoryStoreEntries_MemoryStores_MemoryStoreId",
                table: "MemoryStoreEntries",
                column: "MemoryStoreId",
                principalTable: "MemoryStores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemoryStores_Users_OwnerId",
                table: "MemoryStores",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemoryStores_Workspaces_WorkspaceId",
                table: "MemoryStores",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderResources_Workspaces_WorkspaceId",
                table: "ProviderResources",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceLogs_Workspaces_WorkspaceId",
                table: "ResourceLogs",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Users_UserId",
                table: "Sessions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Workspaces_CurrentWorkspaceId",
                table: "Users",
                column: "CurrentWorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Workspaces_CurrentWorkspaceId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "AgentChannelBindings");

            migrationBuilder.DropTable(
                name: "AgentDefinitions");

            migrationBuilder.DropTable(
                name: "AgentIntegrations");

            migrationBuilder.DropTable(
                name: "AgentMemories");

            migrationBuilder.DropTable(
                name: "AgentPersonalities");

            migrationBuilder.DropTable(
                name: "AgentRateLimits");

            migrationBuilder.DropTable(
                name: "AgentRoutineTriggers");

            migrationBuilder.DropTable(
                name: "AgentRuns");

            migrationBuilder.DropTable(
                name: "AgentSessionContexts");

            migrationBuilder.DropTable(
                name: "AgentSessionResourceAttachments");

            migrationBuilder.DropTable(
                name: "BrowserResources");

            migrationBuilder.DropTable(
                name: "BrowserSessions");

            migrationBuilder.DropTable(
                name: "DeviceCodes");

            migrationBuilder.DropTable(
                name: "IntegrationCredentials");

            migrationBuilder.DropTable(
                name: "IntegrationDeployments");

            migrationBuilder.DropTable(
                name: "Integrations");

            migrationBuilder.DropTable(
                name: "MemoryStoreEntries");

            migrationBuilder.DropTable(
                name: "ProviderResources");

            migrationBuilder.DropTable(
                name: "ResourceLogs");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "SystemEvents");

            migrationBuilder.DropTable(
                name: "WorkspaceMembers");

            migrationBuilder.DropTable(
                name: "ChannelConnections");

            migrationBuilder.DropTable(
                name: "AgentRoutines");

            migrationBuilder.DropTable(
                name: "AgentSessions");

            migrationBuilder.DropTable(
                name: "MemoryStores");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
