using FluentAssertions;
using WhisperTray.Core.Configuration;

namespace WhisperTray.Core.Tests;

public class PassthroughSecretProtectorTests
{
    private readonly PassthroughSecretProtector _sut = new();

    [Fact]
    public void Protect_ThenUnprotect_ReturnsOriginalPlaintext()
    {
        var roundTripped = _sut.Unprotect(_sut.Protect("sk-test-secret"));

        roundTripped.Should().Be("sk-test-secret");
    }

    [Fact]
    public void Protect_EmptyString_RoundTripsToEmptyString()
    {
        _sut.Unprotect(_sut.Protect(string.Empty)).Should().Be(string.Empty);
    }

    [Fact]
    public void Unprotect_NotBase64_ReturnsNull()
    {
        _sut.Unprotect("not valid base64 !!!").Should().BeNull();
    }

    [Fact]
    public void Protect_Null_Throws()
    {
        var act = () => _sut.Protect(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
