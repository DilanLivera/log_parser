namespace LogProcessor.Tests.Fixtures;

/// <summary>
/// Fixture that provides sample log files for testing
/// </summary>
public class LogFileFixture : IDisposable
{
    public string SampleLogPath { get; private set; }
    public string AppLogPath { get; private set; }
    public string EmptyLogPath { get; private set; }
    public string NonExistentLogPath { get; private set; }

    public LogFileFixture()
    {
        string tempDir = Path.GetTempPath();

        SampleLogPath = Path.Combine(tempDir, $"sample_test_{Guid.NewGuid():N}.log");
        File.WriteAllText(SampleLogPath, SampleLogContent);

        AppLogPath = Path.Combine(tempDir, $"app_test_{Guid.NewGuid():N}.log");
        File.WriteAllText(AppLogPath, AppLogContent);

        EmptyLogPath = Path.Combine(tempDir, $"empty_test_{Guid.NewGuid():N}.log");
        File.WriteAllText(EmptyLogPath, string.Empty);

        NonExistentLogPath = Path.Combine(tempDir, $"nonexistent_test_{Guid.NewGuid():N}.log");

        if (!File.Exists(SampleLogPath) || !File.Exists(AppLogPath) || !File.Exists(EmptyLogPath))
        {
            throw new InvalidOperationException("Failed to create test fixture files");
        }
    }

    public const string SampleLogContent = "2024-01-15 10:30:45 [INFO] HTTP GET /api/users responded 200 in 12.456 ms\n2024-01-15 10:30:47 [INFO] HTTP POST /api/auth/login responded 200 in 45.789 ms\n2024-01-15 10:30:50 [ERROR] HTTP GET /api/data responded 404 in 8.123 ms\n2024-01-15 10:30:52 [INFO] HTTP GET /api/users/123 responded 200 in 15.234 ms\n2024-01-15 10:30:55 [INFO] HTTP PUT /api/users/123 responded 200 in 23.567 ms\n2024-01-15 10:30:58 [WARN] HTTP GET /api/slow-endpoint responded 200 in 1234.567 ms\n2024-01-15 10:31:01 [INFO] HTTP DELETE /api/users/456 responded 204 in 18.890 ms\n2024-01-15 10:31:03 [ERROR] HTTP POST /api/data responded 500 in 56.789 ms\n2024-01-15 10:31:05 [INFO] HTTP GET /api/health responded 200 in 2.345 ms\n2024-01-15 10:31:08 [INFO] HTTP POST /api/users responded 201 in 67.890 ms";

    public const string AppLogContent = "2024-01-15 08:30:15 [INFO] User authentication successful for user@example.com\n2024-01-15 08:30:16 [DEBUG] Loading user preferences from database\n2024-01-15 08:30:17 [WARN] Cache miss for user preferences, querying database\n2024-01-15 08:30:18 [INFO] Database query completed in 45ms\n2024-01-15 08:30:19 [ERROR] Failed to connect to external API: timeout after 30s";

    public const string HttpRegexPattern = @"HTTP (?<RequestMethod>\w+) (?<RequestPath>/[^\s]+) responded (?<StatusCode>\d+) in (?<Elapsed>[\d.]+) ms";
    public const string AppLogRegexPattern = @"(?<Timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<Level>\w+)\] (?<Message>.*)";
    public const string InvalidRegexPattern = "[invalid regex (";

    public void Dispose()
    {
        try
        {
            if (File.Exists(SampleLogPath))
            {
                File.Delete(SampleLogPath);
            }
            if (File.Exists(AppLogPath))
            {
                File.Delete(AppLogPath);
            }
            if (File.Exists(EmptyLogPath))
            {
                File.Delete(EmptyLogPath);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}