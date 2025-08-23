using System.Text.RegularExpressions;

using LogProcessor.Models;

using Spectre.Console;

namespace LogProcessor.Pipeline.Steps;

/// <summary>
/// Pipeline step for parsing log entries using regular expressions
/// </summary>
public sealed class LogParserStep : IPipelineStep<(IReadOnlyList<string> lines, string regex), IReadOnlyList<LogEntry>>
{
    /// <summary>
    /// Parses log lines using the provided regular expression
    /// </summary>
    /// <param name="input">Tuple containing lines to parse and the regex pattern</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of parsed log entries</returns>
    public async Task<Result<IReadOnlyList<LogEntry>>> ExecuteAsync(
        (IReadOnlyList<string> lines, string regex) input,
        CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[blue]Parsing log entries...[/]");

        (IReadOnlyList<string> lines, string regexPattern) = input;

        if (string.IsNullOrWhiteSpace(regexPattern))
        {
            return Result<IReadOnlyList<LogEntry>>.Failure("Regex pattern cannot be null or empty");
        }

        List<LogEntry> logEntries = [];
        Regex regex;

        try
        {
            regex = new Regex(regexPattern, options: RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
        catch (ArgumentException ex)
        {
            return Result<IReadOnlyList<LogEntry>>.Failure($"Invalid regex pattern: {ex.Message}");
        }

        int lineNumber = 0;
        int matchCount = 0;

        AnsiConsole.MarkupLine($"[dim]Applying regex pattern: {regexPattern.Replace("[", "[[").Replace("]", "]]") ?? ""}[/]");

        foreach (string line in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            Match match = regex.Match(line);

            if (!match.Success)
            {
                continue;
            }

            matchCount++;
            LogEntry logEntry = new()
                                {
                                    RawLine = line,
                                    LineNumber = lineNumber,
                                    ExtractedData = new Dictionary<string, string>()
                                };

            foreach (string groupName in regex.GetGroupNames())
            {
                if (groupName == "0" || int.TryParse(groupName, out _))
                {
                    continue;
                }

                Group group = match.Groups[groupName];
                if (group.Success)
                {
                    logEntry.ExtractedData[groupName] = group.Value;
                }
            }

            logEntries.Add(logEntry);
        }

        AnsiConsole.MarkupLine($"[dim]Parsed {matchCount:N0} entries from {lineNumber:N0} lines[/]");

        return await Task.FromResult(Result<IReadOnlyList<LogEntry>>.Success(logEntries));
    }

}