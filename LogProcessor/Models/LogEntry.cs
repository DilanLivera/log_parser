namespace LogProcessor.Models;

/// <summary>
/// Represents a parsed log entry with extracted data from regex capture groups
/// </summary>
public sealed class LogEntry
{
    /// <summary>
    /// The original raw log line
    /// </summary>
    public string RawLine { get; init; } = string.Empty;

    /// <summary>
    /// Dictionary containing extracted data where key is capture group name and value is the extracted data
    /// </summary>
    public Dictionary<string, string> ExtractedData { get; init; } = new();

    /// <summary>
    /// Line number in the original log file
    /// </summary>
    public int LineNumber { get; init; }
}