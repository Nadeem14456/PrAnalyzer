namespace PrAnalyzer.ApiService.Models
{
    public class PullRequestReview
    {
        public string RepoUrl { get; set; } = string.Empty;
        public int PrNumber { get; set; }
        public List<ReviewComment> Comments { get; set; } = new();
        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    }
}
