using LogProcessor.Models;
using LogProcessor.Pipeline.Steps;

using Spectre.Console;

namespace LogProcessor.Pipeline;

/// <summary>
/// Main pipeline orchestrator that coordinates the sequential processing of log files
/// </summary>
public sealed class LogProcessingPipeline
{
    private readonly IPipelineStep<string, IReadOnlyList<string>> _fileReader;
    private readonly IPipelineStep<(IReadOnlyList<string> lines, string regex), IReadOnlyList<LogEntry>> _logParser;
    private readonly IPipelineStep<IReadOnlyList<LogEntry>, ProcessingResult> _dataProcessor;
    private readonly IPipelineStep<(ProcessingResult result, string? outputFile), ProcessingResult> _fileSaver;
    private readonly IPipelineStep<ProcessingResult, ProcessingResult> _display;

    public LogProcessingPipeline(
        IPipelineStep<string, IReadOnlyList<string>> fileReader,
        IPipelineStep<(IReadOnlyList<string> lines, string regex), IReadOnlyList<LogEntry>> logParser,
        IPipelineStep<IReadOnlyList<LogEntry>, ProcessingResult> dataProcessor,
        IPipelineStep<(ProcessingResult result, string? outputFile), ProcessingResult> fileSaver,
        IPipelineStep<ProcessingResult, ProcessingResult> display)
    {
        _fileReader = fileReader;
        _logParser = logParser;
        _dataProcessor = dataProcessor;
        _fileSaver = fileSaver;
        _display = display;
    }

    /// <summary>
    /// Executes the complete pipeline to process a log file
    /// </summary>
    /// <param name="filePath">Path to the log file</param>
    /// <param name="regex">Regular expression pattern for parsing</param>
    /// <param name="outputFile">Optional output file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Final processing result</returns>
    public async Task<Result<ProcessingResult>> ExecuteAsync(
        string filePath,
        string regex,
        string? outputFile = null,
        CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[green]Starting log file processing pipeline...[/]");

        Result<ProcessingResult> pipelineResult = await (
            from lines in _fileReader.ExecuteAsync(filePath, cancellationToken)
            from logEntries in _logParser.ExecuteAsync((lines, regex), cancellationToken)
            from processedResult in _dataProcessor.ExecuteAsync(logEntries, cancellationToken)
            from savedResult in _fileSaver.ExecuteAsync((processedResult, outputFile), cancellationToken)
            from displayedResult in _display.ExecuteAsync(processedResult, cancellationToken)
            select displayedResult);

        return pipelineResult.Match(
        onSuccess: result =>
        {
            AnsiConsole.MarkupLine("[green]Pipeline execution completed successfully![/]");

            return Result<ProcessingResult>.Success(result);
        },
        onFailure: ex =>
        {
            AnsiConsole.MarkupLine($"[red]Pipeline execution failed: {ex.Message.Replace("[", "[[").Replace("]", "]]")}[/]");

            return Result<ProcessingResult>.Failure(ex);
        });
    }
}