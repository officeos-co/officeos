#nullable disable

namespace OffceOs.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentCronJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Expression = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentCronJobs", x => x.Id);
                });

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
                name: "IntegrationCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EncryptedCredentials = table.Column<string>(type: "character varying(16384)", maxLength: 16384, nullable: false),
                    ConfiguredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "OAuthTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EncryptedAccessToken = table.Column<string>(type: "character varying(16384)", maxLength: 16384, nullable: true),
                    EncryptedRefreshToken = table.Column<string>(type: "character varying(16384)", maxLength: 16384, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrgMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrgSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StripeCustomerId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StripeOverageItemId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrentAgentLimit = table.Column<int>(type: "integer", nullable: false),
                    CreditBudgetPerMonth = table.Column<long>(type: "bigint", nullable: false),
                    CreditsUsedThisMonth = table.Column<long>(type: "bigint", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OverageEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgSubscriptions", x => x.Id);
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
                    Preferences = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BillingCycle = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StripeCustomerId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StripeOverageItemId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrentAgentLimit = table.Column<int>(type: "integer", nullable: false),
                    CreditBudgetPerMonth = table.Column<long>(type: "bigint", nullable: false),
                    CreditsUsedThisMonth = table.Column<long>(type: "bigint", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OverageEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OAuthGrantedScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OAuthTokenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthGrantedScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OAuthGrantedScopes_OAuthTokens_OAuthTokenId",
                        column: x => x.OAuthTokenId,
                        principalTable: "OAuthTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    EncryptedBackendToken = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agents_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id");
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
                    EncryptedCreds = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelConnections_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
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
                    table.ForeignKey(
                        name: "FK_DeviceCodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IntegrationConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WorkspaceName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RepositoriesJson = table.Column<string>(type: "jsonb", nullable: false),
                    EntitiesJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemoryStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryStores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemoryStores_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
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
                    table.ForeignKey(
                        name: "FK_Sessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Tool = table.Column<string>(type: "text", nullable: true),
                    Integration = table.Column<string>(type: "text", nullable: true),
                    Channel = table.Column<string>(type: "text", nullable: true),
                    ChannelConnectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    InputTokens = table.Column<int>(type: "integer", nullable: true),
                    OutputTokens = table.Column<int>(type: "integer", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    RunId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentRunId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentLogs_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    table.ForeignKey(
                        name: "FK_AgentMemories_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    table.ForeignKey(
                        name: "FK_AgentPersonalities_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.ForeignKey(
                        name: "FK_AgentRuns_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "AgentToolPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillName = table.Column<string>(type: "text", nullable: false),
                    ToolName = table.Column<string>(type: "text", nullable: false),
                    Permission = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentToolPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentToolPermissions_Agents_AgentId",
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
                    table.ForeignKey(
                        name: "FK_BrowserResources_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    table.ForeignKey(
                        name: "FK_AgentChannelBindings_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentChannelBindings_ChannelConnections_ChannelConnectionId",
                        column: x => x.ChannelConnectionId,
                        principalTable: "ChannelConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationActivity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationActivity_IntegrationConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "IntegrationConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationIndexedRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SearchText = table.Column<string>(type: "text", nullable: false),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExternalUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationIndexedRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationIndexedRecords_IntegrationConnections_Connection~",
                        column: x => x.ConnectionId,
                        principalTable: "IntegrationConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationIndexEntityStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RecordCount = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationIndexEntityStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationIndexEntityStatuses_IntegrationConnections_Conne~",
                        column: x => x.ConnectionId,
                        principalTable: "IntegrationConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationIndexJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    RecordsIndexed = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationIndexJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationIndexJobs_IntegrationConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "IntegrationConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationRequestHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Entity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ParamsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationRequestHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationRequestHistory_IntegrationConnections_Connection~",
                        column: x => x.ConnectionId,
                        principalTable: "IntegrationConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    table.ForeignKey(
                        name: "FK_MemoryStoreEntries_MemoryStores_MemoryStoreId",
                        column: x => x.MemoryStoreId,
                        principalTable: "MemoryStores",
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
                name: "IX_AgentCronJobs_AgentId",
                table: "AgentCronJobs",
                column: "AgentId");

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
                name: "IX_AgentLogs_AgentId",
                table: "AgentLogs",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentLogs_AgentId_Time",
                table: "AgentLogs",
                columns: new[] { "AgentId", "Time" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentLogs_ChannelConnectionId",
                table: "AgentLogs",
                column: "ChannelConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentLogs_CorrelationId",
                table: "AgentLogs",
                column: "CorrelationId");

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
                name: "IX_Agents_OwnerId",
                table: "Agents",
                column: "OwnerId");

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
                name: "IX_AgentToolPermissions_AgentId",
                table: "AgentToolPermissions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolPermissions_AgentId_SkillName_ToolName",
                table: "AgentToolPermissions",
                columns: new[] { "AgentId", "SkillName", "ToolName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrowserResources_CurrentAgentId",
                table: "BrowserResources",
                column: "CurrentAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_BrowserResources_OwnerId",
                table: "BrowserResources",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BrowserSessions_AgentId",
                table: "BrowserSessions",
                column: "AgentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelConnections_CreatedById",
                table: "ChannelConnections",
                column: "CreatedById");

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
                name: "IX_IntegrationActivity_ConnectionId",
                table: "IntegrationActivity",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationActivity_CreatedAt",
                table: "IntegrationActivity",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_CreatedById",
                table: "IntegrationConnections",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_Provider",
                table: "IntegrationConnections",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationCredentials_IntegrationName",
                table: "IntegrationCredentials",
                column: "IntegrationName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationIndexedRecords_ConnectionId_Entity",
                table: "IntegrationIndexedRecords",
                columns: new[] { "ConnectionId", "Entity" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationIndexedRecords_ConnectionId_Entity_ExternalId",
                table: "IntegrationIndexedRecords",
                columns: new[] { "ConnectionId", "Entity", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationIndexEntityStatuses_ConnectionId_Entity",
                table: "IntegrationIndexEntityStatuses",
                columns: new[] { "ConnectionId", "Entity" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationIndexJobs_ConnectionId",
                table: "IntegrationIndexJobs",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationIndexJobs_Status",
                table: "IntegrationIndexJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRequestHistory_ConnectionId",
                table: "IntegrationRequestHistory",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRequestHistory_CreatedAt",
                table: "IntegrationRequestHistory",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_Name",
                table: "Integrations",
                column: "Name",
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
                name: "IX_OAuthGrantedScopes_OAuthTokenId_Scope",
                table: "OAuthGrantedScopes",
                columns: new[] { "OAuthTokenId", "Scope" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthTokens_Provider",
                table: "OAuthTokens",
                column: "Provider",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrgMembers_OrganizationId_Email",
                table: "OrgMembers",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrgMembers_UserId",
                table: "OrgMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrgSubscriptions_OrganizationId",
                table: "OrgSubscriptions",
                column: "OrganizationId",
                unique: true);

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
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscriptions",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentChannelBindings");

            migrationBuilder.DropTable(
                name: "AgentCronJobs");

            migrationBuilder.DropTable(
                name: "AgentIntegrations");

            migrationBuilder.DropTable(
                name: "AgentLogs");

            migrationBuilder.DropTable(
                name: "AgentMemories");

            migrationBuilder.DropTable(
                name: "AgentPersonalities");

            migrationBuilder.DropTable(
                name: "AgentRateLimits");

            migrationBuilder.DropTable(
                name: "AgentRuns");

            migrationBuilder.DropTable(
                name: "AgentSessionContexts");

            migrationBuilder.DropTable(
                name: "AgentSessionResourceAttachments");

            migrationBuilder.DropTable(
                name: "AgentToolPermissions");

            migrationBuilder.DropTable(
                name: "BrowserResources");

            migrationBuilder.DropTable(
                name: "BrowserSessions");

            migrationBuilder.DropTable(
                name: "DeviceCodes");

            migrationBuilder.DropTable(
                name: "IntegrationActivity");

            migrationBuilder.DropTable(
                name: "IntegrationCredentials");

            migrationBuilder.DropTable(
                name: "IntegrationIndexedRecords");

            migrationBuilder.DropTable(
                name: "IntegrationIndexEntityStatuses");

            migrationBuilder.DropTable(
                name: "IntegrationIndexJobs");

            migrationBuilder.DropTable(
                name: "IntegrationRequestHistory");

            migrationBuilder.DropTable(
                name: "Integrations");

            migrationBuilder.DropTable(
                name: "MemoryStoreEntries");

            migrationBuilder.DropTable(
                name: "OAuthGrantedScopes");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "OrgMembers");

            migrationBuilder.DropTable(
                name: "OrgSubscriptions");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "SystemEvents");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "ChannelConnections");

            migrationBuilder.DropTable(
                name: "AgentSessions");

            migrationBuilder.DropTable(
                name: "IntegrationConnections");

            migrationBuilder.DropTable(
                name: "MemoryStores");

            migrationBuilder.DropTable(
                name: "OAuthTokens");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
