using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PrAnalyzer.ApiService.Services
{
    public class HuggingFaceService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<HuggingFaceService> _logger;
        private readonly string _sentimentModel = "distilbert-base-uncased-finetuned-sst-2-english"; // Example model

        public HuggingFaceService(HttpClient httpClient, IConfiguration config, ILogger<HuggingFaceService> logger)
        {
            _httpClient = httpClient;
            _apiKey = config["HuggingFace:ApiKey"] ?? string.Empty;
            _logger = logger;

        }

        public async Task<string> GetSentimentAsync(string text, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api-inference.huggingface.co/models/facebook/bart-large-mnli");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);


            //            var categoryDescriptions = new List<string>
            //{
            // "Code formatting, indentation, or style guide violations",
            // "Variable, method, or class naming issues",
            // "Performance issues, algorithmic improvements, or optimization suggestions",
            // "Error handling, try-catch blocks, or exception management",
            // "Design patterns, code structure, or architectural concerns",
            // "Security vulnerabilities or best practices",
            // "Unit tests, test coverage, or testing methodology",
            // "Code comments, API documentation, or README updates",
            // "Potential bugs or logical mistakes in the code",
            // "General coding best practices not covered by other categories",
            // "Comments that don't fit into the above categories"
            //};


            //            string[] categoryKeys = new[]
            //            {
            //                "code_style",
            //                "naming_conventions",
            //                "inefficient_code",
            //                "exception_handling",
            //                "architecture",
            //                "security",
            //                "testing",
            //                "documentation",
            //                "logic_error",
            //                "best_practices",
            //                "other"
            //            };

            //           var categoryDescriptions = new List<string> {


            //           "Code formatting, indentation, or style guide violations",
            // "Variable, method, or class naming issues",
            // //"Consistent use of language features (e.g., arrow functions vs. traditional functions)",
            // "Redundant or unnecessary code (e.g., unused imports, dead code)",
            // "Performance issues, algorithmic improvements, or optimization suggestions",
            // "Inefficient data structures or loops",
            // "Potential bugs or logical mistakes in the code",
            // "Incorrect assumptions or edge case handling",
            // "Race conditions or concurrency issues",
            // "Security vulnerabilities or best practices",
            // "Sensitive data exposure or improper handling",
            // "Input validation and sanitization",
            // "Design patterns, code structure, or architectural concerns",
            // "Separation of concerns or single responsibility violations",
            // "Overengineering or unnecessary abstraction",
            // "Error handling, try-catch blocks, or exception management",
            // "Fail-safe mechanisms or fallback strategies",
            // "Logging and monitoring practices",
            // "Unit tests, test coverage, or testing methodology",
            // "Test readability, maintainability, or naming",
            // "Mocking, stubbing, or test data setup",
            // "Code comments, API documentation, or README updates",
            // "Clarity of commit messages or pull request descriptions",
            // "Inline documentation for complex logic",
            // "General coding best practices not covered by other categories",
            // "Code reusability and modularity",
            // "Dependency management and versioning",
            // "Build scripts, CI/CD pipeline issues, or automation",
            // "Linting, static analysis, or formatting tool configuration",
            // "Environment-specific configurations or secrets management",
            // "Comments that don't fit into the above categories",
            // "Code duplication",
            // "Encapsulation and interface design",
            // "Hardcoded strings",
            // "Date, time, and number formatting for different locales",
            // "Logging practices (verbosity, sensitive data, structured logs)",
            // "Metrics, tracing, or monitoring instrumentation",
            // "Database query optimization or indexing",
            // "Data migration scripts or backward compatibility",
            // "Code complexity or readability concerns",
            // "Deep nesting or long functions",
            // "Magic numbers or unclear constants",
            // "Use of deprecated APIs or libraries",
            // "Legacy code compatibility or modernization suggestions",
            // "API design consistency and RESTful principles",
            //};

            string[] codeReviewCategories = new string[]
{
"Code duplication",
 "Encapsulation and interface design",
 "Hardcoded strings",
 "Date, time, and number formatting for different locales",
 "Logging practices (verbosity, sensitive data, structured logs)",
"Logging and monitoring practices",
 "Unit tests, test coverage, or testing methodology",
 "Test readability, maintainability, or naming",
 "Mocking, stubbing, or test data setup",
 "Code comments, API documentation, or README updates",
"Redundant or unnecessary code (e.g., unused imports, dead code)",
 "Performance issues, algorithmic improvements, or optimization suggestions",
 "Inefficient data structures or loops",
 "Potential bugs or logical mistakes in the code",
"Code formatting, indentation, or style guide violations",
 "Security vulnerabilities or best practices",
 "Sensitive data exposure or improper handling",
 "Input validation and sanitization",
 "Design patterns, code structure, or architectural concerns",
 "Separation of concerns or single responsibility violations",
 "Variable, method, or class naming issues",
 "Database query optimization or indexing",
 "Data migration scripts or backward compatibility",
 "Code complexity or readability concerns",
 "Deep nesting or long functions",
 "Magic numbers or unclear constants",
 "Use of deprecated APIs or libraries",
 "Legacy code compatibility or modernization suggestions",
 "API design consistency and RESTful principles",
    "Code Quality",
    "Security",
    "Maintainability",
    "Correctness / Bug Fixes",
    "Design / Architecture",
    "API Design",
    "Testing",
    "Documentation",
    "Styling / Formatting",
    "Dependency Management",
    "Build / CI/CD",
    "Internationalization / Localization",
    "Accessibility"
};

            var payload = new
            {
                inputs = text,
                parameters = new
                {
                    candidate_labels = codeReviewCategories
                }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("HuggingFace API error: {Status} {Reason}", response.StatusCode, response.ReasonPhrase);
                    return "neutral";
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                try
                {
                    var doc = JsonDocument.Parse(json);
                    var label = doc.RootElement.GetProperty("labels")[0].GetString();
                    return label?.ToLowerInvariant() ?? "neutral";
                }
                catch
                {
                    _logger.LogWarning("Failed to parse HuggingFace response: {Json}", json);
                    return "neutral";
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("HuggingFace request was canceled (timeout or user cancellation).");
                return "timeout";
            }
        }

        public async Task<string> GetTypeClassificationAsync(string text, string[] possibleTypes, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api-inference.huggingface.co/models/facebook/bart-large-mnli");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var payload = new
            {
                inputs = text,
                parameters = new
                {
                    candidate_labels = possibleTypes
                }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("HuggingFace API error: {Status} {Reason}", response.StatusCode, response.ReasonPhrase);
                    return string.Empty;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                try
                {
                    var doc = JsonDocument.Parse(json);
                    var label = doc.RootElement.GetProperty("labels")[0].GetString();
                    return label ?? string.Empty;
                }
                catch
                {
                    _logger.LogWarning("Failed to parse HuggingFace response: {Json}", json);
                    return string.Empty;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("HuggingFace request was canceled (timeout or user cancellation).");
                return string.Empty;
            }
        }
    }
}
