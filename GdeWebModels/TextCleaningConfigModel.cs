using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GdeWebModels
{
    public class TextCleaningConfigModel
    {
        public bool RemoveFillerWords { get; set; } = true;
        public bool CorrectSpelling { get; set; } = true;
        public bool CapitalizeSentences { get; set; } = true;
        public bool RemoveExtraWhitespace { get; set; } = true;
        public bool AddPunctuation { get; set; } = true;
        public List<string> CustomFillerWords { get; set; } = new();
        public string Language { get; set; } = "hu";
    }

    public class TextCleaningResultModel
    {
        public string OriginalText { get; set; }
        public string CleanedText { get; set; }
        public TextCleaningStatistics Statistics { get; set; }
        public List<TextCleaningChange> Changes { get; set; } = new();
    }

    public class TextCleaningStatistics
    {
        public int FillerWordsRemoved { get; set; }
        public int SpellingCorrections { get; set; }
        public int PunctuationAdded { get; set; }
        public int OriginalWordCount { get; set; }
        public int CleanedWordCount { get; set; }
        public double ReductionPercentage { get; set; }
    }

    public class TextCleaningChange
    {
        public ChangeType Type { get; set; }
        public string Original { get; set; }
        public string Replacement { get; set; }
        public int Position { get; set; }
    }

    public enum ChangeType
    {
        FillerWordRemoval = 1,
        SpellingCorrection = 2,
        PunctuationAddition = 3,
        Capitalization = 4,
        WhitespaceRemoval = 5
    }
}
