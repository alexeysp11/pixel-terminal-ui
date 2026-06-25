using AutoFixture;
using FluentAssertions;
using PixelTerminalUI.Contracts.Optimizations;

namespace PixelTerminalUI.Contracts.Tests.Optimizations;

public sealed class PixelBitPackerTests
{
    private sealed class PackingTestData
    {
        public char Character { get; init; }
        public byte Foreground { get; init; }
        public byte Background { get; init; }
        public byte Flags { get; init; }
    }

    [Fact]
    public void PackAndUnpack_ShouldRestoreOriginalValues_WhenInputsAreWithinValidBitBounds()
    {
        // Arrange
        Fixture fixture = new();

        // Constrain values manually to fit within specified bit masks (7 bits for colors, 2 bits for flags)
        PackingTestData expectedData = new()
        {
            Character = fixture.Create<char>(),
            Foreground = (byte)(fixture.Create<byte>() & 0x7F),
            Background = (byte)(fixture.Create<byte>() & 0x7F),
            Flags = (byte)(fixture.Create<byte>() & 0x03)
        };

        // Act
        uint packedResult = PixelBitPacker.Pack(
            expectedData.Character,
            expectedData.Foreground,
            expectedData.Background,
            expectedData.Flags);

        PixelBitPacker.Unpack(
            packedResult,
            out char actualChar,
            out byte actualForeground,
            out byte actualBackground,
            out byte actualFlags);

        // Assert
        actualChar
            .Should()
            .Be(expectedData.Character, "because the 16-bit character payload must be preserved during packing transformations");

        actualForeground
            .Should()
            .Be(expectedData.Foreground, "because the 7-bit foreground index must match the original input value precisely");

        actualBackground
            .Should()
            .Be(expectedData.Background, "because the 7-bit background index must remain unaffected by neighboring bit transitions");

        actualFlags
            .Should()
            .Be(expectedData.Flags, "because system rendering flags must retain their exact bit configuration at the tail of the block");
    }

    [Fact]
    public void Pack_ShouldApplyBitMasks_WhenInputsExceedAllottedBitWidths()
    {
        // Arrange
        char inputChar = 'A';
        byte overflowForeground = 130; // Binary: 10000010, expected 7-bit mask output: 0000010 (2)
        byte overflowBackground = 255; // Binary: 11111111, expected 7-bit mask output: 1111111 (127)
        byte overflowFlags = 5;        // Binary: 00000101, expected 2-bit mask output: 0000001 (1)

        byte expectedForeground = (byte)(overflowForeground & 0x7F);
        byte expectedBackground = (byte)(overflowBackground & 0x7F);
        byte expectedFlags = (byte)(overflowFlags & 0x03);

        // Act
        uint packedResult = PixelBitPacker.Pack(inputChar, overflowForeground, overflowBackground, overflowFlags);

        PixelBitPacker.Unpack(
            packedResult,
            out char actualChar,
            out byte actualForeground,
            out byte actualBackground,
            out byte actualFlags);

        // Assert
        actualChar
            .Should()
            .Be(inputChar, "because character bits occupy separate memory offsets and should never overlap with color ranges");

        actualForeground
            .Should()
            .Be(expectedForeground, "because out-of-bounds color variables must be safely clamped via bitwise logic gates");

        actualBackground
            .Should()
            .Be(expectedBackground, "because background overflows must not leak memory into neighboring structural attributes");

        actualFlags
            .Should()
            .Be(expectedFlags, "because excessive flag values must be truncated to prevent general memory corruption inside the unsigned state integer");
    }

    [Theory]
    [InlineData(char.MinValue, 0, 0, 0)]
    [InlineData(char.MaxValue, 127, 127, 3)]
    [InlineData(' ', 64, 32, 2)]
    [InlineData('⚙', 1, 1, 1)]
    public void PackAndUnpack_ShouldHandleExtremeBoundaryValues_WithoutDataLoss(
        char inputChar,
        byte inputForeground,
        byte inputBackground,
        byte inputFlags)
    {
        // Act
        uint packedResult = PixelBitPacker.Pack(inputChar, inputForeground, inputBackground, inputFlags);

        PixelBitPacker.Unpack(
            packedResult,
            out char actualChar,
            out byte actualForeground,
            out byte actualBackground,
            out byte actualFlags);

        // Assert
        actualChar
            .Should()
            .Be(inputChar, "because maximum and minimum bound unicode characters must survive conversion cycles without sign extension issues");

        actualForeground
            .Should()
            .Be(inputForeground, "because boundary edge color codes must remain stable at the limits of the 7-bit numerical boundaries");

        actualBackground
            .Should()
            .Be(inputBackground, "because boundary background values must map exactly to prevent visual artifacts on empty space matrices");

        actualFlags
            .Should()
            .Be(inputFlags, "because rendering modifiers must retain absolute fidelity at both zero and full capacity bounds");
    }

    [Fact]
    public void Pack_ShouldPreventBitBleeding_WhenOnlyOneFieldHasMaximumValue()
    {
        // Act & Assert 1: Testing character isolation
        uint packedCharOnly = PixelBitPacker.Pack(char.MaxValue, 0, 0, 0);
        PixelBitPacker.Unpack(packedCharOnly, out char charOnlyVal, out byte charOnlyFg, out byte charOnlyBg, out byte charOnlyFlags);

        charOnlyVal.Should().Be(char.MaxValue, "because character bits must be fully populated");
        charOnlyFg.Should().Be(0, "because a maximum character value must not spill into the foreground bit range");
        charOnlyBg.Should().Be(0, "because a maximum character value must not contaminate background bits");
        charOnlyFlags.Should().Be(0, "because a maximum character value must not trigger layout flags accidentally");

        // Act & Assert 2: Testing foreground isolation
        uint packedFgOnly = PixelBitPacker.Pack('\0', 127, 0, 0);
        PixelBitPacker.Unpack(packedFgOnly, out char fgOnlyVal, out byte fgOnlyFg, out byte fgOnlyBg, out byte fgOnlyFlags);

        fgOnlyVal.Should().Be('\0', "because foreground bits must not rewrite the empty character space");
        fgOnlyFg.Should().Be(127, "because foreground bits must be fully saturated");
        fgOnlyBg.Should().Be(0, "because maximum foreground bits must never affect background color indices");
        fgOnlyFlags.Should().Be(0, "because foreground shifts must stop cleanly before touching system flag offsets");

        // Act & Assert 3: Testing background isolation
        uint packedBgOnly = PixelBitPacker.Pack('\0', 0, 127, 0);
        PixelBitPacker.Unpack(packedBgOnly, out char bgOnlyVal, out byte bgOnlyFg, out byte bgOnlyBg, out byte bgOnlyFlags);

        bgOnlyVal.Should().Be('\0', "because background properties must remain completely separated from character text space");
        bgOnlyFg.Should().Be(0, "because background saturation must leave the foreground layer untouched");
        bgOnlyBg.Should().Be(127, "because background bit segments must store their maximum payload correctly");
        bgOnlyFlags.Should().Be(0, "because background boundaries must not overflow into the system execution flags");

        // Act & Assert 4: Testing flags isolation
        uint packedFlagsOnly = PixelBitPacker.Pack('\0', 0, 0, 3);
        PixelBitPacker.Unpack(packedFlagsOnly, out char flagsOnlyVal, out byte flagsOnlyFg, out byte flagsOnlyBg, out byte flagsOnlyFlags);

        flagsOnlyVal.Should().Be('\0', "because trailing flags must not alter high-order character array segments");
        flagsOnlyFg.Should().Be(0, "because active system status flags must not distort foreground color elements");
        flagsOnlyBg.Should().Be(0, "because active system status flags must not shift up into background color partitions");
        flagsOnlyFlags.Should().Be(3, "because execution flags must be correctly registered at the base of the structural byte block");
    }
}
