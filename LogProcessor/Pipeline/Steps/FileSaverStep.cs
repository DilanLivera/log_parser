using System.Text.Json;

using LogProcessor.Models;

using Spectre.Console;

namespace LogProcessor.Pipeline.Steps;

/// <summary>
/// Pipeline step for saving processing results to files
/// </summary>
public sealed class FileSaverStep : IPipelineStep<(ProcessingResult result, string? outputFile), ProcessingResult>
{
    /// <summary>
    /// Saves the processing result to the specified file (if provided)
    /// </summary>
    /// <param name="input">Tuple containing the processing result and optional output file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The same processing result (pass-through) wrapped in a Result</returns>
    public async Task<Result<ProcessingResult>> ExecuteAsync((ProcessingResult result, string? outputFile) input, CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[blue]Saving results...[/]");

        (ProcessingResult result, string? outputFile) = input;

        if (string.IsNullOrEmpty(outputFile))
        {
            AnsiConsole.MarkupLine("[dim]No output file specified. Skipping file save.[/]");

            return await Task.FromResult(Result<ProcessingResult>.Success(result));
        }

        try
        {
            AnsiConsole.MarkupLine($"[blue]Saving results to {outputFile}...[/]");
            string extension = Path.GetExtension(outputFile).ToLowerInvariant();

            switch (extension)
            {
                case ".json":
                    string json = JsonSerializer.Serialize(result,
                                                           new JsonSerializerOptions
                                                           {
                                                               PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                               WriteIndented = true
                                                           });
                    await File.WriteAllTextAsync(outputFile, contents: json, cancellationToken);

                    break;
                case ".csv":
                    if (result.IsCorrelationEnabled && result.CorrelationGroups.Count > 0)
                    {
                        await SaveCorrelationGroupsCsv(outputFile, result, cancellationToken);
                    }
                    else
                    {
                        await SaveRegularCsv(outputFile, result, cancellationToken);
                    }

                    break;
                default:
                    AnsiConsole.MarkupLine($"[yellow]Unsupported output format '{extension}'. Saving as JSON instead.[/]");

                    string jsonOutputPath = Path.ChangeExtension(outputFile, ".json");
                    string defaultJson = JsonSerializer.Serialize(result,
                                                                  new JsonSerializerOptions
                                                                  {
                                                                      PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true
                                                                  });
                    await File.WriteAllTextAsync(jsonOutputPath, contents: defaultJson, cancellationToken);

                    break;
            }

            return await Task.FromResult(Result<ProcessingResult>.Success(result));
        }
        catch (Exception ex)
        {
            return await Task.FromResult(Result<ProcessingResult>.Failure(ex));
        }
    }

    /// <summary>
    /// Saves correlation groups to a CSV file
    /// </summary>
    private static async Task SaveCorrelationGroupsCsv(string outputFile, ProcessingResult result, CancellationToken cancellationToken)
    {
        List<string> lines =
        [
            "\"CorrelationId\",\"EntryCount\",\"EarliestTimestamp\",\"LatestTimestamp\",\"LineNumbers\",\"RawLines\""
        ];

        if (result.CorrelationGroups.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No correlation groups to export. Creating CSV with headers only.[/]");
        }
        else
        {
            foreach (CorrelationGroup group in result.CorrelationGroups)
            {
                string lineNumbers = string.Join(";", group.Entries.Select(e => e.LineNumber));
                string rawLines = string.Join(" | ", group.Entries.Select(e => e.RawLine.Replace("\"", "\"\"")));

                lines.Add($"\"{group.CorrelationId}\"," +
                          $"{group.EntryCount}," +
                          $"\"{group.EarliestTimestamp ?? ""}\"," +
                          $"\"{group.LatestTimestamp ?? ""}\"," +
                          $"\"{lineNumbers}\"," +
                          $"\"{rawLines}\"");
            }
        }

        await File.WriteAllLinesAsync(outputFile, lines, cancellationToken);
    }

    /// <summary>
    /// Saves regular log entries to a CSV file
    /// </summary>
    private static async Task SaveRegularCsv(string outputFile, ProcessingResult result, CancellationToken cancellationToken)
    {
        List<string> lines = [];

        if (result.ColumnNames.Count <= 0)
        {
            AnsiConsole.MarkupLine("[yellow]No columns detected.[/]");
        }
        else
        {
            List<string> columns = result.ColumnNames.OrderBy(c => c).ToList();

            // Add LineNumber and PatternIndex to columns if correlation is enabled
            if (result.IsCorrelationEnabled)
            {
                columns.Insert(0, "LineNumber");
                columns.Insert(1, "PatternIndex");
            }

            lines.Add(string.Join(",", columns.Select(c => $"\"{c}\"")));

            if (result.ParsedEntries.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No data to export. Creating CSV with headers only.[/]");
            }
            else
            {
                foreach (LogEntry entry in result.ParsedEntries)
                {
                    List<string> values = [];

                    if (result.IsCorrelationEnabled)
                    {
                        values.Add($"{entry.LineNumber}");
                        values.Add($"{entry.PatternIndex}");
                    }

                    // Add extracted data values
                    values.AddRange(result.ColumnNames.OrderBy(c => c)
                                          .Select(col => entry.ExtractedData.TryGetValue(col, out string? value) ? $"\"{value}\"" : "\"\""));

                    lines.Add(string.Join(",", values));
                }
            }

            await File.WriteAllLinesAsync(outputFile, lines, cancellationToken);
        }
    }
}