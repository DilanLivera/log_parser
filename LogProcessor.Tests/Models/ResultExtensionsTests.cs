using LogProcessor.Models;

using Shouldly;

namespace LogProcessor.Tests.Models;

public class ResultExtensionsTests
{
    [Fact]
    public async Task TaskResult_LinqQuerySyntax_ShouldWork()
    {
        // Act - Testing LINQ with async Task<Result<T>>
        Result<string> finalResult = await (
            from first in Task.FromResult(Result<int>.Success(10))
            from second in Task.FromResult(Result<int>.Success(first * 2))
            select $"Value: {second}");

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.Value.ShouldBe("Value: 20");
    }

    [Fact]
    public async Task TaskResult_WithFailure_ShouldPropagateError()
    {
        // Arrange
        InvalidOperationException expectedError = new("test error");

        // Act
        Result<string> finalResult = await (
            from first in Task.FromResult(Result<int>.Success(10))
            from second in Task.FromResult(Result<int>.Failure(expectedError))
            select $"Value: {second}");

        // Assert
        finalResult.IsFailure.ShouldBeTrue();
        finalResult.Error.ShouldBe(expectedError);
    }
}