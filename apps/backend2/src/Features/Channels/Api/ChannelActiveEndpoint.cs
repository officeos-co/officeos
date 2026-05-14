namespace OffceOs.Api.Features.Channels;

public static class ChannelActiveEndpoint
{
    public static async Task<IResult> Handle(
        IChannelRepository channelRepository,
        ChannelCredentialProtector protector,
        CancellationToken ct)
    {
        var connections = await channelRepository.ListConnectionsAsync(ct: ct);
        var active = new List<object>();

        foreach (var conn in connections)
        {
            if (!conn.Enabled || string.IsNullOrEmpty(conn.EncryptedCreds))
                continue;

            try
            {
                var decrypted = protector.Unprotect(conn.EncryptedCreds);
                active.Add(new
                {
                    connectionId = conn.Id.ToString(),
                    channelType = conn.ChannelType.ToStorageString(),
                    creds = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted)
                });
            }
            catch
            {
                // Skip connections with corrupted creds
            }
        }

        return Results.Ok(active);
    }
}
