using LogProcessor.Models;

using Shouldly;

namespace LogProcessor.Tests.Models;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Arrange
        const string value = "test value";

        // Act
        Result<string> result = Result<string>.Success(value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe(value);
    }

    [Fact]
    public void Failure_WithException_ShouldCreateFailedResult()
    {
        // Arrange
        InvalidOperationException exception = new("test error");

        // Act
        Result<string> result = Result<string>.Failure(exception);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(exception);
    }

    [Fact]
    public void Failure_WithMessage_ShouldCreateFailedResult()
    {
        // Arrange
        const string errorMessage = "test error message";

        // Act
        Result<string> result = Result<string>.Failure(errorMessage);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBeOfType<InvalidOperationException>();
        result.Error.Message.ShouldBe(errorMessage);
    }

    [Fact]
    public void Value_WhenFailure_ShouldThrowException()
    {
        // Arrange
        Result<string> result = Result<string>.Failure("error");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => result.Value)
              .Message.ShouldBe("Cannot access Value when result is a failure");
    }

    [Fact]
    public void Error_WhenSuccess_ShouldThrowException()
    {
        // Arrange
        Result<string> result = Result<string>.Success("value");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => result.Error)
              .Message.ShouldBe("Cannot access Error when result is a success");
    }

    [Fact]
    public void Match_WithSuccess_ShouldExecuteSuccessFunction()
    {
        // Arrange
        Result<int> result = Result<int>.Success(42);
        bool successCalled = false;
        bool failureCalled = false;

        // Act
        string outcome = result.Match(
        onSuccess: value =>
        {
            successCalled = true;

            return $"Success: {value}";
        },
        onFailure: error =>
        {
            failureCalled = true;

            return $"Error: {error.Message}";
        });

        // Assert
        successCalled.ShouldBeTrue();
        failureCalled.ShouldBeFalse();
        outcome.ShouldBe("Success: 42");
    }

    [Fact]
    public void Match_WithFailure_ShouldExecuteFailureFunction()
    {
        // Arrange
        Result<int> result = Result<int>.Failure("test error");
        bool successCalled = false;
        bool failureCalled = false;

        // Act
        string outcome = result.Match(
        onSuccess: value =>
        {
            successCalled = true;

            return $"Success: {value}";
        },
        onFailure: error =>
        {
            failureCalled = true;

            return $"Error: {error.Message}";
        });

        // Assert
        successCalled.ShouldBeFalse();
        failureCalled.ShouldBeTrue();
        outcome.ShouldBe("Error: test error");
    }

    [Fact]
    public void Map_WithSuccess_ShouldTransformValue()
    {
        // Arrange
        Result<int> result = Result<int>.Success(42);

        // Act
        Result<string> mapped = result.Map(x => x.ToString());

        // Assert
        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBe("42");
    }

    [Fact]
    public void Map_WithFailure_ShouldPropagateError()
    {
        // Arrange
        InvalidOperationException error = new("test error");
        Result<int> result = Result<int>.Failure(error);

        // Act
        Result<string> mapped = result.Map(x => x.ToString());

        // Assert
        mapped.IsFailure.ShouldBeTrue();
        mapped.Error.ShouldBe(error);
    }

    [Fact]
    public void Bind_WithSuccess_ShouldExecuteBindFunction()
    {
        // Arrange
        Result<int> result = Result<int>.Success(42);

        // Act
        Result<string> bound = result.Bind(x => Result<string>.Success(x.ToString()));

        // Assert
        bound.IsSuccess.ShouldBeTrue();
        bound.Value.ShouldBe("42");
    }

    [Fact]
    public void Bind_WithFailure_ShouldPropagateError()
    {
        // Arrange
        InvalidOperationException error = new("test error");
        Result<int> result = Result<int>.Failure(error);

        // Act
        Result<string> bound = result.Bind(x => Result<string>.Success(x.ToString()));

        // Assert
        bound.IsFailure.ShouldBeTrue();
        bound.Error.ShouldBe(error);
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateSuccess()
    {
        // Act
        Result<string> result = "test value";

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("test value");
    }

    [Fact]
    public void ImplicitConversion_FromException_ShouldCreateFailure()
    {
        // Arrange
        InvalidOperationException exception = new("test error");

        // Act
        Result<string> result = exception;

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(exception);
    }
}