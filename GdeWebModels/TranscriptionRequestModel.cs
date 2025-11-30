using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GdeWebModels
{
    public class TranscriptionRequestModel
    {
        public IFormFile AudioFile { get; set; }
        public string Language { get; set; } = "hu";
        public bool GenerateNotes { get; set; } = true;
        public NoteFormatType Format { get; set; } = NoteFormatType.BulletPoints;
        public string? UserId { get; set; }
        public string? MeetingTitle { get; set; }
        public List<string>? Participants { get; set; }
    }

    public enum NoteFormatType
    {
        BulletPoints = 1,
        MeetingMinutes = 2,
        Summary = 3,
        DetailedReport = 4
    }
}
