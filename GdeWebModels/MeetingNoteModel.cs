using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GdeWebModels
{
    public class MeetingNoteModel
    {
        public string NoteId { get; set; }
        public string Title { get; set; }
        public DateTime MeetingDate { get; set; }
        public List<string> Participants { get; set; } = new();
        public string Summary { get; set; }
        public List<KeyPointModel> KeyPoints { get; set; } = new();
        public List<ActionItemModel> ActionItems { get; set; } = new();
        public List<DecisionModel> Decisions { get; set; } = new();
        public List<QuestionModel> Questions { get; set; } = new();
        public string? NextSteps { get; set; }
        public DateTime? NextMeetingDate { get; set; }
    }

    public class KeyPointModel
    {
        public int Order { get; set; }
        public string Topic { get; set; }
        public string Content { get; set; }
        public string? Speaker { get; set; }
        public TimeSpan? Timestamp { get; set; }
        public ImportanceLevel Importance { get; set; }
    }

    public class ActionItemModel
    {
        public string ActionId { get; set; }
        public string Description { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime? DueDate { get; set; }
        public ActionItemPriority Priority { get; set; }
        public ActionItemStatus Status { get; set; }
        public string? Notes { get; set; }
    }

    public class DecisionModel
    {
        public string DecisionId { get; set; }
        public string Description { get; set; }
        public string? DecisionMaker { get; set; }
        public List<string> AffectedParties { get; set; } = new();
        public DateTime? EffectiveDate { get; set; }
    }

    public class QuestionModel
    {
        public string QuestionId { get; set; }
        public string Question { get; set; }
        public string? AskedBy { get; set; }
        public string? Answer { get; set; }
        public string? AnsweredBy { get; set; }
        public bool IsResolved { get; set; }
    }

    public enum ImportanceLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum ActionItemPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Urgent = 4
    }

    public enum ActionItemStatus
    {
        NotStarted = 1,
        InProgress = 2,
        Completed = 3,
        Blocked = 4,
        Cancelled = 5
    }
}
