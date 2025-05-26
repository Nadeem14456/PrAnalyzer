namespace PrAnalyzer.ApiService.Models
{
    public class PullRequestAnalysis
    {
        public string RepoUrl { get; set; } = string.Empty;
        public int PrNumber { get; set; }
        public int TotalComments { get; set; }
        public Dictionary<string, int> CommentsByUser { get; set; } = new();
        public Dictionary<string, int> CommentsByType { get; set; } = new();
        public Dictionary<string, int> CommentsBySentiment { get; set; } = new();
        public List<ReviewComment> Comments { get; set; } = new();
        public DateTime AnalyzedAt { get; set; }
    }
}
