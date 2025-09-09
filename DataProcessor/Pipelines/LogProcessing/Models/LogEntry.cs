namespace DataProcessor.Pipelines.LogProcessing.Models;

/// <summary>
/// Represents a single parsed log entry with extracted data
/// </summary>
public sealed class LogEntry
{
    /// <summary>
    /// The original raw line from the log file
    /// </summary>
    public string RawLine { get; init; } = string.Empty;

    /// <summary>
    /// Line number in the original log file (1-based)
    /// </summary>
    public int LineNumber { get; init; }

    /// <summary>
    /// Index of the regex pattern that matched this entry (0-based)
    /// </summary>
    public int PatternIndex { get; init; }

    /// <summary>
    /// Dictionary of extracted data from named capture groups
    /// </summary>
    public Dictionary<string, string> ExtractedData { get; init; } = new();
}