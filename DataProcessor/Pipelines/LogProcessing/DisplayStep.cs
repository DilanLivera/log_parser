using DataProcessor.Pipelines.LogProcessing.Models;

using Spectre.Console;

namespace DataProcessor.Pipelines.LogProcessing;

/// <summary>
/// Pipeline step for displaying processed data
/// </summary>
public sealed class DisplayStep : IPipelineStep<ProcessingResult, ProcessingResult>
{
    private readonly int _maxDisplayRows;

    public DisplayStep(int maxDisplayRows = 50)
    {
        _maxDisplayRows = maxDisplayRows;
    }

    /// <summary>
    /// Displays the processing results in formatted tables
    /// </summary>
    /// <param name="result">Processing result to display</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The same processing result (pass-through) wrapped in a Result</returns>
    public async Task<Result<ProcessingResult>> ExecuteAsync(ProcessingResult result, CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[blue]Displaying results...[/]");

        // display summary
        Table summaryTable = new Table()
                             .Title("[bold blue]Processing Summary[/]")
                             .BorderColor(Color.Blue)
                             .RoundedBorder();

        summaryTable.AddColumn("[bold]Metric[/]");
        summaryTable.AddColumn("[bold]Value[/]");

        summaryTable.AddRow("Total Lines Processed", $"{result.TotalLinesProcessed:N0}");
        summaryTable.AddRow("Matched Lines", $"[green]{result.MatchedLines:N0}[/]");
        summaryTable.AddRow("Unmatched Lines", $"[red]{result.UnmatchedLines:N0}[/]");
        summaryTable.AddRow("Processing Efficiency", $"{result.Statistics.GetValueOrDefault("processing_efficiency", 0)}%");
        summaryTable.AddRow("Columns Extracted", $"{result.ColumnNames.Count:N0}");

        if (result.IsCorrelationEnabled)
        {
            summaryTable.AddRow("Correlation Field", $"[cyan]{result.CorrelationField}[/]");
            summaryTable.AddRow("Correlation Groups", $"[cyan]{result.CorrelationGroups.Count:N0}[/]");
            summaryTable.AddRow("Patterns Used", $"[cyan]{result.Patterns.Count:N0}[/]");
        }

        AnsiConsole.Write(summaryTable);
        AnsiConsole.WriteLine();

        if (result.ParsedEntries.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No data entries were parsed. Check your regex pattern.[/]");

            return await Task.FromResult(Result<ProcessingResult>.Success(result));
        }

        if (result.IsCorrelationEnabled && result.CorrelationGroups.Count > 0)
        {
            DisplayCorrelationGroups(result);
        }
        else
        {
            DisplayDataTable(result);
        }

        if (result.ColumnNames.Count > 0)
        {
            DisplayStatistics(result);
            DisplayTopValues(result);
        }

        return await Task.FromResult(Result<ProcessingResult>.Success(result));
    }

    /// <summary>
    /// Displays correlation groups in a structured format
    /// </summary>
    private void DisplayCorrelationGroups(ProcessingResult result)
    {
        int groupsToShow = Math.Min(_maxDisplayRows, result.CorrelationGroups.Count);

        Table correlationTable = new Table()
                                 .Title($"[bold green]Correlation Groups[/] [dim](showing first {groupsToShow} groups)[/]")
                                 .BorderColor(Color.Green)
                                 .RoundedBorder();

        correlationTable.AddColumn(new TableColumn("[bold]Correlation ID[/]").Centered());
        correlationTable.AddColumn(new TableColumn("[bold]Entries[/]").Centered());
        correlationTable.AddColumn(new TableColumn("[bold]Earliest Time[/]").Centered());
        correlationTable.AddColumn(new TableColumn("[bold]Latest Time[/]").Centered());
        correlationTable.AddColumn(new TableColumn("[bold]Entry Details[/]").LeftAligned());

        foreach (CorrelationGroup group in result.CorrelationGroups.Take(groupsToShow))
        {
            string entryDetails = string.Join("\n",
                                              group.Entries.Take(3).Select(e =>
                                                                               $"Line {e.LineNumber}: {e.RawLine.EscapeMarkup()}"[..Math.Min(80, e.RawLine.Length)]));

            if (group.Entries.Count > 3)
            {
                entryDetails += $"\n... and {group.Entries.Count - 3} more entries";
            }

            correlationTable.AddRow(
            $"[cyan]{group.CorrelationId}[/]",
            $"{group.EntryCount}",
            group.EarliestTimestamp ?? "[dim]N/A[/]",
            group.LatestTimestamp ?? "[dim]N/A[/]",
            entryDetails
            );
        }

        AnsiConsole.Write(correlationTable);
        AnsiConsole.WriteLine();

        if (result.CorrelationGroups.Count > groupsToShow)
        {
            AnsiConsole.MarkupLine($"[dim]... and {result.CorrelationGroups.Count - groupsToShow} more correlation groups[/]");
            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Displays the parsed data in a table format
    /// </summary>
    private void DisplayDataTable(ProcessingResult result)
    {
        // display data table
        List<string> columns = result.ColumnNames.OrderBy(c => c).ToList();

        if (columns.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No columns found in the parsed data.[/]");
        }
        else
        {
            Table dataTable = new Table()
                              .Title($"[bold green]Parsed Log Data[/] [dim](showing first {Math.Min(_maxDisplayRows, result.ParsedEntries.Count)} rows)[/]")
                              .BorderColor(Color.Green)
                              .RoundedBorder();

            dataTable.AddColumn(new TableColumn("[bold]Line #[/]").Centered());

            foreach (string column in columns)
            {
                dataTable.AddColumn(new TableColumn(header: $"[bold]{EscapeMarkup(column)}[/]").NoWrap());
            }

            IEnumerable<LogEntry> displayEntries = result.ParsedEntries.Take(_maxDisplayRows);

            foreach (LogEntry entry in displayEntries)
            {
                List<string> rowData = [entry.LineNumber.ToString()];
                IEnumerable<string> collection = columns.Select(column => entry.ExtractedData.GetValueOrDefault(column, ""))
                                                        .Select(EscapeMarkup);
                rowData.AddRange(collection);

                dataTable.AddRow(rowData.ToArray());
            }

            AnsiConsole.Write(dataTable);

            if (result.ParsedEntries.Count > _maxDisplayRows)
            {
                AnsiConsole.MarkupLine($"[dim]... and {result.ParsedEntries.Count - _maxDisplayRows:N0} more rows[/]");
            }

            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Displays column statistics in a table format
    /// </summary>
    private void DisplayStatistics(ProcessingResult result)
    {
        // display statistics
        Table statsTable = new Table()
                           .Title("[bold yellow]Column Statistics[/]")
                           .BorderColor(Color.Yellow)
                           .RoundedBorder();

        statsTable.AddColumn("[bold]Column[/]");
        statsTable.AddColumn("[bold]Count[/]");
        statsTable.AddColumn("[bold]Unique[/]");
        statsTable.AddColumn("[bold]Min[/]");
        statsTable.AddColumn("[bold]Max[/]");
        statsTable.AddColumn("[bold]Average[/]");

        foreach (string column in result.ColumnNames.OrderBy(c => c))
        {
            object count = result.Statistics.GetValueOrDefault($"{column}_count", "N/A");
            object unique = result.Statistics.GetValueOrDefault($"{column}_unique_count", "N/A");
            object min = result.Statistics.GetValueOrDefault($"{column}_min", "N/A");
            object max = result.Statistics.GetValueOrDefault($"{column}_max", "N/A");
            object avg = result.Statistics.GetValueOrDefault($"{column}_avg", "N/A");

            statsTable.AddRow(EscapeMarkup(column),
                              count.ToString() ?? "N/A",
                              unique.ToString() ?? "N/A",
                              min.ToString() ?? "N/A",
                              max.ToString() ?? "N/A",
                              avg.ToString() ?? "N/A");
        }

        AnsiConsole.Write(statsTable);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays top values for each column in a table format
    /// </summary>
    private void DisplayTopValues(ProcessingResult result)
    {
        // display top values
        foreach (string column in result.ColumnNames.OrderBy(c => c).Take(3)) // Show top values for first 3 columns
        {
            if (!result.Statistics.TryGetValue($"{column}_top_values", out object? topValuesObj) ||
                topValuesObj is not Dictionary<string, int> topValues)
            {
                continue;
            }

            Table topValuesTable = new Table()
                                   .Title($"[bold blue]Top Values for '{EscapeMarkup(column)}'[/]")
                                   .BorderColor(Color.Blue)
                                   .RoundedBorder();

            topValuesTable.AddColumn("[bold]Value[/]");
            topValuesTable.AddColumn("[bold]Count[/]");

            foreach (KeyValuePair<string, int> kvp in topValues)
            {
                topValuesTable.AddRow(EscapeMarkup(kvp.Key), kvp.Value.ToString());
            }

            AnsiConsole.Write(topValuesTable);
            AnsiConsole.WriteLine();
        }
    }

    private static string EscapeMarkup(string text)
    {
        return text.Replace("[", "[[").Replace("]", "]]");
    }
}