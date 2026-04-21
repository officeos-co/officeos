using System.Text.Json;
using File = System.IO.File;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.Core.Types;
using Microsoft.Extensions.Logging;

namespace EnterpriseAgentOs.Infrastructure.Channels;

/// <summary>
/// Bridges BaileysCSharp's file-based credential storage to our encrypted
/// database-backed ChannelConnectionRecord.EncryptedConfig.
/// </summary>
public sealed class WhatsAppSessionStore
{
    private readonly ChannelConfigProtector _protector;
    private readonly ILogger<WhatsAppSessionStore> _logger;

    public WhatsAppSessionStore(ChannelConfigProtector protector, ILogger<WhatsAppSessionStore> logger)
    {
        _protector = protector;
        _logger = logger;
    }

    /// <summary>
    /// Get or create the session directory for a connection.
    /// If we have persisted credentials in the DB, restore them to the file system
    /// so BaileysCSharp can read them.
    /// </summary>
    public string GetOrCreateSessionDir(ChannelConnectionRecord connection)
    {
        var sessionDir = Path.Combine(Path.GetTempPath(), "eaos-whatsapp", connection.Id.ToString("N"));
        Directory.CreateDirectory(sessionDir);

        // Restore persisted creds from DB if available
        if (!string.IsNullOrEmpty(connection.EncryptedConfig))
        {
            try
            {
                var json = _protector.Unprotect(connection.EncryptedConfig);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict?.TryGetValue("credsJson", out var credsJson) == true && !string.IsNullOrEmpty(credsJson))
                {
                    File.WriteAllText(Path.Combine(sessionDir, "creds.json"), credsJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to restore WhatsApp session for connection {Id}", connection.Id);
            }
        }

        return sessionDir;
    }

    /// <summary>
    /// Check if a connection has persisted session credentials.
    /// </summary>
    public bool HasSession(ChannelConnectionRecord connection)
    {
        if (string.IsNullOrEmpty(connection.EncryptedConfig))
            return false;

        try
        {
            var json = _protector.Unprotect(connection.EncryptedConfig);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return dict?.ContainsKey("credsJson") == true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Persist creds.json from the session directory to the encrypted config.
    /// Returns the encrypted string to save to ChannelConnectionRecord.EncryptedConfig.
    /// </summary>
    public string? PersistSession(string sessionDir)
    {
        var credsPath = Path.Combine(sessionDir, "creds.json");
        if (!File.Exists(credsPath))
            return null;

        var credsJson = File.ReadAllText(credsPath);
        var dict = new Dictionary<string, string> { ["credsJson"] = credsJson };
        return _protector.Protect(JsonSerializer.Serialize(dict));
    }

    /// <summary>
    /// Create a fresh AuthenticationState for BaileysCSharp.
    /// Uses persisted creds if available, otherwise creates new ones.
    /// </summary>
    public AuthenticationState CreateAuthState(string sessionDir)
    {
        var keys = new FileKeyStore(sessionDir);
        var credsPath = Path.Combine(sessionDir, "creds.json");

        AuthenticationCreds creds;
        if (File.Exists(credsPath))
        {
            creds = JsonSerializer.Deserialize<AuthenticationCreds>(File.ReadAllText(credsPath))
                ?? AuthenticationUtils.InitAuthCreds();
        }
        else
        {
            creds = AuthenticationUtils.InitAuthCreds();
        }

        return new AuthenticationState
        {
            Creds = creds,
            Keys = keys
        };
    }
}
