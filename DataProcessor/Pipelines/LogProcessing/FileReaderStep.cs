using Spectre.Console;

namespace DataProcessor.Pipelines.LogProcessing;

/// <summary>
/// Pipeline step for reading log files
/// </summary>
public sealed class FileReaderStep : IPipelineStep<string, IReadOnlyList<string>>
{
    /// <summary>
    /// Reads the log file and returns all lines
    /// </summary>
    /// <param name="filePath">Path to the log file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of lines from the file</returns>
    public async Task<Result<IReadOnlyList<string>>> ExecuteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[blue]Reading log file...[/]");

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Result<IReadOnlyList<string>>.Failure("File path cannot be null or empty");
        }

        if (!File.Exists(filePath))
        {
            return Result<IReadOnlyList<string>>.Failure($"Log file not found: {filePath}");
        }

        try
        {
            AnsiConsole.MarkupLine($"[dim]Reading file: {filePath}[/]");

            string[] lines = await File.ReadAllLinesAsync(filePath, cancellationToken);

            AnsiConsole.MarkupLine($"[dim]Successfully read {lines.Length:N0} lines[/]");

            return Result<IReadOnlyList<string>>.Success(lines);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to read file '{filePath}': {ex.Message}[/]");

            return Result<IReadOnlyList<string>>.Failure(ex);
        }
    }
}