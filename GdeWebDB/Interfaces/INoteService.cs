using GdeWebModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GdeWebDB.Interfaces
{
    public interface INoteService
    {
        // Note operations
        Task<NoteModel> GetNote(NoteModel p);
        Task<NoteListModel> GetUserNotes(int userId);
        Task<NoteListModel> GetCourseNotes(int userId, int courseId);
        Task<ResultModel> AddNote(NoteModel p);
        Task<ResultModel> ModifyNote(NoteModel p);
        Task<ResultModel> DeleteNote(NoteModel p);

        // Monthly Summary operations
        Task<MonthlySummaryModel> GetMonthlySummary(int userId, int year, int month);
        Task<MonthlySummaryListModel> GetUserMonthlySummaries(int userId);
        Task<ResultModel> AddOrUpdateMonthlySummary(MonthlySummaryModel p);
    }
}

