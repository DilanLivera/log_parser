using DataProcessor.Pipelines.LogProcessing;
using DataProcessor.Pipelines.LogProcessing.Models;
using DataProcessor.Tests.Fixtures;

using Shouldly;

namespace DataProcessor.Tests.Pipelines.LogProcessing;

public class LogParserStepTests : IClassFixture<LogFileFixture>
{
    private readonly LogParserStep _step;
    private readonly LogFileFixture _fixture;

    // Add missing pattern constant
    private const string NonMatchingRegexPattern = @"(?<NonExistent>\w+) This pattern will never match";

    public LogParserStepTests(LogFileFixture fixture)
    {
        _step = new LogParserStep();
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteAsync_WithValidHttpLogs_ShouldParseAllEntries()
    {
        // Arrange
        IReadOnlyList<string> lines = await File.ReadAllLinesAsync(_fixture.SampleLogPath);

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, [
            LogFileFixture.HttpRegexPattern
        ]));

        // Assert
        result.IsSuccess.ShouldBeTrue();

        IReadOnlyList<LogEntry> entries = result.Value;
        entries.Count.ShouldBe(10);

        LogEntry firstEntry = entries.First();
        firstEntry.LineNumber.ShouldBe(1);
        firstEntry.PatternIndex.ShouldBe(0);
        firstEntry.ExtractedData.ShouldContainKey("RequestMethod");
        firstEntry.ExtractedData.ShouldContainKey("RequestPath");
        firstEntry.ExtractedData.ShouldContainKey("StatusCode");
        firstEntry.ExtractedData.ShouldContainKey("Elapsed");
        firstEntry.ExtractedData["RequestMethod"].ShouldBe("GET");
        firstEntry.ExtractedData["RequestPath"].ShouldBe("/api/users");
        firstEntry.ExtractedData["StatusCode"].ShouldBe("200");
        firstEntry.ExtractedData["Elapsed"].ShouldBe("12.456");
    }

    [Fact]
    public async Task ExecuteAsync_WithValidAppLogs_ShouldParseAllEntries()
    {
        // Arrange
        IReadOnlyList<string> lines = await File.ReadAllLinesAsync(_fixture.AppLogPath);

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, [
            LogFileFixture.AppLogRegexPattern
        ]));

        // Assert
        result.IsSuccess.ShouldBeTrue();

        IReadOnlyList<LogEntry> entries = result.Value;
        entries.Count.ShouldBe(5);

        LogEntry firstEntry = entries.First();
        firstEntry.LineNumber.ShouldBe(1);
        firstEntry.PatternIndex.ShouldBe(0);
        firstEntry.ExtractedData.ShouldContainKey("Timestamp");
        firstEntry.ExtractedData.ShouldContainKey("Level");
        firstEntry.ExtractedData.ShouldContainKey("Message");
        firstEntry.ExtractedData["Timestamp"].ShouldBe("2024-01-15 08:30:15");
        firstEntry.ExtractedData["Level"].ShouldBe("INFO");
        firstEntry.ExtractedData["Message"].ShouldBe("User authentication successful for user@example.com");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyFile_ShouldReturnEmptyCollection()
    {
        // Arrange
        IReadOnlyList<string> lines = await File.ReadAllLinesAsync(_fixture.EmptyLogPath);

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, [
            LogFileFixture.HttpRegexPattern
        ]));

        // Assert
        result.IsSuccess.ShouldBeTrue();

        IReadOnlyList<LogEntry> entries = result.Value;
        entries.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonMatchingPattern_ShouldReturnEmptyCollection()
    {
        // Arrange
        IReadOnlyList<string> lines = await File.ReadAllLinesAsync(_fixture.SampleLogPath);

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, [
            NonMatchingRegexPattern
        ]));

        // Assert
        result.IsSuccess.ShouldBeTrue();

        IReadOnlyList<LogEntry> entries = result.Value;
        entries.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidRegex_ShouldReturnFailure()
    {
        // Arrange
        string[] invalidLines = ["test line"];

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((invalidLines, [
            LogFileFixture.InvalidRegexPattern
        ]));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Invalid regex pattern");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyPatternArray_ShouldReturnFailure()
    {
        // Arrange
        IReadOnlyList<string> lines = await File.ReadAllLinesAsync(_fixture.SampleLogPath);

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, []));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("At least one regex pattern must be provided");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullPattern_ShouldReturnFailure()
    {
        // Arrange
        IReadOnlyList<string> lines = await File.ReadAllLinesAsync(_fixture.SampleLogPath);

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, [
            null!
        ]));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Pattern 1 cannot be null or empty");
    }

    [Fact]
    public async Task ExecuteAsync_WithMultiplePatterns_ShouldMatchCorrectPattern()
    {
        // Arrange
        IReadOnlyList<string> lines = await File.ReadAllLinesAsync(_fixture.SampleLogPath);
        string[] patterns =
        [
            NonMatchingRegexPattern, // Pattern 0 - won't match HTTP logs
            LogFileFixture.HttpRegexPattern // Pattern 1 - will match HTTP logs
        ];

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, patterns));

        // Assert
        result.IsSuccess.ShouldBeTrue();

        IReadOnlyList<LogEntry> entries = result.Value;
        entries.Count.ShouldBe(10);

        // All entries should have PatternIndex = 1 (second pattern)
        entries.ShouldAllBe(entry => entry.PatternIndex == 1);

        LogEntry firstEntry = entries.First();
        firstEntry.ExtractedData.ShouldContainKey("RequestMethod");
        firstEntry.ExtractedData.ShouldContainKey("RequestPath");
        firstEntry.ExtractedData.ShouldContainKey("StatusCode");
        firstEntry.ExtractedData.ShouldContainKey("Elapsed");
    }

    [Fact]
    public async Task ExecuteAsync_WithMixedContentAndMultiplePatterns_ShouldAssignCorrectPatternIndexes()
    {
        // Arrange - Create mixed content with both HTTP and App log formats
        string[] mixedLines =
        [
            "2024-01-15 10:30:45 [INFO] HTTP GET /api/users responded 200 in 12.456 ms", // HTTP format - pattern 0
            "2024-01-15 10:30:45 [INFO] Application started", // App format - pattern 1
            "2024-01-15 10:30:47 [INFO] HTTP POST /api/orders responded 201 in 45.678 ms", // HTTP format - pattern 0
            "2024-01-15 10:30:50 [ERROR] Database connection failed" // App format - pattern 1
        ];

        string[] patterns =
        [
            LogFileFixture.HttpRegexPattern,
            LogFileFixture.AppLogRegexPattern
        ];

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((mixedLines, patterns));

        // Assert
        result.IsSuccess.ShouldBeTrue();

        IReadOnlyList<LogEntry> entries = result.Value;
        entries.Count.ShouldBe(4);

        // Verify pattern indexes are correct
        entries[0].PatternIndex.ShouldBe(0); // HTTP format
        entries[1].PatternIndex.ShouldBe(1); // App format
        entries[2].PatternIndex.ShouldBe(0); // HTTP format
        entries[3].PatternIndex.ShouldBe(1); // App format

        // Verify extracted data
        entries[0].ExtractedData.ShouldContainKey("RequestMethod");
        entries[1].ExtractedData.ShouldContainKey("Timestamp");
        entries[2].ExtractedData.ShouldContainKey("RequestMethod");
        entries[3].ExtractedData.ShouldContainKey("Level");
    }
}