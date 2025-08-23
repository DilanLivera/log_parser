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
        LogFileFixture.HttpRegexPattern);

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
    }

    [Fact]
    public async Task ExecuteAsync_WithValidHttpLogs_ShouldHaveCorrectStatistics()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();

        // Act
        Result<ProcessingResult> result = await pipeline.ExecuteAsync(
        _fixture.SampleLogPath,
        LogFileFixture.HttpRegexPattern);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Detailed verification of the processing result
        ProcessingResult processingResult = result.Value;
        processingResult.TotalLinesProcessed.ShouldBe(10);
        processingResult.MatchedLines.ShouldBe(10);
        processingResult.UnmatchedLines.ShouldBe(0);
        processingResult.ColumnNames.OrderBy(x => x).ShouldBe([
            "Elapsed", "RequestMethod", "RequestPath", "StatusCode"
        ]);
        processingResult.ParsedEntries.Count.ShouldBe(10);

        // Verify first entry structure
        LogEntry firstEntry = processingResult.ParsedEntries.First();
        firstEntry.LineNumber.ShouldBe(1);
        firstEntry.ExtractedData.ShouldContainKey("RequestMethod");
        firstEntry.ExtractedData.ShouldContainKey("RequestPath");
        firstEntry.ExtractedData.ShouldContainKey("StatusCode");
        firstEntry.ExtractedData.ShouldContainKey("Elapsed");
    }

    [Fact]
    public async Task ExecuteAsync_WithValidAppLogs_ShouldReturnSuccessResult()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();

        // Act
        Result<ProcessingResult> result = await pipeline.ExecuteAsync(
        _fixture.AppLogPath,
        LogFileFixture.AppLogRegexPattern);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        ProcessingResult processingResult = result.Value;
        processingResult.TotalLinesProcessed.ShouldBe(5);
        processingResult.MatchedLines.ShouldBe(5);
        processingResult.ColumnNames.Count.ShouldBe(3);
        processingResult.ColumnNames.ShouldContain("Timestamp");
        processingResult.ColumnNames.ShouldContain("Level");
        processingResult.ColumnNames.ShouldContain("Message");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();

        // Act
        Result<ProcessingResult> result = await pipeline.ExecuteAsync(
        _fixture.NonExistentLogPath,
        LogFileFixture.HttpRegexPattern);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Log file not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidRegex_ShouldReturnFailure()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();

        // Act
        Result<ProcessingResult> result = await pipeline.ExecuteAsync(
        _fixture.SampleLogPath,
        LogFileFixture.InvalidRegexPattern);

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
        LogFileFixture.HttpRegexPattern);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        ProcessingResult processingResult = result.Value;
        processingResult.TotalLinesProcessed.ShouldBe(0);
        processingResult.MatchedLines.ShouldBe(0);
        processingResult.ParsedEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithJsonOutput_ShouldCreateFile()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();
        string outputPath = Path.GetTempFileName() + ".json";

        try
        {
            // Act
            Result<ProcessingResult> result = await pipeline.ExecuteAsync(
            _fixture.SampleLogPath,
            LogFileFixture.HttpRegexPattern,
            outputPath);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            File.Exists(outputPath).ShouldBeTrue();

            string jsonContent = await File.ReadAllTextAsync(outputPath);
            jsonContent.ShouldNotBeEmpty();

            // Verify it's valid JSON
            Should.NotThrow(() => JsonDocument.Parse(jsonContent));
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithCsvOutput_ShouldCreateFile()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();
        string outputPath = Path.GetTempFileName() + ".csv";

        try
        {
            // Act
            Result<ProcessingResult> result = await pipeline.ExecuteAsync(
            _fixture.SampleLogPath,
            LogFileFixture.HttpRegexPattern,
            outputPath);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            File.Exists(outputPath).ShouldBeTrue();

            string[] lines = await File.ReadAllLinesAsync(outputPath);
            lines.Length.ShouldBe(11); // Header + 10 data rows
            lines[0].ShouldContain("\"Elapsed\",\"RequestMethod\",\"RequestPath\",\"StatusCode\"");
            lines[1].ShouldContain("\"12.456\",\"GET\",\"/api/users\",\"200\"");
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithCsvOutput_ShouldHaveCorrectFormat()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();
        string outputPath = Path.GetTempFileName() + ".csv";

        try
        {
            // Act
            Result<ProcessingResult> result = await pipeline.ExecuteAsync(
            _fixture.SampleLogPath,
            LogFileFixture.HttpRegexPattern,
            outputPath);

            // Assert
            result.IsSuccess.ShouldBeTrue();

            string csvContent = await File.ReadAllTextAsync(outputPath);
            string[] lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Verify CSV structure
            lines.Length.ShouldBe(11); // Header + 10 data rows
            lines[0].ShouldContain("\"Elapsed\",\"RequestMethod\",\"RequestPath\",\"StatusCode\"");
            lines[1].ShouldContain("\"12.456\",\"GET\",\"/api/users\",\"200\"");
            lines.All(line => line.Contains("\"")).ShouldBeTrue(); // All values should be quoted
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithUnsupportedOutputFormat_ShouldDefaultToJson()
    {
        // Arrange
        LogProcessingPipeline pipeline = CreatePipeline();
        string outputPath = Path.GetTempFileName() + ".txt";
        string expectedJsonPath = Path.ChangeExtension(outputPath, ".json");

        try
        {
            // Act
            Result<ProcessingResult> result = await pipeline.ExecuteAsync(
            _fixture.SampleLogPath,
            LogFileFixture.HttpRegexPattern,
            outputPath);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            File.Exists(expectedJsonPath).ShouldBeFalse();
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            if (File.Exists(expectedJsonPath))
            {
                File.Delete(expectedJsonPath);
            }
        }
    }

    private static LogProcessingPipeline CreatePipeline(int maxDisplayRows = 50)
    {
        FileReaderStep reader = new();
        LogParserStep parser = new();
        DataProcessorStep processor = new();
        FileSaverStep fileSaver = new();
        DisplayStep display = new(maxDisplayRows);

        return new LogProcessingPipeline(reader, parser, processor, fileSaver, display);
    }
}