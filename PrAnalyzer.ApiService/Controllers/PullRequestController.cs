using Microsoft.AspNetCore.Mvc;
using PrAnalyzer.ApiService.Models;
using PrAnalyzer.ApiService.Services;

namespace PrAnalyzer.ApiService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PullRequestController : ControllerBase
    {
        private readonly GitHubService _gitHubService;
        private readonly CommentClassificationService _classificationService;
        private readonly ILogger<PullRequestController> _logger;

        public PullRequestController(
            GitHubService gitHubService,
            CommentClassificationService classificationService,
            ILogger<PullRequestController> logger)
        {
            _gitHubService = gitHubService;
            _classificationService = classificationService;
            _logger = logger;
        }

        [HttpGet("analyze")]
        public async Task<ActionResult<PullRequestAnalysis>> AnalyzePullRequest(
            [FromQuery] string repoUrl,
            [FromQuery] int prNumber)
        {
            try
            {
                _logger.LogInformation("Analyzing PR {PrNumber} from {RepoUrl}", prNumber, repoUrl);

                var review = await _gitHubService.GetPullRequestReviewsAsync(repoUrl, prNumber);

                //// Exclude comments whose Body contains "resolved" or "fixed" (case-insensitive)
                //// Exclude comments whose Body contains "resolved" or "fixed" (case-insensitive)
                //// and only include comments where User equals "nadeem" (case-insensitive)
                //review.Comments = review.Comments
                //    .Where(comment =>
                //        comment.User.Equals("Nadeem14456", StringComparison.OrdinalIgnoreCase) || comment.User.Equals("jrippington", StringComparison.OrdinalIgnoreCase) || comment.User.Equals("harshal11869", StringComparison.OrdinalIgnoreCase) &&
                //        !(comment.Body.Contains("resolved", StringComparison.OrdinalIgnoreCase) ||
                //          !comment.Body.Contains("fixed", StringComparison.OrdinalIgnoreCase)))
                //    .ToList();

                // Classify each comment (async)
                var classifyTasks = review.Comments.Select(async comment =>
                {
                    comment.Classification = await _classificationService.ClassifyCommentAsync(comment);
                });

                await Task.WhenAll(classifyTasks);

                var analysis = new PullRequestAnalysis
                {
                    RepoUrl = repoUrl,
                    PrNumber = prNumber,
                    TotalComments = review.Comments.Count,
                    CommentsByUser = review.Comments.GroupBy(c => c.User)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    CommentsByType = review.Comments.GroupBy(c => c.Classification.Type)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    CommentsBySentiment = review.Comments.GroupBy(c => c.Classification.Sentiment)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    Comments = review.Comments.OrderByDescending(c => c.CreatedAt).ToList(),
                    AnalyzedAt = DateTime.UtcNow
                };

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing PR {PrNumber} from {RepoUrl}", prNumber, repoUrl);
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
