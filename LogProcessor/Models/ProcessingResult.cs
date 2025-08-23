namespace LogProcessor.Models;

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
    public IReadOnlySet<string> ColumnNames { get; init; } = new HashSet<string>();

    /// <summary>
    /// Processing statistics and summary information
    /// </summary>
    public IReadOnlyDictionary<string, object> Statistics { get; init; } = new Dictionary<string, object>();
}