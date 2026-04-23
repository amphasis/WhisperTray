namespace WhisperTray.Core.Configuration;

/// <summary>Abstraction over a per-user secret protection scheme (DPAPI on Windows).</summary>
public interface ISecretProtector
{
    string Protect(string plaintext);

    /// <summary>Returns null if the ciphertext cannot be decrypted (tampered, copied from another user).</summary>
    string? Unprotect(string ciphertext);
}
