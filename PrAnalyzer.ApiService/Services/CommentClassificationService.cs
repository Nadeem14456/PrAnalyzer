using PrAnalyzer.ApiService.Models;
using System.Text.RegularExpressions;

namespace PrAnalyzer.ApiService.Services
{
    public class CommentClassificationService
    {
        private readonly ILogger<CommentClassificationService> _logger;
        private readonly HuggingFaceService _huggingFaceService;

        // Classification patterns
        private readonly Dictionary<CommentType, List<string>> _typePatterns = new()
        {
            [CommentType.Bug] = new() { "bug", "error", "issue", "problem", "broken", "fails", "doesn't work", "not working" },
            [CommentType.Security] = new() { "security", "vulnerability", "exploit", "unsafe", "injection", "xss", "csrf" },
            [CommentType.Performance] = new() { "performance", "slow", "optimize", "efficiency", "memory", "cpu", "speed" },
            [CommentType.Testing] = new() { "test", "testing", "unit test", "integration test", "coverage", "mock" },
            [CommentType.Documentation] = new() { "documentation", "comment", "readme", "docs", "explain", "clarify" },
            [CommentType.Architecture] = new() { "architecture", "design", "pattern", "structure", "refactor", "organize" },
            [CommentType.Suggestion] = new() { "suggest", "recommend", "consider", "what about", "maybe", "could" },
            [CommentType.Question] = new() { "?", "why", "how", "what", "when", "where", "question" },
            [CommentType.Approval] = new() { "lgtm", "looks good", "approved", "great", "perfect", "nice work" },
            [CommentType.RequestChanges] = new() { "needs", "must", "should", "required", "change", "fix" },
            [CommentType.Nitpick] = new() { "nit", "minor", "style", "formatting", "whitespace", "spacing" }
        };

        private readonly Dictionary<CommentSentiment, List<string>> _sentimentPatterns = new()
        {
            [CommentSentiment.Positive] = new() { "good", "great", "excellent", "nice", "perfect", "well done", "awesome" },
            [CommentSentiment.Negative] = new() { "bad", "wrong", "terrible", "awful", "horrible", "broken", "fails" },
            [CommentSentiment.Constructive] = new() { "suggest", "consider", "improve", "enhance", "better", "recommend" }
        };

        public CommentClassificationService(ILogger<CommentClassificationService> logger, HuggingFaceService huggingFaceService)
        {
            _logger = logger;
            _huggingFaceService = huggingFaceService;
        }

        public async Task<CommentClassification> ClassifyCommentAsync(ReviewComment comment)
        {
            var classification = new CommentClassification
            {
                Type = await ClassifyTypeWithAIAsync(comment.Body),
                //Keywords = ExtractKeywords(comment.Body)
            };

            // Use Hugging Face for sentiment
            var sentimentLabel = await _huggingFaceService.GetSentimentAsync(comment.Body);
            classification.Sentiment = sentimentLabel switch
            {
                "positive" => CommentSentiment.Positive,
                "negative" => CommentSentiment.Negative,
                _ => CommentSentiment.Neutral
            };

            classification.Category = sentimentLabel;
            classification.ConfidenceScore = CalculateConfidence(comment.Body, classification);
            return classification;
        }

        private async Task<CommentType> ClassifyTypeWithAIAsync(string body)
        {
            // Call HuggingFaceService for type classification (assume a zero-shot or custom model is set up)
            string[] possibleTypes = new[]
            {
                "bug", "security", "performance", "testing", "documentation", "architecture", "suggestion", "question", "approval", "request changes", "nitpick", "code review"
            };
            string label = await _huggingFaceService.GetTypeClassificationAsync(body, possibleTypes);
            if (string.IsNullOrWhiteSpace(label))
                return ClassifyType(body); // fallback to pattern-based

            return label.ToLowerInvariant() switch
            {
                "bug" => CommentType.Bug,
                "security" => CommentType.Security,
                "performance" => CommentType.Performance,
                "testing" => CommentType.Testing,
                "documentation" => CommentType.Documentation,
                "architecture" => CommentType.Architecture,
                "suggestion" => CommentType.Suggestion,
                "question" => CommentType.Question,
                "approval" => CommentType.Approval,
                "request changes" => CommentType.RequestChanges,
                "nitpick" => CommentType.Nitpick,
                _ => CommentType.CodeReview
            };
        }

        private CommentType ClassifyType(string body)
        {
            var bodyLower = body.ToLowerInvariant();
            var scores = new Dictionary<CommentType, int>();

            foreach (var (type, patterns) in _typePatterns)
            {
                var score = patterns.Sum(pattern => CountMatches(bodyLower, pattern));
                if (score > 0)
                    scores[type] = score;
            }

            if (scores.Any())
                return scores.OrderByDescending(s => s.Value).First().Key;

            // Default classification based on content analysis
            if (bodyLower.Contains("?"))
                return CommentType.Question;

            return CommentType.CodeReview;
        }

        private List<string> ExtractKeywords(string body)
        {
            var keywords = new List<string>();
            var words = Regex.Split(body.ToLowerInvariant(), @"\W+")
                .Where(w => w.Length > 3)
                .Where(w => !IsCommonWord(w))
                .Take(5);

            keywords.AddRange(words);
            return keywords;
        }

        private double CalculateConfidence(string body, CommentClassification classification)
        {
            var totalMatches = 0;
            var bodyLower = body.ToLowerInvariant();

            if (_typePatterns.ContainsKey(classification.Type))
            {
                totalMatches += _typePatterns[classification.Type]
                    .Sum(pattern => CountMatches(bodyLower, pattern));
            }

            if (_sentimentPatterns.ContainsKey(classification.Sentiment))
            {
                totalMatches += _sentimentPatterns[classification.Sentiment]
                    .Sum(pattern => CountMatches(bodyLower, pattern));
            }

            // Normalize confidence score (0.0 to 1.0)
            return Math.Min(1.0, totalMatches / 5.0);
        }

        private int CountMatches(string text, string pattern)
        {
            return Regex.Matches(text, Regex.Escape(pattern), RegexOptions.IgnoreCase).Count;
        }

        private bool IsCommonWord(string word)
        {
            var commonWords = new HashSet<string>
        {
            "the", "and", "for", "are", "but", "not", "you", "all", "can", "had", "her", "was", "one", "our", "out", "day", "get", "has", "him", "his", "how", "its", "may", "new", "now", "old", "see", "two", "way", "who", "boy", "did", "does", "let", "put", "say", "she", "too", "use", "this", "that", "with", "have", "from", "they", "know", "want", "been", "good", "much", "some", "time", "very", "when", "come", "here", "just", "like", "long", "make", "many", "over", "such", "take", "than", "them", "well", "were"
        };

            return commonWords.Contains(word);
        }
    }
}
