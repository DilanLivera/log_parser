using DataProcessor.Pipelines.LogProcessing.Models;

using Spectre.Console;

namespace DataProcessor.Pipelines.LogProcessing;

/// <summary>
/// Main pipeline orchestrator that coordinates the sequential processing of log files with correlation support
/// </summary>
public sealed class LogProcessingPipeline
{
    private readonly IPipelineStep<string, IReadOnlyList<string>> _fileReader;
    private readonly IPipelineStep<(IReadOnlyList<string> lines, IReadOnlyList<string> patterns), IReadOnlyList<LogEntry>> _logParser;
    private readonly IPipelineStep<(IReadOnlyList<LogEntry> entries, string correlationField), IReadOnlyList<CorrelationGroup>> _correlationStep;
    private readonly IPipelineStep<IReadOnlyList<LogEntry>, ProcessingResult> _dataProcessor;
    private readonly IPipelineStep<(ProcessingResult result, string? outputFile), ProcessingResult> _fileSaver;
    private readonly IPipelineStep<ProcessingResult, ProcessingResult> _display;

    public LogProcessingPipeline(
        IPipelineStep<string, IReadOnlyList<string>> fileReader,
        IPipelineStep<(IReadOnlyList<string> lines, IReadOnlyList<string> patterns), IReadOnlyList<LogEntry>> logParser,
        IPipelineStep<(IReadOnlyList<LogEntry> entries, string correlationField), IReadOnlyList<CorrelationGroup>> correlationStep,
        IPipelineStep<IReadOnlyList<LogEntry>, ProcessingResult> dataProcessor,
        IPipelineStep<(ProcessingResult result, string? outputFile), ProcessingResult> fileSaver,
        IPipelineStep<ProcessingResult, ProcessingResult> display)
    {
        _fileReader = fileReader;
        _logParser = logParser;
        _correlationStep = correlationStep;
        _dataProcessor = dataProcessor;
        _fileSaver = fileSaver;
        _display = display;
    }

    /// <summary>
    /// Executes the complete pipeline to process a log file with correlation support
    /// </summary>
    /// <param name="filePath">Path to the log file</param>
    /// <param name="patterns">Regular expression patterns for parsing</param>
    /// <param name="correlationField">Field name for correlating entries</param>
    /// <param name="outputFile">Optional output file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Final processing result</returns>
    public async Task<Result<ProcessingResult>> ExecuteAsync(
        string filePath,
        IReadOnlyList<string> patterns,
        string correlationField,
        string? outputFile = null,
        CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[green]Starting log file processing pipeline...[/]");

        Result<ProcessingResult> pipelineResult = await (
            from lines in ReadFileWithLogging(filePath, cancellationToken)
            from logEntries in ParseEntriesWithLogging(lines, patterns, cancellationToken)
            from correlationGroups in CorrelateEntriesWithLogging(logEntries, correlationField, cancellationToken)
            from processedResult in ProcessDataWithLogging(logEntries, patterns, correlationField, correlationGroups, cancellationToken)
            from savedResult in SaveResultWithLogging(processedResult, outputFile, cancellationToken)
            from displayedResult in DisplayResultWithLogging(savedResult, cancellationToken)
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

    private async Task<Result<IReadOnlyList<string>>> ReadFileWithLogging(string filePath, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[blue]Reading log file...[/]");

        return await _fileReader.ExecuteAsync(filePath, cancellationToken);
    }

    private async Task<Result<IReadOnlyList<LogEntry>>> ParseEntriesWithLogging(IReadOnlyList<string> lines, IReadOnlyList<string> patterns, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[blue]Parsing log entries...[/]");

        return await _logParser.ExecuteAsync((lines, patterns), cancellationToken);
    }

    private async Task<Result<IReadOnlyList<CorrelationGroup>>> CorrelateEntriesWithLogging(IReadOnlyList<LogEntry> logEntries, string correlationField, CancellationToken cancellationToken)
    {
        return await _correlationStep.ExecuteAsync((logEntries, correlationField), cancellationToken);
    }

    private async Task<Result<ProcessingResult>> ProcessDataWithLogging(
        IReadOnlyList<LogEntry> logEntries,
        IReadOnlyList<string> patterns,
        string correlationField,
        IReadOnlyList<CorrelationGroup> correlationGroups,
        CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[blue]Processing data...[/]");

        // Create enhanced processing result with correlation data
        Result<ProcessingResult> baseResult = await _dataProcessor.ExecuteAsync(logEntries, cancellationToken);

        return baseResult.Match<Result<ProcessingResult>>(
        onSuccess: result => Result<ProcessingResult>.Success(new ProcessingResult
                                                              {
                                                                  ParsedEntries = result.ParsedEntries,
                                                                  CorrelationGroups = correlationGroups,
                                                                  CorrelationField = correlationField,
                                                                  Patterns = patterns,
                                                                  TotalLinesProcessed = result.TotalLinesProcessed,
                                                                  MatchedLines = result.MatchedLines,
                                                                  ColumnNames = result.ColumnNames,
                                                                  Statistics = result.Statistics
                                                              }),
        onFailure: error => Result<ProcessingResult>.Failure(error)
        );
    }

    private async Task<Result<ProcessingResult>> SaveResultWithLogging(ProcessingResult result, string? outputFile, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(outputFile))
        {
            AnsiConsole.MarkupLine("[blue]Saving results...[/]");
        }

        return await _fileSaver.ExecuteAsync((result, outputFile), cancellationToken);
    }

    private async Task<Result<ProcessingResult>> DisplayResultWithLogging(ProcessingResult result, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[blue]Displaying results...[/]");

        return await _display.ExecuteAsync(result, cancellationToken);
    }

}