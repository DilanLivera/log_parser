using DataProcessor.Pipelines.LogProcessing.Models;

using Spectre.Console;

namespace DataProcessor.Pipelines.LogProcessing;

/// <summary>
/// Pipeline step for correlating log entries by correlation ID
/// </summary>
public sealed class CorrelationStep : IPipelineStep<(IReadOnlyList<LogEntry> entries, string correlationField), IReadOnlyList<CorrelationGroup>>
{
    /// <summary>
    /// Groups log entries by their correlation ID
    /// </summary>
    /// <param name="input">Tuple containing log entries and correlation field name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of correlation groups</returns>
    public async Task<Result<IReadOnlyList<CorrelationGroup>>> ExecuteAsync(
        (IReadOnlyList<LogEntry> entries, string correlationField) input,
        CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[blue]Correlating log entries...[/]");

        (IReadOnlyList<LogEntry> entries, string correlationField) = input;

        if (string.IsNullOrWhiteSpace(correlationField))
        {
            return Result<IReadOnlyList<CorrelationGroup>>.Failure("Correlation field cannot be null or empty");
        }

        AnsiConsole.MarkupLine($"[dim]Grouping {entries.Count:N0} entries by correlation field '{correlationField}'[/]");

        // Group entries by correlation ID
        List<CorrelationGroup> correlationGroups = entries.Where(entry => entry.ExtractedData.ContainsKey(correlationField) &&
                                                                          !string.IsNullOrEmpty(entry.ExtractedData[correlationField]))
                                                          .GroupBy(entry => entry.ExtractedData[correlationField])
                                                          .Select(group => CreateCorrelationGroup(group.Key, group.ToList()))
                                                          .OrderBy(group => group.EarliestTimestamp ?? group.CorrelationId)
                                                          .ToList();

        int entriesWithCorrelation = correlationGroups.Sum(g => g.EntryCount);
        int entriesWithoutCorrelation = entries.Count - entriesWithCorrelation;

        AnsiConsole.MarkupLine($"[dim]Created {correlationGroups.Count:N0} correlation groups[/]");
        AnsiConsole.MarkupLine($"[dim]Entries with correlation: {entriesWithCorrelation:N0}[/]");

        if (entriesWithoutCorrelation > 0)
        {
            AnsiConsole.MarkupLine($"[yellow]Entries without correlation (ignored): {entriesWithoutCorrelation:N0}[/]");
        }

        return await Task.FromResult(Result<IReadOnlyList<CorrelationGroup>>.Success(correlationGroups));
    }

    /// <summary>
    /// Creates a correlation group from a collection of related log entries
    /// </summary>
    /// <param name="correlationId">The correlation ID for this group</param>
    /// <param name="entries">The log entries belonging to this group</param>
    /// <returns>A correlation group with computed metadata</returns>
    private static CorrelationGroup CreateCorrelationGroup(string correlationId, List<LogEntry> entries)
    {
        // Try to find timestamp data in common timestamp field names
        string[] timestampFields = ["Timestamp", "Time", "DateTime", "CreatedAt", "LogTime"];

        string? earliestTimestamp = null;
        string? latestTimestamp = null;

        foreach (string timestampField in timestampFields)
        {
            List<string> timestamps = entries.Where(e => e.ExtractedData.ContainsKey(timestampField))
                                             .Select(e => e.ExtractedData[timestampField])
                                             .Where(t => !string.IsNullOrEmpty(t))
                                             .OrderBy(t => t)
                                             .ToList();

            if (timestamps.Count > 0)
            {
                earliestTimestamp = timestamps.First();
                latestTimestamp = timestamps.Last();

                break;
            }
        }

        return new CorrelationGroup
               {
                   CorrelationId = correlationId, Entries = entries.OrderBy(e => e.LineNumber).ToList(), EarliestTimestamp = earliestTimestamp, LatestTimestamp = latestTimestamp
               };
    }
}