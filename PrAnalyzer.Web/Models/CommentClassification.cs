namespace PrAnalyzer.Web.Models
{
    public class CommentClassification
    {
        public CommentType Type { get; set; }
        public CommentSentiment Sentiment { get; set; }
        public List<string> Keywords { get; set; } = new();
        public double ConfidenceScore { get; set; }

        public string Category { get; set; }
    }
}
