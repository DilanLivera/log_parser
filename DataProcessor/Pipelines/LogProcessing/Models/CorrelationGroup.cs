namespace DataProcessor.Pipelines.LogProcessing.Models;

/// <summary>
/// Represents a group of log entries that share the same correlation ID
/// </summary>
public sealed class CorrelationGroup
{
    /// <summary>
    /// The correlation ID that groups these entries together
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Collection of log entries that belong to this correlation group
    /// </summary>
    public IReadOnlyList<LogEntry> Entries { get; init; } = [];

    /// <summary>
    /// The earliest timestamp found in any entry in this group (if timestamp data is available)
    /// </summary>
    public string? EarliestTimestamp { get; init; }

    /// <summary>
    /// The latest timestamp found in any entry in this group (if timestamp data is available)
    /// </summary>
    public string? LatestTimestamp { get; init; }

    /// <summary>
    /// Total number of entries in this correlation group
    /// </summary>
    public int EntryCount => Entries.Count;
}