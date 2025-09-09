using System.Text.Json.Serialization;

namespace DataProcessor.Pipelines.LogProcessing.Models;

/// <summary>
/// Represents the result of log file processing
/// </summary>
public sealed class ProcessingResult
{
    /// <summary>
    /// Collection of successfully parsed log entries
    /// </summary>
    public IReadOnlyList<LogEntry> ParsedEntries { get; init; } = [];

    /// <summary>
    /// Collection of correlation groups (grouped log entries by correlation ID)
    /// </summary>
    public IReadOnlyList<CorrelationGroup> CorrelationGroups { get; init; } = [];

    /// <summary>
    /// Name of the correlation field used for grouping (if correlation is enabled)
    /// </summary>
    public string? CorrelationField { get; init; }

    /// <summary>
    /// Patterns used for parsing the log entries
    /// </summary>
    public IReadOnlyList<string> Patterns { get; init; } = [];

    /// <summary>
    /// Total number of lines processed
    /// </summary>
    public int TotalLinesProcessed { get; init; }

    /// <summary>
    /// Number of lines that matched the regex pattern
    /// </summary>
    public int MatchedLines { get; init; }

    /// <summary>
    /// Number of lines that didn't match the regex pattern
    /// </summary>
    public int UnmatchedLines => TotalLinesProcessed - MatchedLines;

    /// <summary>
    /// Set of all column names (capture group names) found across all entries
    /// </summary>
    [JsonConverter(typeof(ReadOnlySetJsonConverter))]
    public IReadOnlySet<string> ColumnNames { get; init; } = new HashSet<string>();

    /// <summary>
    /// Processing statistics and summary information
    /// </summary>
    public IReadOnlyDictionary<string, object> Statistics { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Whether correlation grouping was enabled for this processing result
    /// </summary>
    public bool IsCorrelationEnabled => !string.IsNullOrEmpty(CorrelationField);
}