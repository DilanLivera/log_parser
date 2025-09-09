using System.Text.RegularExpressions;

using DataProcessor.Pipelines.LogProcessing.Models;

using Spectre.Console;

namespace DataProcessor.Pipelines.LogProcessing;

/// <summary>
/// Pipeline step for parsing log entries using multiple regular expressions
/// </summary>
public sealed class LogParserStep : IPipelineStep<(IReadOnlyList<string> lines, IReadOnlyList<string> patterns), IReadOnlyList<LogEntry>>
{
    /// <summary>
    /// Parses log lines using the provided regular expression patterns
    /// </summary>
    /// <param name="input">Tuple containing lines to parse and the regex patterns</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of parsed log entries</returns>
    public async Task<Result<IReadOnlyList<LogEntry>>> ExecuteAsync(
        (IReadOnlyList<string> lines, IReadOnlyList<string> patterns) input,
        CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[blue]Parsing log entries...[/]");

        (IReadOnlyList<string> lines, IReadOnlyList<string> regexPatterns) = input;

        if (regexPatterns.Count == 0)
        {
            return Result<IReadOnlyList<LogEntry>>.Failure("At least one regex pattern must be provided");
        }

        List<LogEntry> logEntries = [];
        List<Regex> compiledRegexes = [];

        // Compile all regex patterns
        try
        {
            for (int i = 0; i < regexPatterns.Count; i++)
            {
                string pattern = regexPatterns[i];
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    return Result<IReadOnlyList<LogEntry>>.Failure($"Pattern {i + 1} cannot be null or empty");
                }

                AnsiConsole.MarkupLine($"[dim]Compiling pattern {i + 1}: {pattern.Replace("[", "[[").Replace("]", "]]")}[/]");
                compiledRegexes.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }
        }
        catch (ArgumentException ex)
        {
            return Result<IReadOnlyList<LogEntry>>.Failure($"Invalid regex pattern: {ex.Message}");
        }

        int lineNumber = 0;
        int matchCount = 0;

        AnsiConsole.MarkupLine($"[dim]Processing {lines.Count:N0} lines with {regexPatterns.Count} patterns[/]");

        // Execute synchronously without Task.Run
        foreach (string line in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Try each pattern until one matches
            bool matched = false;
            for (int patternIndex = 0; patternIndex < compiledRegexes.Count && !matched; patternIndex++)
            {
                Match match = compiledRegexes[patternIndex].Match(line);

                if (!match.Success)
                {
                    continue;
                }

                matched = true;
                matchCount++;
                LogEntry logEntry = new()
                                    {
                                        RawLine = line, LineNumber = lineNumber, PatternIndex = patternIndex, ExtractedData = new Dictionary<string, string>()
                                    };

                foreach (string groupName in compiledRegexes[patternIndex].GetGroupNames())
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
        }

        AnsiConsole.MarkupLine($"[dim]Parsed {matchCount:N0} entries from {lineNumber:N0} lines using {regexPatterns.Count} patterns[/]");

        return await Task.FromResult(Result<IReadOnlyList<LogEntry>>.Success(logEntries));
    }
}