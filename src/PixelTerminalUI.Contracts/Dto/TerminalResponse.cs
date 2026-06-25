using System.Text.Json.Serialization;

namespace PixelTerminalUI.Contracts.Dto;

/// <summary>
/// Response from the backend to the client.
/// </summary>
[JsonDerivedType(typeof(FullFrameResponse), typeDiscriminator: "full")]
[JsonDerivedType(typeof(DeltaResponse), typeDiscriminator: "delta")]
public abstract record TerminalResponse(
    Guid SessionId,
    int Width,
    int Height);
