namespace OffceOs.Infrastructure.Features.Providers;

internal sealed class CodexAppServerAdapter : ICodexAppServerService
{
    private const string AuthJsonKey = "authJson";
    private readonly CodexAppServerConfig _codexAppServerConfig;
    private readonly ILogger<CodexAppServerAdapter> _logger;
    private readonly ConcurrentDictionary<string, CodexLoginSession> _loginSessions = new(StringComparer.Ordinal);

    public CodexAppServerAdapter(
        CodexAppServerConfig codexAppServerConfig,
        ILogger<CodexAppServerAdapter> logger)
    {
        _codexAppServerConfig = codexAppServerConfig;
        _logger = logger;
    }

    public async Task<CodexOAuthLoginResult> StartLoginAsync(Guid userId, CancellationToken ct = default)
    {
        var home = CreateHomeDirectory("login", userId);
        var client = await CodexRpcClient.StartAsync(_codexAppServerConfig, home, _logger, ct);
        var response = await client.RequestAsync(
            "account/login/start",
            new { type = "chatgpt" },
            TimeSpan.FromSeconds(30),
            ct);
        var result = response.GetProperty("result");
        var loginId = result.GetProperty("loginId").GetString()
            ?? throw new InvalidOperationException("Codex login did not return a login id.");
        var authUrl = result.GetProperty("authUrl").GetString()
            ?? throw new InvalidOperationException("Codex login did not return an authentication URL.");

        var session = new CodexLoginSession(userId, home, client, DateTime.UtcNow.AddSeconds(_codexAppServerConfig.LoginTimeoutSeconds));
        client.NotificationReceived += session.HandleNotification;
        if (!_loginSessions.TryAdd(loginId, session))
        {
            await client.DisposeAsync();
            throw new InvalidOperationException("Codex login id collision.");
        }

        return new CodexOAuthLoginResult(loginId, authUrl, session.ExpiresAt);
    }

    public async Task<CodexOAuthStatusResult> PollLoginAsync(string loginId, CancellationToken ct = default)
    {
        if (!_loginSessions.TryGetValue(loginId, out var session))
            return new CodexOAuthStatusResult(loginId, true, false, "Codex login session was not found or expired.", null, null, new Dictionary<string, string>());

        if (DateTime.UtcNow > session.ExpiresAt)
        {
            await RemoveLoginSessionAsync(loginId, session);
            return new CodexOAuthStatusResult(loginId, true, false, "Codex login session expired.", null, null, new Dictionary<string, string>());
        }

        if (!session.Completed)
            return new CodexOAuthStatusResult(loginId, false, false, null, null, null, new Dictionary<string, string>());

        if (!session.Success)
        {
            await RemoveLoginSessionAsync(loginId, session);
            return new CodexOAuthStatusResult(loginId, true, false, session.Error ?? "Codex login failed.", null, null, new Dictionary<string, string>());
        }

        var account = await ReadAccountAsync(session.Client, ct);
        var authJson = await ReadAuthJsonAsync(session.HomePath, ct);
        await RemoveLoginSessionAsync(loginId, session, deleteHome: true);

        var credentials = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["authKind"] = ProviderAuthKind.CodexChatGptOAuth.ToStorageString(),
            [AuthJsonKey] = authJson,
        };
        if (!string.IsNullOrWhiteSpace(account.Email))
            credentials["accountEmail"] = account.Email;
        if (!string.IsNullOrWhiteSpace(account.PlanType))
            credentials["planType"] = account.PlanType;

        return new CodexOAuthStatusResult(loginId, true, true, null, account.Email, account.PlanType, credentials);
    }

    public async Task<AgentResult<LlmDispatchResponse>> DispatchAsync(ProviderAuthResult auth, string model, JsonElement requestBody, CancellationToken ct = default)
    {
        if (auth.Kind != ProviderAuthKind.CodexChatGptOAuth)
            return new AgentError(AgentErrorCategory.Configuration, "OpenAI Codex requires Codex ChatGPT OAuth.");

        var authJson = auth.Get(AuthJsonKey);
        if (string.IsNullOrWhiteSpace(authJson))
            return new AgentError(AgentErrorCategory.Configuration, "OpenAI Codex OAuth credentials are missing.");

        var home = CreateHomeDirectory("dispatch", Guid.NewGuid());
        try
        {
            await WriteAuthJsonAsync(home, authJson, ct);
            await using var client = await CodexRpcClient.StartAsync(_codexAppServerConfig, home, _logger, ct);
            var account = await ReadAccountAsync(client, ct);
            if (string.IsNullOrWhiteSpace(account.Type))
                return new AgentError(AgentErrorCategory.Configuration, "OpenAI Codex OAuth is not connected.");

            var resolvedModel = model.Equals(ProviderRegistry.DefaultModel, StringComparison.OrdinalIgnoreCase)
                ? "gpt-5.5"
                : model;
            var threadResponse = await client.RequestAsync(
                "thread/start",
                new { model = resolvedModel },
                TimeSpan.FromSeconds(30),
                ct);
            var threadId = threadResponse.GetProperty("result").GetProperty("thread").GetProperty("id").GetString()
                ?? throw new InvalidOperationException("Codex thread start did not return a thread id.");

            var collector = new CodexTurnCollector(threadId);
            client.NotificationReceived += collector.HandleNotification;
            await client.RequestAsync(
                "turn/start",
                new
                {
                    threadId,
                    input = new[] { new { type = "text", text = BuildCodexPrompt(requestBody) } },
                    model = resolvedModel,
                    approvalPolicy = "never",
                    sandboxPolicy = new { type = "readOnly" },
                    cwd = home,
                },
                TimeSpan.FromSeconds(30),
                ct);

            var text = await collector.WaitAsync(TimeSpan.FromSeconds(_codexAppServerConfig.TurnTimeoutSeconds), ct);
            return new LlmDispatchResponse(CreateSseResponse(text), resolvedModel);
        }
        catch (TaskCanceledException ex)
        {
            return new AgentError(AgentErrorCategory.LlmCall, "Codex call timed out", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new AgentError(AgentErrorCategory.Configuration, ex.Message, ex.ToString());
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.LlmCall, $"Unexpected Codex error: {ex.Message}", ex.ToString());
        }
        finally
        {
            TryDeleteDirectory(home);
        }
    }

    private async Task RemoveLoginSessionAsync(string loginId, CodexLoginSession session, bool deleteHome = false)
    {
        _loginSessions.TryRemove(loginId, out _);
        await session.Client.DisposeAsync();
        if (deleteHome)
            TryDeleteDirectory(session.HomePath);
    }

    private async Task<CodexAccount> ReadAccountAsync(CodexRpcClient client, CancellationToken ct)
    {
        var response = await client.RequestAsync(
            "account/read",
            new { refreshToken = true },
            TimeSpan.FromSeconds(30),
            ct);
        var result = response.GetProperty("result");
        if (!result.TryGetProperty("account", out var account) || account.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return new CodexAccount(null, null, null);

        return new CodexAccount(
            account.TryGetProperty("type", out var type) ? type.GetString() : null,
            account.TryGetProperty("email", out var email) ? email.GetString() : null,
            account.TryGetProperty("planType", out var planType) ? planType.GetString() : null);
    }

    private static async Task<string> ReadAuthJsonAsync(string home, CancellationToken ct)
    {
        var path = System.IO.Path.Combine(home, "auth.json");
        if (!System.IO.File.Exists(path))
            throw new InvalidOperationException("Codex did not persist ChatGPT OAuth credentials.");

        return await System.IO.File.ReadAllTextAsync(path, ct);
    }

    private static async Task WriteAuthJsonAsync(string home, string authJson, CancellationToken ct)
    {
        Directory.CreateDirectory(home);
        await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(home, "auth.json"), authJson, ct);
    }

    private string CreateHomeDirectory(string purpose, Guid ownerId)
    {
        var root = System.IO.Path.Combine(_codexAppServerConfig.EffectiveHomeRoot, purpose);
        var home = System.IO.Path.Combine(root, ownerId.ToString("N"), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(home);
        return home;
    }

    private static HttpResponseMessage CreateSseResponse(string? text)
    {
        var payload = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    delta = new
                    {
                        content = text ?? string.Empty,
                    },
                },
            },
        });
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"data: {payload}\n\ndata: [DONE]\n\n", Encoding.UTF8, "text/event-stream"),
        };
        return response;
    }

    private static string BuildCodexPrompt(JsonElement requestBody)
    {
        if (!requestBody.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array)
            return requestBody.GetRawText();

        var builder = new StringBuilder();
        builder.AppendLine("Respond to the following EnterpriseAgentOS agent conversation. Do not run shell commands or edit files.");
        builder.AppendLine();
        foreach (var message in messages.EnumerateArray())
        {
            var role = message.TryGetProperty("role", out var roleElement) ? roleElement.GetString() ?? "message" : "message";
            var content = message.TryGetProperty("content", out var contentElement) ? ExtractContent(contentElement) : "";
            if (string.IsNullOrWhiteSpace(content))
                continue;

            builder.Append(role.ToUpperInvariant());
            builder.AppendLine(":");
            builder.AppendLine(content);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string ExtractContent(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString() ?? "";
        if (element.ValueKind != JsonValueKind.Array)
            return element.GetRawText();

        return string.Join(
            "\n",
            element.EnumerateArray()
                .Select(item =>
                    item.ValueKind == JsonValueKind.Object &&
                    item.TryGetProperty("text", out var text) &&
                    text.ValueKind == JsonValueKind.String
                        ? text.GetString()
                        : null)
                .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }

    private sealed record CodexAccount(string? Type, string? Email, string? PlanType);

    private sealed class CodexLoginSession
    {
        public CodexLoginSession(Guid organizationId, string homePath, CodexRpcClient client, DateTime expiresAt)
        {
            OrganizationId = organizationId;
            HomePath = homePath;
            Client = client;
            ExpiresAt = expiresAt;
        }

        public Guid OrganizationId { get; }
        public string HomePath { get; }
        public CodexRpcClient Client { get; }
        public DateTime ExpiresAt { get; }
        public bool Completed { get; private set; }
        public bool Success { get; private set; }
        public string? Error { get; private set; }

        public void HandleNotification(JsonElement message)
        {
            if (!message.TryGetProperty("method", out var method) ||
                method.GetString() != "account/login/completed" ||
                !message.TryGetProperty("params", out var parameters))
                return;

            Completed = true;
            Success = parameters.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True;
            Error = parameters.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String
                ? error.GetString()
                : null;
        }
    }

    private sealed class CodexTurnCollector
    {
        private readonly string _threadId;
        private readonly StringBuilder _stringBuilder = new();
        private readonly TaskCompletionSource<string?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private string? _finalText;

        public CodexTurnCollector(string threadId) => _threadId = threadId;

        public void HandleNotification(JsonElement message)
        {
            if (!message.TryGetProperty("method", out var methodElement))
                return;

            var method = methodElement.GetString();
            if (method == "item/agentMessage/delta")
            {
                AppendDelta(message);
                return;
            }

            if (method == "item/completed")
            {
                CaptureCompletedAgentMessage(message);
                return;
            }

            if (method == "turn/completed")
                CompleteTurn(message);
        }

        public async Task<string?> WaitAsync(TimeSpan timeout, CancellationToken ct)
        {
            var result = await _completion.Task.WaitAsync(timeout, ct);
            return result;
        }

        private void AppendDelta(JsonElement message)
        {
            if (!message.TryGetProperty("params", out var parameters) || !MatchesThread(parameters))
                return;

            if (parameters.TryGetProperty("delta", out var delta) && delta.ValueKind == JsonValueKind.String)
                _stringBuilder.Append(delta.GetString());
            else if (parameters.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                _stringBuilder.Append(text.GetString());
        }

        private void CaptureCompletedAgentMessage(JsonElement message)
        {
            if (!message.TryGetProperty("params", out var parameters) || !MatchesThread(parameters))
                return;
            if (!parameters.TryGetProperty("item", out var item) ||
                !item.TryGetProperty("type", out var type) ||
                type.GetString() != "agentMessage")
                return;

            if (item.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                _finalText = text.GetString();
        }

        private void CompleteTurn(JsonElement message)
        {
            if (!message.TryGetProperty("params", out var parameters) || !MatchesThread(parameters))
                return;

            var status = parameters.TryGetProperty("turn", out var turn) &&
                turn.TryGetProperty("status", out var statusElement)
                    ? statusElement.GetString()
                    : null;
            if (status == "failed")
            {
                var error = turn.TryGetProperty("error", out var errorElement) &&
                    errorElement.TryGetProperty("message", out var messageElement)
                        ? messageElement.GetString()
                        : "Codex turn failed.";
                _completion.TrySetException(new InvalidOperationException(error));
                return;
            }

            _completion.TrySetResult(_finalText ?? _stringBuilder.ToString());
        }

        private bool MatchesThread(JsonElement parameters)
        {
            if (!parameters.TryGetProperty("threadId", out var threadId))
                return true;

            return threadId.GetString() == _threadId;
        }
    }

    private sealed class CodexRpcClient : IAsyncDisposable
    {
        private readonly Process _process;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> _pending = new();
        private int _nextId;

        private CodexRpcClient(Process process, Microsoft.Extensions.Logging.ILogger logger)
        {
            _process = process;
            _logger = logger;
        }

        public event Action<JsonElement>? NotificationReceived;

        public static async Task<CodexRpcClient> StartAsync(CodexAppServerConfig config, string home, Microsoft.Extensions.Logging.ILogger logger, CancellationToken ct)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = config.Command,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add("app-server");
            startInfo.Environment["CODEX_HOME"] = home;

            var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start Codex app-server process.");
            var client = new CodexRpcClient(process, logger);
            _ = client.ReadStdoutAsync();
            _ = client.ReadStderrAsync();
            await client.InitializeAsync(ct);
            return client;
        }

        public async Task<JsonElement> RequestAsync(string method, object? parameters, TimeSpan timeout, CancellationToken ct)
        {
            var id = Interlocked.Increment(ref _nextId);
            var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[id] = tcs;
            await WriteAsync(new { method, id, @params = parameters }, ct);
            return await tcs.Task.WaitAsync(timeout, ct);
        }

        private async Task InitializeAsync(CancellationToken ct)
        {
            await RequestAsync(
                "initialize",
                new
                {
                    clientInfo = new
                    {
                        name = "enterprise_agent_os",
                        title = "EnterpriseAgentOS",
                        version = "0.1.0",
                    },
                },
                TimeSpan.FromSeconds(30),
                ct);
            await WriteAsync(new { method = "initialized", @params = new { } }, ct);
        }

        private async Task WriteAsync(object message, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(message);
            await _process.StandardInput.WriteLineAsync(json.AsMemory(), ct);
            await _process.StandardInput.FlushAsync(ct);
        }

        private async Task ReadStdoutAsync()
        {
            try
            {
                while (await _process.StandardOutput.ReadLineAsync() is { } line)
                {
                    using var document = JsonDocument.Parse(line);
                    var root = document.RootElement.Clone();
                    if (root.TryGetProperty("id", out var idElement) &&
                        idElement.ValueKind == JsonValueKind.Number &&
                        idElement.TryGetInt32(out var id) &&
                        !root.TryGetProperty("method", out _))
                    {
                        if (_pending.TryRemove(id, out var tcs))
                        {
                            if (root.TryGetProperty("error", out var error))
                            {
                                var message = error.TryGetProperty("message", out var errorMessage)
                                    ? errorMessage.GetString()
                                    : "Codex app-server request failed.";
                                tcs.TrySetException(new InvalidOperationException(message));
                            }
                            else
                            {
                                tcs.TrySetResult(root);
                            }
                        }
                    }
                    else if (root.TryGetProperty("method", out _))
                    {
                        NotificationReceived?.Invoke(root);
                        if (root.TryGetProperty("id", out var requestId))
                            await RespondToServerRequestAsync(requestId.GetInt32());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Codex app-server stdout reader stopped.");
                foreach (var pending in _pending.Values)
                    pending.TrySetException(ex);
            }
        }

        private async Task RespondToServerRequestAsync(int id)
        {
            try
            {
                await WriteAsync(new { id, result = new { decision = "decline" } }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to answer Codex app-server request {RequestId}.", id);
            }
        }

        private async Task ReadStderrAsync()
        {
            try
            {
                while (await _process.StandardError.ReadLineAsync() is { } line)
                    _logger.LogDebug("Codex app-server: {Line}", line);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Codex app-server stderr reader stopped.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (!_process.HasExited)
                    _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
            catch
            {
            }
            finally
            {
                _process.Dispose();
            }
        }
    }
}
