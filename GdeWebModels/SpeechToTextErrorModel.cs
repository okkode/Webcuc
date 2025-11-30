using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GdeWebModels
{
    public class SpeechToTextErrorModel
    {
        public string ErrorId { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
        public string? TranscriptionId { get; set; }
        public string? UserId { get; set; }
    }

    public class TranscriptionValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
    }

    public class ValidationError
    {
        public string Field { get; set; }
        public string Message { get; set; }
        public ValidationErrorType Type { get; set; }
    }

    public enum ValidationErrorType
    {
        Required = 1,
        InvalidFormat = 2,
        FileTooLarge = 3,
        UnsupportedFormat = 4,
        InvalidLanguage = 5,
        DurationTooLong = 6
    }
}
