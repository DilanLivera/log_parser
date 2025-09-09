using DataProcessor.Pipelines.LogProcessing.Models;

using Spectre.Console;

namespace DataProcessor.Pipelines.LogProcessing;

/// <summary>
/// Pipeline step for processing and aggregating parsed log data
/// </summary>
public sealed class DataProcessorStep : IPipelineStep<IReadOnlyList<LogEntry>, ProcessingResult>
{
    private const int TopValuesCount = 5;

    /// <summary>
    /// Processes and aggregates the parsed log entries
    /// </summary>
    /// <param name="logEntries">Collection of parsed log entries</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result with aggregated data and statistics wrapped in a Result</returns>
    public async Task<Result<ProcessingResult>> ExecuteAsync(IReadOnlyList<LogEntry> logEntries, CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[blue]Processing data...[/]");

        AnsiConsole.MarkupLine($"[dim]Processing {logEntries.Count:N0} log entries...[/]");

        HashSet<string> columnNames = logEntries.SelectMany(entry => entry.ExtractedData.Keys)
                                                .Distinct()
                                                .ToHashSet();

        Dictionary<string, object> statistics = new();

        foreach (string columnName in columnNames)
        {
            List<string> values = logEntries.Where(e => e.ExtractedData.ContainsKey(columnName))
                                            .Select(e => e.ExtractedData[columnName])
                                            .Where(v => !string.IsNullOrEmpty(v))
                                            .ToList();

            statistics[$"{columnName}_count"] = values.Count;
            statistics[$"{columnName}_unique_count"] = values.Distinct().Count();

            List<double> numericValues = values.Select(v => double.TryParse(v, out double num) ? (double?)num : null)
                                               .Where(v => v.HasValue)
                                               .Select(v => v!.Value)
                                               .ToList();

            if (numericValues.Count != 0)
            {
                statistics[$"{columnName}_min"] = numericValues.Min();
                statistics[$"{columnName}_max"] = numericValues.Max();
                statistics[$"{columnName}_avg"] = Math.Round(numericValues.Average(), 2);
            }

            statistics[$"{columnName}_top_values"] = values.GroupBy(v => v)
                                                           .OrderByDescending(g => g.Count())
                                                           .Take(TopValuesCount)
                                                           .ToDictionary(g => g.Key, g => g.Count());
        }

        statistics["total_columns"] = columnNames.Count;
        statistics["avg_columns_per_entry"] = logEntries.Count != 0
            ? Math.Round(logEntries.Average(e => e.ExtractedData.Count), 2)
            : 0;

        int totalLinesProcessed = logEntries.LastOrDefault()?.LineNumber ?? 0;
        statistics["processing_efficiency"] = totalLinesProcessed > 0
            ? Math.Round((double)logEntries.Count / totalLinesProcessed * 100, 2)
            : 0;

        ProcessingResult result = new()
                                  {
                                      ParsedEntries = logEntries.ToList(),
                                      MatchedLines = logEntries.Count,
                                      TotalLinesProcessed = totalLinesProcessed,
                                      ColumnNames = columnNames,
                                      Statistics = statistics
                                  };

        AnsiConsole.MarkupLine($"[dim]Generated statistics for {result.ColumnNames.Count} columns[/]");

        return await Task.FromResult(Result<ProcessingResult>.Success(result));
    }
}