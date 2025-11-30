using GdeWebModels;

namespace GdeWeb.Interfaces
{
    public interface INoteService
    {
        Task<NoteModel> GetNote(int noteId);
        Task<NoteListModel> GetUserNotes();
        Task<NoteListModel> GetCourseNotes(int courseId);
        Task<ResultModel> AddNote(NoteModel note);
        Task<ResultModel> ModifyNote(NoteModel note);
        Task<ResultModel> DeleteNote(int noteId);

        Task<MonthlySummaryModel> GetMonthlySummary(int year, int month);
        Task<MonthlySummaryListModel> GetUserMonthlySummaries();
        Task<MonthlySummaryModel> GenerateMonthlySummary(int year, int month);
    }
}

