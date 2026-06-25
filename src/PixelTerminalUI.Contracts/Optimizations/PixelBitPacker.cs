using System.Runtime.CompilerServices;

namespace PixelTerminalUI.Contracts.Optimizations;

/// <summary>
/// Provides zero-allocation, high-performance bit-packing utilities 
/// to compress cell metadata into a primitive 32-bit unsigned integer.
/// </summary>
public static class PixelBitPacker
{
    private const uint CharMask = 0xFFFF;
    private const uint ColorMask = 0x7F;
    private const uint FlagsMask = 0x03;

    private const int CharShift = 16;
    private const int ForegroundShift = 9;
    private const int BackgroundShift = 2;

    /// <summary>
    /// Packs cell attributes into a single 32-bit unsigned integer.
    /// </summary>
    /// <param name="character">The Unicode character represented by 16 bits.</param>
    /// <param name="foreground">The foreground color index restricted to 7 bits.</param>
    /// <param name="background">The background color index restricted to 7 bits.</param>
    /// <param name="flags">System render flags restricted to 2 bits.</param>
    /// <returns>A tightly packed unsigned integer containing all cell metadata.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Pack(char character, byte foreground, byte background, byte flags)
    {
        return (uint)character << CharShift
               | (foreground & ColorMask) << ForegroundShift
               | (background & ColorMask) << BackgroundShift
               | flags & FlagsMask;
    }

    /// <summary>
    /// Unpacks a 32-bit integer back into its composite cell attributes.
    /// </summary>
    /// <param name="packed">The packed integer containing cell state data.</param>
    /// <param name="character">The extracted Unicode character.</param>
    /// <param name="foreground">The extracted foreground color index.</param>
    /// <param name="background">The extracted background color index.</param>
    /// <param name="flags">The extracted system render flags.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Unpack(uint packed, out char character, out byte foreground, out byte background, out byte flags)
    {
        character = (char)(packed >> CharShift & CharMask);
        foreground = (byte)(packed >> ForegroundShift & ColorMask);
        background = (byte)(packed >> BackgroundShift & ColorMask);
        flags = (byte)(packed & FlagsMask);
    }
}
