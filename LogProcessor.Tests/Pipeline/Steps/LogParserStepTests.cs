using LogProcessor.Models;
using LogProcessor.Pipeline.Steps;
using LogProcessor.Tests.Fixtures;

using Shouldly;

namespace LogProcessor.Tests.Pipeline.Steps;

public sealed class LogParserStepTests : IClassFixture<LogFileFixture>
{
    private readonly LogParserStep _step;
    private readonly LogFileFixture _fixture;

    public LogParserStepTests(LogFileFixture fixture)
    {
        _step = new LogParserStep();
        _fixture = fixture;
    }

    [Fact]
    public async Task ProcessAsync_WithValidHttpLogs_ShouldReturnParsedEntries()
    {
        // Arrange
        IReadOnlyList<string> lines = LogFileFixture.SampleLogContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, LogFileFixture.HttpRegexPattern));

        // Assert
        result.IsSuccess.ShouldBeTrue();

        IReadOnlyList<LogEntry> entries = result.Value;
        entries.Count.ShouldBe(10);

        LogEntry firstEntry = entries.First();
        firstEntry.LineNumber.ShouldBe(1);
        firstEntry.RawLine.ShouldContain("HTTP GET /api/users");
        firstEntry.ExtractedData.ShouldContainKey("RequestMethod");
        firstEntry.ExtractedData["RequestMethod"].ShouldBe("GET");
        firstEntry.ExtractedData.ShouldContainKey("RequestPath");
        firstEntry.ExtractedData["RequestPath"].ShouldBe("/api/users");
        firstEntry.ExtractedData.ShouldContainKey("StatusCode");
        firstEntry.ExtractedData["StatusCode"].ShouldBe("200");
        firstEntry.ExtractedData.ShouldContainKey("Elapsed");
        firstEntry.ExtractedData["Elapsed"].ShouldBe("12.456");
    }

    [Fact]
    public async Task ProcessAsync_WithValidAppLogs_ShouldReturnParsedEntries()
    {
        // Arrange
        IReadOnlyList<string> lines = LogFileFixture.AppLogContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, LogFileFixture.AppLogRegexPattern));

        // Assert
        result.IsSuccess.ShouldBeTrue();

        IReadOnlyList<LogEntry> entries = result.Value;
        entries.Count.ShouldBe(5);

        LogEntry firstEntry = entries.First();
        firstEntry.ExtractedData.ShouldContainKey("Timestamp");
        firstEntry.ExtractedData["Timestamp"].ShouldBe("2024-01-15 08:30:15");
        firstEntry.ExtractedData.ShouldContainKey("Level");
        firstEntry.ExtractedData["Level"].ShouldBe("INFO");
        firstEntry.ExtractedData.ShouldContainKey("Message");
        firstEntry.ExtractedData["Message"].ShouldContain("User authentication successful for user@example.com");
    }

    [Fact]
    public async Task ProcessAsync_WithValidLogs_ShouldHaveCorrectStructure()
    {
        // Arrange
        IReadOnlyList<string> lines = LogFileFixture.SampleLogContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, LogFileFixture.HttpRegexPattern));

        // Assert
        result.IsSuccess.ShouldBeTrue();

        IReadOnlyList<LogEntry> entries = result.Value;
        entries.Count.ShouldBe(10);

        foreach (LogEntry entry in entries)
        {
            entry.LineNumber.ShouldBeGreaterThan(0);
            entry.RawLine.ShouldNotBeNullOrEmpty();
            entry.ExtractedData.ShouldNotBeEmpty();
        }
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidRegex_ShouldReturnFailure()
    {
        // Arrange
        IReadOnlyList<string> lines = LogFileFixture.SampleLogContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, LogFileFixture.InvalidRegexPattern));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Invalid regex pattern");
    }

    [Fact]
    public async Task ProcessAsync_WithNullRegex_ShouldReturnFailure()
    {
        // Arrange
        string[] lines = ["sample line"];

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, null!));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Regex pattern cannot be null or empty");
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyRegex_ShouldReturnFailure()
    {
        // Arrange
        string[] lines = ["sample line"];

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, ""));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Regex pattern cannot be null or empty");
    }

    [Fact]
    public async Task ProcessAsync_WithNoMatches_ShouldReturnEmptySuccess()
    {
        // Arrange
        IReadOnlyList<string> lines = ["line with no HTTP data", "another non-matching line"];

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, LogFileFixture.HttpRegexPattern));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_WithMixedMatchingLines_ShouldOnlyParseMatches()
    {
        // Arrange
        IReadOnlyList<string> lines =
        [
            "2024-01-15 10:30:45 [INFO] HTTP GET /api/users responded 200 in 12.456 ms",
            "This line doesn't match the pattern",
            "2024-01-15 10:30:47 [INFO] HTTP POST /api/auth/login responded 200 in 45.789 ms",
            "Another non-matching line"
        ];

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, LogFileFixture.HttpRegexPattern));

        // Assert
        result.IsSuccess.ShouldBeTrue();

        IReadOnlyList<LogEntry> entries = result.Value;
        entries.Count.ShouldBe(2);
        entries[0].LineNumber.ShouldBe(1);
        entries[1].LineNumber.ShouldBe(3);
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyLines_ShouldSkipEmptyLines()
    {
        // Arrange
        IReadOnlyList<string> lines =
        [
            "2024-01-15 10:30:45 [INFO] HTTP GET /api/users responded 200 in 12.456 ms",
            "",
            "   ",
            "2024-01-15 10:30:47 [INFO] HTTP POST /api/auth/login responded 200 in 45.789 ms"
        ];

        // Act
        Result<IReadOnlyList<LogEntry>> result = await _step.ExecuteAsync((lines, LogFileFixture.HttpRegexPattern));

        // Assert
        result.IsSuccess.ShouldBeTrue();

        IReadOnlyList<LogEntry> entries = result.Value;
        entries.Count.ShouldBe(2);
        entries[0].LineNumber.ShouldBe(1);
        entries[1].LineNumber.ShouldBe(4);
    }
}