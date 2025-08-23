using LogProcessor.Models;
using LogProcessor.Pipeline.Steps;
using LogProcessor.Tests.Fixtures;

using Shouldly;

namespace LogProcessor.Tests.Pipeline.Steps;

public class DataProcessorStepTests : IClassFixture<LogFileFixture>
{
    private readonly DataProcessorStep _step;
    private readonly LogFileFixture _fixture;

    public DataProcessorStepTests(LogFileFixture fixture)
    {
        _fixture = fixture;
        _step = new DataProcessorStep();
    }

    [Fact]
    public async Task ProcessAsync_WithValidEntries_ShouldReturnProcessedResult()
    {
        // Arrange
        List<LogEntry> logEntries = CreateSampleLogEntries();

        // Act
        Result<ProcessingResult> result = await _step.ExecuteAsync(logEntries);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        ProcessingResult processingResult = result.Value;
        processingResult.ParsedEntries.Count.ShouldBe(3);
        processingResult.TotalLinesProcessed.ShouldBe(3);
        processingResult.MatchedLines.ShouldBe(3);
        processingResult.UnmatchedLines.ShouldBe(0);
        processingResult.ColumnNames.Count.ShouldBe(3);
        processingResult.ColumnNames.ShouldContain("Method");
        processingResult.ColumnNames.ShouldContain("Path");
        processingResult.ColumnNames.ShouldContain("StatusCode");
    }

    [Fact]
    public async Task ProcessAsync_WithValidEntries_ShouldGenerateCorrectStatistics()
    {
        // Arrange
        List<LogEntry> logEntries = CreateSampleLogEntries();

        // Act
        Result<ProcessingResult> result = await _step.ExecuteAsync(logEntries);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        ProcessingResult processingResult = result.Value;

        // Check count statistics
        processingResult.Statistics["Method_count"].ShouldBe(3);
        processingResult.Statistics["Path_count"].ShouldBe(3);
        processingResult.Statistics["StatusCode_count"].ShouldBe(3);

        // Check unique count statistics
        processingResult.Statistics["Method_unique_count"].ShouldBe(2); // GET, POST
        processingResult.Statistics["Path_unique_count"].ShouldBe(2); // /api/users, /api/data
        processingResult.Statistics["StatusCode_unique_count"].ShouldBe(2); // 200, 404

        // Check numeric statistics for StatusCode
        ((double)processingResult.Statistics["StatusCode_min"]).ShouldBe(200.0);
        ((double)processingResult.Statistics["StatusCode_max"]).ShouldBe(404.0);
        // Expected: (200 + 404 + 200) / 3 = 268.0
        ((double)processingResult.Statistics["StatusCode_avg"]).ShouldBe(268.0, 0.1);

        // Check processing efficiency
        ((double)processingResult.Statistics["processing_efficiency"]).ShouldBe(100.0);
    }

    [Fact]
    public async Task ProcessAsync_WithValidEntries_ShouldGenerateTopValues()
    {
        // Arrange
        List<LogEntry> logEntries = CreateSampleLogEntries();

        // Act
        Result<ProcessingResult> result = await _step.ExecuteAsync(logEntries);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        ProcessingResult processingResult = result.Value;

        // Check top values for Method
        processingResult.Statistics.ContainsKey("Method_top_values").ShouldBeTrue();
        Dictionary<string, int> methodTopValues = (Dictionary<string, int>)processingResult.Statistics["Method_top_values"];
        methodTopValues["GET"].ShouldBe(2);
        methodTopValues["POST"].ShouldBe(1);

        // Check top values for Path
        processingResult.Statistics.ContainsKey("Path_top_values").ShouldBeTrue();
        Dictionary<string, int> pathTopValues = (Dictionary<string, int>)processingResult.Statistics["Path_top_values"];
        pathTopValues["/api/users"].ShouldBe(2);
        pathTopValues["/api/data"].ShouldBe(1);
    }

    [Fact]
    public async Task ProcessAsync_WithValidEntries_ShouldHaveCorrectStructure()
    {
        // Arrange
        List<LogEntry> logEntries = CreateSampleLogEntries();

        // Act
        Result<ProcessingResult> result = await _step.ExecuteAsync(logEntries);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Detailed verification of the processing result structure
        ProcessingResult processingResult = result.Value;
        processingResult.TotalLinesProcessed.ShouldBe(3);
        processingResult.MatchedLines.ShouldBe(3);
        processingResult.UnmatchedLines.ShouldBe(0);
        processingResult.ColumnNames.OrderBy(x => x).ShouldBe([
            "Method", "Path", "StatusCode"
        ]);
        processingResult.ParsedEntries.Count.ShouldBe(3);
        processingResult.Statistics.Keys.Count().ShouldBeGreaterThan(10); // Should have many statistics
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyEntries_ShouldReturnEmptyResult()
    {
        // Arrange
        List<LogEntry> logEntries = [];

        // Act
        Result<ProcessingResult> result = await _step.ExecuteAsync(logEntries);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        ProcessingResult processingResult = result.Value;
        processingResult.ParsedEntries.ShouldBeEmpty();
        processingResult.TotalLinesProcessed.ShouldBe(0);
        processingResult.MatchedLines.ShouldBe(0);
        processingResult.ColumnNames.ShouldBeEmpty();
        ((double)processingResult.Statistics["processing_efficiency"]).ShouldBe(0.0);
    }

    [Fact]
    public async Task ProcessAsync_WithEntriesHavingMissingData_ShouldHandleGracefully()
    {
        // Arrange
        List<LogEntry> logEntries =
        [
            new()
            {
                LineNumber = 1,
                RawLine = "Complete entry",
                ExtractedData = new Dictionary<string, string>
                                {
                                    ["Method"] = "GET", ["Path"] = "/api/users"
                                }
            },

            new()
            {
                LineNumber = 2,
                RawLine = "Incomplete entry",
                ExtractedData = new Dictionary<string, string>
                                {
                                    ["Method"] = "POST"
                                    // Missing Path
                                }
            }
        ];

        // Act
        Result<ProcessingResult> result = await _step.ExecuteAsync(logEntries);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        ProcessingResult processingResult = result.Value;
        processingResult.ColumnNames.Count.ShouldBe(2);
        processingResult.Statistics["Method_count"].ShouldBe(2);
        processingResult.Statistics["Path_count"].ShouldBe(1); // Only one entry has Path
    }

    private static List<LogEntry> CreateSampleLogEntries()
    {
        return
        [
            new LogEntry
            {
                LineNumber = 1,
                RawLine = "GET /api/users 200",
                ExtractedData = new Dictionary<string, string>
                                {
                                    ["Method"] = "GET", ["Path"] = "/api/users", ["StatusCode"] = "200"
                                }
            },

            new LogEntry
            {
                LineNumber = 2,
                RawLine = "POST /api/data 404",
                ExtractedData = new Dictionary<string, string>
                                {
                                    ["Method"] = "POST", ["Path"] = "/api/data", ["StatusCode"] = "404"
                                }
            },

            new LogEntry
            {
                LineNumber = 3,
                RawLine = "GET /api/users 200",
                ExtractedData = new Dictionary<string, string>
                                {
                                    ["Method"] = "GET", ["Path"] = "/api/users", ["StatusCode"] = "200"
                                }
            }
        ];
    }
}