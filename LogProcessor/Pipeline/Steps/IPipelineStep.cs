using LogProcessor.Models;

namespace LogProcessor.Pipeline.Steps;

/// <summary>
/// Interface for pipeline steps that do some work with input
/// </summary>
/// <typeparam name="TInput">Type of input data</typeparam>
/// <typeparam name="TOutput">Type of output data</typeparam>
public interface IPipelineStep<TInput, TOutput>
{
    /// <summary>
    /// Do some work with the input data and returns the output
    /// </summary>
    /// <param name="input">Input data to process</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Processed output data</returns>
    Task<Result<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);
}