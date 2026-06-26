namespace PixelTerminalUI.Persistence.Redis.Configuration;

/// <summary>
/// Encapsulates performance and lifecycle configuration thresholds utilized by the Redis session data store backend.
/// </summary>
public sealed class RedisCacheOptions
{
    /// <summary>
    /// Gets or sets the absolute sliding expiration time window lifespan configuration for runtime user session hashes.
    /// </summary>
    /// <value>
    /// The default absolute duration interval threshold value is predefined at 24 hours.
    /// </value>
    public TimeSpan SessionTtl { get; set; } = TimeSpan.FromHours(24);
}
