using System.Text.Json;

using LogProcessor.Models;
using LogProcessor.Pipeline;
using LogProcessor.Pipeline.Steps;
using LogProcessor.Tests.Fixtures;

using Shouldly;

namespace LogProcessor.Tests.Pipeline;

public class LogProcessingPipelineTests : IClassFixture<LogFileFixture>
{
    private readonly LogFileFixture _fixture;

    public LogProcessingPipelineTests(LogFileFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteAsync_WithValidHttpLogs_ShouldReturnSuccessResult()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();

        // Act
        Result<ProcessingResult> result = await pipeline.ExecuteAsync(
        _fixture.SampleLogPath,
        [LogFileFixture.HttpRegexPattern],
        "RequestMethod"); // Using RequestMethod as correlation field for testing

        // Assert
        result.IsSuccess.ShouldBeTrue();

        ProcessingResult processingResult = result.Value;
        processingResult.TotalLinesProcessed.ShouldBe(10);
        processingResult.MatchedLines.ShouldBe(10);
        processingResult.UnmatchedLines.ShouldBe(0);
        processingResult.ColumnNames.Count.ShouldBe(4);
        processingResult.ColumnNames.ShouldContain("RequestMethod");
        processingResult.ColumnNames.ShouldContain("RequestPath");
        processingResult.ColumnNames.ShouldContain("StatusCode");
        processingResult.ColumnNames.ShouldContain("Elapsed");
        processingResult.ParsedEntries.Count.ShouldBe(10);
        processingResult.IsCorrelationEnabled.ShouldBeTrue();
        processingResult.CorrelationField.ShouldBe("RequestMethod");
    }

    [Fact]
    public async Task ExecuteAsync_WithValidHttpLogs_ShouldHaveCorrectStatistics()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();

        // Act
        Result<ProcessingResult> result = await pipeline.ExecuteAsync(
        _fixture.SampleLogPath,
        [LogFileFixture.HttpRegexPattern],
        "RequestMethod");

        // Assert
        result.IsSuccess.ShouldBeTrue();

        ProcessingResult processingResult = result.Value;
        processingResult.Statistics.ContainsKey("processing_efficiency").ShouldBeTrue();
        processingResult.Statistics.ContainsKey("RequestMethod_count").ShouldBeTrue();
        processingResult.Statistics.ContainsKey("RequestMethod_unique_count").ShouldBeTrue();
        processingResult.Statistics.ContainsKey("RequestPath_count").ShouldBeTrue();
        processingResult.Statistics.ContainsKey("StatusCode_count").ShouldBeTrue();
        processingResult.Statistics.ContainsKey("Elapsed_count").ShouldBeTrue();

        processingResult.Statistics["RequestMethod_count"].ShouldBe(10);
        processingResult.Statistics["RequestMethod_unique_count"].ShouldBe(4); // GET, POST, PUT, DELETE
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();
        string nonExistentFile = "non-existent-file.log";

        // Act
        Result<ProcessingResult> result = await pipeline.ExecuteAsync(
        nonExistentFile,
        [LogFileFixture.HttpRegexPattern],
        "RequestMethod");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("non-existent-file.log");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyPatternArray_ShouldReturnFailure()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();

        // Act
        Result<ProcessingResult> result = await pipeline.ExecuteAsync(
        _fixture.SampleLogPath,
        Array.Empty<string>(),
        "RequestMethod");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("At least one regex pattern must be provided");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidRegexPattern_ShouldReturnFailure()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();
        string invalidPattern = "[invalid regex pattern";

        // Act
        Result<ProcessingResult> result = await pipeline.ExecuteAsync(
        _fixture.SampleLogPath,
        [invalidPattern],
        "RequestMethod");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Invalid regex pattern");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyFile_ShouldReturnSuccessWithNoEntries()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();

        // Act
        Result<ProcessingResult> result = await pipeline.ExecuteAsync(
        _fixture.EmptyLogPath,
        [LogFileFixture.HttpRegexPattern],
        "RequestMethod");

        // Assert
        result.IsSuccess.ShouldBeTrue();

        ProcessingResult processingResult = result.Value;
        processingResult.TotalLinesProcessed.ShouldBe(0);
        processingResult.MatchedLines.ShouldBe(0);
        processingResult.UnmatchedLines.ShouldBe(0);
        processingResult.ParsedEntries.Count.ShouldBe(0);
        processingResult.CorrelationGroups.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultiplePatterns_ShouldCombineResults()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();
        string[] patterns =
        [
            LogFileFixture.HttpRegexPattern,
            "(?<RequestMethod>\\w+) \\[(?<Level>\\w+)\\]" // Additional pattern
        ];

        // Act
        Result<ProcessingResult> result = await pipeline.ExecuteAsync(
        _fixture.SampleLogPath,
        patterns,
        "RequestMethod");

        // Assert
        result.IsSuccess.ShouldBeTrue();

        ProcessingResult processingResult = result.Value;
        processingResult.Patterns.Count.ShouldBe(2);
        processingResult.ParsedEntries.Count.ShouldBeGreaterThan(0);
        processingResult.IsCorrelationEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithOutputFile_ShouldSaveFile()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();
        string outputFile = Path.Combine(Path.GetTempPath(), $"test-output-{Guid.NewGuid()}.json");

        try
        {
            // Act
            Result<ProcessingResult> result = await pipeline.ExecuteAsync(
            _fixture.SampleLogPath,
            [LogFileFixture.HttpRegexPattern],
            "RequestMethod",
            outputFile);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            File.Exists(outputFile).ShouldBeTrue();

            string jsonContent = await File.ReadAllTextAsync(outputFile);
            jsonContent.ShouldNotBeEmpty();

            ProcessingResult? savedResult = JsonSerializer.Deserialize<ProcessingResult>(jsonContent,
                                                                                         new JsonSerializerOptions
                                                                                         {
                                                                                             PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                                                                         });

            savedResult.ShouldNotBeNull();
            savedResult.ParsedEntries.Count.ShouldBe(10);
            savedResult.IsCorrelationEnabled.ShouldBeTrue();
        }
        finally
        {
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithCsvOutputFile_ShouldSaveCsvFile()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();
        string outputFile = Path.Combine(Path.GetTempPath(), $"test-output-{Guid.NewGuid()}.csv");

        try
        {
            // Act
            Result<ProcessingResult> result = await pipeline.ExecuteAsync(
            _fixture.SampleLogPath,
            [LogFileFixture.HttpRegexPattern],
            "RequestMethod",
            outputFile);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            File.Exists(outputFile).ShouldBeTrue();

            string[] csvLines = await File.ReadAllLinesAsync(outputFile);
            csvLines.Length.ShouldBeGreaterThan(1); // Header + data rows

            // Since correlation is enabled, expect correlation groups CSV format
            csvLines[0].ShouldContain("CorrelationId");
            csvLines[0].ShouldContain("EntryCount");
            csvLines[0].ShouldContain("EarliestTimestamp");
            csvLines[0].ShouldContain("LatestTimestamp");
        }
        finally
        {
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
        }
    }

    private static LogProcessingPipeline CreatePipeline(int maxDisplayRows = 50)
    {
        FileReaderStep reader = new();
        LogParserStep parser = new();
        CorrelationStep correlationStep = new();
        DataProcessorStep processor = new();
        FileSaverStep fileSaver = new();
        DisplayStep display = new(maxDisplayRows);

        return new LogProcessingPipeline(reader, parser, correlationStep, processor, fileSaver, display);
    }
}