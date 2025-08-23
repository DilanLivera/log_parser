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

Option<string> regexOption = new(aliases: ["--regex", "-r"],
                                 description: "Regular expression pattern with named capture groups for parsing log statements. Example: --regex \"(?<Timestamp>\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}) \\[(?<Level>\\w+)\\] (?<Message>.*)\"")
                             {
                                 IsRequired = true
                             };

regexOption.AddValidator(result =>
{
    string? regexPattern = result.GetValueForOption(regexOption);
    if (string.IsNullOrWhiteSpace(regexPattern))
    {
        result.ErrorMessage = "Regex pattern cannot be empty";

        return;
    }

    try
    {
        _ = new Regex(regexPattern);
    }
    catch (ArgumentException ex)
    {
        result.ErrorMessage = $"Invalid regex pattern: {ex.Message}";
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
rootCommand.AddOption(regexOption);
rootCommand.AddOption(outputFileOption);
rootCommand.AddOption(maxRowsOption);

rootCommand.SetHandler(handle: async (filePath, regexPattern, outputFile, maxRows) =>
                       {
                           Rule rule = new(title: "[bold blue]Log File Processor[/]")
                                       {
                                           Style = Style.Parse("blue")
                                       };
                           AnsiConsole.Write(rule);
                           AnsiConsole.WriteLine();

                           FileReaderStep reader = new();
                           LogParserStep parser = new();
                           DataProcessorStep processor = new();
                           FileSaverStep fileSaver = new();
                           DisplayStep display = new(maxRows);

                           LogProcessingPipeline pipeline = new(reader, parser, processor, fileSaver, display);

                           Result<ProcessingResult> pipelineResult = await pipeline.ExecuteAsync(filePath, regexPattern, outputFile);

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
                       regexOption,
                       outputFileOption,
                       maxRowsOption);

return await rootCommand.InvokeAsync(args);