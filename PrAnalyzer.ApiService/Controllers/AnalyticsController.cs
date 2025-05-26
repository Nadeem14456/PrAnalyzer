using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace PrAnalyzer.ApiService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(IDistributedCache cache, ILogger<AnalyticsController> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        [HttpGet("repository-stats")]
        public async Task<ActionResult<RepositoryStats>> GetRepositoryStats([FromQuery] string repoUrl)
        {
            try
            {
                var cacheKey = $"repo_stats:{repoUrl}";
                var cachedStats = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedStats))
                {
                    var stats = JsonSerializer.Deserialize<RepositoryStats>(cachedStats);
                    return Ok(stats);
                }

                // Calculate repository statistics
                var repositoryStats = new RepositoryStats
                {
                    RepoUrl = repoUrl,
                    TotalAnalyzedPRs = 0, // This would be calculated from stored data
                    AverageCommentsPerPR = 0,
                    MostActiveReviewers = new Dictionary<string, int>(),
                    CommonCommentTypes = new Dictionary<string, int>(),
                    CalculatedAt = DateTime.UtcNow
                };

                // Cache for 1 hour
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                };
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(repositoryStats), cacheOptions);

                return Ok(repositoryStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repository stats for {RepoUrl}", repoUrl);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("trends")]
        public async Task<ActionResult<ReviewTrends>> GetReviewTrends([FromQuery] string repoUrl, [FromQuery] int days = 30)
        {
            try
            {
                // This would analyze trends over time
                var trends = new ReviewTrends
                {
                    RepoUrl = repoUrl,
                    PeriodDays = days,
                    CommentTrends = new Dictionary<string, List<TrendDataPoint>>(),
                    SentimentTrends = new Dictionary<string, List<TrendDataPoint>>(),
                    ReviewerActivityTrends = new Dictionary<string, List<TrendDataPoint>>(),
                    CalculatedAt = DateTime.UtcNow
                };

                return Ok(trends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review trends for {RepoUrl}", repoUrl);
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class RepositoryStats
    {
        public string RepoUrl { get; set; } = string.Empty;
        public int TotalAnalyzedPRs { get; set; }
        public double AverageCommentsPerPR { get; set; }
        public Dictionary<string, int> MostActiveReviewers { get; set; } = new();
        public Dictionary<string, int> CommonCommentTypes { get; set; } = new();
        public DateTime CalculatedAt { get; set; }
    }

    public class ReviewTrends
    {
        public string RepoUrl { get; set; } = string.Empty;
        public int PeriodDays { get; set; }
        public Dictionary<string, List<TrendDataPoint>> CommentTrends { get; set; } = new();
        public Dictionary<string, List<TrendDataPoint>> SentimentTrends { get; set; } = new();
        public Dictionary<string, List<TrendDataPoint>> ReviewerActivityTrends { get; set; } = new();
        public DateTime CalculatedAt { get; set; }
    }

    public class TrendDataPoint
    {
        public DateTime Date { get; set; }
        public int Value { get; set; }
    }
}
