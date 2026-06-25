namespace TheLostGrid.Server.Enums;

/// <summary>
/// Specifies the operative specialization class archetype of the user session character.
/// </summary>
public enum CharacterType
{
    /// <summary>
    /// Represents an uninitialized or corrupt operative profile identity state.
    /// </summary>
    None = 0,

    /// <summary>
    /// A virtual landscape master who excels at bypassing firewall nodes, 
    /// decrypting secured databases, and bruteforcing mainframe matrix logic streams.
    /// </summary>
    Hacker = 1,

    /// <summary>
    /// A hardware specialist who projects sensory perception into mechanical networks,
    /// coordinating tactical drone reconnaissance and scanning physical infrastructure signatures.
    /// </summary>
    Rigger = 2
}
