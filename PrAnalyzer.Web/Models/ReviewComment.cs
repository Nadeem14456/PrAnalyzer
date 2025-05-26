namespace PrAnalyzer.Web.Models
{
    public class ReviewComment
    {
        public long Id { get; set; }
        public string User { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int? Position { get; set; }
        public string State { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public CommentClassification Classification { get; set; } = new();
    }
}
