namespace EnterpriseAgentOs.Domain.Features.Channels;

/// <summary>
/// Encrypts/decrypts channel connection config blobs.
/// Infrastructure provides the DataProtection-backed implementation.
/// </summary>
public interface IChannelConfigProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}
