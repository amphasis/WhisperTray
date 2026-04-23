using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using WhisperTray.Core.Configuration;

namespace WhisperTray.App.Adapters;

/// <summary>Per-user DPAPI protection. Ciphertext is only decryptable by the same Windows user.</summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiSecretProtector : ISecretProtector
{
    private static readonly byte[] OptionalEntropy = Encoding.UTF8.GetBytes("WhisperTray.v1");

    public string Protect(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = ProtectedData.Protect(plain, OptionalEntropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(cipher);
    }

    public string? Unprotect(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        try
        {
            var cipher = Convert.FromBase64String(ciphertext);
            var plain = ProtectedData.Unprotect(cipher, OptionalEntropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch (FormatException)
        {
            return null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
