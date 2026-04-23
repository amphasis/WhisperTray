using System.Text;
using WhisperTray.Core.Configuration;

namespace WhisperTray.Core.Tests.TestInfrastructure;

/// <summary>Test-only ISecretProtector — base64-encodes plaintext without real encryption.</summary>
public sealed class PassthroughSecretProtector : ISecretProtector
{
    public string Protect(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));
    }

    public string? Unprotect(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(ciphertext));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
