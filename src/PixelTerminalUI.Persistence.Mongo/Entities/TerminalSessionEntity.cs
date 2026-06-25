using MongoDB.Bson.Serialization.Attributes;

namespace PixelTerminalUI.Persistence.Mongo.Entities;

/// <summary>
/// Represents the root infrastructure model storing active connection metadata, 
/// state tracking identifiers, and low-level double buffering primitives for a single user terminal loop.
/// </summary>
public sealed record TerminalSessionEntity
{
    /// <summary>
    /// Gets or sets the unique transaction tracking identifier assigned to the ongoing remote user session context.
    /// </summary>
    [BsonId]
    public Guid SessionId { get; set; }

    /// <summary>
    /// Gets or sets the serialized flat array of packed 32-bit unsigned pixel primitives representing the last successfully broadcast frame state.
    /// Used directly by the adaptive delta engine builder to calculate sub-threshold graphics modifications.
    /// </summary>
    [BsonElement("historicalBuffer")]
    public uint[]? HistoricalBuffer { get; set; }

    /// <summary>
    /// Gets or sets the unique functional database record identifier pointing straight to the active view configuration tree.
    /// </summary>
    [BsonElement("activeScreenId")]
    public Guid ActiveScreenId { get; set; }

    /// <summary>
    /// Gets or sets the structural synchronization version sequence state metric.
    /// Leveraged as an explicit Optimistic Concurrency Control (OCC) guard rail to block race conditions across parallel connections.
    /// </summary>
    [BsonElement("version")]
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the precise operational coordinated universal timestamp recording when the session record envelope was last updated.
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
