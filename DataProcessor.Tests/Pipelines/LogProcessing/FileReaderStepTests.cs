using DataProcessor.Pipelines.LogProcessing;
using DataProcessor.Tests.Fixtures;

using Shouldly;

namespace DataProcessor.Tests.Pipelines.LogProcessing;

public class FileReaderStepTests : IClassFixture<LogFileFixture>
{
    private readonly LogFileFixture _fixture;
    private readonly FileReaderStep _step;

    public FileReaderStepTests(LogFileFixture fixture)
    {
        _fixture = fixture;
        _step = new FileReaderStep();
    }

    [Fact]
    public async Task ProcessAsync_WithValidFile_ShouldReturnSuccess()
    {
        // Act
        Result<IReadOnlyList<string>> result = await _step.ExecuteAsync(_fixture.SampleLogPath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(10);
        result.Value.First().ShouldContain("HTTP GET /api/users");
    }

    [Fact]
    public async Task ProcessAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Act
        Result<IReadOnlyList<string>> result = await _step.ExecuteAsync("nonexistent.log");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Log file not found");
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyPath_ShouldReturnFailure()
    {
        // Act
        Result<IReadOnlyList<string>> result = await _step.ExecuteAsync("");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("File path cannot be null or empty");
    }

    [Fact]
    public async Task ProcessAsync_WithNullPath_ShouldReturnFailure()
    {
        // Act
        Result<IReadOnlyList<string>> result = await _step.ExecuteAsync(null!);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("File path cannot be null or empty");
    }

    [Fact]
    public async Task ProcessAsync_WithWhitespacePath_ShouldReturnFailure()
    {
        // Act
        Result<IReadOnlyList<string>> result = await _step.ExecuteAsync("   ");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("File path cannot be null or empty");
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyFile_ShouldReturnEmptyCollection()
    {
        // Act
        Result<IReadOnlyList<string>> result = await _step.ExecuteAsync(_fixture.EmptyLogPath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(0);
    }
}