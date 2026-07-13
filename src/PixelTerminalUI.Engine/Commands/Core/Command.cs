using System.Runtime.CompilerServices;

namespace PixelTerminalUI.Engine.Commands.Core;

/// <summary>
/// Represents a strongly-typed, multi-step command governed by an enumeration-based finite state machine.
/// </summary>
/// <typeparam name="TEnum">The underlying structure constraint enforcing an explicit execution state roadmap.</typeparam>
public abstract class Command<TEnum> : CommandBase where TEnum : struct, Enum
{
    /// <summary>
    /// Gets or sets the high-level, human-readable step of the execution sequence.
    /// </summary>
    public abstract TEnum State { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// Utilizes zero-allocation bitwise casting via <see cref="Unsafe.As{TFrom, TTo}"/> to map the 
    /// enum state to a primitive integer, bypassing boxing overhead during state persistence.
    /// </remarks>
    public override int RawState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            TEnum localState = State;
            return Unsafe.As<TEnum, int>(ref localState);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            int localValue = value;
            State = Unsafe.As<int, TEnum>(ref localValue);
        }
    }
}
