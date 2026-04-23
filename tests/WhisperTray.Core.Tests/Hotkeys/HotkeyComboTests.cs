using FluentAssertions;
using WhisperTray.Core.Hotkeys;

namespace WhisperTray.Core.Tests.Hotkeys;

public class HotkeyComboTests
{
    [Fact]
    public void Parse_CtrlAltSpace_ProducesExpectedModifiersAndKey()
    {
        var combo = HotkeyCombo.Parse("Ctrl+Alt+Space");

        combo.Modifiers.Should().Be(HotkeyModifiers.Control | HotkeyModifiers.Alt);
        combo.VirtualKey.Should().Be(0x20);
        combo.KeyName.Should().Be("Space");
    }

    [Theory]
    [InlineData("ctrl+alt+space")]
    [InlineData("CTRL+ALT+SPACE")]
    [InlineData("Control+Alt+Space")]
    [InlineData(" Ctrl + Alt + Space ")]
    public void Parse_CaseAndWhitespaceInsensitive(string input)
    {
        var combo = HotkeyCombo.Parse(input);

        combo.Modifiers.Should().Be(HotkeyModifiers.Control | HotkeyModifiers.Alt);
        combo.KeyName.Should().Be("Space");
    }

    [Fact]
    public void Parse_AcceptsModifiersInAnyOrder()
    {
        var combo = HotkeyCombo.Parse("Space+Alt+Ctrl");

        combo.Modifiers.Should().Be(HotkeyModifiers.Control | HotkeyModifiers.Alt);
        combo.KeyName.Should().Be("Space");
    }

    [Fact]
    public void Parse_AcceptsAllFourModifiers()
    {
        var combo = HotkeyCombo.Parse("Ctrl+Alt+Shift+Win+F5");

        combo.Modifiers.Should().Be(
            HotkeyModifiers.Control | HotkeyModifiers.Alt |
            HotkeyModifiers.Shift | HotkeyModifiers.Win);
        combo.KeyName.Should().Be("F5");
    }

    [Theory]
    [InlineData("Win", HotkeyModifiers.Win)]
    [InlineData("Windows", HotkeyModifiers.Win)]
    [InlineData("Meta", HotkeyModifiers.Win)]
    [InlineData("Super", HotkeyModifiers.Win)]
    [InlineData("Control", HotkeyModifiers.Control)]
    [InlineData("Ctrl", HotkeyModifiers.Control)]
    public void Parse_AcceptsModifierAliases(string alias, HotkeyModifiers expected)
    {
        var combo = HotkeyCombo.Parse(alias + "+A");

        combo.Modifiers.Should().Be(expected);
    }

    [Theory]
    [InlineData("Esc", "Escape")]
    [InlineData("PgUp", "PageUp")]
    [InlineData("Return", "Enter")]
    public void Parse_NormalizesKeyAliasesToCanonical(string alias, string canonical)
    {
        var combo = HotkeyCombo.Parse("Ctrl+" + alias);

        combo.KeyName.Should().Be(canonical);
    }

    [Fact]
    public void ToString_ProducesCanonicalForm()
    {
        var combo = HotkeyCombo.Parse("Shift+Ctrl+Space");

        combo.ToString().Should().Be("Ctrl+Shift+Space");
    }

    [Fact]
    public void ToString_OrdersModifiersCtrlAltShiftWin()
    {
        var combo = HotkeyCombo.Parse("Win+Shift+Alt+Ctrl+Delete");

        combo.ToString().Should().Be("Ctrl+Alt+Shift+Win+Delete");
    }

    [Fact]
    public void Parse_ToStringRoundTrip_Stable()
    {
        var original = HotkeyCombo.Parse("Alt+Win+F1");

        var roundTripped = HotkeyCombo.Parse(original.ToString());

        roundTripped.Should().Be(original);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_EmptyInput_Throws(string? input)
    {
        var act = () => HotkeyCombo.Parse(input!);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Parse_BaseKeyWithoutModifier_Throws()
    {
        var act = () => HotkeyCombo.Parse("A");
        act.Should().Throw<FormatException>().WithMessage("*modifier*");
    }

    [Fact]
    public void Parse_FunctionKeyAlone_StillRequiresModifier()
    {
        var act = () => HotkeyCombo.Parse("F5");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Parse_ModifierOnly_Throws()
    {
        var act = () => HotkeyCombo.Parse("Ctrl+Shift");
        act.Should().Throw<FormatException>().WithMessage("*base key*");
    }

    [Fact]
    public void Parse_TwoBaseKeys_Throws()
    {
        var act = () => HotkeyCombo.Parse("Ctrl+A+B");
        act.Should().Throw<FormatException>().WithMessage("*Multiple base keys*");
    }

    [Fact]
    public void Parse_DuplicateModifier_Throws()
    {
        var act = () => HotkeyCombo.Parse("Ctrl+Ctrl+A");
        act.Should().Throw<FormatException>().WithMessage("*Duplicate*");
    }

    [Fact]
    public void Parse_UnknownKey_Throws()
    {
        var act = () => HotkeyCombo.Parse("Ctrl+Wharrgarbl");
        act.Should().Throw<FormatException>().WithMessage("*Unknown key*");
    }

    [Fact]
    public void TryParse_InvalidInput_ReturnsFalseWithReason()
    {
        var ok = HotkeyCombo.TryParse("Ctrl+Wharrgarbl", out var result, out var error);

        ok.Should().BeFalse();
        result.Should().BeNull();
        error.Should().Contain("Unknown key");
    }

    [Fact]
    public void TryParse_ValidInput_ReturnsTrueWithResult()
    {
        var ok = HotkeyCombo.TryParse("Ctrl+A", out var result, out var error);

        ok.Should().BeTrue();
        result.Should().NotBeNull();
        error.Should().BeNull();
    }

    [Fact]
    public void Equality_SameModifiersAndKey_AreEqual()
    {
        HotkeyCombo.Parse("Ctrl+A").Should().Be(HotkeyCombo.Parse("A+Ctrl"));
    }

    [Fact]
    public void Equality_DifferentKey_NotEqual()
    {
        HotkeyCombo.Parse("Ctrl+A").Should().NotBe(HotkeyCombo.Parse("Ctrl+B"));
    }

    [Fact]
    public void Parse_NumberOnTopRow_MapsCorrectly()
    {
        var combo = HotkeyCombo.Parse("Ctrl+5");

        combo.VirtualKey.Should().Be(0x35);
        combo.KeyName.Should().Be("5");
    }

    [Fact]
    public void Parse_F24_Works()
    {
        var combo = HotkeyCombo.Parse("Ctrl+F24");

        combo.VirtualKey.Should().Be(0x87);
    }

    [Fact]
    public void Parse_Numpad5_MapsToNumpadVk()
    {
        var combo = HotkeyCombo.Parse("Ctrl+Numpad5");

        combo.VirtualKey.Should().Be(0x65);
    }
}
