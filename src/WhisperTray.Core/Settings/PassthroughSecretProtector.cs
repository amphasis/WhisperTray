using System.Text;

namespace WhisperTray.Core.Configuration;

/// <summary>Test/placeholder protector — base64-encodes plaintext without real encryption.</summary>
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
