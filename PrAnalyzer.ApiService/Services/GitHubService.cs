using Microsoft.Extensions.Caching.Distributed;
using PrAnalyzer.ApiService.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PrAnalyzer.ApiService.Services
{
    public class GitHubService
    {
        private readonly HttpClient _httpClient;
        private readonly IDistributedCache _cache;
        private readonly ILogger<GitHubService> _logger;

        public GitHubService(HttpClient httpClient, IDistributedCache cache, ILogger<GitHubService> logger)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.github.com/");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PR-Review-Analyzer/1.0");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", "");
            _cache = cache;
            _logger = logger;
        }

        public async Task<PullRequestReview> GetPullRequestReviewsAsync(string repoUrl, int prNumber)
        {
            var cacheKey = $"pr_reviews:{repoUrl}:{prNumber}";
            var cachedResult = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                var cached = JsonSerializer.Deserialize<PullRequestReview>(cachedResult);
                if (cached != null && cached.ExtractedAt > DateTime.UtcNow.AddMinutes(-30))
                {
                    _logger.LogInformation("Returning cached PR reviews for {RepoUrl} PR #{PrNumber}", repoUrl, prNumber);
                    return cached;
                }
            }

            var (owner, repo) = ParseRepoUrl(repoUrl);
            var review = new PullRequestReview
            {
                RepoUrl = repoUrl,
                PrNumber = prNumber,
                ExtractedAt = DateTime.UtcNow
            };

            try
            {
                // Get review comments
                var reviewCommentsUrl = $"repos/{owner}/{repo}/pulls/{prNumber}/comments";
                var reviewCommentsResponse = await _httpClient.GetStringAsync(reviewCommentsUrl);
                var reviewComments = JsonSerializer.Deserialize<JsonElement[]>(reviewCommentsResponse);

                // Get issue comments (general PR comments)
                var issueCommentsUrl = $"repos/{owner}/{repo}/issues/{prNumber}/comments";
                var issueCommentsResponse = await _httpClient.GetStringAsync(issueCommentsUrl);
                var issueComments = JsonSerializer.Deserialize<JsonElement[]>(issueCommentsResponse);

                // Parse review comments (code-specific) - only first comment in each thread
                if (reviewComments != null)
                {
                    // Group by (path, original_commit_id, position) as a thread key, take earliest created_at
                    var threadGroups = reviewComments
                        .Where(c => c.TryGetProperty("path", out _))
                        .GroupBy(c => new
                        {
                            Path = c.GetProperty("path").GetString() ?? string.Empty,
                            CommitId = c.TryGetProperty("original_commit_id", out var ocid) ? ocid.GetString() ?? string.Empty : string.Empty,
                            Position = c.TryGetProperty("position", out var pos) && pos.ValueKind != JsonValueKind.Null ? pos.GetInt32() : 0
                        });
                    foreach (var group in threadGroups)
                    {
                        var firstComment = group.OrderBy(c => c.GetProperty("created_at").GetDateTime()).First();
                        review.Comments.Add(new ReviewComment
                        {
                            Id = firstComment.GetProperty("id").GetInt64(),
                            User = firstComment.GetProperty("user").GetProperty("login").GetString() ?? "",
                            Body = firstComment.GetProperty("body").GetString() ?? "",
                            Path = firstComment.TryGetProperty("path", out var path) ? path.GetString() ?? "" : "",
                            Position = firstComment.TryGetProperty("position", out var pos) && pos.ValueKind != JsonValueKind.Null ? pos.GetInt32() : null,
                            State = firstComment.TryGetProperty("state", out var state) ? state.GetString() ?? "" : "",
                            CreatedAt = firstComment.GetProperty("created_at").GetDateTime(),
                            UpdatedAt = firstComment.GetProperty("updated_at").GetDateTime()
                        });
                    }
                }

                // Parse issue comments (general PR comments) - only first comment if any
                if (issueComments != null && issueComments.Length > 0)
                {
                    var firstIssueComment = issueComments.OrderBy(c => c.GetProperty("created_at").GetDateTime()).First();
                    review.Comments.Add(new ReviewComment
                    {
                        Id = firstIssueComment.GetProperty("id").GetInt64(),
                        User = firstIssueComment.GetProperty("user").GetProperty("login").GetString() ?? "",
                        Body = firstIssueComment.GetProperty("body").GetString() ?? "",
                        Path = "",
                        Position = null,
                        State = "",
                        CreatedAt = firstIssueComment.GetProperty("created_at").GetDateTime(),
                        UpdatedAt = firstIssueComment.GetProperty("updated_at").GetDateTime()
                    });
                }

                // Cache the result for 30 minutes
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                };
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(review), cacheOptions);

                _logger.LogInformation("Extracted {Count} comments from {RepoUrl} PR #{PrNumber}", review.Comments.Count, repoUrl, prNumber);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to fetch PR reviews from GitHub API");
                throw new InvalidOperationException($"Failed to fetch PR reviews: {ex.Message}");
            }

            return review;
        }

        private static (string owner, string repo) ParseRepoUrl(string repoUrl)
        {
            var uri = new Uri(repoUrl);
            var segments = uri.AbsolutePath.Trim('/').Split('/');

            if (segments.Length < 2)
                throw new ArgumentException("Invalid repository URL format");

            return (segments[0], segments[1]);
        }
    }
}
