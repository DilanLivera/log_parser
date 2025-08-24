using System.CommandLine;
using System.Text.RegularExpressions;

using LogProcessor.Models;
using LogProcessor.Pipeline;
using LogProcessor.Pipeline.Steps;

using Spectre.Console;

RootCommand rootCommand = new(description: "A .NET CLI application to parse and process data from log files using regex patterns")
                          {
                              Name = "logprocessor"
                          };

Option<string> fileOption = new(aliases: ["--file", "-f"],
                                description: "Path to the log file to be processed. Example: --file \"logs/app.log\" or --file \"C:\\logs\\server.log\"")
                            {
                                IsRequired = true
                            };

fileOption.AddValidator(result =>
{
    string? filePath = result.GetValueForOption(fileOption);
    if (!string.IsNullOrEmpty(filePath) && !File.Exists(filePath))
    {
        result.ErrorMessage = $"File does not exist: {filePath}";
    }
});

Option<string[]> patternsOption = new(aliases: ["--patterns", "-p"],
                                      description: "Multiple regular expression patterns with named capture groups for parsing log statements. Each pattern must include the correlation field. Example: --patterns \"(?<RequestId>\\w+) HTTP (?<Method>\\w+)\" \"(?<RequestId>\\w+) Query: (?<SQL>.*)\"")
                                  {
                                      IsRequired = true
                                  };

Option<string> correlationFieldOption = new(aliases: ["--correlation-field", "-c"],
                                            description: "Name of the correlation field used to group related log entries. This field must be present as a named capture group in all patterns. Example: --correlation-field \"RequestId\"")
                                        {
                                            IsRequired = true
                                        };

// Add validator for patterns
patternsOption.AddValidator(result =>
{
    string[]? patterns = result.GetValueForOption(patternsOption);
    if (patterns == null || patterns.Length == 0)
    {
        result.ErrorMessage = "At least one pattern must be provided";

        return;
    }

    if (patterns.Length > 3)
    {
        result.ErrorMessage = "Maximum of 3 patterns allowed";

        return;
    }

    foreach (string pattern in patterns)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            result.ErrorMessage = "Pattern cannot be empty";

            return;
        }

        try
        {
            _ = new Regex(pattern);
        }
        catch (ArgumentException ex)
        {
            result.ErrorMessage = $"Invalid regex pattern '{pattern}': {ex.Message}";

            return;
        }
    }
});

// Add validator for correlation field
correlationFieldOption.AddValidator(result =>
{
    string? correlationField = result.GetValueForOption(correlationFieldOption);
    if (string.IsNullOrWhiteSpace(correlationField))
    {
        result.ErrorMessage = "Correlation field cannot be empty";

        return;
    }

    // Validate that correlation field appears in all patterns
    string[]? patterns = result.GetValueForOption(patternsOption);
    if (patterns != null)
    {
        foreach (string pattern in patterns)
        {
            try
            {
                Regex regex = new(pattern);
                if (!regex.GetGroupNames().Contains(correlationField))
                {
                    result.ErrorMessage = $"Correlation field '{correlationField}' must be present as a named capture group in pattern: {pattern}";

                    return;
                }
            }
            catch (ArgumentException)
            {
                // Pattern validation will be caught by patterns validator
            }
        }
    }
});

Option<string?> outputFileOption = new(aliases: ["--output-file", "-o"],
                                       description: "Optional file path to save the extracted data in JSON or CSV format. Example: --output-file \"results.json\" or --output-file \"data.csv\"")
                                   {
                                       IsRequired = false
                                   };

Option<int> maxRowsOption = new(aliases: ["--max-rows", "-m"],
                                description: "Maximum number of rows to display in the output table. Use 0 to show all rows. Example: --max-rows 25 (default: 50)")
                            {
                                IsRequired = false
                            };
maxRowsOption.SetDefaultValue(50);

maxRowsOption.AddValidator(result =>
{
    int maxRows = result.GetValueForOption(maxRowsOption);
    if (maxRows < 0)
    {
        result.ErrorMessage = "Maximum rows cannot be negative";
    }
});

rootCommand.AddOption(fileOption);
rootCommand.AddOption(patternsOption);
rootCommand.AddOption(correlationFieldOption);
rootCommand.AddOption(outputFileOption);
rootCommand.AddOption(maxRowsOption);

rootCommand.SetHandler(handle: async (filePath, patterns, correlationField, outputFile, maxRows) =>
                       {
                           Rule rule = new(title: "[bold blue]Log File Processor[/]")
                                       {
                                           Style = Style.Parse("blue")
                                       };
                           AnsiConsole.Write(rule);
                           AnsiConsole.WriteLine();

                           FileReaderStep reader = new();
                           LogParserStep parser = new();
                           CorrelationStep correlationStep = new();
                           DataProcessorStep processor = new();
                           FileSaverStep fileSaver = new();
                           DisplayStep display = new(maxRows);

                           LogProcessingPipeline pipeline = new(reader, parser, correlationStep, processor, fileSaver, display);

                           Result<ProcessingResult> pipelineResult = await pipeline.ExecuteAsync(filePath, patterns, correlationField, outputFile);

                           if (pipelineResult.IsFailure)
                           {
                               AnsiConsole.MarkupLine($"[red]Processing failed: {pipelineResult.Error.Message.Replace("[", "[[").Replace("]", "]]")}[/]");

                               return;
                           }

                           AnsiConsole.WriteLine();
                           Rule completionRule = new(title: "[bold green]Processing Complete[/]")
                                                 {
                                                     Style = Style.Parse("green")
                                                 };
                           AnsiConsole.Write(completionRule);

                       },
                       fileOption,
                       patternsOption,
                       correlationFieldOption,
                       outputFileOption,
                       maxRowsOption);

return await rootCommand.InvokeAsync(args);