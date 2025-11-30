using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GdeWebModels
{
    public class TranscriptionResponseModel
    {
        public string TranscriptionId { get; set; }
        public string RawText { get; set; }
        public string CleanedText { get; set; }
        public MeetingNoteModel? Notes { get; set; }
        public TranscriptionMetadata Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public TranscriptionStatus Status { get; set; }
    }

    public class TranscriptionMetadata
    {
        public TimeSpan Duration { get; set; }
        public string Language { get; set; }
        public int WordCount { get; set; }
        public string OriginalFileName { get; set; }
        public long FileSizeBytes { get; set; }
        public string AudioFormat { get; set; }
        public double ConfidenceScore { get; set; }
    }

    public enum TranscriptionStatus
    {
        Pending = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4,
        PartiallyCompleted = 5
    }
}
