# Log File Processor

A .NET CLI application that parses and processes data from log files using the **Pipeline pattern**. The application uses **System.CommandLine** for its command-line interface and **Spectre.Console** to display parsed data in visually appealing tables.

## Installation

1. Clone the repository
2. Navigate to the project directory
3. Build the project:
   ```bash
   dotnet build
   ```

## Usage

### Basic Command Structure

```bash
dotnet run -- --file <path_to_log_file> --regex <regular_expression> [--output-file <output_path>] [--max-rows <number>]
```

### Command Line Options

- `--file, -f` (Required): Path to the log file to be processed
- `--regex, -r` (Required): Regular expression pattern with named capture groups
- `--output-file, -o` (Optional): File path to save extracted data (JSON or CSV)
- `--max-rows, -m` (Optional): Maximum number of rows to display (default: 50)

### Examples

#### Example 1: Basic HTTP Log Processing

Process HTTP request logs and extract method, path, status code, and response time:

```bash
dotnet run -- --file "sample.log" --regex "HTTP (?<RequestMethod>\w+) (?<RequestPath>/[^\s]+) responded (?<StatusCode>\d+) in (?<Elapsed>[\d.]+) ms"
```

This regex pattern extracts:

- `RequestMethod`: HTTP method (GET, POST, PUT, etc.)
- `RequestPath`: API endpoint path
- `StatusCode`: HTTP status code
- `Elapsed`: Response time in milliseconds

#### Example 2: Save Results to JSON

```bash
dotnet run -- --file "sample.log" --regex "HTTP (?<RequestMethod>\w+) (?<RequestPath>/[^\s]+) responded (?<StatusCode>\d+) in (?<Elapsed>[\d.]+) ms" --output-file "results.json"
```

#### Example 3: Save Results to CSV

```bash
dotnet run -- --file "sample.log" --regex "HTTP (?<RequestMethod>\w+) (?<RequestPath>/[^\s]+) responded (?<StatusCode>\d+) in (?<Elapsed>[\d.]+) ms" --output-file "results.csv"
```

#### Example 4: Limit Display Rows

```bash
dotnet run -- --file "sample.log" --regex "HTTP (?<RequestMethod>\w+) (?<RequestPath>/[^\s]+) responded (?<StatusCode>\d+) in (?<Elapsed>[\d.]+) ms" --max-rows 20
```

### Sample Log Format

The included `sample.log` file contains HTTP request logs in the following format:

```
2024-01-15 10:30:45 [INFO] HTTP GET /api/users responded 200 in 12.456 ms
2024-01-15 10:30:47 [INFO] HTTP POST /api/auth/login responded 200 in 45.789 ms
2024-01-15 10:30:50 [ERROR] HTTP GET /api/data responded 404 in 8.123 ms
```

### Creating Custom Regex Patterns

When creating regex patterns, use named capture groups to extract specific data:

```regex
(?<GroupName>pattern)
```

**Example patterns:**

1. **Web Server Logs:**
   ```regex
   (?<IP>\d+\.\d+\.\d+\.\d+) - - \[(?<DateTime>[^\]]+)\] "(?<Method>\w+) (?<Path>[^\s]+) HTTP/[\d.]+" (?<Status>\d+) (?<Size>\d+)
   ```

2. **Application Logs with Levels:**
   ```regex
   (?<Timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<Level>\w+)\] (?<Message>.+)
   ```

3. **Database Query Logs:**
   ```regex
   (?<Time>\d{2}:\d{2}:\d{2}\.\d{3}) (?<Duration>\d+)ms (?<Query>SELECT|INSERT|UPDATE|DELETE) (?<Table>\w+)
   ```

## Output

The application provides several types of output:

### 1. Processing Summary

- Total lines processed
- Matched vs unmatched lines
- Processing efficiency percentage
- Number of extracted columns

### 2. Parsed Data Table

- Displays extracted data in a formatted table
- Shows line numbers and all captured groups
- Truncates long values for readability

### 3. Column Statistics

- Count and unique count for each column
- Min, max, and average values for numeric columns
- Most common values for each column

### 4. Export Files

- **JSON**: Complete structured data with all metadata
- **CSV**: Tabular format suitable for spreadsheet applications
