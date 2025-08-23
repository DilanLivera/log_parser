using LogProcessor.Models;

namespace LogProcessor;

/// <summary>
/// LINQ extensions for Result<T> to enable railway-oriented programming and query syntax
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// LINQ SelectMany extension with result selector for Task<Result<T>>
    /// </summary>
    public static async Task<Result<TResult>> SelectMany<TSource, TMiddle, TResult>(
        this Task<Result<TSource>> source,
        Func<TSource, Task<Result<TMiddle>>> selector,
        Func<TSource, TMiddle, TResult> resultSelector)
    {
        Result<TSource> sourceResult = await source;
        if (sourceResult.IsFailure)
        {
            return Result<TResult>.Failure(sourceResult.Error);
        }

        Result<TMiddle> middleResult = await selector(sourceResult.Value);

        return middleResult.IsFailure
            ? Result<TResult>.Failure(middleResult.Error)
            : Result<TResult>.Success(resultSelector(sourceResult.Value, middleResult.Value));

    }
}