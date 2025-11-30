using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using GdeWebModels;

namespace GdeWebAPI.Services
{
    public class TextCleaningService
    {
        private readonly ILogger<TextCleaningService> _logger;

        // Magyar filler words
        private static readonly string[] HungarianFillerWords =
        {
            "öö", "ööö", "ő", "hát", "izé", "úgymond", "tudod", "érted",
            "mondjuk", "tessék", "na", "hm", "hmm", "ühüm", "aha"
        };

        // Angol filler words
        private static readonly string[] EnglishFillerWords =
        {
            "um", "uh", "ah", "ehm", "er", "hmm", "like", "you know",
            "I mean", "sort of", "kind of", "actually", "basically"
        };

        public TextCleaningService(ILogger<TextCleaningService> logger)
        {
            _logger = logger;
        }

        public TextCleaningResultModel CleanText(
            string rawText,
            TextCleaningConfigModel? config = null)
        {
            config ??= new TextCleaningConfigModel();

            var statistics = new TextCleaningStatistics
            {
                OriginalWordCount = CountWords(rawText)
            };

            var changes = new List<TextCleaningChange>();
            var cleanedText = rawText;

            try
            {
                // 1. Filler words eltávolítása
                if (config.RemoveFillerWords)
                {
                    var (text, removed) = RemoveFillerWords(cleanedText, config.Language);
                    cleanedText = text;
                    statistics.FillerWordsRemoved = removed;
                }

                // 2. Extra whitespace eltávolítása
                if (config.RemoveExtraWhitespace)
                {
                    cleanedText = RemoveExtraWhitespace(cleanedText);
                }

                // 3. Mondatok nagybetűsítése
                if (config.CapitalizeSentences)
                {
                    cleanedText = CapitalizeSentences(cleanedText);
                }

                // 4. Írásjel javítás
                if (config.AddPunctuation)
                {
                    cleanedText = FixPunctuation(cleanedText);
                }

                statistics.CleanedWordCount = CountWords(cleanedText);
                statistics.ReductionPercentage = CalculateReduction(
                    statistics.OriginalWordCount,
                    statistics.CleanedWordCount
                );

                _logger.LogInformation(
                    $"Text cleaned: {statistics.FillerWordsRemoved} filler words removed, " +
                    $"{statistics.ReductionPercentage:F2}% reduction"
                );

                return new TextCleaningResultModel
                {
                    OriginalText = rawText,
                    CleanedText = cleanedText,
                    Statistics = statistics,
                    Changes = changes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during text cleaning");
                throw;
            }
        }

        private (string text, int removed) RemoveFillerWords(string text, string language)
        {
            var fillerWords = language.ToLower() == "hu"
                ? HungarianFillerWords
                : EnglishFillerWords;

            int removedCount = 0;
            var result = text;

            foreach (var filler in fillerWords)
            {
                var pattern = $@"\b{Regex.Escape(filler)}\b";
                var matches = Regex.Matches(result, pattern, RegexOptions.IgnoreCase);
                removedCount += matches.Count;
                result = Regex.Replace(result, pattern, "", RegexOptions.IgnoreCase);
            }

            return (result, removedCount);
        }

        private string RemoveExtraWhitespace(string text)
        {
            // Több szóköz → egy szóköz
            text = Regex.Replace(text, @"\s+", " ");

            // Whitespace mondatok előtt/után
            text = text.Trim();

            return text;
        }

        private string CapitalizeSentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Első karakter nagybetű
            var result = char.ToUpper(text[0]) + text.Substring(1);

            // Mondatvégi jelek után nagybetű
            result = Regex.Replace(result, @"([.!?]\s+)(\w)", m =>
                m.Groups[1].Value + char.ToUpper(m.Groups[2].Value[0])
            );

            return result;
        }

        private string FixPunctuation(string text)
        {
            // Szóköz írásjel előtt → eltávolítás
            text = Regex.Replace(text, @"\s+([.,!?;:])", "$1");

            // Írásjel után szóköz
            text = Regex.Replace(text, @"([.,!?;:])(\w)", "$1 $2");

            // Mondat végére pont, ha nincs
            if (!string.IsNullOrWhiteSpace(text) &&
                !Regex.IsMatch(text, @"[.!?]$"))
            {
                text += ".";
            }

            return text;
        }

        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(new[] { ' ', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private double CalculateReduction(int original, int cleaned)
        {
            if (original == 0)
                return 0;

            return ((original - cleaned) / (double)original) * 100;
        }
    }
}